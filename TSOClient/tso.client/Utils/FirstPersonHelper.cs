using FSO.Common;
using FSO.SimAntics;

namespace FSO.Client.Utils
{
    internal static class FirstPersonHelper
    {
        public static float GetTuning(VM vm)
        {
            return vm?.Tuning?.GetTuning("aprilfools", 0, 2023) ?? 0;
        }

        public static bool IsEnabled(VM vm)
        {
            return FSOEnvironment.Enable3D && GetTuning(vm) > 0;
        }
    }
}
