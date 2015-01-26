using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TSO.Files.FAR3;
using Microsoft.Xna.Framework.Graphics;
using TSO.Common.content;

namespace TSO.Content
{
    /// <summary>
    /// Content is a singleton responsible for loading data.
    /// </summary>
    public class Content
    {
        public static void Init(string basepath, GraphicsDevice device){
            INSTANCE = new Content(basepath, device);
        }
        private static Content INSTANCE;
        public static Content Get()
        {
            return INSTANCE;
        }

        /**
         * Content Manager
         */
        public string BasePath;
        public string[] AllFiles;
        private GraphicsDevice Device;

        /// <summary>
        /// Creates a new instance of Content.
        /// </summary>
        /// <param name="basePath">Path to client directory.</param>
        /// <param name="device">A GraphicsDevice instance.</param>
        private Content(string basePath, GraphicsDevice device)
        {
            this.BasePath = basePath;
            this.Device = device;

            UIGraphics = new UIGraphicsProvider(this, Device);
            AvatarMeshes = new AvatarMeshProvider(this, Device);
            AvatarBindings = new AvatarBindingProvider(this);
            AvatarTextures = new AvatarTextureProvider(this, Device);
            AvatarSkeletons = new AvatarSkeletonProvider(this);
            AvatarAppearances = new AvatarAppearanceProvider(this);
            AvatarOutfits = new AvatarOutfitProvider(this);
            AvatarAnimations = new AvatarAnimationProvider(this);
            AvatarPurchasables = new AvatarPurchasables(this);
            AvatarHandgroups = new HandgroupProvider(this, Device);

            WorldObjects = new WorldObjectProvider(this);
            WorldFloors = new WorldFloorProvider(this);
            WorldWalls = new WorldWallProvider(this);
            WorldObjectGlobals = new WorldGlobalProvider(this);

            Audio = new Audio(this);

            Init();
        }

        /// <summary>
        /// Initiates loading for world.
        /// </summary>
        public void InitWorld()
        {
            WorldObjects.Init();
            WorldObjectGlobals.Init();
            WorldWalls.Init();
            WorldFloors.Init();
        }

        /// <summary>
        /// Setup the content manager so it knows where to find various files.
        /// </summary>
        private void Init()
        {
            /** Scan system for files **/
            var allFiles = new List<string>();
            _ScanFiles(BasePath, allFiles);
            AllFiles = allFiles.ToArray();

            Archives = new Dictionary<string, FAR3Archive>();
            UIGraphics.Init();
            AvatarMeshes.Init();
            AvatarBindings.Init();
            AvatarTextures.Init();
            AvatarSkeletons.Init();
            AvatarAppearances.Init();
            AvatarOutfits.Init();
            AvatarAnimations.Init();
            Audio.Init();
            AvatarPurchasables.Init();
            AvatarHandgroups.Init();

            InitWorld();
        }

        /// <summary>
        /// Scans a directory for a list of files.
        /// </summary>
        /// <param name="dir">The directory to scan.</param>
        /// <param name="fileList">The list of files to scan for.</param>
        private void _ScanFiles(string dir, List<string> fileList)
        {
            var fullPath = this.GetPath(dir);
            var files = Directory.GetFiles(fullPath);
            foreach (var file in files)
            {
                fileList.Add(file.Substring(BasePath.Length));
            }

            var dirs = Directory.GetDirectories(fullPath);
            foreach (var subDir in dirs)
            {
                _ScanFiles(subDir, fileList);
            }
        }

        /// <summary>
        /// Gets a path relative to the client's directory.
        /// </summary>
        /// <param name="path">The path to combine with the client's directory.</param>
        /// <returns>The path combined with the client's directory.</returns>
        public string GetPath(string path)
        {
            return Path.Combine(BasePath, path);
        }

        private Dictionary<string, FAR3Archive> Archives;

        /// <summary>
        /// Gets a resource using a path and ID.
        /// </summary>
        /// <param name="path">The path to the file. If this path is to an archive, assetID can be null.</param>
        /// <param name="assetID">The ID for the resource. Can be null if path doesn't point to an archive.</param>
        /// <returns></returns>
        public Stream GetResource(string path, ulong assetID)
        {
            if (path.EndsWith(".dat"))
            {
                /** Archive **/
                if (!Archives.ContainsKey(path))
                {
                    FAR3Archive newArchive = new FAR3Archive(GetPath(path));
                    Archives.Add(path, newArchive);
                }

                var archive = Archives[path];
                var bytes = archive.GetItemByID(assetID);
                return new MemoryStream(bytes, false);
            }

            return File.OpenRead(GetPath(path));
        }

        /** World **/
        public WorldObjectProvider WorldObjects;
        public WorldGlobalProvider WorldObjectGlobals;
        public WorldFloorProvider WorldFloors;
        public WorldWallProvider WorldWalls;

        public UIGraphicsProvider UIGraphics;
        
        /** Avatar **/
        public AvatarMeshProvider AvatarMeshes;
        public AvatarBindingProvider AvatarBindings;
        public AvatarTextureProvider AvatarTextures;
        public AvatarSkeletonProvider AvatarSkeletons;
        public AvatarAppearanceProvider AvatarAppearances;
        public AvatarOutfitProvider AvatarOutfits;
        public AvatarAnimationProvider AvatarAnimations;
        public AvatarPurchasables AvatarPurchasables;
        public HandgroupProvider AvatarHandgroups;

        /** Audio **/
        public Audio Audio;
    }
}
