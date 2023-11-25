namespace TargaImagePCL
{
    public struct Color
    {
        public static Color Empty = Color.FromArgb(0, 0, 0, 0);

        public byte R;
        public byte G;
        public byte B;
        public byte A;
        public static Color FromArgb(int a, int r, int g, int b)
        {
            return new Color()
                 {
                     R = (byte)r,
                     G = (byte)g,
                     B = (byte)b,
                     A = (byte)a
                 };
        }

        public static Color FromArgb(int r, int g, int b)
        {
            return new Color()
            {
                R = (byte)r,
                G = (byte)g,
                B = (byte)b,
                A = (byte)255
            };
        }
    }
}
