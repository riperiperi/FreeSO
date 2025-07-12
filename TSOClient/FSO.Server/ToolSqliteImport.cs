using FSO.Server.Database.DA;
using FSO.Server.Utils;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FSO.Server
{
    public class ToolSqliteImport : ITool
    {
        private struct IndexDescription
        {
            public string Identifier;
            public string ColumnList;
        }

        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private IDAFactory DAFactory;
        private SqliteImportOptions Options;
        private ServerConfiguration Config;

        private Regex CreateRegex = new Regex("^CREATE TABLE (?<Identifier>[A-Za-z0-9_`]+) \\(");
        private Regex ColumnRegex = new Regex("^(?<Identifier>\\s*[A-Za-z0-9_`]+\\s)(?<Type>[a-z]+(\\([A-Za-z0-9_\\,']+\\))?)(?<Unsigned>\\sunsigned)?(?<Extras>(\\s.*))(?<Comma>,?)$");
        private Regex KeyRegex = new Regex("^\\s*((?<KeyType>[A-Z]+)\\s)?KEY(\\s(?<Identifier>[A-Za-z0-9_`]+))?\\s(?<Columns>\\([a-z0-9_`,()]+\\))(?<Comma>,?)$");
        private Regex CommentRegex = new Regex("(^|\\s)COMMENT\\s'.*'");

        private Regex InsertRegex = new Regex("^\\s*INSERT INTO (?<Identifier>[A-Za-z0-9_`]+) VALUES \\(");

        private Regex RemoveCountsRegex = new Regex("`\\([0-9]+\\)");
        private Regex CommentStartRegex = new Regex("/\\*![0-9]{5}");

        private string InventoryStateColumn = @"ALTER TABLE `fso_objects`
            ADD COLUMN `inventory_state` BLOB DEFAULT NULL;";

        public static string[] ImportOrder = new string[]
        {
            //=== Free (no dependency)

            "fso_auth_tickets",
            "fso_db_changes",
            "fso_dyn_payouts",
            "fso_email_confirm",
            "fso_events",
            "fso_lot_claims",
            "fso_relationships", //somehow...
            "fso_shard_tickets",
            "fso_transactions",
            "fso_tuning",

            //===

            "fso_update_addons",

            //---

            "fso_update_branch", //(fso_update_addons)

            //---

            "fso_updates", //(fso_update_addons, fso_update_branch, fso_updates)

            //---

            "fso_users",
            "fso_shards", //(fso_updates)

            //---

            "fso_user_authenticate", //(fso_users)
            "fso_auth_attempts", //(fso_users)

            "fso_lots", //(fso_shards)

            "fso_election_cycles", //(fso_neighborhoods ^)

            "fso_avatars", //(fso_users, fso_shards, fso_neighborhoods ^)

            "fso_event_participation", //(fso_events, fso_users)

            //=== CIRCULAR ===

            "fso_neighborhoods", //(fso_election_cycles, fso_avatars, fso_shards, fso_lots)

            //---

            "fso_bonus", //(fso_avatars)
            "fso_bookmarks", //(fso_avatars)
            "fso_avatar_claims", //(fso_avatars)
            "fso_bulletin_posts", //(fso_lots, fso_neighborhoods, fso_avatars)

            "fso_election_candidates", //(fso_avatars, fso_election_cycles)
            "fso_election_cyclemail", //(fso_avatars, fso_election_cycles)
            "fso_election_freevotes", //(fso_avatars, fso_election_cycles, fso_neighborhoods)
            "fso_election_votes", //(fso_election_cycles, fso_avatars)

            "fso_generic_avatar_participation", //(fso_avatars)
            "fso_global_cooldowns", //(fso_avatars, fso_users)
            "fso_inbox", //(fso_avatars)
            "fso_ip_ban", //(fso_users)
            "fso_joblevels", //(fso_avatars)
            "fso_lot_admit", //(fso_avatars, fso_lots)
            "fso_lot_top_100", //(fso_lots, fso_shards)
            "fso_lot_visit_totals", //(fso_lots)
            "fso_lot_visits", //(fso_avatars, fso_lots)

            "fso_mayor_ratings", //(fso_users, fso_avatars, fso_users, fso_avatars)
            "fso_nhood_ban", //(fso_users)
            "fso_objects", //(fso_lots, fso_shards, fso_avatars)
            "fso_roommates", //(fso_avatars, fso_lots)
            "fso_tasks", //(fso_shards)

            "temp_candy_punish", //(fso_avatars), shouldn't be imported

            //---

            "fso_object_attributes", //(fso_objects)
            "fso_lot_server_tickets", //(fso_avatar_claims)

            //---

            "fso_outfits", //(fso_avatars, fso_objects)

            //---

            "fso_hosts", //(fso_updates, fso_shards)

            //--- (events)

            "fso_tuning_presets",

            //---

            "fso_tuning_preset_items", //(fso_tuning_presets)

            //"routines" //TODO... sqlite does not support stored procedures or functions, so these need to be moved to the da code
        };

        public ToolSqliteImport(SqliteImportOptions options, IDAFactory factory, ServerConfiguration config)
        {
            DAFactory = factory;
            Options = options;
            Config = config;
        }

        public string RemoveComments(string sql)
        {
            int startIndex = 0;
            do
            {
                var startMatch = CommentStartRegex.Match(sql, startIndex);
                startIndex = !startMatch.Success ? -1 : startMatch.Index;
                //startIndex = sql.IndexOf("/*", startIndex);

                if (startIndex != -1)
                {
                    int endIndex = sql.IndexOf("*/", startIndex + 2);

                    if (endIndex != -1)
                    {
                        sql = sql.Substring(0, startIndex) + sql.Substring(endIndex + 2);
                    }
                }
            } while (startIndex != -1);

            return sql;
        }

        private enum InsertValuesContext
        {
            Root,
            Tuple,
            String
        }

        public string SqliteEscape(string value, char quoteChar, bool isBinary)
        {
            if (isBinary)
            {
                // Render as a hex string
                var data = StringToBytes(value);
                var sb = new StringBuilder();

                sb.Append('x');
                sb.Append(quoteChar);

                foreach (byte b in data)
                {
                    sb.Append(b.ToString("x2"));
                }

                sb.Append(quoteChar);

                return sb.ToString();
            }

            bool hasQuote = value.IndexOf(quoteChar) != -1;

            if (hasQuote)
            {
                value = value.Replace($"{quoteChar}", $"{quoteChar}{quoteChar}");
            }

            // Reinterpret the string as UTF8.

            value = Encoding.UTF8.GetString(StringToBytes(value));

            return $"{quoteChar}{value}{quoteChar}";
        }

        public string RewriteInsert(string line, int valuesBegin, List<bool> columnBinary)
        {
            // Comma separated array of (value1, value2, ...) until the end of the line.
            // When inserting string values, use special handling to remove mysql escape characters and work in sqlite escapes.

            var result = new StringBuilder();
            result.Append(line.Substring(0, valuesBegin));

            char quotechar = '"';
            var stringBuilder = new StringBuilder();
            int tupleIndex = 0;
            InsertValuesContext context = InsertValuesContext.Root;

            for (int i = valuesBegin; i < line.Length; i++)
            {
                char c = line[i];
                switch (context)
                {
                    case InsertValuesContext.Root:
                        result.Append(c);

                        if (c == '(')
                        {
                            // Begins a value tuple
                            context = InsertValuesContext.Tuple;
                            tupleIndex = 0;
                        }
                        break;
                    case InsertValuesContext.Tuple:
                        if (c == '\'' || c == '"')
                        {
                            stringBuilder.Clear();
                            quotechar = c;
                            context = InsertValuesContext.String;
                        }
                        else
                        {
                            result.Append(c);
                            if (c == ',')
                            {
                                tupleIndex++;
                            }
                            else if (c == ')')
                            {
                                context = InsertValuesContext.Root;
                            }
                        }
                        break;
                    case InsertValuesContext.String:
                        if (c == quotechar)
                        {
                            // End of string.
                            // re-escape the string for sqlite and append it to the result

                            result.Append(SqliteEscape(stringBuilder.ToString(), quotechar, columnBinary[tupleIndex]));

                            context = InsertValuesContext.Tuple;
                        }
                        else if (c == '\\' && i + 1 < line.Length)
                        {
                            // Begin mysql escape character
                            bool keepBackslash = false;

                            char escapeType = line[++i];
                            char escape = escapeType;

                            switch (escapeType)
                            {
                                case '0':
                                    escape = '\0';
                                    break;
                                case '\'':
                                    escape = '\'';
                                    break;
                                case '"':
                                    escape = '"';
                                    break;
                                case 'b':
                                    escape = '\b';
                                    break;
                                case 'n':
                                    escape = '\n';
                                    break;
                                case 'r':
                                    escape = '\r';
                                    break;
                                case 't':
                                    escape = '\t';
                                    break;
                                case 'Z':
                                    escape = '\u001a';
                                    break;
                                case '\\':
                                    escape = '\\';
                                    break;

                                // different if in pattern matching context... but they include the \ otherwise.
                                case '%':
                                    keepBackslash = true;
                                    escape = '%';
                                    break;
                                case '_':
                                    keepBackslash = true;
                                    escape = '_';
                                    break;
                            }

                            if (keepBackslash)
                            {
                                stringBuilder.Append('\\');
                            }

                            stringBuilder.Append(escape);
                        }
                        else
                        {
                            stringBuilder.Append(c);
                        }

                        break;
                }
            }

            return result.ToString();
        }

        public string MysqlEscapesToSqlite(string sql)
        {
            char[] output = new char[sql.Length];
            int outputLength = 0;

            for (int i = 0; i < sql.Length - 1; i++)
            {
                if (sql[i] == '\\')
                {
                    bool keepBackslash = false;

                    char escapeType = sql[++i];
                    char result = escapeType;

                    switch (escapeType)
                    {
                        case '0':
                            result = '\0';
                            break;
                        case '\'':
                            result = '\'';
                            break;
                        case '"':
                            result = '"';
                            break;
                        case 'b':
                            result = '\b';
                            break;
                        case 'n':
                            result = '\n';
                            break;
                        case 'r':
                            result = '\r';
                            break;
                        case 't':
                            result = '\t';
                            break;
                        case 'Z':
                            result = '\u001a';
                            break;
                        case '\\':
                            result = '\\';
                            break;

                        // different if in pattern matching context... but they include the \ otherwise.
                        case '%':
                            keepBackslash = true;
                            result = '%';
                            break;
                        case '_':
                            keepBackslash = true;
                            result = '_';
                            break;
                    }

                    if (keepBackslash)
                    {
                        output[outputLength++] = '\\';
                        output[outputLength++] = result;
                    }
                    else
                    {
                        if (result == '\'')
                        {
                            // Sqlite doubles the quote. Assume that we're only ever in single quotes, as that's how mariadb export works.
                            output[outputLength++] = '\'';
                            output[outputLength++] = '\'';
                        }
                        else
                        {
                            output[outputLength++] = result;
                        }
                    }
                }
                else
                {
                    output[outputLength++] = sql[i];
                }
            }

            return new String(output, 0, outputLength);
        }

        public List<string> ScanDumps()
        {
            var files = Directory.GetFiles(Options.ImportDir);

            var ordered = new List<string>();
            var missing = new List<string>();
            var toRead = new HashSet<string>(files);

            string startAt = null;

            var fileList = startAt == null ? ImportOrder : ImportOrder.Skip(Array.IndexOf(ImportOrder, startAt));

            foreach (var file in fileList)
            {
                var expectedName = Path.Combine(Options.ImportDir, $"fso_{file}.sql");

                if (toRead.Contains(expectedName))
                {
                    ordered.Add(expectedName);
                    toRead.Remove(expectedName);
                }
                else
                {
                    missing.Add(expectedName);
                }
            }

            return ordered;
        }

        public string RewriteImport(string name, string sql)
        {
            sql = sql.Replace("\r", "");

            sql = RemoveComments(sql);

            var indexes = new List<IndexDescription>();

            // Split the query into lines. Assumes formatting from mariadb export.

            var lines = sql.Split('\n').Where(line =>
            {
                // Remove some unsupported queries.

                if (line.StartsWith("CREATE DATABASE "))
                {
                    return false;
                }

                if (line == ";" || line == ";;")
                {
                    return false;
                }

                if (line == "USE `fso`;")
                {
                    return false;
                }

                if (line.StartsWith("LOCK TABLES "))
                {
                    return false;
                }

                if (line == "UNLOCK TABLES;")
                {
                    return false;
                }

                if (line.StartsWith("DELIMITER "))
                {
                    return false;
                }

                return true;
            }).ToList();

            // Second, try to find the range of the "create table" query.
            // Assume there is only one of these per file...

            bool creatingTable = false;
            bool anyAutoIncrement = false;
            string tableIdentifier = "";

            var columnBinary = new List<bool>();

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];

                if (creatingTable)
                {
                    if (line.StartsWith(") ENGINE=InnoDB"))
                    {
                        creatingTable = false;

                        // Previous line can't end with a comma.
                        var lastLine = lines[i - 1];
                        if (lastLine[lastLine.Length - 1] == ',')
                        {
                            lines[i - 1] = lastLine.Substring(0, lastLine.Length - 1);
                        }

                        // TODO: remember then apply rowid? UPDATE SQLITE_SEQUENCE SET seq = <n> WHERE name = '<table>'
                        lines[i] = ");"; // Default charset is already utf-8, can't use innodb.

                        // Create indexes before inserting stuff.
                        foreach (var index in indexes)
                        {
                            lines.Insert(++i, $"CREATE INDEX {index.Identifier} ON {tableIdentifier}{index.ColumnList};");
                        }
                        continue;
                    }

                    // Rewrite individual lines to fix type definitions, attributes like auto increment, and primary/foreign/unique keys.

                    var keyMatch = KeyRegex.Match(line);

                    if (keyMatch.Success)
                    {
                        var keyType = keyMatch.Groups["KeyType"].Value;
                        var identifier = keyMatch.Groups["Identifier"].Value;
                        var columns = keyMatch.Groups["Columns"].Value;
                        var comma = keyMatch.Groups["Comma"].Value;

                        columns = RemoveCountsRegex.Replace(columns, "`");

                        switch (keyType)
                        {
                            case "UNIQUE":
                                // Remove "KEY" and identifier.
                                lines[i] = $"  UNIQUE {columns}{comma}";
                                break;
                            case "PRIMARY":
                                // Keep intact
                                if (anyAutoIncrement)
                                {
                                    // TODO: check if primary key is composite and leave the other ones...
                                    lines.RemoveAt(i--);
                                }
                                else
                                {
                                    lines[i] = $"  {keyType}{(keyType == "" ? "" : " ")}KEY {identifier}{(identifier == "" ? "" : " ")}{columns}{comma}";
                                }
                                break;
                            default:
                                // Remove
                                if (keyType == "")
                                {
                                    // This is an index - we want to add these at the end with CREATE INDEX.

                                    indexes.Add(new IndexDescription()
                                    {
                                        Identifier = identifier,
                                        ColumnList = columns
                                    });
                                }
                                lines.RemoveAt(i--);
                                break;
                        }
                    }
                    else
                    {
                        // Probably a column definition
                        var parsed = ColumnRegex.Match(line);

                        if (parsed.Success)
                        {
                            var identifier = parsed.Groups["Identifier"].Value;
                            var type = parsed.Groups["Type"].Value;
                            var unsigned = parsed.Groups["Unsigned"].Value;
                            var extras = parsed.Groups["Extras"].Value.TrimEnd(',');
                            extras = CommentRegex.Replace(extras, "");
                            extras = extras.Replace(" ON UPDATE current_timestamp()", ""); // TODO: make this a trigger automatically?

                            bool isBlob = type.Contains("blob") || type.Contains("binary");

                            bool autoIncrement = false;

                            var extrasSplit = extras.Split(' ').Where(extra => extra != "zerofill").Select(extra =>
                            {
                                switch (extra)
                                {
                                    case "current_timestamp()":
                                        return "current_timestamp";
                                    case "AUTO_INCREMENT":
                                        // Supposedly expensive, but we need this to match mysql.
                                        autoIncrement = true;
                                        anyAutoIncrement = true;
                                        return "PRIMARY KEY AUTOINCREMENT";
                                }

                                return extra;
                            }).ToList();

                            if (autoIncrement)
                            {
                                // Must be signed integer to allow this.
                                type = "INTEGER";
                                unsigned = "";
                            }

                            var unsignedPrefix = (unsigned != "") ? "unsigned " : "";

                            if (type.StartsWith("enum("))
                            {
                                // Convert enum into string with checks

                                var trimdentifier = identifier.Trim();

                                type = $"TEXT CHECK({trimdentifier} IN {type.Substring(4)})";
                            }

                            lines[i] = $"{identifier}{unsignedPrefix}{type}{string.Join(" ", extrasSplit)},";

                            columnBinary.Add(isBlob);
                        }
                        else
                        {
                            LOG.Error($"Unknown line: {line}");
                        }
                    }
                }
                else
                {
                    var createMatch = CreateRegex.Match(line);
                    if (createMatch.Success)
                    {
                        creatingTable = true;

                        tableIdentifier = createMatch.Groups["Identifier"].Value;
                    }

                    var insertMatch = InsertRegex.Match(line);
                    if (!creatingTable && insertMatch.Success)
                    {
                        lines[i] = RewriteInsert(line, insertMatch.Length - 1, columnBinary);
                    }
                    else
                    {
                        if (line.IndexOf("\\") != -1)
                        {
                            lines[i] = MysqlEscapesToSqlite(line);
                        }
                    }
                }
            }

            return string.Join("\n", lines);
        }

        private void RunCommand(SqlDA da, string sql)
        {
            var context = da.Context;

            var command = context.Connection.CreateCommand();

            command.CommandText = sql;

            try
            {
                var result = command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void ImportTable(string name, string sql)
        {
            LOG.Info($"Importing table {name}...");

            using (var da = (SqlDA)DAFactory.Get())
            {
                var sqlRewrite = RewriteImport(name, sql);

                RunCommand(da, sqlRewrite);
            }
        }

        public void CreateTriggers()
        {
            LOG.Info($"Creating triggers...");

            using (var da = (SqlDA)DAFactory.Get())
            {
                var context = da.Context;

                foreach (var trigger in SqliteFunctions.All)
                {
                    RunCommand(da, trigger);
                }
            }
        }

        public void SetPragmas()
        {
            using (var da = (SqlDA)DAFactory.Get())
            {
                RunCommand(da, "PRAGMA journal_mode=WAL;");
            }
        }

        private string BytesToString(byte[] data)
        {
            // Like ascii but funnier.
            var result = new char[data.Length];

            int i = 0;
            foreach (var b in data)
            {
                result[i++] = (char)b;
            }

            return new string(result);
        }

        private byte[] StringToBytes(string data)
        {
            // Like ascii but funnier.
            var result = new byte[data.Length];

            int i = 0;
            foreach (var c in data)
            {
                result[i++] = (byte)c;
            }

            return result;
        }

        private bool DeleteIfEmpty(string dir)
        {
            if (Directory.GetFileSystemEntries(dir).Length == 0)
            {
                Directory.Delete(dir);

                return true;
            }

            return false;
        }

        public void MigrateInventoryState()
        {
            // First, create the table in the database.
            using (var da = (SqlDA)DAFactory.Get())
            {
                LOG.Info($"Adding inventory state column to database...");
                try
                {
                    RunCommand(da, InventoryStateColumn);
                }
                catch (Exception)
                {
                    LOG.Info($"- Seems like it's already there... continuing.");
                }

                var objs = Directory.GetDirectories(Path.Combine(Config.SimNFS, "Objects/"));
                LOG.Info($"Migrating inventory to database... ({objs.Length} entries)");

                int migratedCount = 0;
                int folderDeletionCount = 0;
                int processedCount = 0;

                foreach (var obj in objs)
                {
                    if (!uint.TryParse(Path.GetFileName(obj), NumberStyles.HexNumber, null, out uint id))
                    {
                        continue;
                    }

                    var statePath = Path.Combine(obj, "inventoryState.fsoo");
                    if (File.Exists(statePath))
                    {
                        var data = File.ReadAllBytes(statePath);

                        if (!da.Objects.SetDbObjectState(id, data))
                        {
                            LOG.Info($"Current database configuration does not support inventory in database.");
                            return; // invalid?
                        }

                        File.Delete(statePath);

                        migratedCount++;

                        if (DeleteIfEmpty(obj))
                        {
                            folderDeletionCount++;
                        }
                    }

                    if ((++processedCount % 1000) == 0)
                    {
                        LOG.Info($"- {processedCount}/{objs.Length}...");
                    }
                }

                LOG.Info($"Finished migration: {migratedCount}/{objs.Length} objects migrated to db, {folderDeletionCount} folders deleted.");
            }
        }

        private void Commit()
        {
            using (var da = (SqlDA)DAFactory.Get())
            {
                RunCommand(da, "PRAGMA wal_checkpoint(TRUNCATE)");
                RunCommand(da, "vacuum");
                RunCommand(da, "PRAGMA wal_checkpoint(TRUNCATE)");
            }
        }

        public int Run()
        {
            SetPragmas();

            MigrateInventoryState();

            Commit();

            return 1;

            var files = ScanDumps();

            foreach (var file in files)
            {
                var sql = BytesToString(File.ReadAllBytes(file));

                ImportTable(Path.GetFileNameWithoutExtension(file), sql);
            }

            CreateTriggers();

            MigrateInventoryState();

            Commit();

            return 1;
        }
    }
}
