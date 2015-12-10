using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FSO.Content;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.Formats.IFF;
using System.Reflection;
using System.Threading;
using FSO.IDE.EditorComponent;
using FSO.IDE.ResourceBrowser.ResourceEditors;

namespace FSO.IDE.ResourceBrowser
{
    public partial class IFFResComponent : UserControl
    {
        public GameIffResource ActiveIff;
        public GameObject ActiveObject;

        public Dictionary<Type, Type> ChunkToControl = new Dictionary<Type, Type>()
        {
            { typeof(BHAV), typeof(BHAVResourceControl) }
        };

        public Type[] ChunkTypes = new Type[]
        {
            typeof(BHAV),
            typeof(TTAB),
            typeof(STR),
            typeof(BCON),
            typeof(SLOT)
        };
        public string[] TypeNames = new string[]
        {
            "Trees",
            "Tree Tables",
            "Strings",
            "Constants",
            "SLOTs"
        };

        private ContextMenu ResRightClick;
        private MenuItem ResRCAlpha;
        private MenuItem ResRCShowID;

        private bool AlphaOrder = true;
        private bool ShowID = true;

        public IFFResComponent()
        {
            InitializeComponent();

            ResTypeCombo.Items.Clear();
            for (int i = 0; i < ChunkTypes.Length; i++) {
                ResTypeCombo.Items.Add(new ComboChunkType(TypeNames[i], ChunkTypes[i]));
            }
            ResTypeCombo.SelectedIndex = 0;

            ResRightClick = new ContextMenu();
            ResRCAlpha = new MenuItem() { Text = "Alphabetical Order", Index = 0, Checked = true };
            ResRCAlpha.Click += ResRCAlpha_Select;

            ResRCShowID = new MenuItem() { Text = "Show IDs", Index = 1, Checked = true };
            ResRCShowID.Click += ResRCShowID_Select;

            ResRightClick.MenuItems.AddRange(new MenuItem[]{ ResRCAlpha, ResRCShowID });
            ResList.ContextMenu = ResRightClick;
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

            MethodInfo method = typeof(GameIffResource).GetMethod("ListArray");
            MethodInfo generic = method.MakeGenericMethod(selectedType.ChunkType);
            var chunks = (object[])generic.Invoke(ActiveIff, null);

            var items = GetResList((IffChunk[])chunks);
            object[] listItems;
            if (AlphaOrder) listItems = items.OrderBy(x => x.Name).ToArray();
            else listItems = items.OrderBy(x => x.ID).ToArray();

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
                result.Add(new ObjectResourceEntry(chunk.ChunkLabel.TrimEnd('\0'), chunk.ChunkID));
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
                resInt.SetActiveResource(ActiveIff.Get<BHAV>(((ObjectResourceEntry)ResList.SelectedItem).ID), ActiveIff);
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
                    new Thread(() =>
                    {
                        var bhav = ActiveIff.Get<BHAV>(item.ID);
                        var editor = new BHAVEditor(bhav, new EditorScope(ActiveObject, bhav), null);
                        Application.Run(editor);
                    }).Start();
                }
            }
        }

        private void ResTypeCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ResTypeCombo.SelectedIndex != -1) RefreshResList();
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
