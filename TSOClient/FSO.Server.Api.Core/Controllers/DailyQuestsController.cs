using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FSO.Server.Api.Core.Utils;
using FSO.Server.Database.DA.DailyQuests;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace FSO.Server.Api.Core.Controllers
{
    // Live-progress read + immediate-claim endpoints for the daily-quests
    // system. Backed by fso_daily_quests and updated by event hooks in
    // SqlAvatars / SqlActionLog. See edenso_server_data/
    // design_daily_quests_v1.md.
    //
    // ** Auth (phase 2) **
    // Both endpoints require the JWT minted by InitialConnectServlet.
    // The client retrieves the token from UserAuthorized.FSOApiAuthToken
    // at login, stashes it in GameFacade.ApiAuthToken, and presents it
    // as 'Authorization: Bearer <token>'. RequireAuthentication validates
    // the signature against the server's JWT secret. avatar_id is taken
    // from a query param but server-side verified to belong to the
    // authenticated user — claiming someone else's avatar returns 403.
    //
    // Exploit surface after this commit:
    //   - Unauthenticated griefing: closed (401).
    //   - Information disclosure about other players' quests: closed (403).
    //   - Race conditions on Claim: closed in 1.5 (mark-then-credit pattern).
    //   - Forged JWT: requires the server's signing secret; not feasible.
    [EnableCors]
    [ApiController]
    public class DailyQuestsController : ControllerBase
    {
        // GET /userapi/quests/today?avatar_id=NNN
        // Headers: Authorization: Bearer <token>
        //   → 200 [{slot,type,description,target,progress,reward,completed,claimed}]
        //   → 401 if the JWT is missing / invalid / expired
        //   → 403 if avatar_id doesn't belong to the authenticated user
        //   → 404 if no quests rolled yet today
        [HttpGet]
        [Route("userapi/quests/today")]
        public IActionResult Today([FromQuery(Name = "avatar_id")] uint avatar_id)
        {
            var api = Api.INSTANCE;
            var user = api.RequireAuthentication(Request);

            using (var da = api.DAFactory.Get())
            {
                var avatar = da.Avatars.Get(avatar_id);
                if (avatar == null)
                    return ApiResponse.Json(HttpStatusCode.NotFound,
                        new JSONError("avatar not found"));
                if (avatar.user_id != user.UserID)
                    return ApiResponse.Json(HttpStatusCode.Forbidden,
                        new JSONError("not your avatar"));

                uint today = (uint)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 86400);
                var quests = da.DailyQuests.GetForDay(avatar_id, today).ToList();
                if (quests.Count == 0)
                    return ApiResponse.Json(HttpStatusCode.NotFound,
                        new JSONError("no quests rolled yet for today"));

                var result = quests.Select(ToJSON).ToList();
                return ApiResponse.Json(HttpStatusCode.OK, new JSONQuestList { quests = result });
            }
        }

        // POST /userapi/quests/claim/{slot}?avatar_id=NNN
        // Headers: Authorization: Bearer <token>
        //   → 200 { reward, new_balance }
        //   → 401 / 403 / 404 as above
        //   → 409 if quest not yet completed
        //   → 410 if already claimed (race-safe via MarkPaid rows-affected check)
        [HttpPost]
        [Route("userapi/quests/claim/{slot}")]
        public IActionResult Claim(byte slot, [FromQuery(Name = "avatar_id")] uint avatar_id)
        {
            var api = Api.INSTANCE;
            var user = api.RequireAuthentication(Request);

            using (var da = api.DAFactory.Get())
            {
                var avatar = da.Avatars.Get(avatar_id);
                if (avatar == null)
                    return ApiResponse.Json(HttpStatusCode.NotFound,
                        new JSONError("avatar not found"));
                if (avatar.user_id != user.UserID)
                    return ApiResponse.Json(HttpStatusCode.Forbidden,
                        new JSONError("not your avatar"));

                uint today = (uint)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 86400);
                var quest = da.DailyQuests.GetForDay(avatar_id, today)
                    .FirstOrDefault(q => q.slot == slot);

                if (quest == null)
                    return ApiResponse.Json(HttpStatusCode.NotFound,
                        new JSONError("no such quest"));

                if (!quest.completed_ts.HasValue)
                    return ApiResponse.Json(HttpStatusCode.Conflict,
                        new JSONError("quest not yet completed"));

                if (quest.paid_ts.HasValue)
                    return ApiResponse.Json(HttpStatusCode.Gone,
                        new JSONError("already claimed"));

                // Race-safe claim: mark FIRST (atomic via the WHERE
                // paid_ts IS NULL guard in MarkPaid), credit ONLY if the
                // mark succeeded. Two concurrent claim requests both
                // pass the .HasValue checks above; without this ordering
                // they'd both call CreditBudget and the player would
                // double-collect. With this ordering the loser sees 0
                // rows affected and gets a 410 — only one credit fires.
                uint nowEpoch = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                int claimed = da.DailyQuests.MarkPaid(avatar_id, today, slot, nowEpoch);
                if (claimed == 0)
                    return ApiResponse.Json(HttpStatusCode.Gone,
                        new JSONError("already claimed"));

                // Plain CreditBudget — quest rewards must NOT loop back
                // into EARN quest progress. Same guarantee as the cron.
                da.Avatars.CreditBudget(avatar_id, (int)quest.reward);

                int newBalance = da.Avatars.GetBudget(avatar_id);
                return ApiResponse.Json(HttpStatusCode.OK,
                    new JSONClaimResult { reward = quest.reward, new_balance = newBalance });
            }
        }

        // Maps DbDailyQuest → wire format. Descriptions are computed
        // server-side so the client doesn't need to ship the formatting
        // rules; keeps everything localizable in one place later.
        private static JSONQuest ToJSON(DbDailyQuest q)
        {
            return new JSONQuest
            {
                slot = q.slot,
                type = QuestTypeName(q.quest_type),
                description = Describe(q.quest_type, q.target),
                target = q.target,
                progress = q.progress,
                reward = q.reward,
                completed = q.completed_ts.HasValue,
                claimed = q.paid_ts.HasValue
            };
        }

        private static string QuestTypeName(byte t)
        {
            switch (t)
            {
                case QuestType.Earn:  return "EARN";
                case QuestType.Skill: return "SKILL";
                case QuestType.Visit: return "VISIT";
                case QuestType.Buy:   return "BUY";
                default:              return "UNKNOWN";
            }
        }

        private static string Describe(byte type, ulong target)
        {
            switch (type)
            {
                case QuestType.Earn:  return $"Earn §{target:N0} today";
                case QuestType.Skill: return $"Gain {target / 100} skill point(s) today";
                case QuestType.Visit: return $"Visit {target} unique lot(s) today";
                case QuestType.Buy:   return $"Spend §{target:N0} at the catalog today";
                default:              return "(unknown quest)";
            }
        }
    }

    // Wire types — public so Newtonsoft.Json can serialize them via
    // ApiResponse.Json. Snake-cased to match the rest of /userapi.
    public class JSONQuestList
    {
        public List<JSONQuest> quests { get; set; }
    }

    public class JSONQuest
    {
        public byte slot { get; set; }
        public string type { get; set; }
        public string description { get; set; }
        public ulong target { get; set; }
        public ulong progress { get; set; }
        public uint reward { get; set; }
        public bool completed { get; set; }
        public bool claimed { get; set; }
    }

    public class JSONClaimResult
    {
        public uint reward { get; set; }
        public int new_balance { get; set; }
    }

    public class JSONError
    {
        public string error { get; set; }
        public JSONError(string msg) { error = msg; }
    }
}