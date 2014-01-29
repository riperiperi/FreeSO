/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TSOClient.VM;
using SimsLib.ThreeD;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TSOClient.Code.Rendering.Sim;

namespace TSOClient.Code.Data
{
    /// <summary>
    /// Place to get information and assets related to sims, e.g. skins, thumbnails etc
    /// </summary>
    public class SimCatalog
    {
        public static void GetCollection(ulong fileID)
        {
            var collectionData = ContentManager.GetResourceFromLongID(fileID);
            var reader = new BinaryReader(new MemoryStream(collectionData));
        }

        public SimCatalog()
        {
        }

        private static Dictionary<ulong, Binding> Bindings = new Dictionary<ulong, Binding>();
        public static Binding GetBinding(ulong id)
        {
            if (Bindings.ContainsKey(id))
            {
                return Bindings[id];
            }

            var bytes = ContentManager.GetResourceFromLongID(id);
            var binding = new Binding(bytes);
            Bindings.Add(id, binding);
            return binding;
        }

        private static Dictionary<ulong, Appearance> Appearances = new Dictionary<ulong, Appearance>();
        public static Appearance GetAppearance(ulong id)
        {
            if (Appearances.ContainsKey(id))
            {
                return Appearances[id];
            }

            var bytes = ContentManager.GetResourceFromLongID(id);
            var app = new Appearance(bytes);
            Appearances.Add(id, app);
            return app;
        }

        private static Dictionary<ulong, Outfit> Outfits = new Dictionary<ulong, Outfit>();
        public static Outfit GetOutfit(ulong id)
        {
            if (Outfits.ContainsKey(id))
            {
                return Outfits[id];
            }

            var bytes = ContentManager.GetResourceFromLongID(id);
            var outfit = new Outfit(bytes);
            Outfits.Add(id, outfit);
            return outfit;
        }

        private static Dictionary<ulong, Texture2D> OutfitTextures = new Dictionary<ulong, Texture2D>();
        public static Texture2D GetOutfitTexture(ulong id)
        {
            if (OutfitTextures.ContainsKey(id))
            {
                return OutfitTextures[id];
            }

            var bytes = ContentManager.GetResourceFromLongID(id);
            using (var stream = new MemoryStream(bytes))
            {
                var texture = Texture2D.FromFile(GameFacade.GraphicsDevice, stream);
                OutfitTextures.Add(id, texture);
                return texture;
            }
        }

        private static Dictionary<ulong, Mesh> OutfitMeshes = new Dictionary<ulong, Mesh>();
        public static Mesh GetOutfitMesh(ulong id)
        {
            if (OutfitMeshes.ContainsKey(id))
            {
                return OutfitMeshes[id].Clone();
            }

            var mesh = new Mesh();
            mesh.Read(ContentManager.GetResourceFromLongID(id));
            mesh.ProcessMesh();
            OutfitMeshes.Add(id, mesh);
            return mesh;
        }

        public static void LoadSim3D(Sim sim)
        {
            LoadSimHead(sim);
            LoadSimBody(sim);
            LoadSimHands(sim);

            sim.Reposition();
        }

        public static void LoadSimHead(Sim sim)
        {
            var appearance = SimCatalog.GetOutfit(sim.HeadOutfitID)
                            .GetAppearanceObject(sim.AppearanceType);

            sim.HeadBindings = appearance.BindingIDs.Select(
                x => new SimModelBinding(x)
            ).ToList();
        }

        public static void LoadSimBody(Sim sim)
        {
            var appearance = SimCatalog.GetOutfit(sim.BodyOutfitID)
                            .GetAppearanceObject(sim.AppearanceType);

            sim.BodyBindings = appearance.BindingIDs.Select(
                x => new SimModelBinding(x)
            ).ToList();
        }

        public static void LoadSimHands(Sim sim)
        {
            ulong ID = SimCatalog.GetOutfit(sim.BodyOutfitID).HandgroupID;

            Hag HandGrp = new Hag(ContentManager.GetResourceFromLongID(ID));
            Appearance Apr;

            //This is UGLY, there must be a better way of doing this. :\
            switch (sim.AppearanceType)
            {
                case AppearanceType.Light:
                    if (HandGrp.LightSkin.LeftHand.FistGesture != 0)
                    {
                        Apr = new Appearance(ContentManager.GetResourceFromLongID(
                            HandGrp.LightSkin.LeftHand.FistGesture));

                        sim.LeftHandBindings.FistBindings = Apr.BindingIDs.Select(x => new SimModelBinding(x)).ToList();

                        Apr = new Appearance(ContentManager.GetResourceFromLongID(
                            HandGrp.LightSkin.RightHand.FistGesture));

                        sim.RightHandBindings.FistBindings = Apr.BindingIDs.Select(x => new SimModelBinding(x)).ToList();
                    }

                    if (HandGrp.LightSkin.LeftHand.IdleGesture != 0)
                    {
                        Apr = new Appearance(ContentManager.GetResourceFromLongID(
                            HandGrp.LightSkin.LeftHand.IdleGesture));

                        sim.LeftHandBindings.IdleBindings = Apr.BindingIDs.Select(x => new SimModelBinding(x)).ToList();

                        Apr = new Appearance(ContentManager.GetResourceFromLongID(
                            HandGrp.LightSkin.RightHand.IdleGesture));

                        sim.RightHandBindings.IdleBindings = Apr.BindingIDs.Select(x => new SimModelBinding(x)).ToList();
                    }

                    if (HandGrp.LightSkin.LeftHand.PointingGesture != 0)
                    {
                        Apr = new Appearance(ContentManager.GetResourceFromLongID(
                            HandGrp.LightSkin.LeftHand.PointingGesture));

                        sim.LeftHandBindings.PointingBindings = Apr.BindingIDs.Select(x => new SimModelBinding(x)).ToList();

                        Apr = new Appearance(ContentManager.GetResourceFromLongID(
                            HandGrp.LightSkin.RightHand.PointingGesture));

                        sim.RightHandBindings.PointingBindings = Apr.BindingIDs.Select(x => new SimModelBinding(x)).ToList();
                    }
                    break;
                case AppearanceType.Medium:
                    if (HandGrp.MediumSkin.LeftHand.FistGesture != 0)
                    {
                        Apr = new Appearance(ContentManager.GetResourceFromLongID(
                            HandGrp.MediumSkin.LeftHand.FistGesture));

                        sim.LeftHandBindings.FistBindings = Apr.BindingIDs.Select(x => new SimModelBinding(x)).ToList();

                        Apr = new Appearance(ContentManager.GetResourceFromLongID(
                            HandGrp.MediumSkin.RightHand.FistGesture));

                        sim.RightHandBindings.FistBindings = Apr.BindingIDs.Select(x => new SimModelBinding(x)).ToList();
                    }

                    if (HandGrp.MediumSkin.LeftHand.IdleGesture != 0)
                    {
                        Apr = new Appearance(ContentManager.GetResourceFromLongID(
                            HandGrp.MediumSkin.LeftHand.IdleGesture));

                        sim.LeftHandBindings.IdleBindings = Apr.BindingIDs.Select(x => new SimModelBinding(x)).ToList();

                        Apr = new Appearance(ContentManager.GetResourceFromLongID(
                            HandGrp.MediumSkin.RightHand.IdleGesture));

                        sim.RightHandBindings.IdleBindings = Apr.BindingIDs.Select(x => new SimModelBinding(x)).ToList();
                    }

                    if (HandGrp.MediumSkin.LeftHand.PointingGesture != 0)
                    {
                        Apr = new Appearance(ContentManager.GetResourceFromLongID(
                            HandGrp.MediumSkin.LeftHand.PointingGesture));

                        sim.LeftHandBindings.PointingBindings = Apr.BindingIDs.Select(x => new SimModelBinding(x)).ToList();

                        Apr = new Appearance(ContentManager.GetResourceFromLongID(
                            HandGrp.MediumSkin.RightHand.PointingGesture));

                        sim.RightHandBindings.PointingBindings = Apr.BindingIDs.Select(x => new SimModelBinding(x)).ToList();
                    }
                    break;
                case AppearanceType.Dark:
                    if (HandGrp.DarkSkin.LeftHand.FistGesture != 0)
                    {
                        Apr = new Appearance(ContentManager.GetResourceFromLongID(
                            HandGrp.DarkSkin.LeftHand.FistGesture));

                        sim.LeftHandBindings.FistBindings = Apr.BindingIDs.Select(x => new SimModelBinding(x)).ToList();

                        Apr = new Appearance(ContentManager.GetResourceFromLongID(
                            HandGrp.DarkSkin.RightHand.FistGesture));

                        sim.RightHandBindings.FistBindings = Apr.BindingIDs.Select(x => new SimModelBinding(x)).ToList();
                    }

                    if (HandGrp.DarkSkin.LeftHand.IdleGesture != 0)
                    {
                        Apr = new Appearance(ContentManager.GetResourceFromLongID(
                            HandGrp.DarkSkin.LeftHand.IdleGesture));

                        sim.LeftHandBindings.IdleBindings = Apr.BindingIDs.Select(x => new SimModelBinding(x)).ToList();

                        Apr = new Appearance(ContentManager.GetResourceFromLongID(
                            HandGrp.DarkSkin.RightHand.IdleGesture));

                        sim.RightHandBindings.IdleBindings = Apr.BindingIDs.Select(x => new SimModelBinding(x)).ToList();
                    }

                    if (HandGrp.DarkSkin.LeftHand.PointingGesture != 0)
                    {
                        Apr = new Appearance(ContentManager.GetResourceFromLongID(
                            HandGrp.DarkSkin.LeftHand.PointingGesture));

                        sim.LeftHandBindings.PointingBindings = Apr.BindingIDs.Select(x => new SimModelBinding(x)).ToList();

                        Apr = new Appearance(ContentManager.GetResourceFromLongID(
                            HandGrp.DarkSkin.RightHand.PointingGesture));

                        sim.RightHandBindings.PointingBindings = Apr.BindingIDs.Select(x => new SimModelBinding(x)).ToList();
                    }
                    break;
            }
        }
    }

    public static class SimsLibExtensions
    {
        public static Appearance GetAppearanceObject(this Outfit outfit, AppearanceType type)
        {
            return SimCatalog.GetAppearance(outfit.GetAppearance(type));
        }

        public static Binding GetBinding(this Appearance appearance)
        {
            return SimCatalog.GetBinding(appearance.BindingIDs[0]);
        }

        public static Mesh LoadMesh(this Binding binding)
        {
            return SimCatalog.GetOutfitMesh(binding.MeshAssetID);
        }

        public static Texture2D LoadTexture(this Binding binding)
        {
            return SimCatalog.GetOutfitTexture(binding.TextureAssetID);
        }
    }
}
