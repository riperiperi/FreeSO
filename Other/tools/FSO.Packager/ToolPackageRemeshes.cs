using FSO.Common.Utils;
using FSO.Common.WorldGeometry.Paths;
using FSO.Files;
using FSO.Files.Formats.DBPF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.RC;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace FSO.Packager
{
    internal class GamePackager
    {
        private class AuthorMetadataJson
        {
            [JsonProperty("name")]
            public required string Name { get; set; }

            [JsonProperty("thread")]
            public string? Thread { get; set; }

            [JsonProperty("url")]
            public string? Url { get; set; }

            [JsonProperty("description")]
            public string? Description { get; set; }
        }

        private class GroupMetadataJson
        {
            [JsonProperty("name")]
            public required string Name { get; set; }

            [JsonProperty("description")]
            public string? Description { get; set; }

            [JsonProperty("url")]
            public string? Url { get; set; }

            [JsonProperty("game")]
            public required string Game { get; set; } = "freeso,simitone";

            [JsonProperty("priority")]
            public required int Priority { get; set; } = 0;
        }

        private class RemeshAliasSplitJson
        {
            [JsonProperty("name")]
            public string? Name { get; set; }

            [JsonProperty("to")]
            public required string To { get; set; }

            [JsonProperty("dgrpFrom")]
            public required int DgrpFrom { get; set; }

            [JsonProperty("dgrpTo")]
            public required int DgrpTo { get; set; }

            [JsonProperty("range")]
            public required int Range { get; set; }
        }

        private class RemeshAliasJson
        {
            [JsonProperty("from")]
            public required string From { get; set; }

            [JsonProperty("to")]
            public string? To { get; set; }

            [JsonProperty("split")]
            public RemeshAliasSplitJson[]? Split { get; set; }
        }

        private class PackageMetadataJson
        {
            [JsonProperty("name")]
            public required string Name { get; set; }

            [JsonProperty("id")]
            public required string ID { get; set; }

            [JsonProperty("description")]
            public string? Description { get; set; }

            [JsonProperty("url")]
            public string? Url { get; set; }

            [JsonProperty("alias")]
            public Dictionary<string, RemeshAliasJson[]>? Alias { get; set; }
        }

        private readonly PackageRemeshesOptions Options;
        private readonly string Game;

        private readonly DBPFFile CompressedPackage;
        private readonly DBPFFile UncompressedPackage;
        private readonly DBPFFile CreditsPackage;

        private readonly Dictionary<uint, uint> CompressedIDs;
        private readonly Dictionary<uint, uint> UncompressedIDs;

        private readonly FSO3DDirectory DirectoryChunk;
        private readonly FSO3DCredits Credits;

        private ZipArchive? LegacyPackage;

        private Dictionary<string, RemeshAliasJson> Aliases = [];

        public GamePackager(PackageRemeshesOptions options, string game)
        {
            Options = options;
            Game = game;

            CompressedPackage = new DBPFFile();
            UncompressedPackage = new DBPFFile();
            CreditsPackage = new DBPFFile();

            CompressedIDs = [];
            UncompressedIDs = [];

            DirectoryChunk = new FSO3DDirectory()
            {
                Entries = []
            };

            Credits = new FSO3DCredits()
            {
                Authors = []
            };
        }

        private void AddFile(DBPFFile file, uint id, DBPFTypeID type, Action<Stream> fileWriter)
        {
            byte[] data;

            using (var mem = new MemoryStream())
            {
                fileWriter(mem);

                data = mem.ToArray();
            }

            // Add an entry to the file.
            file.AddOrReplace(((ulong)id << 32) | (ulong)type, DBPFGroupID.RemeshPackage, data);
        }

        private uint AddFile(DBPFFile file, Dictionary<uint, uint> lastIds, DBPFTypeID type, Action<Stream> fileWriter)
        {
            // Get a free ID for the file
            if (!lastIds.TryGetValue((uint)type, out uint lastId))
            {
                lastId = uint.MaxValue;
            }

            uint id = lastId + 1;
            lastIds[(uint)type] = id;

            AddFile(file, id, type, fileWriter);

            return id;
        }

        private void Warning(string message)
        {
            Console.WriteLine($"    WARNING: {message}");
        }

        private void CompressTextureTo(string srcPath, Stream dstStream)
        {
            // Load image from path

            var data = ImageLoader.DataFromStream(null, File.OpenRead(srcPath)).Value.Data.Value;

            if ((data.Width % 4) != 0 || (data.Height % 4) != 0)
            {
                Warning($"{Path.GetFileName(srcPath)} does not align to the 4x4 block size and will force runtime UV scaling");
            }

            // If it has alpha, compress to DXT5
            // if not, compress to DXT1

            var colorData = data.Data;
            bool hasAlpha = colorData.Any(col => col.A != 255);

            var mtex2 = new MTX2()
            {
                Width = data.Width,
                Height = data.Height,
                Compression = MTX2CompressionType.GZip,
            };

            if (hasAlpha)
            {
                var dxt5Data = TextureUtils.GenerateDXT5WithMips(data.Width, data.Height, colorData);
                mtex2.SetData(MTX2Format.DXT5, dxt5Data);
            }
            else
            {
                var dxt1Data = TextureUtils.GenerateDXT1WithMips(data.Width, data.Height, colorData);
                mtex2.SetData(MTX2Format.DXT1, dxt1Data);
            }

            mtex2.Write(null, dstStream);
        }

        private int DirectoryID = 0;

        private FSO3DDirectoryEntry GetDirectoryEntry(string name)
        {
            if (!DirectoryChunk.Entries.TryGetValue(name, out var entry))
            {
                entry = new FSO3DDirectoryEntry()
                {
                    ID = (DirectoryID++),
                    Filename = name,
                    Meshes = [],
                    Textures = [],
                };

                DirectoryChunk.Entries[name] = entry;
            }

            return entry;
        }

        private T ReadMetadata<T>(string path, string typeName)
        {
            try
            {
                var metadataJSON = File.ReadAllText(path);

                return JsonConvert.DeserializeObject<T>(metadataJSON) ?? throw new Exception("Metadata cannot be null");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to read metadata JSON for {typeName} {Path.GetFileName(Path.GetDirectoryName(path))}. Invalid or missing?");

                throw;
            }
        }

        private FSO3DCreditsGroup? ProcessGroup(string dir)
        {
            var metadata = ReadMetadata<GroupMetadataJson>(Path.Join(dir, "metadata.json"), "group");

            var games = metadata.Game ?? "freeso,simitone";
            var gameList = games.Split(',');

            if (!gameList.Contains(Game))
            {
                // This remesh isn't for this game.
                return null;
            }

            var credits = new FSO3DCreditsGroup()
            {
                Metadata = new FSO3DGroupMetadata()
                {
                    Name = metadata.Name,
                    Description = metadata.Description ?? ""
                },
                Files = []
            };

            var files = Directory.GetFiles(dir);

            foreach (var file in files)
            {
                bool isMesh = Path.GetExtension(file) == ".fsom";
                bool isPng = !isMesh && Path.GetExtension(file) == ".png";
                if (isMesh || isPng)
                {
                    var cType = isPng ? DBPFTypeID.MTX2 : DBPFTypeID.FSOM;
                    var uType = isPng ? DBPFTypeID.MTEX : DBPFTypeID.FSOM;

                    // Try parse the id at the end of the filename.
                    string name = Path.GetFileNameWithoutExtension(file);
                    int lastUnderscore = name.LastIndexOf('_');

                    if (lastUnderscore == -1 || !ushort.TryParse(name.AsSpan(lastUnderscore + 1), out ushort chunkId))
                    {
                        throw new InvalidDataException($"Remesh file {file} doesn't have a valid format (expected resource id after underscore).");
                    }

                    string directoryName = name[..lastUnderscore];

                    if (isPng)
                    {
                        if (!directoryName.EndsWith("_TEX"))
                        {
                            throw new InvalidDataException($"Remesh texture {file} must have TEX_ before the texture ID.");
                        }

                        directoryName = directoryName[..^4];
                    }

                    directoryName = directoryName.ToLowerInvariant();

                    string legacyName = Path.GetFileName(file);

                    if (Aliases.TryGetValue(directoryName, out var alias))
                    {
                        if (alias.To != null)
                        {
                            directoryName = alias.To;

                            legacyName = alias.To + legacyName.Substring(alias.From.Length);
                        }
                        else if (alias.Split != null)
                        {
                            foreach (var split in alias.Split)
                            {
                                if (chunkId >= split.DgrpFrom && chunkId < split.DgrpFrom + split.Range)
                                {
                                    directoryName = split.To;

                                    if (isMesh)
                                    {
                                        chunkId = (ushort)((chunkId - split.DgrpFrom) + split.DgrpTo);
                                        legacyName = $"{directoryName}_{chunkId}.fsom";
                                    }
                                    else
                                    {
                                        legacyName = $"{directoryName}_TEX_{chunkId}.png";
                                    }
                                }
                            }
                        }
                    }

                    // Add resource

                    uint fileId = AddFile(CompressedPackage, CompressedIDs, cType, (stream) =>
                    {
                        if (cType == DBPFTypeID.FSOM)
                        {
                            using var fileStream = File.OpenRead(file);
                            fileStream.CopyTo(stream);
                        }
                        else
                        {
                            CompressTextureTo(file, stream);
                        }
                    });

                    // We expect this ID to be the same...
                    uint fileId2 = AddFile(UncompressedPackage, UncompressedIDs, cType, (stream) =>
                    {
                        using var fileStream = File.OpenRead(file);
                        fileStream.CopyTo(stream);
                    });

                    var entry = GetDirectoryEntry(directoryName);
                    var ref3d = new FSO3DRef(chunkId, fileId, (uint)cType);

                    if (isPng)
                    {
                        entry.Textures[chunkId] = ref3d;
                    }
                    else
                    {
                        entry.Meshes[chunkId] = ref3d;
                    }

                    credits.Files.Add(ref3d);

                    LegacyPackage?.CreateEntryFromFile(file, legacyName, CompressionLevel.SmallestSize);
                }
            }

            return credits;
        }

        private void ProcessContributor(string dir)
        {
            var metadata = ReadMetadata<AuthorMetadataJson>(Path.Join(dir, "metadata.json"), "author");

            var author = new FSO3DCreditsAuthor()
            {
                Metadata = new FSO3DAuthorMetadata()
                {
                    Name = metadata.Name,
                    Description = metadata.Description ?? ""
                },
                Groups = []
            };

            Credits.Authors.Add(author);

            var groupDirs = Directory.GetDirectories(dir);

            foreach (var groupDir in groupDirs)
            {
                var group = ProcessGroup(groupDir);

                if (group != null)
                {
                    author.Groups.Add(group);
                }
            }
        }

        private void AddDirectoryChunk(DBPFFile file)
        {
            AddFile(file, 0, DBPFTypeID.FSO3DDirectory, (stream) => DirectoryChunk.Write(stream));
        }

        private void AddCreditsChunk(DBPFFile file)
        {
            AddFile(file, 0, DBPFTypeID.FSO3DCredits, (stream) => Credits.Write(stream));
        }

        private FSO3DRef ReplaceType(FSO3DRef item, DBPFTypeID from, DBPFTypeID to)
        {
            if ((DBPFTypeID)item.TypeID == from)
            {
                return new FSO3DRef(item.ID, item.FileID, (uint)to);
            }

            return item;
        }

        private void ReplaceTypes(DBPFTypeID from, DBPFTypeID to)
        {
            // In directory
            foreach (var entry in DirectoryChunk.Entries.Values)
            {
                foreach (var key in entry.Meshes.Keys)
                {
                    entry.Meshes[key] = ReplaceType(entry.Meshes[key], from, to);
                }

                foreach (var key in entry.Textures.Keys)
                {
                    entry.Textures[key] = ReplaceType(entry.Textures[key], from, to);
                }
            }

            foreach (var author in Credits.Authors)
            {
                foreach (var group in author.Groups)
                {
                    var files = CollectionsMarshal.AsSpan(group.Files);
                    for (int i = 0; i < files.Length; i++)
                    {
                        ref var item = ref files[i];

                        item = ReplaceType(item, from, to);
                    }
                }
            }
        }

        public int Run()
        {
            Console.WriteLine($"Packaging remeshes for game '{Game}'.");

            if (Options.Legacy)
            {
                var path = Path.Combine(Options.OutDirectory, $"{Game}-remeshes-legacy.zip");
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                LegacyPackage = ZipFile.Open(path, ZipArchiveMode.Create);
            }

            var metadata = ReadMetadata<PackageMetadataJson>(Path.Join(Options.SourceDirectory, "metadata.json"), "root package");

            Credits.Metadata = new FSO3DPackageMetadata()
            {
                Name = metadata.Name,
                Description = metadata.Description ?? "",
                Url = metadata.Url ?? "",
                ID = metadata.ID,
            };

            if (metadata.Alias != null && metadata.Alias.TryGetValue(Game, out var aliases))
            {
                if (aliases != null)
                {
                    foreach (var alias in aliases)
                    {
                        Aliases[alias.From] = alias;
                    }
                }
            }

            // Rough DBPF File structure

            // Multiple items
            // FSOM: Remesh models
            // FTEX: Remesh PNG textures
            // FTX2: Remesh compressed textures

            // Single item (id 0)
            // FSO3DDirectory Remesh Directory (list of entries):
            //  - UID (for ref from credits)
            //  - Target filename (eg. chairconnectingtheater_iff)
            //  - List of FSOM
            //    - (dgrp num, FSOM id)
            //  - List of FTEX
            //    - (tex num, FTEX/FTX2 id, type id)
            // FSO3DCredits Credits
            //  - Root metadata
            //  - List of strings
            //  - List of remeshers
            //    - Remesher metadata
            //    - List of mesh packages
            //      - Package metadata
            //      - List of entries in the Remesh Directory (UID, fsom #, tex #)

            // Scan the remeshes
            Console.WriteLine($"  - Adding files...");

            var contributorDirs = Directory.GetDirectories(Options.SourceDirectory);
            foreach (var contributorDir in contributorDirs)
            {
                ProcessContributor(contributorDir);
            }

            Console.WriteLine($"  - Finalizing packages...");

            AddDirectoryChunk(CompressedPackage);

            Credits.Metadata.Format = FSO3DPackageTextureFormat.Dxt;
            AddCreditsChunk(CompressedPackage);

            Credits.Metadata.Format = FSO3DPackageTextureFormat.Credits;
            AddCreditsChunk(CreditsPackage);

            // For the uncompressed package, rewrite the texture types to all use MTEX
            ReplaceTypes(DBPFTypeID.MTX2, DBPFTypeID.MTEX);
            AddDirectoryChunk(UncompressedPackage);
            Credits.Metadata.Format = FSO3DPackageTextureFormat.Png;
            AddCreditsChunk(UncompressedPackage);

            Console.WriteLine($"  - Writing packages...");

            using (var file = File.Open(Path.Combine(Options.OutDirectory, $"{Game}-remeshes-dxt.dat"), FileMode.Create))
            {
                CompressedPackage.Write(file);
            }

            using (var file = File.Open(Path.Combine(Options.OutDirectory, $"{Game}-remeshes-png.dat"), FileMode.Create))
            {
                UncompressedPackage.Write(file);
            }

            using (var file = File.Open(Path.Combine(Options.OutDirectory, $"{Game}-remeshes-credits.dat"), FileMode.Create))
            {
                CreditsPackage.Write(file);
            }

            Console.WriteLine($"  - Done!");

            LegacyPackage?.Dispose();

            return 0;
        }
    }

    internal class ToolPackageRemeshes : ITool
    {

        private readonly PackageRemeshesOptions Options;

        public ToolPackageRemeshes(PackageRemeshesOptions opts)
        {
            Options = opts;
        }

        private void GeneratePackage(string game)
        {
            var packager = new GamePackager(Options, game);
            packager.Run();
        }

        public int Run()
        {
            var games = Options.Games.Split(',');

            foreach (var game in games)
            {
                GeneratePackage(game);
            }

            return 0;
        }
    }
}
