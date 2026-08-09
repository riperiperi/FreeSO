using System;

namespace FSO.AgentBridge
{
    /// <summary>
    /// Hard ceiling on a single build. Exists because a runaway loop costs real money — one
    /// 32-turn run burned ~$5 and produced nothing — and "be careful" is not a control.
    /// A run that hits the cap stops and says so, rather than continuing.
    /// </summary>
    public class RunLimits
    {
        /// <summary>The one successful build took ~15 turns; 25 leaves room for
        /// self-correction without letting a loop run indefinitely.</summary>
        public int MaxTurns = 25;

        public static RunLimits FromEnvironment()
        {
            var limits = new RunLimits();
            if (int.TryParse(Environment.GetEnvironmentVariable("FSO_MAX_TURNS"), out var turns) && turns > 0)
                limits.MaxTurns = turns;
            return limits;
        }

        public override string ToString() => $"max {MaxTurns} turns";
    }
}
