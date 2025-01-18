using System;
using System.Collections.Generic;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using FSO.Content;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.Formats.IFF;
using System.Reflection;
using System.Threading;
using FSO.IDE.ResourceBrowser.ResourceEditors;
using System.IO;
using FSO.Files.Utils;
using FSO.Files.Formats.OTF;

namespace FSO.IDE.ResourceBrowser
{
    public partial class IFFResComponent : UserControl
    {
        public GameIffResource ActiveIff;
        public GameObject ActiveObject;

        public Dictionary<Type, Type> ChunkToControl = new Dictionary<Type, Type>()
        {
            { typeof(BHAV), typeof(BHAVResourceControl) },
            { typeof(STR), typeof(STRResourceControl) },
            { typeof(CTSS), typeof(STRResourceControl) },
            { typeof(TTAB), typeof(TTABResourceControl) },
            { typeof(SPR2), typeof(SPR2ResourceControl) },
            { typeof(SPR), typeof(SPRResourceControl) },
            { typeof(BCON), typeof(BCONResourceControl) },
            { typeof(SLOT), typeof(SLOTResourceControl) },
            { typeof(FCNS), typeof(STRResourceControl) },
            { typeof(OTFFile), typeof(OTFResourceControl) }
        };

        public Type[] ChunkTypes = new Type[]
        {
            typeof(BHAV),
            typeof(TTAB),
            typeof(STR),
            typeof(BCON),
            typeof(SLOT),
            typeof(CTSS),
            typeof(SPR2),
            typeof(SPR),
            typeof(FCNS),
            typeof(OTFFile)
        };
        public string[] TypeNames = new string[]
        {
            "Trees",
            "Tree Tables",
            "Strings",
            "Constants",
            "SLOTs",
            "Catalog Strings",
            "Sprites (SPR2)",
            "Sprites (SPR#)",
            "Simulator Constants",
            "Tuning (OTF)"
        };
        public OBJDSelector[][] OBJDSelectors = new OBJDSelector[][]
        {
            new OBJDSelector[]{ },
            new OBJDSelector[]{new OBJDSelector("My Tree Table", "TreeTableID") },
            new OBJDSelector[]
            {
                new OBJDSelector("My Animation Table", "AnimationTableID"),
                new OBJDSelector("My Body Strings", "BodyStringID"),
            },
            new OBJDSelector[] { },
            new OBJDSelector[] { new OBJDSelector("My SLOTs", "SlotID") },
            new OBJDSelector[] { new OBJDSelector("My Catalog Strings", "CatalogStringsID") },
            new OBJDSelector[] { },
            new OBJDSelector[] { },
            new OBJDSelector[] { }
        };

        private ContextMenu ResRightClick;
        private MenuItem ResRCAlpha;
        private MenuItem ResRCShowID;
        private List<ObjectResourceEntry> VisibleChunks;
        private OBJDSelector[] ActiveSelectors;

        private bool AlphaOrder = true;
        private bool ShowID = true;

        public IFFResComponent()
        {
            InitializeComponent();

            ResRightClick = new ContextMenu();
            ResRCAlpha = new MenuItem() { Text = "Alphabetical Order", Index = 0, Checked = true };
            ResRCAlpha.Click += ResRCAlpha_Select;

            ResRCShowID = new MenuItem() { Text = "Show IDs", Index = 1, Checked = true };
            ResRCShowID.Click += ResRCShowID_Select;

            ResRightClick.MenuItems.AddRange(new MenuItem[]{ ResRCAlpha, ResRCShowID });
            ResList.ContextMenu = ResRightClick;
            ResList.DrawMode = DrawMode.OwnerDrawFixed;
            ResList.DrawItem += ResList_DrawItem;
        }

        public void Init(Type[] cTypes, string[] cNames, OBJDSelector[][] selectors)
        {
            ChunkTypes = cTypes;
            TypeNames = cNames;
            OBJDSelectors = selectors;

            Init();
        }

        public void Init()
        {
            ResTypeCombo.Items.Clear();
            for (int i = 0; i < ChunkTypes.Length; i++)
            {
                ResTypeCombo.Items.Add(new ComboChunkType(TypeNames[i], ChunkTypes[i]));
            }
            ResTypeCombo.SelectedIndex = 0;
        }

        private void ResList_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            Brush myBrush = Brushes.Black;

            if (e.Index == -1) return;
            var item = (ObjectResourceEntry)ResList.Items[e.Index];

            foreach (var sel in ActiveSelectors)
            {
                if (ActiveObject != null && sel.FieldName != null && item.ID == ActiveObject.OBJ.GetPropertyByName<ushort>(sel.FieldName))
                    myBrush = Brushes.BlueViolet;
            }

            e.Graphics.DrawString(item.ToString(),
                e.Font, myBrush, e.Bounds, StringFormat.GenericDefault);
            // If the ListBox has focus, draw a focus rectangle around the selected item.
            e.DrawFocusRectangle();
        }
        public void SetAlphaOrder(bool value)
        {
            AlphaOrder = value;
            RefreshResList();
            ResRCAlpha.Checked = AlphaOrder;
        }

        private void ResRCShowID_Select(object sender, EventArgs e)
        {
            ShowID = !ShowID;
            RefreshResList();
            ResRCShowID.Checked = ShowID;
        }

        private void ResRCAlpha_Select(object sender, EventArgs e)
        {
            AlphaOrder = !AlphaOrder;
            RefreshResList();
            ResRCAlpha.Checked = AlphaOrder;
        }

        public void ChangeIffSource(GameIffResource iff)
        {
            ActiveIff = iff;
            RefreshResList();
        }

        public void ChangeActiveObject(GameObject obj)
        {
            ActiveObject = obj;
        }

        public void RefreshResList()
        {
            if (ActiveIff == null) return;
            var selectedType = (ComboChunkType)ResTypeCombo.SelectedItem;

            ActiveSelectors = OBJDSelectors[Array.IndexOf(ChunkTypes, selectedType.ChunkType)];

            List<ObjectResourceEntry> items;
            if (selectedType.ChunkType == typeof(OTFFile)) {
                items = new List<ObjectResourceEntry>() { new ObjectResourceEntry("OTF File", 0) };
            }
            else
            {
                MethodInfo method = typeof(GameIffResource).GetMethod("ListArray");
                MethodInfo generic = method.MakeGenericMethod(selectedType.ChunkType);
                var chunks = (object[])generic.Invoke(ActiveIff, new object[0]);

                items = GetResList((IffChunk[])chunks);
            }

            object[] listItems;
            if (AlphaOrder) listItems = items.OrderBy(x => x.Name).ToArray();
            else listItems = items.OrderBy(x => x.ID).ToArray();

            VisibleChunks = items;

            ResList.SelectedIndex = -1;
            ResList_SelectedIndexChanged(ResList, new EventArgs());
            ResList.Items.Clear();
            ResList.Items.AddRange(listItems);
        }

        public List<ObjectResourceEntry> GetResList(IffChunk[] list)
        {
            var result = new List<ObjectResourceEntry>();
            foreach (var item in list)
            {
                var chunk = (IffChunk)item;
                result.Add(new ObjectResourceEntry(chunk.ChunkLabel, chunk.ChunkID));
            }
            return result;
        }

        private class ComboChunkType
        {
            public string Name;
            public Type ChunkType;

            public ComboChunkType(string name, Type chunkType)
            {
                Name = name;
                ChunkType = chunkType;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        private void ResList_SelectedIndexChanged(object sender, EventArgs e)
        {
            ResControlPanel.Controls.Clear();
            UserControl control;

            if (ResList.SelectedIndex == -1)
            {
                var c = new UnknownResourceControl();
                c.SetErrorMsg("No resource selected.");
                control = c;
            }
            else
            {
                var selectedType = (ComboChunkType)ResTypeCombo.SelectedItem;

                Type controlType = null;
                ChunkToControl.TryGetValue(selectedType.ChunkType, out controlType);
                if (controlType == null) controlType = typeof(UnknownResourceControl);

                control = (UserControl)Activator.CreateInstance(controlType);

                var resInt = (IResourceControl)control;
                resInt.SetActiveObject(ActiveObject);

                MethodInfo method = typeof(GameIffResource).GetMethod("Get");
                MethodInfo generic = method.MakeGenericMethod(selectedType.ChunkType);
                var chunk = (IffChunk)generic.Invoke(ActiveIff, new object[] { ((ObjectResourceEntry)ResList.SelectedItem).ID });

                resInt.SetActiveResource(chunk, ActiveIff);
                resInt.SetOBJDAttrs(ActiveSelectors);
            }

            control.Dock = DockStyle.Fill;
            ResControlPanel.Controls.Add(control);
        }

        private void ResList_DoubleClick(object sender, EventArgs e)
        {
            if (ResList.SelectedItem != null)
            {
                var item = (ObjectResourceEntry)ResList.SelectedItem;
                var selectedType = (ComboChunkType)ResTypeCombo.SelectedItem;

                if (selectedType.ChunkType == typeof(BHAV))
                {
                    var bhav = ActiveIff.Get<BHAV>(item.ID);
                    MainWindow.Instance.BHAVManager.OpenEditor(bhav, ActiveObject);
                }
            }
        }

        private void ResTypeCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ResTypeCombo.SelectedIndex != -1) RefreshResList();
        }

        private void NewRes_Click(object sender, EventArgs e)
        {
            var selectedType = (ComboChunkType)ResTypeCombo.SelectedItem;

            var chunk = (IffChunk)Activator.CreateInstance(selectedType.ChunkType, new object[] { });
            var type = chunk.GetType();

            if ((type == typeof(SPR2) || type == typeof(SPR) || type == typeof(DGRP))
                && ActiveIff is GameObjectResource && ((GameObjectResource)ActiveIff).Sprites != null)
                chunk.ChunkParent = ((GameObjectResource)ActiveIff).Sprites;
            else
                chunk.ChunkParent = ActiveIff.MainIff;
            chunk.ChunkProcessed = true;
            chunk.ChunkType = IffFile.CHUNK_TYPES.FirstOrDefault(x => x.Value == type).Key ?? "";
            var dialog = new IffNameDialog(chunk, true);
            dialog.ShowDialog();
            /*chunk.ChunkLabel = "New Chunk";
            chunk.ChunkID = GetFreeID();
            chunk.ChunkProcessed = true;
            ActiveIff.MainIff.AddChunk(chunk);*/
            RefreshResList();
        }

        private ushort GetFreeID()
        {
            //start at the lowest ID shown. 
            var idSort = VisibleChunks.OrderBy(x => x.ID);
            ushort lastID = 0;
            foreach (var chk in idSort)
            {
                if (lastID == 0) lastID = chk.ID;
                else
                {
                    if ((ushort)(chk.ID - lastID) > 1) return (ushort)(chk.ID + 1);
                    lastID = chk.ID;
                }
            }
            return (ushort)(lastID + 1);
        }

        private void RenameRes_Click(object sender, EventArgs e)
        {
            if (ResList.SelectedItem == null) return;
            var selectedType = (ComboChunkType)ResTypeCombo.SelectedItem;

            MethodInfo method = typeof(GameIffResource).GetMethod("Get");
            MethodInfo generic = method.MakeGenericMethod(selectedType.ChunkType);
            var chunk = (IffChunk)generic.Invoke(ActiveIff, new object[] { ((ObjectResourceEntry)ResList.SelectedItem).ID });

            var dialog = new IffNameDialog(chunk, false);
            dialog.ShowDialog();
            RefreshResList();
        }

        public void GotoResource(Type type, ushort ID)
        {
            ResTypeCombo.SelectedIndex = Array.IndexOf(ChunkTypes, type);
            var destination = new object[ResList.Items.Count];
            ResList.Items.CopyTo(destination, 0);
            ResList.SelectedIndex = destination.ToList().FindIndex(x => ((ObjectResourceEntry)x).ID == ID);
        }

        private IffChunk GetChunk()
        {
            if (ResList.SelectedItem == null) return null;
            var selectedType = (ComboChunkType)ResTypeCombo.SelectedItem;

            MethodInfo method = typeof(GameIffResource).GetMethod("Get");
            MethodInfo generic = method.MakeGenericMethod(selectedType.ChunkType);
            var chunk = (IffChunk)generic.Invoke(ActiveIff, new object[] { ((ObjectResourceEntry)ResList.SelectedItem).ID });
            return chunk;
        }

        private void DeleteRes_Click(object sender, EventArgs e)
        {
            var chunk = GetChunk();
            if (chunk == null) return;

            Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
            {
                chunk.ChunkParent.FullRemoveChunk(chunk);
                Content.Content.Get().Changes.ChunkChanged(chunk);
            }));
            RefreshResList();
        }

        private void CopyRes_Click(object sender, EventArgs e)
        {
            var data = new DataObject();
            var chunk = GetChunk();
            if (chunk == null) return;

            using (var memStream = new MemoryStream())
            {
                using (var io = IoWriter.FromStream(memStream, ByteOrder.BIG_ENDIAN))
                    chunk.ChunkParent.WriteChunk(io, chunk);

                data.SetData("rawbinary", false, memStream);

                StaExecute(() =>
                {
                    Clipboard.SetDataObject(data, true);
                });
            }
        }

        private void PasteRes_Click(object sender, EventArgs e)
        {
            //try to paste this into the iff file.

            DataObject retrievedData = null;
            IffChunk chunk = null;
            StaExecute(() =>
            {
                retrievedData = Clipboard.GetDataObject() as DataObject;
                if (retrievedData == null || !retrievedData.GetDataPresent("rawbinary"))
                    return;
                MemoryStream memStream = retrievedData.GetData("rawbinary") as MemoryStream;
                if (memStream == null)
                    return;

                memStream.Seek(0, SeekOrigin.Begin);
                try
                {
                    using (var io = IoBuffer.FromStream(memStream, ByteOrder.BIG_ENDIAN))
                        chunk = ActiveIff.MainIff.AddChunk(memStream, io, false);
                }
                catch (Exception)
                {

                }
            });

            if (chunk != null)
            {
                chunk.ChunkParent = ActiveIff.MainIff;
                var dialog = new IffNameDialog(chunk, true);
                dialog.ShowDialog();
            }

            RefreshResList();
        }


        private void StaExecute(Action action)
        {
            // ༼ つ ◕_◕ ༽つ IMPEACH STAThread ༼ つ ◕_◕ ༽つ
            var wait = new AutoResetEvent(false);
            var thread = new Thread(() => {
                action();
                wait.Set();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            wait.WaitOne();
            return;
        }
    }
    public class ObjectResourceEntry
    {
        public string Name;
        public ushort ID;

        public ObjectResourceEntry(string name, ushort id)
        {
            Name = name;
            ID = id;
        }

        public override string ToString()
        {
            return ID + " - " + Name;
        }
    }
}
