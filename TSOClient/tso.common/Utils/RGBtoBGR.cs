using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace FSO.Common.Utils
{
    public static class RGBtoBGR
    {
        private const ulong MaskR = 0x000000FF000000FF;
        private const ulong MaskB = 0x00FF000000FF0000;
        private const ulong MaskElse = 0xFF00FF00FF00FF00;

        private const uint UintMaskR = 0x000000FF;
        private const uint UintMaskB = 0x00FF0000;
        private const uint UintMaskElse = 0xFF00FF00;

        private static readonly Vector256<byte> BgrIndices256 = Vector256.Create<byte>([
            2, 1, 0, 3,
            6, 5, 4, 7,
            10, 9, 8, 11,
            14, 13, 12, 15,
            18, 17, 16, 19,
            22, 21, 20, 23,
            26, 25, 24, 27,
            30, 29, 28, 31,
        ]);

        private static readonly Vector128<byte> BgrIndices128 = Vector128.Create<byte>([
            2, 1, 0, 3,
            6, 5, 4, 7,
            10, 9, 8, 11,
            14, 13, 12, 15
        ]);

        private static readonly Vector64<byte> BgrIndices64_1 = Vector64.Create<byte>([
            2, 1, 0, 3,
            6, 5, 4, 7,
        ]);

        private static readonly Vector64<byte> BgrIndices64_2 = Vector64.Create<byte>([
            10, 9, 8, 11,
            14, 13, 12, 15
        ]);

        public static void Convert(Span<byte> data)
        {
            if (Avx2.IsSupported)
            {
                var v256 = MemoryMarshal.Cast<byte, Vector256<byte>>(data);
                var bgrIndices = BgrIndices256;

                for (int i = 0; i < v256.Length; i++)
                {
                    v256[i] = Avx2.Shuffle(v256[i], bgrIndices);
                }

                data = data[(v256.Length * 32)..];
            }
            else if (Ssse3.IsSupported || AdvSimd.IsSupported)
            {
                var v128 = MemoryMarshal.Cast<byte, Vector128<byte>>(data);

                if (Ssse3.IsSupported)
                {
                    var bgrIndices = BgrIndices128;
                    for (int i = 0; i < v128.Length; i++)
                    {
                        v128[i] = Ssse3.Shuffle(v128[i], bgrIndices);
                    }
                }
                else if (AdvSimd.IsSupported)
                {
                    var v64 = MemoryMarshal.Cast<Vector128<byte>, Vector64<byte>>(v128);
                    var bgrIndices1 = BgrIndices64_1;
                    var bgrIndices2 = BgrIndices64_2;

                    int i64 = 0;
                    for (int i = 0; i < v128.Length; i++)
                    {
                        var src = v128[i];
                        v64[i64++] = AdvSimd.VectorTableLookup(src, bgrIndices1);
                        v64[i64++] = AdvSimd.VectorTableLookup(src, bgrIndices2);
                    }
                }

                data = data[(v128.Length * 16)..];
            }
            else
            {
                // 8 Bytes is as long as we can get without hardware accelleration.
                var longData = MemoryMarshal.Cast<byte, ulong>(data);

                for (int i = 0; i < longData.Length; i++)
                {
                    ulong px = longData[i];
                    longData[i] = ((px >> 16) & MaskR) | ((px << 16) & MaskB) | (px & MaskElse);
                }

                data = data[(longData.Length * 8)..];
            }

            if (data.Length > 0)
            {
                var uintData = MemoryMarshal.Cast<byte, uint>(data);

                for (int i = 0; i < uintData.Length; i++)
                {
                    uint px = uintData[i];
                    uintData[i] = ((px >> 16) & UintMaskR) | ((px << 16) & UintMaskB) | (px & UintMaskElse);
                }
            }
        }
    }
}
