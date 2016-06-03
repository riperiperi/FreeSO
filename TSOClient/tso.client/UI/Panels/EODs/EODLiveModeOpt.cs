using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.EODs
{
    public class EODLiveModeOpt
    {
        public EODHeight Height;
        public EODLength Length;
        public EODTextTips Tips;
        public EODTimer Timer;
        public byte Buttons; //0,1,2. graphics for 3 are present but currently unused.
        public bool Expandable; //enables "double panel" mode. can only be used with tall EOD.
    }

    public enum EODHeight
    {
        Normal,
        Tall
    }

    public enum EODLength
    {
        Short,
        Medium,
        Full
    }

    public enum EODTextTips
    {
        None,
        Short, //has straight variation for short EOD, onlinejobs?
        Long,
    }

    public enum EODTimer
    {
        None,
        Normal,
        Straight //used for OnlineJobs. technically not an EOD??
    }
}
