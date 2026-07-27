using FSO.Common;

namespace FSO.SimAntics.Utils
{
    public static class FirstPersonHelper
    {
        public static float GetTuning(VM vm)
        {
            return 1;
            //return vm?.Tuning?.GetTuning("aprilfools", 0, 2023) ?? 0;
        }

        public static bool IsEnabled(VM vm)
        {
            return FSOEnvironment.Enable3D && GetTuning(vm) > 0;
        }
    }
}
