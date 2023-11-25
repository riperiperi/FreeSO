using FSO.SimAntics.Engine;

namespace FSO.SimAntics.JIT.Runtime
{
    public static class Op
    {
        public static T Read<T>(byte[] data) where T : VMPrimitiveOperand, new()
        {
            var result = new T();
            result.Read(data);
            return result;
        }
    }
}
