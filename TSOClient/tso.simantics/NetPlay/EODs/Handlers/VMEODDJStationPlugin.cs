using FSO.SimAntics.NetPlay.EODs.Handlers.Data;
using FSO.SimAntics.NetPlay.EODs.Model;
using Microsoft.Xna.Framework;
using System;
using System.Linq;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODDJStationPlugin : VMEODHandler
    {
        public VMEODClient ControllerClient;
        public VMEODClient PlayerClient;

        public VMEODFreshnessTracker Fresh = new VMEODFreshnessTracker(4);

        public VMEODNightclubControllerPlugin ControllerPlugin;
        public string[] Patterns = { "000", "000", "000", "000" };
        public bool[] PatternDirty = { false, false, false, false };
        public bool[] PatternCorrect = { false, false, false, false };
        public float[] PatternRatings = { 0, 0, 0, 0 };
        public int LastRating = -1;
        public int Rating = 0;
        public int GroupID;

        public int TimeToRating = 0;

        public VMEODDJStationPlugin(VMEODServer server) : base(server)
        {
            PlaintextHandlers["close"] = P_Close;
            PlaintextHandlers["press_button"] = P_DJButton;
        }

        public void Reset()
        {
            Fresh.Reset();
            //pick random patterns
            var rand = new Random();
            for (int i=0; i<4; i++)
            {
                Patterns[i] = new string(new char[] {
                    (char) (rand.Next(4) + '0'),
                    (char) (rand.Next(4) + '0'),
                    (char) (rand.Next(4) + '0'),
                });
                PatternDirty[i] = true;
                PatternRatings[i] = 0;
                PatternCorrect[i] = false;
            }
            if (PlayerClient != null) SendPatterns(PlayerClient);
            Rating = 0;
            LastRating = -1;

            TimeToRating = GroupID * 45;
        }

        public override void Tick()
        {
            base.Tick();
            Fresh.Tick();
            //push out ratings when we have a real client
            //under event 1
            if (ControllerPlugin == null) ControllerPlugin = Server.vm.EODHost.GetFirstHandler<VMEODNightclubControllerPlugin>();
            else if (ControllerClient != null)
            {
                if (TimeToRating-- == 0)
                {
                    TimeToRating = 5 * 30;
                    //todo: periodic send
                    bool newPatternCorrect = false;
                    for (int i = 0; i < 4; i++)
                    {
                        if (PatternDirty[i])
                        {
                            var p = Patterns[i];
                            var ind = (short)((p[0] - '0') * 16 + (p[1] - '0') * 4 + (p[2] - '0'));
                            var dist = ControllerPlugin.CurrentWinningDJPattern[i] - ind;

                            if (dist == 0 && !PatternCorrect[i])
                            {
                                PatternCorrect[i] = true;
                                newPatternCorrect = true;
                            }
                            Fresh.SendCommand(ind, i);

                            var distPct = 1 - Math.Abs(dist / 64f);

                            PatternRatings[i] = (distPct) * (distPct);
                            PatternDirty[i] = false;
                        }
                    }
                    //recalculate our rating. send it out too!
                    var avg = PatternRatings.Average();

                    Rating = (int)Math.Min(100, Math.Round(100 * (0.8f * avg * Fresh.GoodScoreFreshness + 0.25f * ControllerPlugin.RoundPercentage())));
                    ControllerClient.SendOBJEvent(new VMEODEvent(1, (short)Rating));

                    if (newPatternCorrect)
                    {
                        var stationPos = ControllerClient.Invoker.Position;
                        ControllerPlugin.DanceFloor.AddParticle(VMEODNCParticleType.Arrow, 
                            ControllerPlugin.DanceFloor.GetDirection(new Point(stationPos.TileX, stationPos.TileY)), 0, GroupID);
                    }
                    else if (LastRating != Rating && LastRating != -1)
                    {
                        if (LastRating > Rating)
                        {
                            ControllerPlugin.DanceFloor.AddParticle(VMEODNCParticleType.Colder, 0, 0, GroupID);
                        } else
                        {
                            var rand = new Random();
                            ControllerPlugin.DanceFloor.AddParticle(VMEODNCParticleType.Line, (float)(rand.Next(8) * Math.PI / 4), 0, GroupID);
                        }
                    }
                    LastRating = Rating;
                }
            }
        }

        public void P_Close(string evt, string text, VMEODClient client)
        {
            Server.Disconnect(client);
        }

        public void P_DJButton(string evt, string text, VMEODClient client)
        {
            if (text.Length < 3) return;
            byte category = 0;
            if (!byte.TryParse(text[0].ToString(), out category) || category > 3) return;
            byte ind1 = 0;
            if (!byte.TryParse(text[1].ToString(), out ind1) || ind1 > 3) return; // (abc), (most sig digit), (least sig digit)
            byte ind2 = 0;
            if (!byte.TryParse(text[2].ToString(), out ind2) || ind2 > 3) return; //number

            PatternDirty[category] = true;
            var catPat = Patterns[category].ToCharArray();
            catPat[ind1] = ind2.ToString()[0];
            var p = new string(catPat);
            Patterns[category] = p;
            var ind = (short)((p[0] - '0') * 16 + (p[1] - '0') * 4 + (p[2] - '0'));

            //make up for a HIT typo - object attribute order mismatches HIT's expectations!
            if (category == 0) category = 1;
            else if (category == 1) category = 0;

            if (ControllerClient != null) ControllerClient.SendOBJEvent(new VMEODEvent((short)(category + 10), ind));

            SendPatterns(client);
        }

        public void SendPatterns(VMEODClient client)
        {
            client.Send("dj_active", string.Join("|", Patterns));
        }

        public override void OnConnection(VMEODClient client)
        {
            if (client.Avatar != null)
            {
                client.Send("dj_show", GroupID.ToString());
                SendPatterns(client);
                PlayerClient = client;
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
            if (PlayerClient == client)
            {
                PlayerClient = null;
                Server.Disconnect(ControllerClient);
            }
            if (ControllerClient == client) ControllerClient = null;
            base.OnDisconnection(client);
        }
    }
}
