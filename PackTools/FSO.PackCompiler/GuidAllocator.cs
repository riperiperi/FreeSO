using System.Security.Cryptography;
using System.Text;

namespace FSO.PackCompiler
{
    /// <summary>
    /// Deterministic, registry-free GUID allocation for community-authored objects
    /// (MCP-DESIGN.md §6). Same (packId, objectId) always yields the same GUID on any
    /// machine, with no shared server and no counter to keep in sync across sessions —
    /// a content hash instead of a registry. Base-game GUIDs are well below this range;
    /// 0x7F000000-0x7FFFFFFF is reserved for community content (the range FSO.ModServer's
    /// placeholder counter already used). Two different (pack, object) ids landing on the
    /// same GUID is possible but rare (~17M slots) — the compiler's existing GUID-collision
    /// check (PackParser) is the backstop, same as it is for hand-picked GUIDs.
    /// </summary>
    public static class GuidAllocator
    {
        public const uint CommunityRangeStart = 0x7F000000;
        public const uint CommunityRangeEnd = 0x7FFFFFFF;

        public static uint Allocate(string packId, string objectId)
        {
            using var sha = SHA256.Create();
            var input = packId + "#" + objectId;
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Big-endian by hand, not BitConverter — keeps the result identical regardless
            // of host CPU endianness, since this must reproduce across machines.
            uint hash = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];

            uint span = CommunityRangeEnd - CommunityRangeStart;
            return CommunityRangeStart + (hash % span);
        }
    }
}
