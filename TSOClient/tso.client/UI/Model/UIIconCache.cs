using FSO.Common.Rendering.Framework;
using FSO.Common.Rendering.Framework.Camera;
using FSO.SimAntics;
using FSO.Vitaboy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace FSO.Client.UI.Model
{
    /// <summary>
    /// Caches icons for objects missing them, eg. heads, some catalog objects..
    /// </summary>
    public static class UIIconCache
    {
        //indexed as mesh:texture
        private static Dictionary<ulong, Texture2D> AvatarHeadCache = new Dictionary<ulong, Texture2D>();

        /*
        public static Texture2D GetObject(VMEntity obj)
        {
            if (obj is VMAvatar)
            {
                var ava = (VMAvatar)obj;
                var headname = ava.HeadOutfit.Name;
                if (headname == "") headname = ava.BodyOutfit.OftData.TS1TextureID;
                var id = headname +":"+ ava.HeadOutfit.OftData.TS1TextureID;

                Texture2D result = null;
                if (!AvatarHeadCache.TryGetValue(id, out result))
                {
                    result = GenHeadTex(ava);
                    AvatarHeadCache[id] = result;
                }
                return result;
            }
            else if (obj is VMGameObject)
            {
                if (obj.Object.OBJ.GUID == 0x000007C4) return Content.Get().CustomUI.Get("int_gohere.png").Get(GameFacade.GraphicsDevice);
                else return obj.GetIcon(GameFacade.GraphicsDevice, 0);
            }
            return null;
        }*/

        public static Texture2D GetObject(VMEntity obj)
        {
            if (obj is VMAvatar)
            {
                var ava = (VMAvatar)obj;
                return GenHeadTex(ava.HeadOutfit?.ID ?? 0, ava.BodyOutfit.ID);
            }
            return null;
        }

        public static Texture2D GenHeadTex(ulong headOft, ulong bodyOft)
        {
            if (headOft == 0) headOft = bodyOft;

            Texture2D result = null;
            if (!AvatarHeadCache.TryGetValue(headOft, out result))
            {
                var ofts = Content.Content.Get().AvatarOutfits;
                var oft = ofts.Get(headOft);
                if (oft == null) return null;
                else
                {
                    result = GenHeadTex(oft, ofts.GetNameByID(headOft));
                }
                AvatarHeadCache[headOft] = result;
            }
            return result;
        }

        public static Texture2D GenHeadTex(Outfit headOft, string name)
        {
            var skels = Content.Content.Get().AvatarSkeletons;
            Skeleton skel = null;
            bool pet = false;
            if (name.StartsWith("uaa"))
            {
                //pet
                if (name.Contains("cat")) skel = skels.Get("cat.skel");
                else skel = skels.Get("dog.skel");
                pet = true;
            } else
            {
                skel = skels.Get("adult.skel");
            }

            var m_Head = new SimAvatar(skel);
            m_Head.Head = headOft;
            m_Head.ReloadSkeleton();
            m_Head.StripAllButHead();

            var HeadCamera = new BasicCamera(GameFacade.GraphicsDevice, new Vector3(0.0f, 7.0f, -17.0f), Vector3.Zero, Vector3.Up);

            var pos2 = m_Head.Skeleton.GetBone("HEAD").AbsolutePosition;
            pos2.Y += (pet)?((name.Contains("dog"))?0.16f:0.1f):0.12f;
            HeadCamera.Position = new Vector3(0, pos2.Y, 12.5f);
            HeadCamera.FOV = (float)Math.PI / 3f;
            HeadCamera.Target = pos2;
            HeadCamera.ProjectionOrigin = new Vector2(66/2, 66/2);

            var HeadScene = new _3DTargetScene(GameFacade.GraphicsDevice, HeadCamera, new Point(66, 66), 0);// (GlobalSettings.Default.AntiAlias) ? 8 : 0);
            HeadScene.ID = "UIPieMenuHead";
            HeadScene.ClearColor = new Color(49, 65, 88);

            m_Head.Scene = HeadScene;
            m_Head.Scale = new Vector3(1f);

            HeadCamera.Zoom = 19.5f;

            //rotate camera, similar to pie menu

            double xdir = 0;//Math.Atan(0);
            double ydir = 0;//Math.Atan(0);

            Vector3 off = new Vector3(0, 0, 13.5f);
            Matrix mat = Microsoft.Xna.Framework.Matrix.CreateRotationY((float)xdir) * Microsoft.Xna.Framework.Matrix.CreateRotationX((float)ydir);

            HeadCamera.Position = new Vector3(0, pos2.Y, 0) + Vector3.Transform(off, mat);

            if (pet)
            {
                HeadCamera.Zoom *= 1.3f;
            }
            //end rotate camera

            HeadScene.Initialize(GameFacade.Scenes);
            HeadScene.Add(m_Head);

            HeadScene.Draw(GameFacade.GraphicsDevice);
            return Common.Utils.TextureUtils.Decimate(HeadScene.Target, GameFacade.GraphicsDevice, 2, true);
        }
    }
}
