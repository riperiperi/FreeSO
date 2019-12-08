using FSO.Common.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FSO.Content.Framework
{
    public class TSOAvatarContentProvider<T> : CompositeProvider<T>
    {
        public FAR3Provider<T> FAR;
        public FileProvider<T> Files;
        public RuntimeProvider<T> Runtime;

        private string FileFolder;

        public List<string> Names
        {
            get
            {
                var farp = Providers.FirstOrDefault(x => x is FAR3Provider<T>) as FAR3Provider<T>;
                var runtime = Providers.FirstOrDefault(x => x is RuntimeProvider<T>) as RuntimeProvider<T>;
                var files = Files.List().Select(x => Path.GetFileName((x as FileContentReference<T>).Name).ToLowerInvariant());

                return FAR.EntriesByName.Keys.ToList().Concat(files).Concat(Runtime.EntriesByName.Keys).ToList(); //expose so we can list all animations, for now.
            }
        }

        public TSOAvatarContentProvider(Content contentManager, IContentCodec<T> codec, Regex farRegex, Regex fileRegex) : base()
        {
            FAR = new FAR3Provider<T>(contentManager, codec, farRegex);
            Files = new FileProvider<T>(contentManager, codec, fileRegex);
            Files.UseContent = true;
            Files.FAR3IDs = true;
            Runtime = new RuntimeProvider<T>();

            var fileFolder = fileRegex.ToString();
            var lastSlash = fileFolder.LastIndexOf('/');
            if (lastSlash != -1)
            {
                fileFolder = fileFolder.Substring(0, lastSlash + 1);
            }
            FileFolder = Path.Combine("Content/", fileFolder);

            SetProviders(new List<IContentProvider<T>> {
                FAR,
                Files,
                Runtime
            });
        }

        private ulong GenerateId()
        {
            // firstly, find the typeid that files in this FAR typically use
            var type = FAR.EstimateTypeId();
            var random = new Random((int)DateTime.Now.Ticks + FAR.EntriesByName.Count);
            
            var tryD = (uint)random.Next();
            tryD |= (uint)random.Next(2) << 31;

            while (Get(type, tryD) != null)
            {
                tryD = (uint)random.Next();
                tryD |= (uint)random.Next(2) << 31;
            }

            return (ulong)type | ((ulong)tryD << 32);
        }

        public ulong CreateFile(string name, T obj, byte[] data, bool runtime)
        {
            var id = GenerateId();
            Runtime.Add(name, id, obj);

            if (!runtime)
            {
                //splice in the id to the filename
                var strID = id.ToString("x16");
                var split = name.Split('.').ToList();
                split.Insert(Math.Max(1, split.Count - 1), strID);
                name = string.Join(".", split);

                //write the file
                var folder = Path.Combine(FileFolder, "User/");
                Directory.CreateDirectory(folder);
                using (var file = File.Open(Path.Combine(folder, name), FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                {
                    file.Write(data, 0, data.Length);
                }
            }
            return id;
        }

        public string GetNameByID(ulong ID)
        {
            return FAR.GetNameByID(ID);
        }

        public void Init()
        {
            FAR.Init();
            Files.Init();
        }
    }
}
