using System;
using System.Collections.Generic;

namespace FSO.SimAntics.NetPlay.EODs.Handlers.Data
{
    public class VMEODFreshnessTracker
    {
        private const float FRESH_DECREMENT = 1f / (30 * 30); //about 30 seconds of the same thing to get from 1 freshness to .5
        private const float FRESH_PER_NEW_CMD = 0.15f;
        private float InternalFreshness = 0f;
        public float Freshness
        {
            get
            {
                return Math.Min(1, InternalFreshness);
            }
        }

        public float GoodScoreFreshness
        {
            get
            {
                return 0.75f + Freshness * 0.25f;
            }
        }

        public List<int>[] LastCommands;
        public VMEODFreshnessTracker(int categories)
        {
            LastCommands = new List<int>[categories];
            Reset();
        }

        public void Reset()
        {
            InternalFreshness = 0f;
            for (int i = 0; i < LastCommands.Length; i++)
            {
                LastCommands[i] = new List<int>() { -1, -1, -1, -1 };
            }
        }
        
        public void Tick()
        {
            InternalFreshness -= FRESH_DECREMENT * InternalFreshness;
            if (InternalFreshness < 0) InternalFreshness = 0;
        }

        public void SendCommand(int cmd, int cat)
        {
            var cmds = LastCommands[cat];
            var ind = cmds.IndexOf(cmd);
            if (ind != -1)
            {
                cmds.RemoveAt(ind);
                cmds.Add(cmd);
                InternalFreshness += FRESH_PER_NEW_CMD * ((3-ind) / 4f);
            } else
            {
                if (cmds.Count > 3) cmds.RemoveAt(0);
                InternalFreshness += FRESH_PER_NEW_CMD;
            }
            if (InternalFreshness > 1.1f) InternalFreshness = 1.1f;
        }

        public void SendCommand(int cmd)
        {
            SendCommand(cmd, 0);
        }
    }
}
