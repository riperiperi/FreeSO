using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FSO.SimAntics.Model
{
    /// <summary>
    /// Arguments for a SimAntics stack frame.
    /// These typically have 4 elements, but can use more if required.
    /// </summary>
    public struct VMArguments
    {
        // If this is zero, default to 4.
        private readonly int LengthPlusOne;
        private short Arg1;
#pragma warning disable IDE0044 // Add readonly modifier
        private short Arg2;
        private short Arg3;
        private short Arg4;
#pragma warning restore IDE0044 // Add readonly modifier

        public readonly int Length => LengthPlusOne == 0 ? 4 : (LengthPlusOne - 1);

        private readonly short[] ExtraArgs;
        private Span<short> BaseArgs => MemoryMarshal.CreateSpan(ref Arg1, 4);

        private static void ThrowIndexOutOfRangeException()
        {
            throw new IndexOutOfRangeException();
        }

        public short this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (index >= Length)
                {
                    ThrowIndexOutOfRangeException();
                }

                if (index < 4)
                {
                    return BaseArgs[index];
                }

                return ExtraArgs[index - 4];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (index >= Length)
                {
                    ThrowIndexOutOfRangeException();
                }

                if (index < 4)
                {
                    BaseArgs[index] = value;
                }
                else
                {
                    ExtraArgs[index - 4] = value;
                }
            }
        }

        public VMArguments(ReadOnlySpan<short> args)
        {
            LengthPlusOne = args.Length + 1;

            args[..Math.Min(4, args.Length)].CopyTo(BaseArgs);

            if (args.Length > 4)
            {
                // Extras go on a heap array
                ExtraArgs = new short[args.Length - 4];

                args[4..].CopyTo(ExtraArgs);
            }
        }

        public VMArguments(int size)
        {
            LengthPlusOne = size + 1;

            if (size > 4)
            {
                ExtraArgs = new short[size - 4];
            }
        }

        public short[] Clone()
        {
            if (ExtraArgs != null)
            {
                short[] array = [.. BaseArgs, .. ExtraArgs];
                return array;
            }

            return [ ..BaseArgs[..Length] ];
        }

        public Span<short> ToSpan()
        {
            if (ExtraArgs != null)
            {
                short[] array = [.. BaseArgs, .. ExtraArgs];
                return array;
            }

            return BaseArgs[..Length];
        }
    }
}
