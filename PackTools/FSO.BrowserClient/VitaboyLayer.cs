using System;
using System.Collections.Generic;
using System.Linq;
using FSO.LotView;
using FSO.LotView.Model;
using FSO.SimAntics;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model;
using FSO.Vitaboy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO_BrowserClient
{
    /// <summary>
    /// Real Sims bodies in the browser: a skinned Vitaboy mesh per VM avatar,
    /// posed from the shared VM's position and facing.
    ///
    /// This deliberately does NOT go through Blueprint.Avatars /
    /// WorldEntities.DrawAvatars. That pass brackets its work in
    /// _2d.PrepareImmediate(...), which is the sprite-batch path the browser has
    /// never got a fragment out of (see SESSION-LANES: DGRP sprites). The avatar
    /// mesh itself needs none of it — it is an ordinary indexed draw through
    /// WorldContent.AvatarEffect — so we replicate DrawAvatars' device and effect
    /// setup and skip the batch entirely.
    ///
    /// Behind ?vitaboy=1. Capsules stay the default until this is proven.
    /// </summary>
    public class VitaboyLayer
    {
        /// <summary>?vitaboy=1. Also gates passing a GraphicsDevice to the content
        /// boot — the avatar mesh/texture providers are device-gated, so without
        /// it Content.AvatarMeshes is null and there is nothing to draw.</summary>
        public static bool Enabled;

        public static string Status { get; private set; } = "off";

        // One model per VM avatar, keyed by ObjectID. Building a SimAvatar pulls
        // meshes and textures out of the FAR3 archives and uploads them, so it is
        // far too expensive to redo per frame.
        readonly Dictionary<short, SimAvatar> models = new Dictionary<short, SimAvatar>();
        readonly HashSet<short> failed = new HashSet<short>();
        int drawn;
        bool loggedFirst;

        public int Drawn => drawn;

        /// <summary>True once this avatar has a real body on screen, so the caller
        /// can drop its placeholder capsule.</summary>
        public bool HasModel(short objectID) => models.ContainsKey(objectID);

        /// <summary>
        /// Resolve (building on first call) the model for an avatar, without
        /// drawing it. The caller uses this to decide sprite-capsule vs. real-body
        /// *before* it commits to a position in its own depth-sorted draw list —
        /// see DrawResolved's doc comment for why draw and resolve are split.
        /// </summary>
        public SimAvatar TryGetModel(VMAvatar ava) => ModelFor(ava);

        /// <summary>
        /// Draw one already-resolved avatar. Split from resolution (TryGetModel)
        /// so a caller — VmLotClient.DrawEntities — can interleave real bodies with
        /// sprite-drawn furniture in true per-tile depth order: sort furniture and
        /// avatars into one list by tile.X+tile.Y, then for each entry either draw
        /// the sprite or call this, flushing the SpriteBatch around it. Before this
        /// split, every real body drew in one pass *after* all furniture with depth
        /// testing off, which was a deliberate "never invisible" tradeoff — but it
        /// meant a sim standing on a tile behind a table still drew in front of it,
        /// which reads as the body standing on or inside the furniture. Wrong.
        /// </summary>
        public void DrawResolved(GraphicsDevice gd, WorldState state, VMAvatar ava, SimAvatar model)
        {
            var effect = WorldContent.AvatarEffect;
            if (effect == null || model?.Skeleton == null) return;

            var prevDepth = gd.DepthStencilState;
            var prevBlend = gd.BlendState;
            var prevRaster = gd.RasterizerState;

            // Depth off, cull off. The browser world draw never populates a usable
            // depth buffer (the software-depth path is disabled here), so testing
            // against it just discards the sim. True per-pixel depth against walls
            // is the ledgered problem this layer doesn't solve; per-tile draw order
            // (the caller's job) is what it does solve.
            gd.DepthStencilState = DepthStencilState.None;
            gd.BlendState = BlendState.AlphaBlend;
            gd.RasterizerState = RasterizerState.CullNone;

            // NoSSAA — the plain lit technique. The browser has no SSAA target and
            // no advanced lighting, so the higher techniques have nothing to read.
            effect.CurrentTechnique = effect.Techniques[0];
            Set(effect, "View", state.View);
            Set(effect, "Projection", state.Projection);
            // Both must be explicitly false. The shader discards fragments whose
            // packed depth loses to depthMapSampler, and depthMap is never bound in
            // the browser — leaving SoftwareDepth unset is how the object sprite
            // pass ended up drawing nothing at all.
            SetBool(effect, "SoftwareDepth", false);
            SetBool(effect, "depthOutMode", false);
            Set(effect, "AmbientLight", Vector4.One);

            // Tile units → world units, then face the VM's direction. Same two
            // transforms AvatarComponent applies (EntityComponent.World is a plain
            // translation; the rotation is AvatarComponent's).
            var pos = ava.VisualPosition;
            model.Position = WorldSpace.GetWorldFromTile(pos);
            var world = Matrix.CreateRotationY((float)(Math.PI - ava.RadianDirection))
                * Matrix.CreateTranslation(WorldSpace.GetWorldFromTile(pos));

            Set(effect, "ObjectID", ava.ObjectID / 65535f);
            Set(effect, "Level", (float)ava.Position.Level + 0.0001f);
            Set(effect, "World", world);

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                model.DrawGeometry(gd, effect);
            }
            drawn++;

            if (!loggedFirst)
            {
                loggedFirst = true;
                // Where do the matrices actually put this sim? A silent layer and a
                // layer drawing off-screen look identical from the console.
                var headBone = model.Skeleton.GetBone("HEAD")?.AbsolutePosition ?? Vector3.Zero;
                var clip = Vector4.Transform(new Vector4(headBone, 1), world * state.View * state.Projection);
                var vp = gd.Viewport;
                Console.WriteLine($"vitaboy: sim {ava.ObjectID} at tile {pos.X:F1},{pos.Y:F1}, " +
                    $"head on screen {(int)((clip.X / clip.W + 1) * 0.5f * vp.Width)}," +
                    $"{(int)((1 - clip.Y / clip.W) * 0.5f * vp.Height)}");
            }
            Status = $"{drawn} sims drawn";

            gd.DepthStencilState = prevDepth;
            gd.BlendState = prevBlend;
            gd.RasterizerState = prevRaster;
        }

        SimAvatar ModelFor(VMAvatar ava)
        {
            if (models.TryGetValue(ava.ObjectID, out var existing)) return existing;
            if (failed.Contains(ava.ObjectID)) return null;

            var step = "start";
            try
            {
                var content = FSO.Content.Content.Get();
                if (content?.AvatarMeshes == null)
                {
                    Status = "content has no avatar meshes (device-gated providers off)";
                    failed.Add(ava.ObjectID);
                    return null;
                }

                step = "model ctor";
                var model = new AdultVitaboyModel();
                if (model.Skeleton == null)
                {
                    Status = "adult.skel missing from bundle";
                    failed.Add(ava.ObjectID);
                    return null;
                }
                // The VM keeps outfit references whether or not it has a world, so
                // the browser reads what the host already decided for this sim — and
                // then fixes it up, because in a sandbox nobody decided anything.
                step = "outfit lookup";
                var look = Look.For(ava.PersistID);
                var body = Dressed(content, ava.BodyOutfit, look.Body, "body");
                var head = Dressed(content, ava.HeadOutfit, look.Head, "head");

                step = "appearance";
                // The outfit carries light/medium/dark variants; picking the tone here
                // rather than trusting SkinTone means the fallback look is complete.
                model.Appearance = ava.SkinTone == AppearanceType.Light ? look.Skin : ava.SkinTone;

                step = "body";
                if (body != null)
                {
                    model.Body = body;
                    model.Handgroup = body; // adults use the body outfit for hands
                }
                step = "head";
                if (head != null) model.Head = head;
                if (body == null && head == null)
                {
                    Status = $"sim {ava.ObjectID} has no usable outfit";
                    failed.Add(ava.ObjectID);
                    return null;
                }

                step = "reload skeleton";
                model.ReloadSkeleton();
                models[ava.ObjectID] = model;
                Console.WriteLine($"vitaboy: dressed sim {ava.ObjectID} as {look.Name} " +
                    $"({model.Bindings.Count} bindings, {model.Appearance} skin)");
                return model;
            }
            catch (Exception e)
            {
                // One bad sim must not take the tab down — record it and keep the
                // capsule for that avatar.
                failed.Add(ava.ObjectID);
                Status = "sim build failed: " + e.Message;
                Console.WriteLine($"vitaboy: {Status} (at {step})");
                Console.WriteLine(e.StackTrace);
                return null;
            }
        }

        /// <summary>
        /// Resolve an outfit, replacing TSO's own placeholder with a real one.
        ///
        /// A sim who never went through character creation gets
        /// VMAvatarDefaultSuits.Daywear, and that is literally
        /// "mab000_xy__proxy.oft" — the blue question-mark texture TSO shows for
        /// content it has not downloaded. It resolves perfectly and renders an
        /// alien. Anything whose name says proxy gets swapped for the chosen look.
        /// </summary>
        static Outfit Dressed(FSO.Content.Content content, VMOutfitReference vmRef, ulong fallbackId, string what)
        {
            var outfits = content.AvatarOutfits;
            var chosen = vmRef?.GetContent();
            if (chosen != null && vmRef.ID != 0)
            {
                var name = outfits.GetNameByID(vmRef.ID) ?? "";
                if (!name.Contains("proxy", StringComparison.OrdinalIgnoreCase)) return chosen;
            }
            var real = outfits.Get(fallbackId);
            if (real == null) Console.WriteLine($"vitaboy: no {what} outfit for 0x{fallbackId:x16}");
            return real ?? chosen;
        }

        /// <summary>
        /// A stable appearance per player, derived from PersistID.
        ///
        /// Nobody in this build has a character creator, so without this every sim on
        /// the lot is the same person. PersistID is assigned by the host and is
        /// identical in every client, so each player looks the same to everyone
        /// without adding a single byte to the lockstep protocol.
        ///
        /// IDs are TSO base-game outfit file ids (type 0x0D), read out of
        /// avatardata/{bodies,heads}/outfits. Bodies and heads are gendered, so both
        /// are picked from the same side of the split.
        /// </summary>
        readonly struct Look
        {
            public readonly ulong Body;
            public readonly ulong Head;
            public readonly AppearanceType Skin;
            public readonly string Name;

            Look(ulong body, ulong head, AppearanceType skin, string name)
            { Body = body; Head = head; Skin = skin; Name = name; }

            const ulong Oft = 0x0000000D; // outfit type id, low half of the content id

            static ulong Id(uint file) => Oft | ((ulong)file << 32);

            static readonly (uint body, uint head, string name)[] Male =
            {
                (0x252, 0x3a1, "casual"),
                (0x256, 0x3a2, "nerd"),
                (0x25b, 0x3a4, "slob"),
                (0x25d, 0x3a6, "alt"),
                (0x258, 0x3a8, "ross"),
            };

            static readonly (uint body, uint head, string name)[] Female =
            {
                (0x007, 0x186, "mom"),
                (0x00d, 0x18b, "ave"),
                (0x011, 0x18e, "lynn"),
                (0x00a, 0x190, "scrubs"),
            };

            static readonly AppearanceType[] Skins =
            { AppearanceType.Light, AppearanceType.Medium, AppearanceType.Dark };

            public static Look For(uint persistID)
            {
                // Knuth multiplicative hash: adjacent PersistIDs (which is what a
                // sandbox hands out) must not produce adjacent-looking sims.
                var h = persistID * 2654435761u;
                var set = ((h >> 8) & 1) == 0 ? Male : Female;
                var pick = set[(h >> 9) % (uint)set.Length];
                return new Look(Id(pick.body), Id(pick.head),
                    Skins[(h >> 16) % (uint)Skins.Length], pick.name);
            }
        }

        static void Set(Effect effect, string name, Matrix value)
        {
            effect.Parameters[name]?.SetValue(value);
        }

        static void Set(Effect effect, string name, Vector4 value)
        {
            effect.Parameters[name]?.SetValue(value);
        }

        static void Set(Effect effect, string name, float value)
        {
            effect.Parameters[name]?.SetValue(value);
        }

        static void SetBool(Effect effect, string name, bool value)
        {
            effect.Parameters[name]?.SetValue(value);
        }
    }
}
