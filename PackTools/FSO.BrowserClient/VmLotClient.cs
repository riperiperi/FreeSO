using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FSO.Common.Utils;
using FSO.LotView;
using FSO.LotView.Model;
using FSO.SimAntics;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.NetPlay.Drivers;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;

namespace FSO_BrowserClient
{
    /// <summary>
    /// The browser's seat in the shared lockstep VM: BrowserSandboxClient (WS) ↔
    /// VMClientDriver ↔ a full local SimAntics VM, wired exactly like the native
    /// SmokeClient / desktop SandboxGameScreen external mode. UseWorld stays false
    /// (the KNI DGRP renderer anomaly is ledgered); rendering reads VM state
    /// directly — architecture copied into the LotView blueprint once synced,
    /// entities drawn as pack billboards / capsule sims each frame.
    /// </summary>
    public class VmLotClient
    {
        public VM vm;
        public bool Synced { get; private set; }
        public string Status { get; private set; } = "starting";
        public readonly List<string> ChatLog = new List<string>();
        public uint PersistID { get; }
        public string AvatarName { get; }

        BrowserSandboxClient cli;
        VMClientDriver driver;
        double tickAccum;
        long localTicks;
        long syncedAtTick = -1;
        bool autoChatSent;
        bool interactArmed, interactSeen;

        /// <summary>When set, sent as chat ~3s after sync — lets a concurrent native
        /// smoke client observe the browser without any UI interaction.</summary>
        public string AutoChat;

        /// <summary>Formatted chat lines for the DOM overlay.</summary>
        public event Action<string> OnChatLine;

        // Billboard resources (packs manifest: guid → png under objects/).
        readonly Dictionary<uint, Texture2D> texByGuid = new Dictionary<uint, Texture2D>();
        Texture2D simTex;
        bool texturesReady;

        public VmLotClient(string avatarName)
        {
            AvatarName = avatarName;
            PersistID = (uint)new Random().Next(1000, int.MaxValue);
        }

        public void Start(string gatewayBase)
        {
            var wsBase = gatewayBase.Replace("http://", "ws://").Replace("https://", "wss://").TrimEnd('/');
            var wsUrl = wsBase + "/sandbox";

            driver = new VMClientDriver((state, progress) =>
                Console.WriteLine($"vm net state {state} ({progress:F2})"));
            cli = new BrowserSandboxClient();
            driver.OnClientCommand += (msg) => cli.Write(new VMNetMessage(VMNetMessageType.Command, msg));
            driver.OnShutdown += (reason) => { Status = "shutdown: " + reason; cli.Disconnect(); };
            cli.OnMessage += driver.ServerMessage;
            cli.OnError += (err) => { Status = "ws error: " + err; Console.WriteLine("vm ws error: " + err); };

            cli.OnConnectComplete += () =>
            {
                Console.WriteLine($"vm connected, sending AvatarData persist={PersistID}");
                Status = "joined, syncing";
                var myState = new VMNetAvatarPersistState()
                {
                    Name = AvatarName,
                    DefaultSuits = new VMAvatarDefaultSuits(false),
                    BodyOutfit = 0x24C0000000D,
                    HeadOutfit = 0x000000000D,
                    PersistID = PersistID,
                    SkinTone = 0,
                    Gender = 1,
                    Permissions = VMTSOAvatarPermissions.Admin,
                    Budget = 1000000,
                };
                var dat = new MemoryStream();
                var str = new BinaryWriter(dat);
                myState.SerializeInto(str);
                cli.Write(new VMNetMessage(VMNetMessageType.AvatarData, dat.ToArray()));
                dat.Close();
            };

            vm = new VM(new VMContext(null), driver, new VMNullHeadlineProvider());
            vm.Init();
            vm.MyUID = PersistID;
            vm.OnChatEvent += (evt) =>
            {
                var text = evt.Text is string[] arr ? string.Join(" | ", arr) : evt.Text?.ToString();
                var line = $"chat[{evt.SenderUID}] {text}";
                ChatLog.Add(line);
                Console.WriteLine("vm " + line);
                // Sender name is resolved VM-side into the text for talk events;
                // show the raw line — good enough for the overlay.
                OnChatLine?.Invoke(text ?? "");
            };

            Console.WriteLine($"vm connecting {wsUrl}...");
            Status = "connecting " + wsUrl;
            cli.Connect(wsUrl);
        }

        /// <summary>Per-frame: drain the socket, tick the VM at 30Hz.</summary>
        public void Update(double elapsedSeconds)
        {
            if (vm == null) return;
            cli.Pump();

            tickAccum += elapsedSeconds;
            const double tickLen = 1.0 / 30.0;
            int guard = 0;
            while (tickAccum >= tickLen && guard++ < 10)
            {
                tickAccum -= tickLen;
                GameThread.UpdateExecuting = true;
                GameThread.DigestUpdate(new FSO.Common.Rendering.Framework.Model.UpdateState());
                vm.Tick();
                GameThread.UpdateExecuting = false;
                localTicks++;
            }
            if (guard >= 10) tickAccum = 0; // fell far behind (tab hidden); drop time

            if (!Synced && vm.Context.Architecture != null && vm.Entities.Count > 0)
            {
                Synced = true;
                syncedAtTick = localTicks;
                Status = $"vm ready: {vm.Entities.Count} entities";
                Console.WriteLine($"vm ready: SYNCED at local tick {localTicks}: {vm.Entities.Count} entities, " +
                    $"arch {vm.Context.Architecture.Width}x{vm.Context.Architecture.Height}, hash={EntityHash()}");
            }

            if (Synced && AutoChat != null && !autoChatSent && localTicks - syncedAtTick > 90)
            {
                autoChatSent = true;
                SendChat(AutoChat);
                Console.WriteLine("vm sent chat: " + AutoChat);
            }

            if (Synced && localTicks % 300 == 0)
                Console.WriteLine($"vm tick={localTicks} entities={vm.Entities.Count} hash={EntityHash()}");

            // After SendInteraction, report when it lands in our avatar's queue —
            // the "interaction executes in the VM" acceptance line.
            if (interactArmed && !interactSeen)
            {
                var act = MyAvatar?.Thread?.Queue?.FirstOrDefault(q => q.Name != null && q.Name != "Idle");
                if (act != null)
                {
                    interactSeen = true;
                    Console.WriteLine($"vm INTERACTION IN QUEUE at tick {localTicks}: {act.Name} " +
                        $"(mode {act.Mode}, priority {act.Priority})");
                }
            }
        }

        public void SendChat(string message)
        {
            vm?.SendCommand(new VMNetChatCmd { Message = message });
        }

        public VMAvatar MyAvatar =>
            vm?.Entities.OfType<VMAvatar>().FirstOrDefault(a => a.PersistID == PersistID);

        /// <summary>
        /// Real TTAB pie menu for the nearest in-world object to a tile — the same
        /// GetPieMenu (TestFunction check trees included) the desktop client runs.
        /// </summary>
        public (VMEntity target, List<VMPieMenuInteraction> pie) PieMenuAt(Vector2 tile)
        {
            var ava = MyAvatar;
            if (ava == null || !Synced) return (null, null);
            VMEntity best = null;
            float bestDist = 1.6f; // ~1.5 tile pick radius
            foreach (var ent in vm.Entities)
            {
                if (ent is VMAvatar) continue;
                if (ent.Position == LotTilePos.OUT_OF_WORLD) continue;
                var d = Vector2.Distance(new Vector2(ent.Position.x / 16f, ent.Position.y / 16f), tile);
                if (d < bestDist) { bestDist = d; best = ent; }
            }
            if (best == null) return (null, null);
            var pie = best.GetPieMenu(vm, ava, false, true);
            return (best, pie);
        }

        public void SendInteraction(short calleeID, byte interactionID)
        {
            vm?.SendCommand(new VMNetInteractionCmd
            {
                Interaction = interactionID,
                CalleeID = calleeID,
                Global = false,
            });
            interactArmed = true;
            interactSeen = false;
            Console.WriteLine($"vm sent interaction {interactionID} on object {calleeID}");
        }

        /// <summary>Same per-entity hash LotHostLite logs, for cross-runtime comparison.</summary>
        public long EntityHash()
        {
            long h = 17;
            foreach (var ent in vm.Entities)
            {
                unchecked
                {
                    h = h * 31 + ent.ObjectID;
                    h = h * 31 + (long)(ent.Object?.OBJ?.GUID ?? 0);
                    h = h * 31 + ent.Position.x;
                    h = h * 31 + ent.Position.y;
                    h = h * 31 + ent.Position.Level;
                }
            }
            return h;
        }

        /// <summary>
        /// Copy the synced VM architecture into the render blueprint (same
        /// WallTile/FloorTile structs) — the VM-fed equivalent of
        /// BlueprintArchLoader.Load. The blueprint must already be sized
        /// Architecture.Width × Architecture.Height.
        /// </summary>
        public void ApplyArchitecture(Blueprint bp)
        {
            var arch = vm.Context.Architecture;
            var w = bp.Width;

            for (int i = 0; i < bp.RoomMap.Length; i++)
                bp.RoomMap[i] = bp.RoomMap[i] ?? new uint[bp.Width * bp.Height];
            if (bp.Rooms == null || bp.Rooms.Count == 0)
                bp.Rooms = new List<Room> { new Room { IsOutside = true } };

            var stories = Math.Min(bp.Stories, arch.Stories);
            for (int level = 0; level < stories; level++)
            {
                Array.Copy(arch.Floors[level], bp.Floors[level],
                    Math.Min(arch.Floors[level].Length, bp.Floors[level].Length));
                var walls = arch.Walls[level];
                var n = Math.Min(walls.Length, bp.Walls[level].Length);
                for (int off = 0; off < n; off++)
                {
                    bp.Walls[level][off] = walls[off];
                    if (walls[off].Segments != 0) bp.WallsAt[level].Add((ushort)off);
                }
            }

            bp.SignalFloorChange();
            bp.SignalRoomChange();
            bp.SignalWallChange();
            Console.WriteLine($"vm arch applied: {bp.WallsAt[0].Count} wall tiles on ground floor");
        }

        /// <summary>
        /// Fetch pack billboard textures (packs/manifest.json guid → objects/png).
        /// Progressive: capsules + any already-fetched furniture draw immediately;
        /// fetches run concurrently (a sequential loop starves for minutes when two
        /// SwiftShader tabs share the CPU).
        /// </summary>
        public async Task LoadBillboardsAsync(GraphicsDevice gd, string baseUrl)
        {
            try
            {
                simTex = MakeSimTexture(gd);
                texturesReady = true;
                using var http = new HttpClient();
                Console.WriteLine("vm billboards: fetching manifest");
                var manifest = JArray.Parse(await http.GetStringAsync(
                    new Uri(new Uri(baseUrl), "packs/manifest.json")).ConfigureAwait(true));
                var tasks = new List<Task>();
                foreach (var o in manifest)
                {
                    var png = (string)o["png"];
                    var guidStr = (string)o["guid"];
                    if (png == null || guidStr == null) continue;
                    var guid = Convert.ToUInt32(guidStr.Replace("0x", ""), 16);
                    tasks.Add(FetchTextureAsync(http, gd, baseUrl, png, guid));
                }
                await Task.WhenAll(tasks).ConfigureAwait(true);
                Console.WriteLine($"vm billboards ready: {texByGuid.Count} pack textures");
            }
            catch (Exception ex)
            {
                Console.WriteLine("vm billboards failed: " + ex.Message);
            }
        }

        async Task FetchTextureAsync(HttpClient http, GraphicsDevice gd, string baseUrl, string png, uint guid)
        {
            try
            {
                var bytes = await http.GetByteArrayAsync(
                    new Uri(new Uri(baseUrl), "objects/" + png)).ConfigureAwait(true);
                using var ms = new MemoryStream(bytes);
                texByGuid[guid] = Texture2D.FromStream(gd, ms);
            }
            catch { /* individual sprite missing is non-fatal */ }
        }

        static Texture2D MakeSimTexture(GraphicsDevice gd)
        {
            const int w = 24, h = 52;
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var dx = (x - w / 2f) / (w / 2f);
                    float dy;
                    if (y < w / 2) dy = (y - w / 2f) / (w / 2f);
                    else if (y > h - w / 2) dy = (y - (h - w / 2f)) / (w / 2f);
                    else dy = 0;
                    px[y * w + x] = (dx * dx + dy * dy <= 1) ? Color.White : Color.Transparent;
                }
            }
            var tex = new Texture2D(gd, w, h);
            tex.SetData(px);
            return tex;
        }

        /// <summary>
        /// Live entity billboards: pack objects by GUID texture, avatars as tinted
        /// capsules — positions straight out of the shared VM every frame.
        /// </summary>
        public void DrawEntities(SpriteBatch sb, WorldState state)
        {
            if (!texturesReady || vm == null) return;
            var space = state.WorldSpace;
            var offset = space.GetPointScreenOffset();
            var scale = space.TilePxWidthHalf / 64f;

            var draws = new List<(float sortKey, Texture2D tex, Vector2 tile, bool isSim, Color tint)>();
            foreach (var ent in vm.Entities)
            {
                if (ent.Position == LotTilePos.OUT_OF_WORLD) continue;
                var tile = new Vector2(ent.Position.x / 16f, ent.Position.y / 16f);
                if (ent is VMAvatar ava)
                {
                    var tint = TintFor(ava.PersistID);
                    draws.Add((tile.X + tile.Y + 0.01f, simTex, tile, true, tint));
                }
                else
                {
                    // one billboard per multitile group, at the lead part
                    if (ent.MultitileGroup != null && ent.MultitileGroup.Objects.Count > 0
                        && ent.MultitileGroup.Objects[0] != ent) continue;
                    var guid = ent.Object?.OBJ?.GUID ?? 0;
                    if (!texByGuid.TryGetValue(guid, out var tex)) continue;
                    draws.Add((tile.X + tile.Y, tex, tile, false, Color.White));
                }
            }

            foreach (var d in draws.OrderBy(d => d.sortKey))
            {
                var screen = space.GetScreenFromTile(d.tile) + offset;
                var drawScale = d.isSim ? 1f : scale;
                var w = d.tex.Width * drawScale;
                var h = d.tex.Height * drawScale;
                var pos = new Vector2(screen.X - w / 2,
                    screen.Y - h + space.TilePxHeightHalf * (d.isSim ? 0.5f : 1f));
                sb.Draw(d.tex, pos, null, d.tint, 0f, Vector2.Zero, drawScale, SpriteEffects.None, 0f);
            }
        }

        static Color TintFor(uint persistID)
        {
            // stable, readable tint per avatar
            var hue = (persistID * 2654435761u) % 360u;
            return HsvToRgb(hue, 0.55f, 0.95f);
        }

        static Color HsvToRgb(float h, float s, float v)
        {
            var c = v * s;
            var x = c * (1 - Math.Abs((h / 60f) % 2 - 1));
            var m = v - c;
            float r, g, b;
            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }
            return new Color(r + m, g + m, b + m);
        }
    }
}
