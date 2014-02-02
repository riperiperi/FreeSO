using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SimsLib.FAR3;
using Microsoft.Xna.Framework.Graphics;
using tso.common.content;

namespace tso.content
{
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

        private string BasePath;
        public string[] AllFiles;
        private GraphicsDevice Device;

        public Content(string basePath, GraphicsDevice device){
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

            WorldObjects = new WorldObjectProvider(this);
            WorldFloors = new WorldFloorProvider(this);
            WorldObjectGlobals = new WorldObjectGlobals(this);

            Init();
        }


        public void InitWorld(){
            WorldObjects.Init();
            WorldObjectGlobals.Init();
            WorldFloors.Init();
        }

        /** 
         * Setup the content manager so it knows where to find
         * various files
         */
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

            InitWorld();
            
        }

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



        public string GetPath(string path)
        {
            return Path.Combine(BasePath, path);
        }

        private Dictionary<string, FAR3Archive> Archives;
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
        public WorldObjectGlobals WorldObjectGlobals;
        public WorldFloorProvider WorldFloors;


        public UIGraphicsProvider UIGraphics;
        
        /** Avatar **/
        public AvatarMeshProvider AvatarMeshes;
        public AvatarBindingProvider AvatarBindings;
        public AvatarTextureProvider AvatarTextures;
        public AvatarSkeletonProvider AvatarSkeletons;
        public AvatarAppearanceProvider AvatarAppearances;
        public AvatarOutfitProvider AvatarOutfits;
        public AvatarAnimationProvider AvatarAnimations;
    }
}
