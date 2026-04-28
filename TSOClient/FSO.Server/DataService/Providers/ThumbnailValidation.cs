using FSO.Common.Serialization.Primitives;
using System.Security;

namespace FSO.Server.DataService.Providers
{
    internal static class ThumbnailValidation
    {
        // 1 MB — generous for a thumbnail, still tight enough to prevent disk exhaustion
        private const int MaxBytes = 1024 * 1024;

        private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        /// <summary>
        /// Validates that <paramref name="value"/> is a non-empty cTSOGenericData blob
        /// whose content is a valid PNG file within the allowed size limit.
        /// Throws <see cref="SecurityException"/> on any violation.
        /// </summary>
        public static void Validate(object value)
        {
            var data = value as cTSOGenericData;
            if (data?.Data == null)
                throw new SecurityException("Thumbnail: expected cTSOGenericData");

            if (data.Data.Length > MaxBytes)
                throw new SecurityException("Thumbnail exceeds maximum size (1 MB)");

            if (data.Data.Length < PngMagic.Length)
                throw new SecurityException("Thumbnail must be a valid PNG file");

            for (int i = 0; i < PngMagic.Length; i++)
            {
                if (data.Data[i] != PngMagic[i])
                    throw new SecurityException("Thumbnail must be a valid PNG file");
            }
        }
    }
}