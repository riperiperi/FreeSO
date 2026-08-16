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
using FSO.Vitaboy;
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
        uint lastSyncTickLogged = uint.MaxValue; // dedupe: CurrentSyncTick can repeat within a catch-up burst
        long syncedAtTick = -1;
        bool autoChatSent;
        bool interactArmed, interactSeen;
        string wsUrl;
        bool wsConnected;
        bool wsErrored;
        int wsRetryTicks;
        bool walkInSent;
        Texture2D markerTex;
        List<(short X, short Y)> walkCandidates;
        (short X, short Y)? walkTarget;
        Vector2? houseCentre;
        long lastWalkTry = -1000;

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
            wsUrl = wsBase + "/sandbox";

            driver = new VMClientDriver((state, progress) =>
                Console.WriteLine($"vm net state {state} ({progress:F2})"));

            // The VM lives across connection retries; only the socket is remade.
            vm = new VM(new VMContext(null), driver, new VMNullHeadlineProvider());
            vm.Init();
            vm.MyUID = PersistID;
            // Diagnostic only: VMThread's per-tick exception handler already catches
            // SimAntics exceptions and calls SignalDialog with the details, but nothing
            // in this pipeline read it, so every engine-level failure looked identical
            // to a clean, silent routing rejection. Surface it so real errors are visible.
            vm.OnDialog += (info) =>
                Console.WriteLine($"vm dialog [{info.Title}] {info.Message}");
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

            ConnectWs();
        }

        /// <summary>(Re)create the WS client and connect. The tab often loads
        /// before the lot host finishes booting; a one-shot connect turns that
        /// race into a permanent "ws error", so Update retries through here.</summary>
        void ConnectWs()
        {
            wsErrored = false;
            cli = new BrowserSandboxClient();
            driver.OnClientCommand += (msg) => cli.Write(new VMNetMessage(VMNetMessageType.Command, msg));
            driver.OnShutdown += (reason) => { Status = "shutdown: " + reason; cli.Disconnect(); };
            cli.OnMessage += driver.ServerMessage;
            cli.OnError += (err) =>
            {
                wsErrored = true;
                if (!wsConnected) Status = "game server not reachable — retrying…";
                else Status = "ws error: " + err;
                Console.WriteLine("vm ws error: " + err);
            };

            cli.OnConnectComplete += () =>
            {
                wsConnected = true;
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

            Console.WriteLine($"vm connecting {wsUrl}...");
            if (!wsConnected) Status = "connecting to game server…";
            cli.Connect(wsUrl);
        }

        /// <summary>Per-frame: drain the socket, tick the VM at 30Hz.</summary>
        public void Update(double elapsedSeconds)
        {
            if (vm == null) return;
            cli.Pump();

            // Retry a never-established connection every ~3s: the tab often
            // loads before the lot host/gateway finish booting.
            if (!wsConnected && wsErrored && ++wsRetryTicks >= 180)
            {
                wsRetryTicks = 0;
                Console.WriteLine("vm ws retrying...");
                try { cli.Disconnect(); } catch { }
                ConnectWs();
            }

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

                // Sample right after each real vm.Tick() call, keyed on the real
                // synced TickID, not on localTicks — see CurrentSyncTick's doc
                // comment. Checking here (once per real tick this client executes)
                // is what makes the modulo boundary land exactly in the normal
                // 1-tick-per-call steady state; checking after the whole burst that
                // a catch-up call can run would frequently skip past it.
                if (Synced && lastSyncTickLogged != vm.CurrentSyncTick && vm.CurrentSyncTick % 30 == 0)
                {
                    lastSyncTickLogged = vm.CurrentSyncTick;
                    Console.WriteLine($"vm tick={localTicks} synctick={vm.CurrentSyncTick} " +
                        $"entities={vm.Entities.Count} hash={EntityHash()}");
                }
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

            // Avatars join at the lot edge and just stand there. Route ours into
            // the house so the sim is visibly in the scene — the same walk the
            // desktop client queues, via the engine's own goto routing.
            // Retry down the candidate list: a failed route is silent (the queue
            // just empties), so one attempt is indistinguishable from a sim that
            // refuses to move — which is exactly what shipped a moment ago.
            if (Synced && !walkInSent && localTicks - syncedAtTick > 60
                && localTicks - lastWalkTry > 75)
            {
                var me = MyAvatar;
                if (me != null && me.GetValue(VMStackObjectVariable.Hidden) == 0)
                {
                    if (walkCandidates == null) walkCandidates = WalkTargets();
                    // Never stack gotos: while the sim is walking its queue holds
                    // "Run Here", and re-sending sent it back out of the house again.
                    var busy = me.Thread?.Queue?.Any(q => q.Name != null && q.Name != "Idle") ?? false;
                    if (busy) { lastWalkTry = localTicks; return; }
                    var arrived = houseCentre.HasValue
                        && Vector2.Distance(new Vector2(me.Position.TileX, me.Position.TileY),
                                            houseCentre.Value) <= 5f;
                    if (arrived)
                    {
                        walkInSent = true;
                        Console.WriteLine("vm sim is inside the house");
                    }
                    else if (walkCandidates.Count > 0)
                    {
                        var target = walkCandidates[0];
                        walkCandidates.RemoveAt(0);
                        walkTarget = target;
                        lastWalkTry = localTicks;
                        // VMNetGotoCmd's x/y are LotTilePos units (1/16 tile), not
                        // tiles: passing tiles walked the sim to the lot corner and
                        // off-screen. Same conversion FromBigTile does.
                        vm.SendCommand(new VMNetGotoCmd
                        {
                            x = (short)((target.X << 4) + 8),
                            y = (short)((target.Y << 4) + 8),
                            level = 1,
                            Interaction = 4, // "walk here" on the goto object
                            Param0 = 0,
                        });
                        Console.WriteLine($"vm walking into the house → tile {target.X},{target.Y}");
                    }
                    else walkInSent = true; // out of candidates; stop trying
                }
            }

            if (Synced && AutoChat != null && !autoChatSent && localTicks - syncedAtTick > 90)
            {
                autoChatSent = true;
                SendChat(AutoChat);
                Console.WriteLine("vm sent chat: " + AutoChat);
            }


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

        /// <summary>
        /// Walkable tiles inside the house, nearest the middle first. The furniture
        /// centroid alone is not routable — it lands in a wall or on a table, the
        /// route fails silently and the sim never leaves the lot edge — so candidates
        /// are floor tiles with nothing standing on them, and the caller retries down
        /// the list.
        /// </summary>
        List<(short X, short Y)> WalkTargets(int max = 40)
        {
            var arch = vm.Context.Architecture;
            if (arch?.Floors == null || arch.Floors.Length == 0) return new List<(short, short)>();
            var floors = arch.Floors[0];

            long sx = 0, sy = 0; int n = 0;
            var occupied = new HashSet<int>();
            foreach (var ent in vm.Entities)
            {
                if (ent.Position == LotTilePos.OUT_OF_WORLD) continue;
                if (ent is VMAvatar) continue;
                occupied.Add(ent.Position.TileY * arch.Width + ent.Position.TileX);
                sx += ent.Position.TileX; sy += ent.Position.TileY; n++;
            }
            if (n == 0) return new List<(short, short)>();
            var cx = sx / (float)n; var cy = sy / (float)n;

            houseCentre = new Vector2(cx, cy);
            var candidates = new List<((short X, short Y) tile, float dist)>();
            for (int y = 0; y < arch.Height; y++)
            {
                for (int x = 0; x < arch.Width; x++)
                {
                    var off = y * arch.Width + x;
                    if (off >= floors.Length || floors[off].Pattern == 0) continue; // no floor = outside
                    if (occupied.Contains(off)) continue;
                    var dx = x - cx; var dy = y - cy;
                    // Floor tiles exist beyond the house too (patios, the blueprint's
                    // wider floor area); anything far from the furniture is not "inside".
                    if (dx * dx + dy * dy > 100) continue;
                    // Rank by elbow room, do not demand it. Requiring a completely
                    // clear ring was survivable when every object was a 1x1 pack
                    // billboard; against real EA furniture — a bed is three tiles, a
                    // sofa two or three — it eliminated the whole middle of the house
                    // and left only closets and bathrooms, which route badly. Open
                    // tiles still sort first, so the good targets are tried first and
                    // the cramped ones remain as fallbacks instead of vanishing.
                    var blockedNeighbours = 0;
                    for (int ny = -1; ny <= 1; ny++)
                        for (int nx = -1; nx <= 1; nx++)
                            if (occupied.Contains((y + ny) * arch.Width + (x + nx))) blockedNeighbours++;
                    candidates.Add((((short)x, (short)y), blockedNeighbours * 100f + dx * dx + dy * dy));
                }
            }
            return candidates.OrderBy(c => c.dist).Take(max).Select(c => c.tile).ToList();
        }

        /// <summary>Tile of my sim, for the camera to follow. Null until it exists.</summary>
        public Vector2? MyTile()
        {
            var me = MyAvatar;
            if (me == null || me.Position == LotTilePos.OUT_OF_WORLD) return null;
            return new Vector2(me.Position.x / 16f, me.Position.y / 16f);
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
            // Do NOT redirect a multitile part to its master here. Parts carry
            // TreeTableID 65535, but VMEntity.UseTreeTableOf copies the master's
            // table onto every part at creation, so each part has the full menu —
            // and which part you clicked is meaningful: the cushions of a sofa are
            // separate seats. Retargeting would sit everyone on the same end.
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
                markerTex = MakeMarkerTexture(gd);
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

        /// <summary>Down-pointing arrow drawn over your own sim — with capsule
        /// placeholders and no canvas font, players couldn't tell which one is
        /// theirs (or spot it at all against the lot).</summary>
        static Texture2D MakeMarkerTexture(GraphicsDevice gd)
        {
            const int w = 18, h = 14;
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                var half = (int)((1f - y / (float)h) * (w / 2f));
                for (int x = 0; x < w; x++)
                {
                    var inside = Math.Abs(x - w / 2) <= half;
                    px[y * w + x] = inside ? Color.White : Color.Transparent;
                }
            }
            var tex = new Texture2D(gd, w, h);
            tex.SetData(px);
            return tex;
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
        /// Live entities in true per-tile depth order: pack objects by GUID
        /// texture, avatars as tinted capsules or (with a VitaboyLayer) real
        /// skinned bodies — positions straight out of the shared VM every frame.
        /// </summary>
        /// <param name="vitaboy">
        /// When given, a sim with a resolved body draws as that body, in its
        /// correct place in the depth-sorted list, instead of a capsule. Real
        /// bodies used to draw in one unsorted pass *after* every sprite here,
        /// which always looked right for an empty room and always looked wrong
        /// next to furniture: a sim on a tile "behind" a table still drew in
        /// front of it, i.e. standing on or inside it. Interleaving avatar draws
        /// into this same sorted list — flushing the SpriteBatch around each one,
        /// since a raw 3D draw can't be batched — fixes that; it does not add
        /// real depth against walls, which stays the ledgered problem it was.
        /// </param>
        public void DrawEntities(GraphicsDevice gd, SpriteBatch sb, WorldState state, VitaboyLayer vitaboy = null)
        {
            if (!texturesReady || vm == null) return;
            var space = state.WorldSpace;
            var offset = space.GetPointScreenOffset();
            var scale = space.TilePxWidthHalf / 64f;

            var draws = new List<(float sortKey, Texture2D tex, Vector2 tile, bool isSim, Color tint,
                VMAvatar bodyAva, SimAvatar body)>();
            foreach (var ent in vm.Entities)
            {
                if (ent.Position == LotTilePos.OUT_OF_WORLD) continue;
                var tile = new Vector2(ent.Position.x / 16f, ent.Position.y / 16f);
                if (ent is VMAvatar ava)
                {
                    // Resolve now, at list-build time, not draw time: HasModel can
                    // only ever become true once TryGetModel has run for this
                    // avatar at least once, so deciding "capsule or body" from a
                    // stale HasModel here would leave a brand new avatar drawing a
                    // capsule forever, since nothing would ever call TryGetModel
                    // for it later either.
                    var body = vitaboy?.TryGetModel(ava);
                    var tint = TintFor(ava.PersistID);
                    if (body?.Skeleton == null)
                        draws.Add((tile.X + tile.Y + 0.01f, simTex, tile, true, tint, null, null));
                    else
                        draws.Add((tile.X + tile.Y + 0.01f, null, tile, true, tint, ava, body));
                    if (ava.PersistID == PersistID && markerTex != null)
                        draws.Add((tile.X + tile.Y + 0.02f, markerTex, tile, true, Color.Yellow, null, null));
                }
                else
                {
                    var guid = ent.Object?.OBJ?.GUID ?? 0;
                    if (!texByGuid.TryGetValue(guid, out var tex))
                    {
                        // No art for this GUID. If it is a part of a multitile group
                        // whose lead does have art, the lead already drew for the
                        // whole group (that is how our single-billboard pack objects
                        // work) — so drop it silently.
                        continue;
                    }
                    // Real EA furniture has art per part, and the VM puts each part on
                    // its own tile, so drawing every part with its own billboard is
                    // what reassembles a 1x3 bed. Pack objects only ever have art on
                    // the lead, so they are unaffected.
                    draws.Add((tile.X + tile.Y, tex, tile, false, Color.White, null, null));
                }
            }

            var batchOpen = true; // sb.Begin() was already called by our caller
            foreach (var d in draws.OrderBy(d => d.sortKey))
            {
                if (d.bodyAva != null)
                {
                    // Can't mix a raw 3D draw into an open SpriteBatch — flush,
                    // draw the body, reopen so the remaining sprites still batch.
                    if (batchOpen) { sb.End(); batchOpen = false; }
                    vitaboy.DrawResolved(gd, state, d.bodyAva, d.body);
                    continue;
                }
                if (!batchOpen) { sb.Begin(); batchOpen = true; }

                var screen = space.GetScreenFromTile(d.tile) + offset;
                var drawScale = d.isSim ? 1f : scale;
                var w = d.tex.Width * drawScale;
                var h = d.tex.Height * drawScale;
                var pos = new Vector2(screen.X - w / 2,
                    screen.Y - h + space.TilePxHeightHalf * (d.isSim ? 0.5f : 1f));
                // Float the "this is you" arrow above the capsule's head.
                if (d.tex == markerTex) pos.Y -= 56f;
                sb.Draw(d.tex, pos, null, d.tint, 0f, Vector2.Zero, drawScale, SpriteEffects.None, 0f);
            }
            if (!batchOpen) sb.Begin(); // leave it open — caller's End() expects that
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
