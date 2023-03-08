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
        // Method for other game screens
        public static void SendFSOPresence(string state, string details = null)
        {

            if (!Active) return; // RPC not active
            var presence = new DiscordRpc.RichPresence();
            
            presence.largeImageKey = "sunrise_crater";
            presence.largeImageText = "Sunrise Crater";

            presence.state = state;
            presence.details = details == null ? "" : details;

            DiscordRpc.UpdatePresence(ref presence);
        }
        // Standard DiscordRpc presence method
        public static void SendFSOPresence(string activeSim, string lotName, int lotID, int players, int maxSize, int catID, string cdnUrl, bool isPrivate = false)
        {
            if (!Active) return;
            var presence = new DiscordRpc.RichPresence();

            bool isJob = false;

            if (!isPrivate)
            {
                if (lotName?.StartsWith("{job:") == true)
                {
                    isJob = true;

                    var jobStr = "";
                    var split = lotName.Split(':');
                    if (split.Length > 2)
                    {
                        switch (split[1])
                        {
                            case "0": // Robot Factory
                                jobStr = "Robot Factory";
                                break;
                            case "1": // Restaurant
                                jobStr = "Restaurant";
                                break;
                            case "2": // Nightclub
                                jobStr = "Nightclub";
                                break;
                            default: // Other
                                jobStr = "Job Lot";
                                break;
                        }
                        jobStr += " | Level " + split[2].Trim('}');
                    }
                    else
                        jobStr = "Job Lot";
                    if (activeSim != null) presence.details = "Playing as " + activeSim;
                    presence.state = jobStr;
                }
                else
                {
                    if (activeSim == null)
                    {
                        presence.state = lotName ?? "Idle in City";
                        presence.details = "";
                    }                       
                    else
                    {
                        presence.details = "Playing as " + activeSim;
                        presence.state = lotName ?? "Idle in City";
                    }
                }                
                
            }
            else
            {
                presence.state = "Online";
                presence.details = "Privacy Enabled";
            }
            

            presence.largeImageKey = "sunrise_crater";
            presence.largeImageText = "Sunrise Crater";

            if (lotName != null && !isPrivate)
            {
                presence.joinSecret = lotID + "#" + lotName;
                //presence.matchSecret = lotID + "#" + lotName+".";
                presence.spectateSecret = lotID + "#" + lotName + "..";
                presence.partyMax = maxSize;
                presence.partySize = players;
                presence.partyId = lotID.ToString();

                if (cdnUrl != null && !isJob)
                {
                    presence.smallImageKey = "cat_" + catID;
                    presence.smallImageText = CapFirstWord(((LotCategory)catID).ToString());

                    presence.largeImageKey = $"{cdnUrl}/userapi/city/1/{lotID}.png";
                    presence.largeImageText = presence.state;
                }
                else
                {
                    presence.largeImageKey = "cat_" + catID;
                    presence.largeImageText = CapFirstWord(((LotCategory)catID).ToString());
                }
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
