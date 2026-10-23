using FSO.Common.Enum;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.ArchiveTop100;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace FSO.Server
{
    internal class ToolImportArchiveTop100 : ITool
    {
        private Regex Timestamp = new Regex("^(?<Hours>[0-9]{2}):(?<Minutes>[0-9]{2}):(?<Seconds>[0-9]{2})");
        private Regex LogName = new Regex("^(?<Year>[0-9]{4})-(?<Month>[0-9]{2})-(?<Day>[0-9]{2})\\.log$");
        private Regex ServerRestart = new Regex("^([0-9]{2}:[0-9]{2}:[0-9]{2}) FSO\\.Server\\.ToolRunServer Starting server$");
        private Regex LotOpen = new Regex("^([0-9]{2}:[0-9]{2}:[0-9]{2}) FSO\\.Server\\.Servers\\.Lot\\.Domain\\.LotContainer Starting to host lot with dbid = (?<LotId>[0-9]+)$");
        private Regex LotClose = new Regex("^([0-9]{2}:[0-9]{2}:[0-9]{2}) FSO\\.Server\\.Servers\\.Lot\\.Domain\\.LotHostEntry Background Thread completely shut down for lot (?<LotId>[0-9]+)$");
        private Regex AvatarJoin = new Regex("^([0-9]{2}:[0-9]{2}:[0-9]{2}) FSO\\.Server\\.Servers\\.Lot\\.Domain\\.LotContainer Avatar (?<AvatarName>.*) \\((?<AvatarId>[0-9]+)\\) has joined lot (?<LotId>[0-9]+)$");
        private Regex AvatarLeave = new Regex("^([0-9]{2}:[0-9]{2}:[0-9]{2}) FSO\\.Server\\.Servers\\.Lot\\.Domain\\.LotContainer Avatar (?<AvatarId>[0-9]+) left lot (?<LotId>[0-9]+)$");
        private Regex ServerShutdown = new Regex("^([0-9]{2}:[0-9]{2}:[0-9]{2}) FSO\\.Server\\.ToolRunServer Shutdown commencing\\.$"); //

        private class LotVisitEntry
        {
            public uint LotId;
            public HashSet<uint> RoomieIds = [];
            public Dictionary<uint, long> AvatarVisitTicks = [];
            public Dictionary<uint, long> AvatarJoinTimes = [];
            public bool HasAnyoneJoined = false;

            public LotVisitEntry(uint lotId)
            {
                LotId = lotId;
            }

            public void BeginSpan(long ticks, uint avatarId)
            {
                AvatarJoinTimes[avatarId] = ticks;
            }

            public void EndSpan(long ticks, uint avatarId)
            {
                if (AvatarJoinTimes.TryGetValue(avatarId, out long initial))
                {
                    var diff = ticks - initial;

                    if (diff > 0)
                    {
                        if (!AvatarVisitTicks.TryGetValue(avatarId, out long sum))
                        {
                            sum = 0;
                        }

                        AvatarVisitTicks[avatarId] = sum + diff;
                    }

                    AvatarJoinTimes.Remove(avatarId);
                }
            }

            public void EndAllSpans(long ticks, bool addTime)
            {
                if (addTime)
                {
                    foreach (var ava in AvatarJoinTimes)
                    {
                        var diff = ticks - ava.Value;

                        if (diff > 0)
                        {
                            if (!AvatarVisitTicks.TryGetValue(ava.Key, out long sum))
                            {
                                sum = 0;
                            }

                            AvatarVisitTicks[ava.Key] = sum + diff;
                        }
                    }
                }

                AvatarJoinTimes.Clear();
            }
        }

        private class LotVisitCollection
        {
            public Dictionary<uint, LotVisitEntry> Lots = [];
            public Dictionary<uint, long> Avatars = [];

            public LotVisitCollection()
            {

            }
        }

        private struct ArchiveTop100Entry
        {
            public int shard_id;
            public uint lot_id;
            public int minutes;
            public LotCategory category;
            public int rank;
        }

        private IDAFactory DAFactory;
        private ImportArchiveTop100Options Options;

        public ToolImportArchiveTop100(ImportArchiveTop100Options options, IDAFactory factory)
        {
            this.Options = options;
            this.DAFactory = factory;
        }

        public int Run()
        {
            if (Options.LogDirectory == null)
            {
                Console.WriteLine("Please pass: <shard id> <server log directory>");
                return 1;
            }

            var collection = ParseLogs(Options.LogDirectory);

            var tickToMinutes = 1 / (double)(Stopwatch.Frequency * 60);

            Dictionary<LotCategory, List<DbArchiveTop100>> archiveTop100 = [];
            using (var da = DAFactory.Get())
            {
                var allAvatars = da.Avatars.All(Options.ShardId);
                var avatarById = allAvatars.ToDictionary(x => x.avatar_id);

                foreach (var lot in collection.Lots)
                {
                    var dbLot = da.Lots.Get((int)lot.Key);

                    if (dbLot != null)
                    {
                        bool allowRoomies = dbLot.category == LotCategory.community || dbLot.admit_mode >= 4;
                        long totalTicks = 0;
                        var roomies = lot.Value.RoomieIds;

                        foreach (var visitorTicks in lot.Value.AvatarVisitTicks)
                        {
                            avatarById.TryGetValue(visitorTicks.Key, out var myAva);
                            if (allowRoomies || !roomies.Contains(visitorTicks.Key) || (myAva?.moderation_level ?? 0) > 0)
                            {
                                if (myAva != null)
                                {
                                    collection.Avatars.TryGetValue(visitorTicks.Key, out long sum);
                                    collection.Avatars[visitorTicks.Key] = sum + visitorTicks.Value;
                                }

                                totalTicks += visitorTicks.Value;
                            }
                        }

                        var entry = new DbArchiveTop100
                        {
                            shard_id = Options.ShardId,
                            lot_id = (int)lot.Key,
                            category = (int)dbLot.category,
                            minutes = (int)(totalTicks * tickToMinutes),
                            rank = 0
                        };

                        if (!archiveTop100.TryGetValue(dbLot.category, out var categoryList))
                        {
                            categoryList = [];
                            archiveTop100[dbLot.category] = categoryList;
                        }

                        if (entry.minutes > 0)
                        {
                            categoryList.Add(entry);
                        }
                    }
                }

                var keys = archiveTop100.Keys.ToArray();

                foreach (var key in keys)
                {
                    List<DbArchiveTop100> ordered = [.. archiveTop100[key].OrderByDescending(x => x.minutes)];

                    var orderedSpan = CollectionsMarshal.AsSpan(ordered);

                    for (int i = 0; i < orderedSpan.Length; i++)
                    {
                        orderedSpan[i].rank = i + 1;
                    }

                    archiveTop100[key] = ordered;
                }

                da.ArchiveTop100.Clear(Options.ShardId);

                foreach (var category in archiveTop100)
                {
                    foreach (var entry in category.Value)
                    {
                        da.ArchiveTop100.Create(entry);
                    }
                }

                foreach (var avatar in collection.Avatars)
                {
                    da.ArchiveTop100.CreateAvatar(new DbArchiveTop100Avatar()
                    {
                        avatar_id = (int)avatar.Key,
                        minutes = (int)(avatar.Value * tickToMinutes),
                        shard_id = Options.ShardId
                    });
                }
            }

            return 0;
        }

        private LotVisitCollection ParseLogs(string dir)
        {
            var result = new LotVisitCollection();
            var oneHourTicks = Stopwatch.Frequency * 60 * 60;

            var logs = Directory.GetFiles(dir).Where(x => Path.GetExtension(x) == ".log").OrderBy(x => Path.GetFileName(x));

            LotVisitEntry TryGetLot(uint lotId)
            {
                if (!result.Lots.TryGetValue(lotId, out LotVisitEntry entry))
                {
                    entry = new LotVisitEntry(lotId);
                    result.Lots[lotId] = entry;
                }

                return entry;
            }

            long? lastEventTicks = null;

            foreach (var path in logs)
            {
                // Due to a configuration issue, FreeSO server logs recorded 12h time instead of 24h, but didn't include AM/PM.
                bool secondHalf = false;
                var name = LogName.Match(Path.GetFileName(path));

                if (!name.Success ||
                    !int.TryParse(name.Groups["Year"].Value, out int year) ||
                    !int.TryParse(name.Groups["Month"].Value, out int month) ||
                    !int.TryParse(name.Groups["Day"].Value, out int day))
                {
                    continue;
                }

                var logLines = File.ReadAllLines(path);

                int lastHour = -1;

                foreach (var line in logLines)
                {
                    var timestampMatch = Timestamp.Match(line);

                    if (timestampMatch.Success)
                    {
                        var hour = int.Parse(timestampMatch.Groups["Hours"].Value) % 12;
                        var minute = int.Parse(timestampMatch.Groups["Minutes"].Value);
                        var second = int.Parse(timestampMatch.Groups["Seconds"].Value);

                        if (hour < lastHour)
                        {
                            secondHalf = true;
                        }

                        lastHour = hour;

                        if (secondHalf)
                        {
                            hour += 12;
                        }

                        var time = new DateTime(year, month, day, hour, minute, second);
                        var timeTicks = time.Ticks;

                        if (lastEventTicks.HasValue)
                        {
                            var sinceLastEvent = timeTicks - lastEventTicks.Value;

                            if (sinceLastEvent > oneHourTicks)
                            {
                                // There's a large gap since the large time - we can't really trust that any of the spans are valid.
                                foreach (var lot in result.Lots.Values)
                                {
                                    lot.EndAllSpans(timeTicks, false);
                                }
                            }
                        }

                        lastEventTicks = timeTicks;

                        var restartMatch = ServerRestart.Match(line);
                        var openMatch = LotOpen.Match(line);
                        var closeMatch = LotClose.Match(line);
                        var joinMatch = AvatarJoin.Match(line);
                        var leaveMatch = AvatarLeave.Match(line);
                        var shutdownMatch = ServerShutdown.Match(line);

                        if (restartMatch.Success)
                        {
                            // End any avatar spans that are still open, but don't count the time.
                            // They're likely to be open due to an unsafe shutdown.
                            foreach (var lot in result.Lots.Values)
                            {
                                lot.EndAllSpans(timeTicks, false);
                            }
                        }
                        else if (openMatch.Success)
                        {
                            var lot = TryGetLot(uint.Parse(openMatch.Groups["LotId"].Value));

                            // If there's any open spans... They shouldn't be.

                            lot.EndAllSpans(timeTicks, false);
                            lot.HasAnyoneJoined = false;
                        }
                        else if (closeMatch.Success)
                        {
                            var lot = TryGetLot(uint.Parse(closeMatch.Groups["LotId"].Value));
                            // If there's anyone else on the lot, add the remaining span time.

                            lot.EndAllSpans(timeTicks, true);
                        }
                        else if (joinMatch.Success)
                        {
                            var lot = TryGetLot(uint.Parse(joinMatch.Groups["LotId"].Value));
                            var avatar = uint.Parse(joinMatch.Groups["AvatarId"].Value);

                            lot.BeginSpan(timeTicks, avatar);

                            // If nobody else has joined, then the user joining is a roomie (or admin, or this is a community lot)
                            // Either way, we don't want to count them in the visitor hours.

                            if (!lot.HasAnyoneJoined)
                            {
                                lot.RoomieIds.Add(avatar);
                                lot.HasAnyoneJoined = true;
                            }
                        }
                        else if (leaveMatch.Success)
                        {
                            var lot = TryGetLot(uint.Parse(leaveMatch.Groups["LotId"].Value));
                            var avatar = uint.Parse(leaveMatch.Groups["AvatarId"].Value);

                            lot.EndSpan(timeTicks, avatar);
                        }
                        else if (shutdownMatch.Success)
                        {
                            // End any avatar spans that are still open (and count the time)
                            foreach (var lot in result.Lots.Values)
                            {
                                lot.EndAllSpans(timeTicks, true);
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}
