using FSO.Common;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.RC;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Concurrent;

namespace FSO.Content
{
    public class RCMeshProvider
    {
        public GraphicsDevice GD;
        public HashSet<string> CacheFiles;
        public HashSet<string> ReplaceFiles;

        public RCMeshProvider(GraphicsDevice gd)
        {
            GD = gd;
            DGRP3DGeometry.ReplTextureProvider = GetTex;

            var repldir = Path.Combine(FSOEnvironment.ContentDir, "MeshReplace/");
            var userrepl = Path.Combine(FSOEnvironment.UserDir, "MeshReplace/");
            if (Directory.Exists(userrepl)) repldir = userrepl;
            var dir = Path.Combine(FSOEnvironment.UserDir, "MeshCache/");
            try
            {
                Directory.CreateDirectory(dir);
                Directory.CreateDirectory(repldir);
            } catch
            {
            }
            CacheFiles = [.. Directory.GetFiles(dir).Select(x => Path.GetFileName(x).ToLowerInvariant())];
            ReplaceFiles = [.. Directory.GetFiles(repldir).Select(x => Path.GetFileName(x).ToLowerInvariant())];
            Packages = new RCDBPFContent(repldir);
        }

        public readonly Dictionary<DGRP, DGRP3DMesh> Cache = [];
        public readonly HashSet<DGRP> IgnoreRCCache = [];
        public readonly ConcurrentDictionary<string, IDGRP3DTextureHolder> ReplacementTex = new();
        public readonly Dictionary<string, DGRP3DMesh> NameCache = [];
        public readonly RCDBPFContent Packages;

        public DGRP3DMesh Get(DGRP dgrp, OBJD obj)
        {
            DGRP3DMesh result = null;
            var repldir = Path.Combine(FSOEnvironment.ContentDir, "MeshReplace/");
            var dir = Path.Combine(FSOEnvironment.UserDir, "MeshCache/");
            if (!Cache.TryGetValue(dgrp, out result))
            {
                // Does it exist in the loaded remesh packs?
                string baseFile = obj.ChunkParent.Filename.Replace('.', '_').ToLowerInvariant();

                if (Packages.TryGetRemesh(dgrp, GD, baseFile, dgrp.ChunkID, out result))
                {
                    Cache[dgrp] = result;

                    return result;
                }

                //does it exist in replacements
                var name = baseFile + "_" + dgrp.ChunkID + ".fsom";
                if (ReplaceFiles.Contains(name))
                {
                    try
                    {
                        result = new DGRP3DMesh(dgrp, Path.Combine(repldir, name), GD);
                    }
                    catch (Exception)
                    {
                        result = null;
                    }
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

                if (CacheFiles.Contains(name))
                {
                    if (result == null && !IgnoreRCCache.Contains(dgrp))
                    {
                        //does it exist in rc cache
                        try
                        {
                            result = new DGRP3DMesh(dgrp, Path.Combine(dir, name), GD);
                        }
                        catch (Exception)
                        {
                            result = null;
                        }
                    }
                } else
                {

                }

                //create it anew
                if (result == null)
                {
                    result = new DGRP3DMesh(dgrp, obj, GD, dir);
                    CacheFiles.Add(name);
                }
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
                    result = new DGRP3DMesh(null, Path.Combine(repldir, name), GD);
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

            var name = dgrp.ChunkParent.Filename.Replace('.', '_').ToLowerInvariant() + "_" + dgrp.ChunkID + ".fsom";
            var repldir = Path.Combine(FSOEnvironment.ContentDir, "MeshReplace/");
            ReplaceFiles.Add(name);
            mesh.SaveDirectory = repldir;
            mesh.Save();

            Cache[dgrp] = mesh;
        }

        public DGRP3DTextureSource? GetTex(string baseName, ushort pixelSPR)
        {
            IDGRP3DTextureHolder result = null;

            string name = baseName;
            if (pixelSPR != 65535)
            {
                name += "_TEX_" + pixelSPR + ".png";
            }

            // TODO: Could have load the same texture multiple times due to a race condition?
            if (!ReplacementTex.TryGetValue(name, out result))
            {
                string lookupName = name;
                if (!Packages.TryGetRemeshTexture(baseName, pixelSPR, out result))
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
                        var path = Path.Combine(dir, name);

                        if (File.Exists(path))
                        {
                            result = new MTEX(File.OpenRead(path));
                        }
                    }
                    catch (Exception)
                    {
                        result = null;
                    }
                }

                ReplacementTex[lookupName] = result;
            }

            return DGRP3DTextureSource.WithDecoded(result, GD);
        }
    }
}
