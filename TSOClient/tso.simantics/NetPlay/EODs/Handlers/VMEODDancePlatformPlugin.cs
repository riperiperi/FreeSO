using FSO.SimAntics.NetPlay.EODs.Handlers.Data;
using FSO.SimAntics.NetPlay.EODs.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODDancePlatformPlugin : VMEODHandler
    {
        public VMEODClient ControllerClient;
        public VMEODClient PlayerClient;
        public VMEODNightclubControllerPlugin ControllerPlugin;

        public VMEODFreshnessTracker Fresh = new VMEODFreshnessTracker(1);
        public List<float> SectionRatings = new List<float>();
        public int[] CorrectDances = { 0, 0, 0 };

        public int GroupID;
        public int Rating;

        public VMEODDancePlatformPlugin(VMEODServer server) : base(server)
        {
            PlaintextHandlers["close"] = P_Close;
            PlaintextHandlers["press_button"] = P_DanceButton;
        }

        public void CalculateFinalRating()
        {
            Rating = (int)Math.Min(Math.Round(SectionRatings.Average() + ControllerPlugin.RoundPercentage() * 20), 100);
            TimeSinceRatingChange = 0;
            //Console.WriteLine(Rating);
            //send it to the object

            if (ControllerClient != null) ControllerClient.SendOBJEvent(new VMEODEvent(1, (short)Rating));
        }

        public void Reset()
        {
            SectionRatings = new List<float>();
            Rating = 25;
            for (int i = 0; i < 3; i++) CorrectDances[i] = 0;
            Fresh.Reset();
        }

        public int ActiveInteractionID;
        public ushort? ActiveInteractionUID;

        public int TimeSinceRatingChange = 0;

        public override void Tick()
        {
            base.Tick();
            Fresh.Tick();

            if (ControllerPlugin == null) ControllerPlugin = Server.vm.EODHost.GetFirstHandler<VMEODNightclubControllerPlugin>();

            if (PlayerClient != null && ControllerPlugin.RoundActive)
            {
                //active interaction is in index 0, callee is the invoker, dance id is 

                var queue = PlayerClient.Avatar.Thread.Queue;
                var active = queue.FirstOrDefault();
                var danceID = 0;

                if (PlayerClient.Avatar.Thread.ActiveQueueBlock > -1 && active.Callee == ControllerClient.Invoker && active.InteractionNumber > 5)
                {
                    danceID = active.InteractionNumber - 6;
                } else
                {
                    active = null;
                }

                if (active?.UID != ActiveInteractionUID)
                {
                    if (ActiveInteractionUID != null)
                    {
                        //a dance ended. 
                        DanceCompleted(ActiveInteractionID);
                    }
                    ActiveInteractionUID = active?.UID;
                    ActiveInteractionID = danceID;
                }

                if (++TimeSinceRatingChange > 30*5)
                {
                    CalculateCurrentDanceRating();
                }
            }

            //we register a dance as completed when its interaction 

        }

        private void InitIfRequired(int currentDance)
        {
            while (currentDance >= SectionRatings.Count)
            {
                for (int i = 0; i < 3; i++) CorrectDances[i] = 0;
                SectionRatings.Add(0);
            }
        }
        
        public void DanceCompleted(int dance)
        {
            Fresh.SendCommand(dance);

            var currentDance = ControllerPlugin.DancePatternNum;
            InitIfRequired(currentDance);

            var correct = false;
            for (int i=0; i<3; i++)
            {
                if (dance == ControllerPlugin.CurrentWinningDancePatterns[i] && CorrectDances[i] == 0)
                {
                    correct = true;
                    CorrectDances[i] = 1;
                }
            }

            if (correct)
            {
                ControllerPlugin.DanceFloor.AddParticle(VMEODNCParticleType.Rect, 0, 0, GroupID);
                if (CorrectDances.Sum() == 3)
                {
                    ControllerPlugin.DanceFloor.AddParticle(VMEODNCParticleType.Rect, 0, -2, GroupID);
                    ControllerPlugin.DanceFloor.AddParticle(VMEODNCParticleType.Rect, 0, -4, GroupID);
                }
            }

            CalculateCurrentDanceRating();
        }

        public void CalculateCurrentDanceRating()
        {
            var currentDance = ControllerPlugin.DancePatternNum;
            InitIfRequired(currentDance);

            int totalRating = 0;
            for (int i = 0; i < 3; i++)
            {
                totalRating += CorrectDances[i] * ControllerPlugin.PatternRatingScale[i];
            }
            totalRating = (int)Math.Round(Math.Sqrt(totalRating / 100f)*100);

            SectionRatings[currentDance] = (25 * Fresh.Freshness) + (totalRating) * 0.80f * Fresh.GoodScoreFreshness;
            CalculateFinalRating();
        }

        public void P_Close(string evt, string text, VMEODClient client)
        {
            Server.Disconnect(client);
        }

        public void P_DanceButton(string evt, string text, VMEODClient client)
        {
            byte num = 0;
            if (!byte.TryParse(text, out num)) return;
            if (ControllerClient != null) ControllerClient.SendOBJEvent(new VMEODEvent((short)(num), client.Avatar.ObjectID));
        }

        public override void OnConnection(VMEODClient client)
        {
            if (client.Avatar != null)
            {
                PlayerClient = client;
                client.Send("dance_show", GroupID.ToString());
            }
            else
            {
                //we're the dance floor controller!
                ControllerClient = client;
                GroupID = client.Invoker.GetValue(SimAntics.Model.VMStackObjectVariable.GroupID);
            }
        }

        public override void OnDisconnection(VMEODClient client)
        {
            base.OnDisconnection(client);

            if (client == PlayerClient)
            {
                PlayerClient = null;
                Server.Disconnect(ControllerClient);
            }
            if (client == ControllerClient) ControllerClient = null;
        }
    }
}
