using System.Globalization;

namespace MSDFExtension
{
    public static class FloatHelper
    {
        public static float ParseInvariant(string value) => float.Parse(value, CultureInfo.InvariantCulture);
    }
}
