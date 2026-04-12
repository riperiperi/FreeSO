using FSO.Files.Formats.DBPF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.RC;
using FSO.Vitaboy;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Content
{
    internal class RCDBPFFile
    {
        private readonly DBPFFile File;
        private readonly Lock StreamLock = new();
        public readonly FSO3DDirectory Directory;

        private readonly Dictionary<FSO3DRef, DGRP3DMesh> Meshes = [];
        private readonly Dictionary<FSO3DRef, IDGRP3DTextureHolder> Textures = [];

        public RCDBPFFile(string path)
        {
            File = new DBPFFile(path);

            var directoryData = File.GetItemByID(DBPFTypeID.FSO3DDirectory, 0);

            if (directoryData == null)
            {
                throw new InvalidDataException($"Remesh package {path} doesn't contain a directory chunk.");
            }

            Directory = new FSO3DDirectory();
            using var dirStream = new MemoryStream(directoryData);
            Directory.Read(dirStream);
        }

        public FSO3DRef? GetRef(string fileName, ushort chunkId, bool mesh)
        {
            if (!Directory.Entries.TryGetValue(fileName, out var entry))
            {
                return null;
            }

            var lookup = mesh ? entry.Meshes : entry.Textures;

            if (!lookup.TryGetValue(chunkId, out var result))
            {
                return null;
            }

            return result;
        }

        public DGRP3DMesh GetMesh(DGRP dgrp, GraphicsDevice gd, FSO3DRef reference)
        {
            lock (StreamLock)
            {
                if (!Meshes.TryGetValue(reference, out var result))
                {
                    var meshData = File.GetItemByID((DBPFTypeID)reference.TypeID, reference.FileID);

                    // Deliberately doesn't close, as this is done asynchronously by DGRP3DMesh.
                    var meshStream = new MemoryStream(meshData);

                    try
                    {
                        result = new DGRP3DMesh(dgrp, meshStream, gd);
                    }
                    catch (Exception e)
                    {
                        result = null;
                    }

                    Meshes[reference] = result;
                }

                return result;
            }
        }

        public IDGRP3DTextureHolder GetTexture(FSO3DRef reference)
        {
            lock (StreamLock)
            {
                if (!Textures.TryGetValue(reference, out var result))
                {
                    var texData = File.GetItemByID((DBPFTypeID)reference.TypeID, reference.FileID);
                    using var texStream = new MemoryStream(texData);

                    try
                    {
                        switch ((DBPFTypeID)reference.TypeID)
                        {
                            case DBPFTypeID.MTX2:
                                {
                                    var mtx2 = new MTX2();
                                    mtx2.Read(null, texStream);
                                    result = mtx2;
                                    break;
                                }
                            case DBPFTypeID.MTEX:
                                {
                                    var mtex = new MTEX();
                                    mtex.Read(null, texStream);
                                    result = mtex;
                                    break;
                                }
                            default:
                                throw new NotSupportedException($"Unsupported remesh texture type {reference.TypeID:x8}");
                        }
                    }
                    catch (Exception e)
                    {
                        result = null;
                    }

                    Textures[reference] = result;
                }

                return result;
            }
        }

        public FSO3DCredits GetCredits()
        {
            lock (StreamLock)
            {
                var creditsData = File.GetItemByID(DBPFTypeID.FSO3DCredits, 0);

                var credits = new FSO3DCredits();
                using var creditsStream = new MemoryStream(creditsData);
                credits.Read(creditsStream);

                return credits;
            }
        }
    }

    public class RCDBPFContent
    {
        private readonly List<RCDBPFFile> Files = [];

        public RCDBPFContent(string rootDir)
        {
            // Scan for dbpf

            var files = Directory.GetFiles(rootDir);

            foreach (var file in files)
            {
                if (file.EndsWith(".dat"))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    try
                    {
                        var collection = new RCDBPFFile(file);

                        AddCollection(collection);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to load remesh package {name}: {e}");
                    }
                }
            }
        }

        private void AddCollection(RCDBPFFile file)
        {
            Files.Add(file);
        }

        public bool TryGetRemesh(DGRP dgrp, GraphicsDevice gd, string file, ushort chunkId, out DGRP3DMesh mesh)
        {
            foreach (var collection in Files)
            {
                var ref3d = collection.GetRef(file, chunkId, true);

                if (ref3d == null)
                {
                    continue;
                }

                mesh = collection.GetMesh(dgrp, gd, ref3d.Value);
                return true;
            }

            mesh = null;
            return false;
        }

        public bool TryGetRemeshTexture(string file, ushort chunkId, out IDGRP3DTextureHolder texture)
        {
            foreach (var collection in Files)
            {
                var ref3d = collection.GetRef(file, chunkId, false);

                if (ref3d == null)
                {
                    continue;
                }

                texture = collection.GetTexture(ref3d.Value);
                return true;
            }

            texture = null;
            return false;
        }

        public FSO3DCredits[] GetCredits()
        {
            return [.. Files.Select(x => x.GetCredits())];
        }
    }
}
