using System;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using FSO.Content;
using FSO.SimAntics;

namespace FSO_BrowserClient
{
    /// <summary>
    /// Boots the full TSO content system inside the browser: fetches the trimmed
    /// content.tar.gz (make_browser_content.py), extracts it into MEMFS, then runs
    /// the stock SERVER-mode Content.Init against it — the same content boot
    /// LotHostLite does natively, so every lockstep participant resolves the same
    /// GUID set from the same bytes.
    ///
    /// The fetch/extract is async (the "prefetch async, serve sync" rule); the
    /// Content.Init itself is synchronous and blocks the main thread for a few
    /// seconds — acceptable as a loading phase.
    /// </summary>
    public static class BrowserContentBoot
    {
        public static string Status { get; private set; } = "idle";
        public static bool Ready { get; private set; }
        public static bool Failed { get; private set; }
        public static bool Started { get; private set; }

        const string BundleRoot = "/bundle";

        public static async Task RunAsync(string contentTarUrl)
        {
            if (Started) return;
            Started = true;
            try
            {
                var sw = Stopwatch.StartNew();
                Status = "fetching content bundle";
                Console.WriteLine($"content boot: fetching {contentTarUrl}");
                using var http = new HttpClient();
                using var req = new HttpRequestMessage(HttpMethod.Get, contentTarUrl);
                // Without this, Blazor's fetch handler buffers the entire ~200MB
                // response before handing over a stream — double peak memory.
                req.Options.Set(new HttpRequestOptionsKey<bool>("WebAssemblyEnableStreamingResponse"), true);
                using var resp = await http.SendAsync(req,
                    HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(true);
                resp.EnsureSuccessStatusCode();
                using var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(true);
                using var gz = new GZipStream(stream, CompressionMode.Decompress);

                Status = "extracting to MEMFS";
                Directory.CreateDirectory(BundleRoot);
                var extracted = await ExtractUstarAsync(gz, BundleRoot).ConfigureAwait(true);
                Console.WriteLine($"content boot: extracted {extracted} entries in {sw.Elapsed.TotalSeconds:F1}s");

                Status = "Content.Init (SERVER)";
                // Content scanners resolve "Content/..." relative paths — same workdir
                // layout LotHostLite symlinks together natively.
                Directory.SetCurrentDirectory(BundleRoot + "/work");
                VM.UseWorld = false;
                VMContext.InitVMConfig(false);
                FSO.Content.Content.Init(BundleRoot + "/tso/", ContentMode.SERVER);
                Console.WriteLine($"Content.Init done ({sw.Elapsed.TotalSeconds:F1}s total)");

                Status = "content ready";
                Ready = true;
            }
            catch (Exception ex)
            {
                Failed = true;
                Status = "content boot failed: " + ex.Message;
                Console.WriteLine(Status);
                Console.WriteLine(ex.StackTrace);
                if (ex.InnerException != null)
                    Console.WriteLine("  inner: " + ex.InnerException.Message);
            }
        }

        /// <summary>
        /// Minimal USTAR reader — System.Formats.Tar is PlatformNotSupported on
        /// browser-wasm. make_browser_content.py emits strict USTAR (no PAX/GNU
        /// extensions, all names ≤ 100 chars), so 512-byte headers + octal size
        /// is the whole format.
        /// </summary>
        static async Task<int> ExtractUstarAsync(Stream src, string destRoot)
        {
            var header = new byte[512];
            var copyBuf = new byte[1 << 16];
            int entries = 0;
            while (true)
            {
                if (!await FillAsync(src, header, 512).ConfigureAwait(true)) break;
                if (header[0] == 0) break; // two zero blocks end the archive

                var name = ReadString(header, 0, 100);
                var prefix = ReadString(header, 345, 155);
                if (prefix.Length > 0) name = prefix + "/" + name;
                var size = Convert.ToInt64(ReadString(header, 124, 12).Trim(), 8);
                var type = (char)header[156];
                if (entries % 128 == 0) Status = $"downloading + extracting content ({entries}/~1540 files)";

                var dest = Path.Combine(destRoot, name);
                if (type == '5' || name.EndsWith("/"))
                {
                    Directory.CreateDirectory(dest);
                }
                else if (type == '0' || type == '\0')
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    using (var outFile = File.Create(dest))
                    {
                        long remaining = size;
                        while (remaining > 0)
                        {
                            var want = (int)Math.Min(remaining, copyBuf.Length);
                            var got = await src.ReadAsync(copyBuf, 0, want).ConfigureAwait(true);
                            if (got <= 0) throw new EndOfStreamException("tar truncated in " + name);
                            outFile.Write(copyBuf, 0, got);
                            remaining -= got;
                        }
                    }
                    var pad = (int)(512 - (size % 512)) % 512;
                    if (pad > 0) await FillAsync(src, header, pad).ConfigureAwait(true);
                }
                else
                {
                    throw new NotSupportedException($"tar entry type '{type}' ({name}) — bundle must be strict ustar");
                }
                entries++;
            }
            return entries;
        }

        static async Task<bool> FillAsync(Stream src, byte[] buf, int count)
        {
            int off = 0;
            while (off < count)
            {
                var got = await src.ReadAsync(buf, off, count - off).ConfigureAwait(true);
                if (got <= 0) return off > 0 ? throw new EndOfStreamException() : false;
                off += got;
            }
            return true;
        }

        static string ReadString(byte[] buf, int offset, int max)
        {
            int end = offset;
            while (end < offset + max && buf[end] != 0) end++;
            return System.Text.Encoding.ASCII.GetString(buf, offset, end - offset);
        }
    }
}
