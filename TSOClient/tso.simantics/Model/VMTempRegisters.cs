using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FSO.SimAntics.Model
{
    [InlineArray(20)]
    public struct VMTempRegisters
    {
        private short _element0;
        public static int Length => 20;

        public VMTempRegisters(Span<short> data)
        {
            data.CopyTo(AsSpan());
        }

        public Span<short> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref _element0, 20);
        }
    }

    [InlineArray(2)]
    public struct VMTempXLRegisters
    {
        private int _element0;
        public static int Length => 2;

        public VMTempXLRegisters(Span<int> data)
        {
            data.CopyTo(AsSpan());
        }

        public Span<int> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref _element0, 2);
        }
    }
}
