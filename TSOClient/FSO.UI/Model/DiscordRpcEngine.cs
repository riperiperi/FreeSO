using FSO.Common.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FSO.UI.Model.DiscordRpc;

namespace FSO.UI.Model
{
    public static class DiscordRpcEngine
    {
        public static bool Active;
        public static bool Disable;
        public static string Secret;
        public static EventHandlers Events;

        public static void Init()
        {
            try
            {
                var handlers = new EventHandlers();
                handlers.readyCallback += Ready;
                handlers.errorCallback += Error;
                handlers.joinCallback += Join;
                handlers.spectateCallback += Spectate;
                handlers.disconnectedCallback += Disconnected;
                handlers.requestCallback += Request;
                Events = handlers;

                DiscordRpc.Initialize("378352963468525569", ref handlers, true, null);
            } catch (Exception)
            {
                Active = false;
            }
        }

        public static void Update()
        {
            if (Disable) return;
            try
            {
                DiscordRpc.RunCallbacks();
            }
            catch (Exception)
            {
                Active = false;
                Disable = true;
            }
        }

        public static void SendFSOPresence(string lotName, int lotID, int players, int maxSize, int catID)
        {
            if (!Active) return;
            var presence = new DiscordRpc.RichPresence();
            if (lotName?.StartsWith("{job:") == true)
            {
                var split = lotName.Split(':');
                if (split.Length > 2)
                {
                    switch (split[1])
                    {
                        case "0":
                            presence.state = "Playing Robot Factory Job";
                            break;
                        case "1":
                            presence.state = "Playing Restaurant Job";
                            break;
                        default:
                            presence.state = "Playing A Job Lot";
                            break;
                    }
                    presence.details = "Level " + split[2];
                }
                else
                {
                    presence.state = "Playing a Job Lot";
                }
            }
            else presence.state = (lotName == null) ? "Idle in city" : "In Lot: " + lotName;

            presence.largeImageKey = "sunrise_crater";
            presence.largeImageText = "Sunrise Crater";

            if (lotName != null)
            {
                presence.joinSecret = lotID + "#" + lotName;
                //presence.matchSecret = lotID + "#" + lotName+".";
                presence.spectateSecret = lotID + "#" + lotName + "..";
                presence.partyMax = maxSize;
                presence.partySize = players;
                presence.partyId = lotID.ToString();

                presence.largeImageKey = "cat_" + catID;
                presence.largeImageText = CapFirstWord(((LotCategory)catID).ToString());
            }

            DiscordRpc.UpdatePresence(ref presence);
        }

        private static string CapFirstWord(string cat)
        {
            return char.ToUpperInvariant(cat[0]) + cat.Substring(1);
        }

        public static void Ready()
        {
            Active = true;
        }

        public static void Error(int errorCode, string message)
        {

        }

        public static void Join(string secret)
        {
            Secret = secret;
        }

        public static void Spectate(string secret)
        {
            Secret = secret;
        }

        public static void Disconnected(int errorCode, string message)
        {

        }

        public static void Request(DiscordRpc.JoinRequest request)
        {

        }
    }
}
