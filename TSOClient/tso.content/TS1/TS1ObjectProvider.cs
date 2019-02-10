using FSO.Common.Utils;
using FSO.Content.Framework;
using FSO.Content.Interfaces;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content.TS1
{
    public class TS1ObjectProvider : AbstractObjectProvider, IObjectCatalog
    {
        private TS1SubProvider<IffFile> GameObjects;
        private static List<ObjectCatalogItem>[] ItemsByCategory;
        private static Dictionary<uint, ObjectCatalogItem> ItemsByGUID;

        public TS1ObjectProvider(Content contentManager, TS1Provider provider) : base(contentManager)
        {
            GameObjects = new TS1SubProvider<IffFile>(provider, ".iff");
        }

        public void Init()
        {
            GameObjects.Init();

            Entries = new Dictionary<ulong, GameObjectReference>();
            Cache = new TimedReferenceCache<ulong, GameObject>();

            ItemsByGUID = new Dictionary<uint, ObjectCatalogItem>();
            ItemsByCategory = new List<ObjectCatalogItem>[30];
            for (int i = 0; i < 30; i++) ItemsByCategory[i] = new List<ObjectCatalogItem>();

            var allIffs = GameObjects.ListGeneric();
            foreach (var iff in allIffs)
            {
                var file = (IffFile)iff.GetThrowawayGeneric();
                file.MarkThrowaway();
                var objects = file.List<OBJD>();
                if (objects != null)
                {
                    foreach (var obj in objects)
                    {
                        Entries[obj.GUID] = new GameObjectReference(this)
                        {
                            FileName = Path.GetFileName(iff.ToString().Replace('\\', '/')),
                            ID = obj.GUID,
                            Name = obj.ChunkLabel,
                            Source = GameObjectSource.Far,
                            Group = (short)obj.MasterID,
                            SubIndex = obj.SubIndex
                        };

                        //does this object appear in the catalog?
                        if ((obj.FunctionFlags > 0 || obj.BuildModeType > 0) && obj.Disabled == 0 && (obj.MasterID == 0 || obj.SubIndex == -1))
                        {
                            //todo: more than one of these set? no normal game objects do this
                            //todo: room sort
                            var cat = (sbyte)Math.Log(obj.FunctionFlags, 2);
                            if (obj.FunctionFlags == 0) cat = (sbyte)(obj.BuildModeType+7);
                            var item = new ObjectCatalogItem()
                            {
                                Category = (sbyte)(cat), //0-7 buy categories. 8-15 build mode categories
                                GUID = obj.GUID,
                                DisableLevel = 0,
                                Price = obj.Price,
                                Name = obj.ChunkLabel,

                                Subsort = (byte)obj.FunctionSubsort,
                                CommunitySort = (byte)obj.CommunitySubsort,
                                DowntownSort = (byte)obj.DTSubsort,
                                MagictownSort = (byte)obj.MTSubsort,
                                StudiotownSort = (byte)obj.STSubsort,
                                VacationSort = (byte)obj.VacationSubsort
                            };
                            ItemsByCategory[item.Category].Add(item);
                            ItemsByGUID[item.GUID] = item;
                        }
                    }
                }
            }

            ContentManager.Neighborhood.LoadCharacters(false);
        }

        protected override Func<string, GameObjectResource> GenerateResource(GameObjectReference reference)
        {
            return (fname) =>
            {
                /** Better set this up! **/
                IffFile iff = null;

                if (reference.Source == GameObjectSource.Far)
                {
                    iff = GameObjects.Get(reference.FileName.ToLower());
                    if (iff != null) iff.RuntimeInfo.Path = reference.FileName;
                }
                else
                {
                    //unused
                    iff = new IffFile(reference.FileName, reference.Source == GameObjectSource.User);
                    iff.RuntimeInfo.Path = reference.FileName;
                    iff.RuntimeInfo.State = IffRuntimeState.Standalone;
                }

                if (iff != null)
                {
                    if (iff != null && iff.RuntimeInfo.State == IffRuntimeState.PIFFPatch)
                    {
                        //OBJDs may have changed due to patch. Remove all file references
                        ResetFile(iff);
                    }
                    iff.RuntimeInfo.UseCase = IffUseCase.Object;
                }

                return new GameObjectResource(iff, null, null, reference.FileName, ContentManager);
            };
        }

        public List<ObjectCatalogItem> All()
        {
            var result = new List<ObjectCatalogItem>();
            foreach (var cat in ItemsByCategory)
            {
                result.AddRange(cat);
            }
            return result;
        }

        public List<ObjectCatalogItem> GetItemsByCategory(sbyte category)
        {
            return ItemsByCategory[category];
        }

        public ObjectCatalogItem? GetItemByGUID(uint guid)
        {
            ObjectCatalogItem item;
            if (ItemsByGUID.TryGetValue(guid, out item))
                return item;
            else return null;
        }
    }
}
