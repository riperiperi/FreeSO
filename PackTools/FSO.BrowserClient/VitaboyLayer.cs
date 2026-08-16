using System;
using System.Collections.Generic;
using System.Linq;
using FSO.LotView;
using FSO.LotView.Model;
using FSO.SimAntics;
using FSO.SimAntics.Entities;
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

        public void Draw(GraphicsDevice gd, WorldState state, VM vm)
        {
            if (!Enabled || vm == null) return;
            var effect = WorldContent.AvatarEffect;
            if (effect == null) { Status = "no avatar effect"; return; }

            var avatars = new List<VMAvatar>();
            foreach (var ent in vm.Entities)
            {
                if (ent is VMAvatar ava && ent.Position != LotTilePos.OUT_OF_WORLD)
                    avatars.Add(ava);
            }
            if (avatars.Count == 0) { Status = "no avatars in vm"; return; }

            var prevDepth = gd.DepthStencilState;
            var prevBlend = gd.BlendState;
            var prevRaster = gd.RasterizerState;

            // Depth off, cull off. The browser world draw never populates a usable
            // depth buffer (the software-depth path is disabled here), so testing
            // against it just discards the sim; and the sprite layers that follow
            // already paint over everything regardless. Sorting against walls is the
            // ledgered depth problem, not this layer's.
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

            drawn = 0;
            foreach (var ava in avatars)
            {
                var model = ModelFor(ava);
                if (model?.Skeleton == null) continue;

                // Tile units → world units, then face the VM's direction. Same two
                // transforms AvatarComponent applies (EntityComponent.World is a
                // plain translation; the rotation is AvatarComponent's).
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
                    // Where do the matrices actually put this sim? A silent layer and
                    // a layer drawing off-screen look identical from the console.
                    var head = model.Skeleton.GetBone("HEAD")?.AbsolutePosition ?? Vector3.Zero;
                    var clip = Vector4.Transform(new Vector4(head, 1), world * state.View * state.Projection);
                    var ndc = new Vector2(clip.X / clip.W, clip.Y / clip.W);
                    var vp = gd.Viewport;
                    Console.WriteLine($"vitaboy: sim {ava.ObjectID} tile={pos.X:F1},{pos.Y:F1} " +
                        $"head-ndc={ndc.X:F2},{ndc.Y:F2} screen=" +
                        $"{(int)((ndc.X + 1) * 0.5f * vp.Width)},{(int)((1 - ndc.Y) * 0.5f * vp.Height)} " +
                        $"bindings={model.Bindings.Count} " +
                        $"meshes={model.Bindings.Count(b => b.Mesh != null)} " +
                        $"textures={model.Bindings.Count(b => b.Texture != null)}");
                }
            }

            gd.DepthStencilState = prevDepth;
            gd.BlendState = prevBlend;
            gd.RasterizerState = prevRaster;

            Status = $"{drawn}/{avatars.Count} sims drawn";
            if (!loggedFirst && drawn > 0)
            {
                loggedFirst = true;
                Console.WriteLine($"vitaboy: first sim drawn ({Status})");
            }
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

                step = "skeleton";
                var skel = content.AvatarSkeletons?.Get("adult.skel");
                Console.WriteLine($"vitaboy: adult.skel={(skel == null ? "MISSING" : skel.Bones?.Length.ToString() + " bones")}");
                step = "model ctor";
                var model = new AdultVitaboyModel();
                if (model.Skeleton == null)
                {
                    Status = "adult.skel missing from bundle";
                    failed.Add(ava.ObjectID);
                    return null;
                }
                step = "appearance";
                model.Appearance = ava.SkinTone;

                // The VM keeps outfit references whether or not it has a world, so
                // the browser reads what the host already decided for this sim.
                step = "outfit lookup";
                var body = ava.BodyOutfit?.GetContent();
                var head = ava.HeadOutfit?.GetContent();
                Console.WriteLine($"vitaboy: sim {ava.ObjectID} bodyRef={ava.BodyOutfit?.Name ?? "null"}/{ava.BodyOutfit?.ID ?? 0:x16} " +
                    $"headRef={ava.HeadOutfit?.Name ?? "null"}/{ava.HeadOutfit?.ID ?? 0:x16} " +
                    $"bodyContent={(body == null ? "null" : "ok")} headContent={(head == null ? "null" : "ok")}");
                step = "body";
                if (body != null)
                {
                    model.Body = body;
                    model.Handgroup = body; // adults use the body outfit for hands
                }
                step = "head";
                // Sandbox joins carry no head reference, so without a fallback the
                // sim renders decapitated. This is the same default VMAvatar itself
                // reaches for when a person has no head data.
                head = head ?? FSO.Content.Content.Get().AvatarOutfits.Get(0x000003a00000000D);
                if (head != null) model.Head = head;
                if (body == null && head == null)
                {
                    Status = $"sim {ava.ObjectID} has no outfit references";
                    failed.Add(ava.ObjectID);
                    return null;
                }

                step = "reload skeleton";
                model.ReloadSkeleton();
                models[ava.ObjectID] = model;
                Console.WriteLine($"vitaboy: built sim {ava.ObjectID} " +
                    $"body={body?.GetType().Name ?? "none"} head={head?.GetType().Name ?? "none"} " +
                    $"bindings={model.Bindings.Count}");
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
