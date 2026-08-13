using FSO.Common;
using FSO.Common.Utils;
using FSO.Files.Formats.DBPF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.FSO;
using FSO.Files.RC;
using Microsoft.Xna.Framework.Graphics;
using System.Net;
using System.Security.Cryptography;

namespace FSO.Content
{
    internal class RCDBPFFile : IDisposable
    {
        private readonly DBPFFile File;
        // object (not System.Threading.Lock) — Lock is net9+; LotView closure dual-targets net8.
        private readonly object StreamLock = new();
        public readonly FSO3DDirectory Directory;

        private readonly Dictionary<FSO3DRef, DGRP3DMesh> Meshes = [];
        private readonly Dictionary<FSO3DRef, IDGRP3DTextureHolder> Textures = [];

        private bool Disposed;

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
                    if (Disposed) return null;

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
                    if (Disposed) return null;

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

        public void Dispose()
        {
            lock (StreamLock)
            {
                Disposed = true;
                File.Dispose();
            }
        }
    }

    public class RCDBPFContent
    {
        public static float? DownloadPercentage;

        private readonly List<RCDBPFFile> Files = [];
        private string RootDir;

        public RCDBPFContent(string rootDir)
        {
            RootDir = rootDir;
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

        private void TryDeleteFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
                // Do nothing
            }
        }

        private FSO3DPackageTextureFormat GetPreferredFormat()
        {
            return FSOEnvironment.TexCompressSupport ? FSO3DPackageTextureFormat.Dxt : FSO3DPackageTextureFormat.Png;
        }

        private FSORemeshFile SelectFileByFormat(FSORemeshChannel channel, FSO3DPackageTextureFormat format)
        {
            FSORemeshFile result = null;

            switch (format)
            {
                case FSO3DPackageTextureFormat.Png:
                    result = channel.png;
                    break;
                case FSO3DPackageTextureFormat.Dxt:
                    result = channel.dxt;
                    break;
            }

            return result ?? channel.png;
        }

        public void TryUpdate(FSOUpdateResponse response)
        {
            if ((response.remeshes?.Length ?? 0) == 0)
            {
                return;
            }

            // Try find a remesh package to update.

            foreach (var file in Files)
            {
                var credits = file.GetCredits();

                var meta = credits.Metadata;

                // Try find a channel that matches this installed remesh package.

                var matching = response.remeshes.FirstOrDefault(x => x.channel == meta.ChannelName && x.publicKey == meta.PublicKey);

                if (matching.version > meta.Version)
                {
                    DownloadPackage(matching, SelectFileByFormat(matching, meta.Format), file, () => TryUpdate(response));

                    // We can come back to update other packages after we download this one.
                    return;
                }
            }

            // Should we be downloading a channel automatically?

            if (Files.Count == 0 && response.autoRemeshChannel != null)
            {
                var target = response.remeshes.FirstOrDefault(x => x.channel == response.autoRemeshChannel);

                if (target != null && target.publicKey == FSOVersionInfo.Current.publicKey)
                {
                    var file = SelectFileByFormat(target, GetPreferredFormat());

                    if (file != null)
                    {
                        DownloadPackage(target, file);
                    }
                }
            }
        }

        private static RSA TryGetCrypto(string publicKey)
        {
            try
            {
                var rsa = RSA.Create();

                rsa.ImportFromPem(publicKey.Replace('^', '\n'));

                return rsa;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void Unload(RCDBPFFile toReplace)
        {
            Files.Remove(toReplace);
            toReplace.Dispose();
        }


        private void DownloadPackage(FSORemeshChannel channel, FSORemeshFile file, RCDBPFFile toReplace = null, Action onComplete = null)
        {
            if (!Uri.TryCreate(file.url, UriKind.Absolute, out var uri))
            {
                return;
            }

            if (!string.IsNullOrEmpty(channel.publicKey))
            {
                // Make sure the file signature is valid.

                var crypto = TryGetCrypto(channel.publicKey);

                if (crypto == null || !crypto.VerifyHash(Convert.FromBase64String(file.hash), Convert.FromBase64String(file.signature), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                {
                    return;
                }
            }

            string name = Path.GetFileName(uri.LocalPath);

            var localPath = PathUtils.SafeCombine(RootDir, name);
            var client = new WebClient();

            DownloadPercentage = 0;

            client.DownloadProgressChanged += (obj, evt) =>
            {
                DownloadPercentage = evt.ProgressPercentage / 100f;
            };

            client.DownloadFileCompleted += (obj, evt) =>
            {
                DownloadPercentage = null;

                if (evt.Cancelled || evt.Error != null)
                {
                    TryDeleteFile(localPath);
                    return;
                }

                if (file.size != 0)
                {
                    var size = new FileInfo(localPath).Length;

                    if (size != file.size)
                    {
                        // Not valid.
                        return;
                    }
                }

                if (file.hash != null)
                {
                    using FileStream fileStr = File.OpenRead(localPath);
                    var hash = SHA256.HashData(fileStr);

                    if (Convert.ToBase64String(hash) != file.hash)
                    {
                        // Not valid.
                        return;
                    }
                }

                // Try and load the new package.

                GameThread.InUpdate(() =>
                {
                    try
                    {
                        var collection = new RCDBPFFile(localPath);

                        AddCollection(collection);

                        onComplete?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to load downloaded remesh package {name}: {e}");

                        // It's probably corrupted.
                        TryDeleteFile(localPath);
                    }
                });
            };
            
            try
            {
                if (toReplace != null)
                {
                    Unload(toReplace);
                    TryDeleteFile(localPath);
                }

                client.DownloadFileAsync(uri, localPath);
            }
            catch
            {

            }
        }
    }
}
