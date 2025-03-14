using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FSO.IDE.Utils
{
    public partial class HouseSpy : Form
    {
        public short SelectedObjectID = -1;
        public FileSystemWatcher Watcher;
        public string FilePath;

        public OBJM CurrentOBJM;
        public List<short> PersonIDs = new List<short>();

        public HouseSpy()
        {
            InitializeComponent();
            personBox.Visible = false;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "House##.iff|*.iff";
            dialog.Title = "Select House Iff File";
            FormsUtils.StaExecute(() =>
            {
                dialog.ShowDialog();
            });
            if (dialog.FileName == "") return;

            SelectedObjectID = -1;

            if (Watcher != null)
            {
                Watcher.Dispose();
                Watcher = null;
            }

            FilePath = dialog.FileName;
            personBox.Visible = false;

            Watcher = new FileSystemWatcher(Path.GetDirectoryName(FilePath));

            Watcher.Renamed += Watcher_Changed;
            Watcher.Changed += Watcher_Changed;

            Watcher.EnableRaisingEvents = true;

            ReloadFile();
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath != FilePath)
            {
                return;
            }

            try
            {
                using (var stream = File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (stream.Length == 0)
                    {
                        return;
                    }
                }
            }
            catch (Exception)
            {
                return;
            }

            ReloadFile();
        }

        public void ReloadFile()
        {
            var content = FSO.Content.Content.Get();

            var house = new IffFile(FilePath);

            var objt = house.Get<OBJT>(0);
            var objm = house.Get<OBJM>(1);

            if (objt == null || objm == null)
            {
                return;
            }

            objm.Prepare((ushort typeID) =>
            {
                var entry = objt.Entries[typeID - 1];
                return new OBJMResource()
                {
                    OBJD = content.WorldObjects.Get(entry.GUID)?.OBJ,
                    OBJT = entry
                };
            });

            Invoke(new Action(() =>
            {
                CurrentOBJM = objm;
                Redraw();
            }));
        }

        public void Redraw()
        {
            objectList.BeginUpdate();

            PersonIDs.Clear();
            objectList.Items.Clear();

            foreach (var obj in CurrentOBJM.ObjectData)
            {
                var mobj = obj.Value;
                var objt = mobj.Instance.OBJT;

                if (objt.OBJDType == OBJDType.Person)
                {
                    PersonIDs.Add((short)obj.Key);
                    objectList.Items.Add($"{obj.Key}: {objt.Name}");
                }
            }

            objectList.EndUpdate();

            if (SelectedObjectID != -1)
            {
                RedrawSelectedPerson();
            }

            var time = DateTime.Now;
            updatedLabel.Text = $"Last Updated: {time.ToLongTimeString()}";
        }

        private string ObjectString(short id)
        {
            if (CurrentOBJM.ObjectData != null && CurrentOBJM.ObjectData.TryGetValue(id, out var mapped))
            {
                var type = mapped.Instance.OBJT;

                if (type.Name == "")
                {
                    // Try find object by GUID

                    var content = FSO.Content.Content.Get();

                    var res = content.WorldObjects.Get(type.GUID);

                    if (res != null)
                    {
                        return $"{res.OBJ.ChunkLabel} ({id})";
                    }
                }
                else
                {
                    return $"{type.Name} ({id})";
                }
            }

            return id.ToString();
        }

        private ListViewItem GetInteraction(string type, OBJMInteraction action)
        {
            var item = new ListViewItem(new string[]
                {
                    type,
                    action.UID.ToString(),
                    action.CallerID.ToString(),
                    ObjectString(action.TargetID),
                    action.TargetID == action.Icon ? "<-" : ObjectString(action.Icon),
                    action.TTAIndex.ToString(),
                    string.Join(", ", action.Args.Select(x => x.ToString())),
                    action.Priority.ToString(),
                    action.ActionTreeID.ToString(),
                    action.Attenuation.ToString(),
                    ((int)action.Flags).ToString(),
                });

            if (type == "#")
            {
                item.ForeColor = System.Drawing.Color.Gray;
            }

            return item;
        }

        public void RedrawSelectedPerson()
        {
            if (!CurrentOBJM.ObjectData.TryGetValue(SelectedObjectID, out var mapped))
            {
                personBox.Visible = false;
                return;
            }

            var obj = mapped.Instance;
            
            if (!obj.PersonData.HasValue)
            {
                personBox.Visible = false;
                return;
            }

            var person = obj.PersonData.Value;

            positionLabel.Text = $"X: {obj.X / 16f}, Y: {obj.Y / 16f}";
            unknownsLabel.Text = $"AnimEventCount: {person.AnimEventCount}, Engaged: {person.Engaged}, RoutingState: {person.RoutingState}\nRoutingFrameCount: {person.RoutingFrameCount}";
            animationLabel.Text = $"Animation: {person.Animation}\nBase: {person.BaseAnimation}\nCarry: {person.CarryAnimation}";

            floatsList.BeginUpdate();

            floatsList.Items.Clear();
            foreach (var f in person.FirstFloats)
            {
                floatsList.Items.Add(f);
            }

            floatsList.EndUpdate();

            accessoriesList.BeginUpdate();

            accessoriesList.Items.Clear();
            foreach (var acc in person.Accessories)
            {
                if (acc.Binding != "")
                {
                    accessoriesList.Items.Add($"{acc.Binding}: {acc.Name}");
                }
                else
                {
                    accessoriesList.Items.Add($"{acc.Name}");
                }
            }

            accessoriesList.EndUpdate();

            useCountList.BeginUpdate();

            useCountList.Items.Clear();
            foreach (var count in person.ObjectUses)
            {
                useCountList.Items.Add(new ListViewItem(new string[]
                {
                    ObjectString(count.TargetID),
                    count.StackLength.ToString(),
                    count.Unknown2.ToString()
                }));
            }

            useCountList.EndUpdate();

            motiveChangeList.BeginUpdate();

            motiveChangeList.Items.Clear();
            foreach (var delta in person.MotiveDeltas)
            {
                motiveChangeList.Items.Add(new ListViewItem(new string[]
                {
                    ((VMMotive)delta.Motive).ToString(),
                    (delta.TickDelta * 1800).ToString(),
                    delta.StopAt.ToString()
                }));
            }

            motiveChangeList.EndUpdate();

            queueList.BeginUpdate();

            queueList.Items.Clear();
            queueList.Items.Add(GetInteraction("*", person.ActiveInteraction));
            foreach (var action in person.InteractionQueue)
            {
                queueList.Items.Add(GetInteraction("", action));
            }
            queueList.Items.Add(GetInteraction("#", person.LastInteraction));

            queueList.EndUpdate();

            if (!personBox.Visible)
            {
                personBox.Visible = true;
            }
        }

        private void objectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ind = objectList.SelectedIndex;
            if (ind < 0 || ind >= PersonIDs.Count)
            {
                SelectedObjectID = -1;
                personBox.Visible = false;

                return;
            }

            SelectedObjectID = PersonIDs[ind];
            RedrawSelectedPerson();
        }

        private void HouseSpy_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Watcher != null)
            {
                Watcher.Dispose();
                Watcher = null;
            }
        }
    }
}
