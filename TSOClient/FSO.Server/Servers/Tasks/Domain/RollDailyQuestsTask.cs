using System;
using System.Collections.Generic;
using System.Linq;
using FSO.Files.Formats.tsodata;
using FSO.Server.Common;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.DailyQuests;
using FSO.Server.Database.DA.Tasks;
using FSO.Server.Domain;
using FSO.Server.Protocol.Gluon.Packets;
using NLog;

namespace FSO.Server.Servers.Tasks.Domain
{
    // Nightly cron driving the daily-quest meta loop. See
    // edenso_server_data/design_daily_quests_v1.md.
    //
    // Each run (00:00 UTC):
    //   1) Pay rewards for yesterday's completed-but-unpaid quests, mail
    //      the recipient per completed quest.
    //   2) Roll 3 fresh quests for every active avatar for today.
    //   3) Mail each active avatar a single "Today's Quests" summary.
    //   4) Purge fso_action_log rows older than 30 days.
    //
    // Out-of-pool quest types in v1: SKILL is omitted until the live
    // SKILL_GAINED hook lands in phase 1.5. The remaining three (EARN,
    // VISIT, BUY) are always rolled per avatar.
    public class RollDailyQuestsTask : ITask
    {
        private IDAFactory DAFactory;
        private IGluonHostPool HostPool;
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        // Days of fso_action_log history to retain. Older rows get deleted
        // at the end of each run.
        private const int ACTION_LOG_RETENTION_DAYS = 30;

        // "Active" = the user behind the avatar logged in within this window.
        // Inactive avatars don't get fresh quests rolled.
        private const int ACTIVITY_WINDOW_DAYS = 30;

        // v1 quest type pool. Order doesn't matter — we always roll all
        // three, one per slot, since the pool size equals slots/day.
        private static readonly byte[] V1_QUEST_POOL = {
            QuestType.Earn,
            QuestType.Visit,
            QuestType.Buy
        };

        // RNG kept private but not Thread-static — the task runs in one
        // process at a time on its scheduled tick.
        private readonly Random _rng = new Random();

        public RollDailyQuestsTask(IDAFactory daFactory, IGluonHostPool hostPool)
        {
            DAFactory = daFactory;
            HostPool = hostPool;
        }

        public void Abort() { }

        public DbTaskType GetTaskType() => DbTaskType.roll_daily_quests;

        public void Run(TaskContext context)
        {
            uint nowEpoch = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            uint today = nowEpoch / 86400;
            uint yesterday = today - 1;
            uint activityCutoff = nowEpoch - (uint)(ACTIVITY_WINDOW_DAYS * 86400);

            using (var da = DAFactory.Get())
            {
                // ---- 1. Pay out yesterday's completed-but-unpaid quests ----
                var payoutMail = new List<MessageItem>();
                var unpaid = da.DailyQuests.GetUnpaidForDay(yesterday).ToList();
                LOG.Info($"Daily quests: {unpaid.Count} unpaid completions from day {yesterday}");

                foreach (var q in unpaid)
                {
                    // NOTE: plain CreditBudget — quest rewards must NOT count
                    // toward EARN quest progress, or a completed EARN quest's
                    // payout would re-bump the (now-completed) EARN quest and
                    // feed back into tomorrow's EARN target trivially.
                    da.Avatars.CreditBudget(q.avatar_id, (int)q.reward);
                    da.DailyQuests.MarkPaid(q.avatar_id, q.day, q.slot, nowEpoch);

                    payoutMail.Add(new MessageItem
                    {
                        Subject = "Quest Reward Received",
                        Body = $"Your daily quest \"{DescribeQuest(q.quest_type, q.target)}\" has been " +
                               $"paid out. §{q.reward:N0} simoleons have been added to your account.\n\n" +
                               "— EdenSO Daily Quests",
                        SenderID = uint.MaxValue,
                        SenderName = "EdenSO Daily Quests",
                        TargetID = q.avatar_id,
                        Type = 4,
                        Subtype = 0
                    });
                }

                // ---- 2. Identify active avatars who don't yet have today's quests ----
                // Active = the owning user logged in within the last
                // ACTIVITY_WINDOW_DAYS. Avoids rolling for dormant accounts.
                // The DA method's LEFT JOIN against today's existing quests
                // makes the task safe to re-run.
                var avatarsToRoll = da.DailyQuests
                    .GetAvatarsNeedingRoll(today, activityCutoff)
                    .ToList();

                LOG.Info($"Daily quests: rolling for {avatarsToRoll.Count} active avatars");

                // ---- 3. Roll three quests + assemble announcement mail ----
                var announceMail = new List<MessageItem>();
                foreach (var av in avatarsToRoll)
                {
                    uint ageDays = av.created_epoch == 0
                        ? 0
                        : (nowEpoch - av.created_epoch) / 86400;

                    // Shuffle the pool and take the first len(slots) types.
                    // With pool size == slot count today, this is just a random
                    // permutation — keeps the order varied across days.
                    var types = V1_QUEST_POOL.OrderBy(_ => _rng.Next()).ToArray();

                    var quests = new List<DbDailyQuest>(types.Length);
                    for (byte slot = 0; slot < types.Length; slot++)
                    {
                        var (target, reward) = ScaleQuest(types[slot], ageDays);
                        var quest = new DbDailyQuest
                        {
                            avatar_id = av.avatar_id,
                            day = today,
                            slot = slot,
                            quest_type = types[slot],
                            target = target,
                            progress = 0,
                            reward = reward,
                            parameter = null,
                            completed_ts = null,
                            paid_ts = null
                        };
                        da.DailyQuests.Insert(quest);
                        quests.Add(quest);
                    }

                    // One mail per avatar describing today's three quests.
                    announceMail.Add(new MessageItem
                    {
                        Subject = "Today's Daily Quests",
                        Body = BuildAnnouncementBody(quests),
                        SenderID = uint.MaxValue,
                        SenderName = "EdenSO Daily Quests",
                        TargetID = av.avatar_id,
                        Type = 4,
                        Subtype = 0
                    });
                }

                // ---- 4. Dispatch mail through city servers ----
                // TODO: MULTI-CITY — split by shard_id when more than one shard
                // is supported. Same pattern as BirthdayGiftTask.
                var allMail = payoutMail.Concat(announceMail).ToList();
                if (allMail.Count > 0)
                {
                    var cityServers = HostPool.GetByRole(Database.DA.Hosts.DbHostRole.city);
                    foreach (var city in cityServers)
                    {
                        city.Write(new SendCityMail(allMail));
                    }
                    LOG.Info($"Daily quests: dispatched {payoutMail.Count} payout + {announceMail.Count} announcement messages");
                }

                // ---- 5. Action-log retention purge ----
                uint purgeBefore = today - ACTION_LOG_RETENTION_DAYS;
                int purged = da.ActionLog.Purge(purgeBefore);
                if (purged > 0)
                    LOG.Info($"Daily quests: purged {purged} action_log rows older than day {purgeBefore}");
            }
        }

        // Per-quest-type target + reward scaling. Targets ramp with avatar
        // age over the first two weeks then plateau, so day-one players
        // aren't crushed but veterans aren't trivialised. Reward formulas
        // come straight from the design doc.
        private static (ulong target, uint reward) ScaleQuest(byte questType, uint ageDays)
        {
            uint capDays = Math.Min(ageDays, 14);
            switch (questType)
            {
                case QuestType.Earn:
                {
                    ulong target = 2000UL + 500UL * capDays;          // 2k → 9k
                    uint reward = (uint)Math.Min(5000UL, target / 10); // cap 5k
                    return (target, reward);
                }
                case QuestType.Visit:
                {
                    ulong target = 2UL + Math.Min(ageDays / 10UL, 4UL); // 2 → 6
                    uint reward = (uint)(400UL * target);
                    return (target, reward);
                }
                case QuestType.Buy:
                {
                    ulong target = 1000UL + 200UL * capDays;           // 1k → 3.8k
                    uint reward = (uint)Math.Min(3000UL, (target * 15UL) / 100UL); // cap 3k
                    return (target, reward);
                }
                case QuestType.Skill:
                {
                    ulong target = 100UL + Math.Min(ageDays / 7UL, 2UL) * 100UL; // 1 → 3 points (hundredths)
                    uint reward = 1500;
                    return (target, reward);
                }
                default:
                    // Unknown quest type — give them a freebie rather than 0/0
                    // which would auto-complete on insert.
                    return (1UL, 500);
            }
        }

        // Human-readable quest description for mail bodies. Kept as strings
        // here rather than .cst entries since the cron mails are server-only
        // and never go through the client UIScript localiser.
        private static string DescribeQuest(byte questType, ulong target)
        {
            switch (questType)
            {
                case QuestType.Earn:
                    return $"Earn §{target:N0} today";
                case QuestType.Skill:
                    return $"Gain {target / 100} skill point(s) today";
                case QuestType.Visit:
                    return $"Visit {target} unique lot(s) today";
                case QuestType.Buy:
                    return $"Spend §{target:N0} at the catalog today";
                default:
                    return "(unknown quest)";
            }
        }

        private static string BuildAnnouncementBody(IList<DbDailyQuest> quests)
        {
            var lines = new List<string>
            {
                "Three daily quests are waiting for you. Complete them before",
                "midnight UTC to earn the listed simoleon rewards — they'll",
                "land in your inbox tomorrow morning.",
                ""
            };
            foreach (var q in quests)
            {
                lines.Add($"  • {DescribeQuest(q.quest_type, q.target)}  —  reward §{q.reward:N0}");
            }
            lines.Add("");
            lines.Add("— EdenSO Daily Quests");
            return string.Join("\n", lines);
        }

    }
}