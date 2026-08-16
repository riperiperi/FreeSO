using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using FSO.Content;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using Newtonsoft.Json.Linq;

namespace FSO_BrowserClient
{
    /// <summary>
    /// Fetches the compiled pack .iffs over HTTP (wwwroot/packs/manifest.json) and
    /// builds real <see cref="GameObject"/>s from them — no FSO.Content boot. Pack
    /// iffs are self-contained (OBJD+DGRP+SPR2+PALT inline), so the 2D DGRP render
    /// path resolves everything through the object's own IffFile.
    /// </summary>
    public class PackObjectLoader
    {
        public class Entry
        {
            public string Id;
            public uint Guid;
            public string Png;
            public GameObject Object;
        }

        readonly Dictionary<string, Entry> byId = new Dictionary<string, Entry>();
        readonly Dictionary<uint, Entry> byGuid = new Dictionary<uint, Entry>();
        public bool Ready { get; private set; }
        public int Count => byId.Count;
        bool loggedStack;

        public Entry Get(string id) => byId.TryGetValue(id, out var e) ? e : null;
        public Entry Get(uint guid) => byGuid.TryGetValue(guid, out var e) ? e : null;

        public Microsoft.Xna.Framework.Graphics.GraphicsDevice ProbeDevice;

        public async Task LoadAsync(string baseUrl)
        {
            // Without SimAntics loaded, GameObjectResource's BHAV recache would NRE on
            // the static assembler delegate (only VMContext.BindAssembler sets it).
            // ??= so a real binding from a later VM boot is never clobbered.
            GameObjectResource.BHAVAssembler ??= (bhav, res) => null;

            using var http = new HttpClient();
            var packsBase = new Uri(new Uri(baseUrl), "packs/");
            var manifest = JArray.Parse(await http.GetStringAsync(new Uri(packsBase, "manifest.json")));

            foreach (var o in manifest)
            {
                var id = (string)o["id"];
                var guid = Convert.ToUInt32((string)o["guid"], 16);
                // EA objects in the manifest carry no iff of their own — their
                // behaviour comes from objiff.far in the content bundle, and only
                // their billboard png is ours. Nothing to load here.
                if ((string)o["iff"] == null) continue;
                try
                {
                    var bytes = await http.GetByteArrayAsync(new Uri(packsBase, (string)o["iff"]));
                    var iff = new IffFile();
                    using (var ms = new MemoryStream(bytes)) iff.Read(ms);
                    iff.SetFilename((string)o["iff"]);

                    OBJD objd = null;
                    foreach (var cand in iff.List<OBJD>())
                    {
                        if (cand.GUID == guid) { objd = cand; break; }
                        objd = objd ?? cand;
                    }
                    if (objd == null)
                    {
                        Console.WriteLine($"pack {id}: no OBJD, skipped");
                        continue;
                    }

                    var resource = new GameObjectResource(iff, null, null, id, null);
                    var entry = new Entry
                    {
                        Id = id,
                        Guid = guid,
                        Png = (string)o["png"],
                        Object = new GameObject { GUID = objd.GUID, OBJ = objd, Resource = resource },
                    };
                    byId[id] = entry;
                    byGuid[guid] = entry;

                    if (byId.Count == 1)
                    {
                        // CPU-side decode probe: is the WASM SPR2 decoder producing pixels?
                        var dgrp = resource.Get<DGRP>(objd.BaseGraphicID);
                        DGRPImage img = null;
                        foreach (var i2 in dgrp?.Images ?? Array.Empty<DGRPImage>())
                            if (i2.Zoom == 3) { img = i2; break; }
                        var spr2 = img != null ? resource.Get<SPR2>((ushort)img.Sprites[0].SpriteID) : null;
                        var frame = spr2?.Frames[img.Sprites[0].SpriteFrameIndex];
                        frame?.DecodeIfRequired(false);
                        int vis = -1;
                        if (frame?.PixelData != null)
                        {
                            vis = 0;
                            foreach (var p in frame.PixelData) if (p.A > 0) vis++;
                        }
                        Console.WriteLine($"spr2probe {id}: frame {frame?.Width}x{frame?.Height} visiblePx={vis}");

                        if (ProbeDevice != null && frame != null)
                        {
                            // GPU round-trip: engine path (CachableTexture2D + 5-arg SetData)
                            var wt = frame.GetWorldTexture(ProbeDevice);
                            int gpuVis = -1;
                            if (wt.Pixel != null)
                            {
                                var buf = new Microsoft.Xna.Framework.Color[wt.Pixel.Width * wt.Pixel.Height];
                                wt.Pixel.GetData(buf);
                                gpuVis = 0;
                                foreach (var p in buf) if (p.A > 0) gpuVis++;
                            }
                            // Control: plain Texture2D + 1-arg SetData of the same pixels
                            int ctrlVis = -1;
                            if (frame.PixelData != null)
                            {
                                var t2 = new Microsoft.Xna.Framework.Graphics.Texture2D(ProbeDevice, frame.Width, frame.Height);
                                t2.SetData(frame.PixelData);
                                var buf2 = new Microsoft.Xna.Framework.Color[frame.Width * frame.Height];
                                t2.GetData(buf2);
                                ctrlVis = 0;
                                foreach (var p in buf2) if (p.A > 0) ctrlVis++;
                            }
                            // Variant: plain Texture2D + 5-arg SetData (the engine overload)
                            int fiveVis = -1;
                            if (frame.PixelData != null)
                            {
                                var t3 = new Microsoft.Xna.Framework.Graphics.Texture2D(ProbeDevice, frame.Width, frame.Height);
                                t3.SetData(0, null, frame.PixelData, 0, frame.PixelData.Length);
                                var buf3 = new Microsoft.Xna.Framework.Color[frame.Width * frame.Height];
                                t3.GetData(buf3);
                                fiveVis = 0;
                                foreach (var p in buf3) if (p.A > 0) fiveVis++;
                            }
                            Console.WriteLine($"gpuprobe {id}: engine={gpuVis} plain1arg={ctrlVis} plain5arg={fiveVis}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"pack {id}: load failed: {ex.Message}");
                    if (byId.Count == 0 && !loggedStack)
                    {
                        loggedStack = true;
                        Console.WriteLine(ex.ToString());
                    }
                }
            }

            Ready = true;
            Console.WriteLine($"pack objects ready: {byId.Count} GameObjects");
        }
    }
}
