namespace FSO.LotView.Components.Model
{
    public readonly struct FloorTileIndices
    {
        [System.Runtime.CompilerServices.InlineArray(6)]
        public struct Buffer6<T>
        {
            private T _element0;
        }

        public readonly int Length;
        public readonly Buffer6<int> Data;

        public FloorTileIndices(int i1, int i2, int i3)
        {
            Length = 3;
            Data[0] = i1;
            Data[1] = i2;
            Data[2] = i3;
        }

        public FloorTileIndices(int i1, int i2, int i3, int i4, int i5, int i6)
        {
            Length = 6;
            Data[0] = i1;
            Data[1] = i2;
            Data[2] = i3;
            Data[3] = i4;
            Data[4] = i5;
            Data[5] = i6;
        }
    }

    public static class FloorTileIndicesExtensions
    {
        public static ReadOnlySpan<int> GetSpan(this ref FloorTileIndices indices)
        {
            if (indices.Length < 6)
            {
                return indices.Data[..indices.Length];
            }

            return indices.Data;
        }
    }
}
