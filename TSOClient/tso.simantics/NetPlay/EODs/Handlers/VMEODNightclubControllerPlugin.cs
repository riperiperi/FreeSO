using FSO.SimAntics.NetPlay.EODs.Model;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODNightclubControllerPlugin : VMEODHandler
    {
        public VMEODClient ControllerClient;

        // design explanation

        // freshness (0-1.1) starts at zero, then increments each time a distinct pattern is used.
        // a local history of 4 is kept to determine how much this increment is.
        // the last pattern used gives 0, linearly scaling up to FRESHNESS_INC for the 4th most recent pattern.
        // freshness decreases over time. This decrease is proportional to the current freshness,
        // and should only allow the user to hit max freshness when cycling more then 3 patterns. 
        // (more than 2 should still be good)
        // note that when freshness is accessed, it is clamped to [0-1]. The internal 1.1 max limit
        // is so that freshness does not immediately decrease and prevent the user from getting a 100%

        // goodScoreFreshModifier = 0.75 + 0.25*freshModifier;
        // we don't want a lack of freshness to remove all of a player's "correct answer" score, so only
        // 75% of scores of that kind should be a given. 0 freshness is hard to reach, so bad players would
        // likely have freshness hover above half, resulting in this score being 90% of its full potential.

        // Dance floor has 3 active patterns at a time, ranging from high to low bonus rating. 
        // All other dances are "mediocre" and only really good for avoiding the repeat dance penalty.
        // These change every minute and a half, and are called "pattern round"s. 
        // The people on the dance floor will have the winning
        // dances pushed onto them as a hint, though the proportion doing the best dance will be lower
        // than the other two, so you have to spot it out. Swapping between the best 3 dances will keep
        // your score for that dance slightly above 100%.

        // (dance freshness increase and successful dances only count when their interaction ends. 
        // spamming the button then cancelling interactions will do nothing.)

        // dance score is calculated as a weighted average, which increases with intensity as the rounds go on.
        // we want a little leeway for mistakes here, since the user can't get 100% til the end, and their screwups
        // in past "pattern rounds" will come back to haunt them.
        // therefore the max score is actually 120%.
        // danceScore[i] = (25 * freshModifier) + (sum(targetDanceWeights[] * danceFound[]) / sum(targetDanceWeights[])) * 95 * goodScoreFreshModifier;
        // score = (roundTime / 6 minutes) * (avg(danceScores))

        // DJ is more complicated. There is one winning pattern that players should vary. 
        // for example, this could be A12, D42, C32, C31 (internally 001, 331, 221, 220)
        // for each category, you can calulate an index for each sample from the digits. index = code[0]*12 + code[1]*4 + code[2];
        // a "closer pattern" gets a higher score. Note that the weighting is abs(targIndex - index) squared, to punish distance from the correct pattern more.
        // you can ONLY get perfect rating by finding the correct pattern and then keeping up variation
        // 100 is hit, the bonus flag is set and you're good.

        // (DJ score, hint and freshness checks are periodic. This is to stop the DJ from spamming random patterns and seeing what gets an instant response)

        // score = 80*closeness*goodScoreFreshModifier + (roundTime / 6 minutes) * 20

        //other plugins in the lot
        public bool Connected;
        public VMEODNCDanceFloorPlugin DanceFloor;
        public List<VMEODDancePlatformPlugin> DancePlatforms
        {
            get
            {
                return Server.vm.EODHost.GetHandlers<VMEODDancePlatformPlugin>().ToList();
            }
        }
        public List<VMEODDJStationPlugin> DJPlatforms
        {
            get
            {
                return Server.vm.EODHost.GetHandlers<VMEODDJStationPlugin>().ToList();
            }
        }
        public List<VMAvatar> Dancers;

        public int[] CurrentWinningDJPattern = { 0, 0, 0, 0 }; //in index format. s[0]*16 + s[1] * 4 + s[2]
        public int[] CurrentWinningDancePatterns = { 0, 0, 0 }; //best, second best, third best pattern

        public int DancePatternNum = 0;
        public int[] PatternRatingScale = { 55, 25, 20 }; //amount of rating the pattern will get us (for this section)
        public int[] PatternDancerChance = { 20, 35, 45 }; //chance that a dancer will do this move

        public bool RoundActive
        {
            get { return RoundTicks > -1; }
        }
        public int RoundTicks = -1;
        public int DancerIndex = 0;

        public VMEODNightclubControllerPlugin(VMEODServer server) : base(server)
        {
            SimanticsHandlers[(short)VMEODNightclubEventTypes.RoundStart] = RoundStartHandler;
            SimanticsHandlers[(short)VMEODNightclubEventTypes.RoundEnd] = RoundEndHandler;

            HidePortals();
        }

        public void HidePortals()
        {
            var portals = Server.vm.Entities.Where(x => x.Object.Resource.Name == "oj-nc-portals").Select(x => x.ObjectID).ToArray();
            var states = portals.Select(x => (byte)255).ToArray();

            Server.vm.SendCommand(new VMNetBatchGraphicCmd()
            {
                Objects = portals,
                Graphics = states
            });
        }

        public override void Tick()
        {
            base.Tick();

            if (!Connected)
            {
                //need to connect to child plugins.
                DanceFloor = Server.vm.EODHost.GetFirstHandler<VMEODNCDanceFloorPlugin>();
                if (DanceFloor != null)
                {
                    Connected = true;
                    /*DancePlatforms = Server.vm.EODHost.GetHandlers<VMEODDancePlatformPlugin>().ToList();
                    if (DancePlatforms.Count == 4)
                    {
                        DJPlatforms = Server.vm.EODHost.GetHandlers<VMEODDJStationPlugin>().ToList();
                        if (DJPlatforms.Count == 4)
                        {
                            Connected = true;
                        }
                    }*/
                }
            } else
            {
                //game behaviour.
                if (RoundActive)
                {
                    var danceCycle = RoundTicks % (30 * 60);
                    if (danceCycle == 0)
                    {
                        //set up a new random dance pattern
                        var rand = new Random();
                        for (int i = 0; i < 3; i++)
                        {
                            var rd = rand.Next(24);
                            while (CurrentWinningDancePatterns.Any(x => x == rd)) rd = rand.Next(24);
                            CurrentWinningDancePatterns[i] = rd;
                            //the winning pattern will always be new. There is a chance 2 and 3 may be repeats.
                        }
                        if (RoundTicks != 0) DancePatternNum++;
                    }

                    if (RoundTicks % (5+Math.Min(danceCycle/30, 25)) == 0)
                    {
                        //give a dancer a new dance
                        var rand = new Random();
                        var pct = rand.Next(100);
                        int i;
                        for (i=0; i<3; i++)
                        {
                            if (pct < PatternDancerChance[i]) break;
                            pct -= PatternDancerChance[i];
                        }
                        var pattern = CurrentWinningDancePatterns[i];
                        var dancer = Dancers[DancerIndex++];
                        //Console.WriteLine("hint given..");
                        Server.vm.ForwardCommand(new VMNetInteractionCmd()
                        {
                            CalleeID = dancer.ObjectID,
                            CallerID = dancer.ObjectID,
                            Global = false,
                            Interaction = (ushort)(pattern + 4)
                        });
                        if (DancerIndex >= Dancers.Count) DancerIndex = 0;
                    }

                    RoundTicks++;
                }
            }
        }

        public float RoundPercentage()
        {
            return RoundTicks / (30 * 5 * 60f);
        }

        private void RoundStartHandler(short evt, VMEODClient client)
        {
            //init dance mode
            //random dancer pattern is set on tick
            DancePatternNum = 0;
            RoundTicks = 0;

            //set the winning dj pattern
            var rand = new Random();
            for (int i=0; i<4; i++)
            {
                CurrentWinningDJPattern[i] = rand.Next(64);
            }

            //reset all plugins
            foreach (var dj in DJPlatforms) dj.Reset();
            foreach (var dancer in DancePlatforms) dancer.Reset();

            //let's identify dancer avatars to push interactions onto.
            Dancers = new List<VMAvatar>();
            foreach (var avatar in Server.vm.Context.ObjectQueries.Avatars)
            {
                if (avatar.Object.Resource.Name == "oj-nc-npc-dancer")
                {
                    Dancers.Insert(rand.Next(Dancers.Count+1), (VMAvatar)avatar);
                }
            }
        }

        private void RoundEndHandler(short evt, VMEODClient client)
        {
            RoundTicks = -1;
        }

        public override void OnConnection(VMEODClient client)
        {
            if (client.Avatar == null)
            {
                //we're the controller!
                ControllerClient = client;
            }
        }
    }

    public enum VMEODNightclubEventTypes : short
    {
        RoundStart = 2,
        RoundEnd = 3
    }
}
