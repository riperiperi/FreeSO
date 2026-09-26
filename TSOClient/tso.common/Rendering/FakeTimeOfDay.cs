using System.Diagnostics;

namespace FSO.Common.Rendering
{
    public static class FakeTimeOfDay
    {
        private static float SpeedChangePerSecond = 0.2f;

        private static long LastTimestamp;
        private static float LastTime;
        private static float Speed; // Time of day speed in days per second
        private static float TargetSpeed;

        public static void TickAnimation(float secondsSince)
        {
            if (!IsActive())
            {
                return;
            }
            
            var now = Stopwatch.GetTimestamp();
            LastTimestamp = now;

            if (Speed != TargetSpeed)
            {
                Speed += (Speed < TargetSpeed ?
                    Math.Min(TargetSpeed - Speed, SpeedChangePerSecond * secondsSince) :
                    Math.Max(TargetSpeed - Speed, -SpeedChangePerSecond * secondsSince));
            }

            var prevTime = LastTime;

            LastTime += Speed * secondsSince; //?
            LastTime %= 1f;

            if (prevTime < 0.5f && LastTime >= 0.5f)
            {
                // Clear fake finale flag for the next night cycles.
                FinaleUtils.SetFakeFinale(false);
            }
        }

        public static void SetFakeTimeOfDaySpeedTarget(float now, float speed)
        {
            if (LastTimestamp == 0)
            {
                Speed = speed;
            }

            LastTime = now;
            LastTimestamp = Stopwatch.GetTimestamp();

            TargetSpeed = speed;
        }

        public static void Clear()
        {
            LastTimestamp = 0;
        }

        public static bool IsActive()
        {
            return LastTimestamp != 0;
        }

        public static float GetTime()
        {
            return LastTime;
        }

        public static float GetSpeed()
        {
            return Speed;
        }
    }
}
