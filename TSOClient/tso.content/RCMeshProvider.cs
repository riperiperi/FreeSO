using FSO.Common;
using FSO.Common.Content;
using FSO.Common.Utils;
using FSO.Files;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.RC;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content
{
    public class RCMeshProvider
    {
        public GraphicsDevice GD;
        public RCMeshProvider(GraphicsDevice gd)
        {
            GD = gd;
            DGRP3DGeometry.ReplTextureProvider = GetTex;
        }
        public Dictionary<DGRP, DGRP3DMesh> Cache = new Dictionary<DGRP, DGRP3DMesh>();
        public HashSet<DGRP> IgnoreRCCache = new HashSet<DGRP>();
        public Dictionary<string, Texture2D> ReplacementTex = new Dictionary<string, Texture2D>();
        public Dictionary<string, DGRP3DMesh> NameCache = new Dictionary<string, DGRP3DMesh>();

        public DGRP3DMesh Get(DGRP dgrp, OBJD obj)
        {
            DGRP3DMesh result = null;
            var repldir = Path.Combine(FSOEnvironment.ContentDir, "MeshReplace/");
            var dir = Path.Combine(FSOEnvironment.UserDir, "MeshCache/");
            if (!Cache.TryGetValue(dgrp, out result))
            {
                //does it exist in replacements
                var name = obj.ChunkParent.Filename.Replace('.', '_') + "_" + dgrp.ChunkID + ".fsom";
                try
                {
                    using (var file = File.OpenRead(Path.Combine(repldir, name)))
                    {
                        result = new DGRP3DMesh(dgrp, file, GD);
                    }
                }
                catch (Exception)
                {
                    result = null;
                }

                if (result == null)
                {
                    //does it exist in iff
                    try
                    {
                        result = dgrp.ChunkParent.Get<FSOM>(dgrp.ChunkID)?.Get(dgrp, GD);
                    }
                    catch (Exception)
                    {
                        result = null;
                    }
                }

                if (result == null && !IgnoreRCCache.Contains(dgrp))
                {
                    //does it exist in rc cache
                    try
                    {
                        using (var file = File.OpenRead(Path.Combine(dir, name)))
                        {
                            result = new DGRP3DMesh(dgrp, file, GD);
                        }
                    }
                    catch (Exception)
                    {
                        result = null;
                    }
                }

                //create it anew
                if (result == null) result = new DGRP3DMesh(dgrp, obj, GD, dir);
                Cache[dgrp] = result;
            }
            return result;
        }

        public DGRP3DMesh Get(string name)
        {
            DGRP3DMesh result = null;
            var repldir = Path.Combine(FSOEnvironment.ContentDir, "3D/");
            if (!NameCache.TryGetValue(name, out result))
            {
                //does it exist in replacements
                try
                {
                    using (var file = File.OpenRead(Path.Combine(repldir, name)))
                    {
                        result = new DGRP3DMesh(null, file, GD);
                    }
                }
                catch (Exception)
                {
                    result = null;
                }
                NameCache[name] = result;
            }
            return result;
        }

        public void ClearCache(DGRP dgrp)
        {
            //todo: dispose old?
            IgnoreRCCache.Add(dgrp);
            Cache.Remove(dgrp);
        }

        public void Replace(DGRP dgrp, DGRP3DMesh mesh)
        {
            //todo: dispose old?

            var name = dgrp.ChunkParent.Filename.Replace('.', '_') + "_" + dgrp.ChunkID + ".fsom";
            var repldir = Path.Combine(FSOEnvironment.ContentDir, "MeshReplace/");
            mesh.SaveDirectory = repldir;
            mesh.Save();

            Cache[dgrp] = mesh;
        }

        public Texture2D GetTex(string name)
        {
            Texture2D result = null;
            if (!ReplacementTex.TryGetValue(name, out result))
            {
                string dir;
                if (name.StartsWith("FSO_"))
                {
                    dir = Path.Combine(FSOEnvironment.ContentDir, "3D/");
                    name = name.Substring(4);
                }
                else dir = Path.Combine(FSOEnvironment.ContentDir, "MeshReplace/");
                //load from meshreplace folder
                try
                {
                    using (var file = File.OpenRead(Path.Combine(dir, name)))
                    {
                        result = ImageLoader.FromStream(GD, file);
                        if (FSOEnvironment.EnableNPOTMip)
                        {
                            var data = new Color[result.Width * result.Height];
                            result.GetData(data);
                            var n = new Texture2D(GD, result.Width, result.Height, true, SurfaceFormat.Color);
                            TextureUtils.UploadWithMips(n, GD, data);
                            result.Dispose();
                            result = n;
                        }
                    };
                }
                catch (Exception)
                {
                    result = null;
                }
                ReplacementTex[name] = result;
            }
            return result;
        }
    }
}
