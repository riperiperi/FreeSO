using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.RegularExpressions;

namespace FSO.Server.Api.Core.Controllers
{
    /// <summary>
    /// Serves the FreeSO emoji set sourced from the Discord bot's mirror.
    ///
    /// Layout under <c>{NFSdir}/Emojis/</c> (the bot writes files here; the API
    /// only reads):
    ///
    ///   atlas.png        — pre-built bulk atlas of the standard (unicode) set
    ///   atlas.json       — { version, atlas_grid, cell_size, cells: { codepoint: cell_index } }
    ///   customs.json     — { version, emojis: [{ name, animated, checksum }] }
    ///   custom/{name}.png — one PNG per Discord guild custom emoji
    ///
    /// All GETs go through <see cref="Utils.ApiResponse.FileWithCache"/> so
    /// browsers/CDNs can issue conditional requests and we return 304s when
    /// nothing changed. The client polls atlas.json's ETag on startup; a 304
    /// means the cached PNG is still good.
    /// </summary>
    [EnableCors]
    [ApiController]
    public class EmojiController : ControllerBase
    {
        // Custom emoji names are URL-segment substituted; constrain hard to defeat
        // path traversal (../../etc/passwd) and weird filesystem characters.
        // Discord custom emoji names are validated to [a-zA-Z0-9_] anyway.
        private static readonly Regex SafeName = new Regex(@"^[a-zA-Z0-9_\-]{1,64}$", RegexOptions.Compiled);

        private static string EmojiRoot()
        {
            var nfs = Api.INSTANCE?.Config?.NFSdir;
            return string.IsNullOrEmpty(nfs) ? null : Path.Combine(nfs, "Emojis");
        }

        // GET /userapi/emoji/atlas.png — the bulk standard atlas. Single round-trip
        // primes the entire unicode emoji set on the client.
        [HttpGet]
        [Route("userapi/emoji/atlas.png")]
        public IActionResult GetStandardAtlas()
        {
            var root = EmojiRoot();
            if (root == null) return NotFound();
            return Utils.ApiResponse.FileWithCache(Request, Path.Combine(root, "atlas.png"))
                ?? (IActionResult)NotFound();
        }

        // GET /userapi/emoji/atlas.json — index sidecar for atlas.png.
        [HttpGet]
        [Route("userapi/emoji/atlas.json")]
        public IActionResult GetStandardAtlasIndex()
        {
            var root = EmojiRoot();
            if (root == null) return NotFound();
            return Utils.ApiResponse.FileWithCache(Request, Path.Combine(root, "atlas.json"), "application/json")
                ?? (IActionResult)NotFound();
        }

        // GET /userapi/emoji/customs.json — manifest of every Discord guild custom
        // emoji currently mirrored. The client uses this to know what custom names
        // exist (and which to lazy-fetch from /custom/{name}.png on first use).
        [HttpGet]
        [Route("userapi/emoji/customs.json")]
        public IActionResult GetCustomsManifest()
        {
            var root = EmojiRoot();
            if (root == null) return NotFound();
            return Utils.ApiResponse.FileWithCache(Request, Path.Combine(root, "customs.json"), "application/json")
                ?? (IActionResult)NotFound();
        }

        // GET /userapi/emoji/custom/{name}.png — single Discord guild custom emoji.
        // Lazy-fetched by the client when a `:name:` token resolves to a custom.
        [HttpGet]
        [Route("userapi/emoji/custom/{name}.png")]
        public IActionResult GetCustomEmoji(string name)
        {
            if (string.IsNullOrEmpty(name) || !SafeName.IsMatch(name))
                return NotFound();
            var root = EmojiRoot();
            if (root == null) return NotFound();

            // Path.Combine is safe here because name is regex-validated to a single
            // alphanumeric+underscore+dash segment — no separators, no traversal.
            return Utils.ApiResponse.FileWithCache(Request, Path.Combine(root, "custom", name + ".png"))
                ?? (IActionResult)NotFound();
        }
    }
}