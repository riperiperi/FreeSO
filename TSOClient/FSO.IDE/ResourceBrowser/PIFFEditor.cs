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
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using System.Reflection;
using System.IO;
using System.Collections;
using System.Threading;

namespace FSO.IDE.ResourceBrowser
{
    public partial class PIFFEditor : UserControl
    {
        public static HashSet<string> IgnoreProps = new HashSet<string>
        {
            "RuntimeInfo",
            "Function",
            "RawData",
            "InteractionByIndex"
        };
        public static HashSet<string> IgnoreChunks = new HashSet<string>
        {
            "PALT",
            "SPR2",
            "SPR#",
            "MTEX",
            "BMP_",
            "FSOR",
            "FSOM",
            "FSOV",
            "PNG_"
        };
        public GameIffResource ActiveObj;
        public IffFile ActiveIff;

        public int ActivePIFFIndex = -1;

        public IffFile[] ActivePIFFs = new IffFile[]{
            null, //PIFF
            null, //SPF
            null //STR
        };

        public IffFile ActivePIFF;
        public PIFF ActivePIFFChunk;

        public PIFFListItem ActiveItem;
        public PIFFEditor()
        {
            InitializeComponent();
        }

        public void SetActiveObject(GameObject obj)
        {
            ActiveObj = obj.Resource;
            ActiveIff = obj.Resource.MainIff;
            var piffs = ActiveIff.RuntimeInfo.Patches;

            ActivePIFF = piffs.FirstOrDefault();
            ActivePIFFs[0] = obj.Resource.Iff.RuntimeInfo.Patches.FirstOrDefault(x => x.Filename?.EndsWith(".str.piff") != true);
            ActivePIFFs[1] = (obj.Resource.Sprites == null) ? null : obj.Resource.Sprites.RuntimeInfo.Patches.FirstOrDefault();
            ActivePIFFs[2] = obj.Resource.Iff.RuntimeInfo.Patches.FirstOrDefault(x => x.Filename?.EndsWith(".str.piff") == true);

            PIFFButton.Enabled = ActivePIFFs[0] != null;
            SPFButton.Enabled = ActivePIFFs[1] != null;
            STRButton.Enabled = ActivePIFFs[2] != null;

            SelectPIFF(Array.FindIndex(ActivePIFFs, x => x != null));
        }

        public void SetActiveIff(GameIffResource iff)
        {
            ActiveObj = iff;
            ActiveIff = iff.MainIff;
            var piffs = ActiveIff.RuntimeInfo.Patches;

            ActivePIFF = piffs.FirstOrDefault();
            ActivePIFFs[0] = ActiveIff.RuntimeInfo.Patches.FirstOrDefault(x => x.Filename?.EndsWith(".str.piff") != true);
            ActivePIFFs[1] = null;
            ActivePIFFs[2] = ActiveIff.RuntimeInfo.Patches.FirstOrDefault(x => x.Filename?.EndsWith(".str.piff") == true);

            PIFFButton.Enabled = ActivePIFFs[0] != null;
            SPFButton.Enabled = ActivePIFFs[1] != null;
            STRButton.Enabled = ActivePIFFs[2] != null;

            SelectPIFF(Array.FindIndex(ActivePIFFs, x => x != null));
        }

        public void SelectPIFF(int index)
        {
            if (index == -1) ActivePIFF = null;
            else ActivePIFF = ActivePIFFs[index];

            ActiveIff = (index == 1 ? (ActiveObj as GameObjectResource)?.Sprites : ActiveObj.MainIff) ?? ActiveObj.MainIff;

            Render();
            EntryList.SelectedItem = null;
        }

        public void RenderEmpty()
        {
            PIFFBox.Enabled = false;
        }

        public void Render()
        {
            EntryList.Items.Clear();

            PIFFName.Enabled = ActivePIFF != null;
            PIFFComment.Enabled = ActivePIFF != null;
            if (ActivePIFF != null)
            {
                var piffs = ActivePIFF.List<PIFF>();
                if ((piffs?.Count ?? 0) == 0) {
                    RenderEmpty(); return;
                }
                var piff = piffs[0];
                ActivePIFFChunk = piff;

                PIFFName.Text = ActivePIFF.Filename;
                PIFFComment.Text = piff.Comment;

                PIFFBox.Enabled = true;

                foreach (var entry in piff.Entries)
                {
                    EntryList.Items.Add(new PIFFListItem(entry, ActiveIff));
                }
            } else
            {
                PIFFName.Text = "None";
                PIFFComment.Text = "Make changes and save then using the volcanic main window to view or edit the PIFF.";
            }
        }

        private void EntryList_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActiveItem = EntryList.SelectedItem as PIFFListItem;

            if (ActiveItem == null)
            {
                EntryComment.Text = "";
                EntrySummary.Text = "";
                EntryComment.Enabled = false;
            }
            else
            {
                var entry = ActiveItem.Entry;
                EntryComment.Text = entry.Comment;
                EntrySummary.Text = GetEntrySummary();
                EntryComment.Enabled = true;
            }
        }

        private string GetEntrySummary()
        {
            var to = ActiveItem.Replaced;
            if (to == null || to.OriginalData == null || IgnoreChunks.Contains(to.ChunkType)) return "Change summary not available.";

            Type chunkClass = IffFile.CHUNK_TYPES[to.ChunkType];
            IffChunk newChunk = (IffChunk)Activator.CreateInstance(chunkClass);
            newChunk.ChunkLabel = to.OriginalLabel;
            newChunk.ChunkID = to.OriginalID;
            newChunk.OriginalID = to.OriginalID;
            newChunk.OriginalData = to.OriginalData;
            newChunk.OriginalLabel = to.OriginalLabel;
            using (var str = new MemoryStream(to.OriginalData)) {
                newChunk.Read(to.ChunkParent, str);
            }
            var from = newChunk;
            //instruction mode - load original and new as Routine before we compare fields. (to get operands)

            //default mode - load original and use reflection to compare fields.

            var builder = new StringBuilder();
            if (from is BHAV)
            {
                var froutine = SimAntics.Engine.VMTranslator.INSTANCE.Assemble(from as BHAV, null);
                var troutine = SimAntics.Engine.VMTranslator.INSTANCE.Assemble(to as BHAV, null);
                CompareObject("", froutine, troutine, builder);
            } else
            {
                CompareObject("", from, to, builder);
            }
            
            return builder.ToString();
        }

        private void PrintObject(object obj, StringBuilder builder)
        {
            if (builder == null) return;
            var fromType = obj.GetType();
            if (fromType.IsPrimitive || fromType.Equals(typeof(string)))
            {
                builder.Append(obj.ToString());
                return;
            }
            var fromProps = fromType.GetProperties();
            var fromMembers = fromType.GetFields();

            builder.Append("{ ");

            foreach (var prop in fromProps)
            {
                builder.Append(prop.Name + ": " + (prop.GetValue(obj)?.ToString() ?? "null") + ", ");
            }

            foreach (var prop in fromMembers)
            {
                builder.Append(prop.Name + ": " + (prop.GetValue(obj)?.ToString() ?? "null") + ", ");
            }

            builder.Append("}");
        }

        private bool CompareObject(string depth, object from, object to, StringBuilder builder)
        {
            return CompareObject(depth, from, to, builder, new HashSet<object>());
        }

        private List<object> CollectionToList(ICollection collection)
        {
            var result = new List<object>();
            foreach (var obj in collection)
            {
                result.Add(obj);
            }
            return result;
        }

        private bool CompareObject(string depth, object from, object to, StringBuilder builder, HashSet<object> visitedFrom)
        {
            var fromType = from.GetType();
            if (fromType.IsPrimitive || fromType.Equals(typeof(string)))
            {
                if (!(from?.Equals(to) ?? false))
                {
                    builder?.Append($"{depth} set from ");
                    PrintObject(from, builder);
                    builder?.Append(" to ");
                    PrintObject(to, builder);
                    builder?.AppendLine();
                    return true;
                }
                return false;
            }
            if (visitedFrom.Contains(from)) return false;
            if (from is IList && to is IList)
            {
                return CompareList(depth, from as IList, to as IList, builder);
            }
            if (from is IDictionary && to is IDictionary)
            {
                return CompareDictionary(depth, from as IDictionary, to as IDictionary, builder);
            }
            var fromProps = fromType.GetProperties();
            var fromMembers = fromType.GetFields();

            var combined = Enumerable.Concat<MemberInfo>(fromProps, fromMembers);
            var changed = false;

            foreach (var cprop in combined)
            {
                if (cprop.Name.StartsWith("Chunk")) continue;
                if (IgnoreProps.Contains(cprop.Name)) continue;
                object fromVal = null;
                object toVal = null;
                try
                {
                    fromVal = (cprop as PropertyInfo)?.GetValue(from) ?? (cprop as FieldInfo)?.GetValue(from);
                    toVal = (cprop as PropertyInfo)?.GetValue(to) ?? (cprop as FieldInfo)?.GetValue(to);
                }
                catch
                {
                    continue;
                }
                if (fromVal != null && toVal != null)
                {
                    var newDepth = depth;
                    if (newDepth.Length > 0) newDepth += '.';
                    var type = ((cprop as FieldInfo)?.FieldType) ?? ((cprop as PropertyInfo)?.PropertyType);
                    var realType = fromVal.GetType().Name;
                    string subName = (realType == type.Name) ? cprop.Name : realType;
                    newDepth += subName;

                    if (fromVal.GetType() != toVal.GetType())
                    {
                        builder?.Append($"{newDepth} set from ");
                        PrintObject(fromVal, builder);
                        builder?.Append(" to ");
                        PrintObject(toVal, builder);
                        builder.AppendLine();
                        changed = true;
                    }
                    else
                    {
                        //types are equal.
                        if (from.Equals(to))
                        {

                        }
                        else
                        {

                            CompareObject(newDepth, fromVal, toVal, builder, visitedFrom);
                        }
                    }
                }
                else if (fromVal != null)
                {
                    builder?.Append($"{depth} set to NULL from: ");
                    PrintObject(fromVal, builder);
                    builder?.AppendLine();
                    changed = true;
                }
                else if (toVal != null)
                {
                    builder?.Append($"{depth} set from NULL: ");
                    PrintObject(toVal, builder);
                    builder?.AppendLine();
                    changed = true;
                }
            }
            return changed;
        }

        private bool CompareDictionary(string depth, IDictionary from, IDictionary to, StringBuilder builder)
        {
            var kc = CompareList(depth + "(keys)", CollectionToList(from.Keys), CollectionToList(to.Keys), builder);
            var vc = CompareList(depth + "(values)", CollectionToList(from.Values), CollectionToList(to.Values), builder);
            return kc || vc;
        }

        private bool CompareList(string depth, IList from, IList to, StringBuilder builder)
        {
            var fromCount = from.Count;
            var toCount = to.Count;

            var changed = false;
            var shared = Math.Min(fromCount, toCount);
            var i = 0;
            foreach (var item in from)
            {
                if (i >= shared) break;
                var toItem = to[i];
                //compare items
                if (CompareObject(depth + "[" + i + "]", item, toItem, builder))
                {
                    changed = true;
                }
                ++i;
            }

            if (from.Count > to.Count)
            {
                //removed last in from
                for (int j=shared; j<from.Count; j++)
                {
                    builder?.Append(depth + "[" + j + "] removed: ");
                    PrintObject(from[j], builder);
                    builder?.AppendLine();
                }
                changed = true;
            }
            else
            {
                //added last in to
                for (int j = shared; j < to.Count; j++)
                {
                    builder?.Append(depth + "[" + j + "] added: ");
                    PrintObject(to[j], builder);
                    builder?.AppendLine();
                }
                changed = true;
            }
            return changed;
        }

        private void RegisterChange()
        {
            var changes = Content.Content.Get().Changes;
            changes.IffChanged(ActiveIff);
        }

        private void PIFFComment_TextChanged(object sender, EventArgs e)
        {
            if (ActivePIFFChunk == null) return;
            ActivePIFFChunk.Comment = PIFFComment.Text;
            RegisterChange();
        }

        private void EntryComment_TextChanged(object sender, EventArgs e)
        {
            if (ActiveItem == null) return;
            ActiveItem.Entry.Comment = EntryComment.Text;
            RegisterChange();
        }

        private void PIFFName_TextChanged(object sender, EventArgs e)
        {
            if (ActivePIFF == null) return;
            ActivePIFF.Filename = PIFFName.Text;
            RegisterChange();
        }

        private void PIFFButton_Click(object sender, EventArgs e)
        {
            SelectPIFF(0);
        }

        private void SPFButton_Click(object sender, EventArgs e)
        {
            SelectPIFF(1);
        }

        private void STRButton_Click(object sender, EventArgs e)
        {
            SelectPIFF(2);
        }

        private void EntryDelete_Click(object sender, EventArgs e)
        {
            //revert the changes to this chunk, similar to main window but actually all the way to originaldata
            //we do this by removing the selected patch entry, then reloading the iff.
            if (ActiveItem == null || ActivePIFF == null || ActivePIFFChunk == null) return;
            var result = MessageBox.Show("Deleting a PIFF entry will revert all changes to that chunk, " +
                "but will also reset any unsaved changes you've made to the IFF. This action cannot be reversed, " +
                "as it immediately saves the PIFF. Are you sure you want to do this?",
                "Deleting PIFF Entry", MessageBoxButtons.YesNo);
            if (result == DialogResult.No) return;

            //remove the current entry
            var entries = ActivePIFFChunk.Entries.ToList();
            entries.Remove(ActiveItem.Entry);
            ActivePIFFChunk.Entries = entries.ToArray();
            if (ActiveItem.Entry.EntryType == PIFFEntryType.Add)
            {
                ActiveIff.RemoveChunk(ActiveItem.Replaced);
            }

            var changes = Content.Content.Get().Changes;
            changes.DiscardChange(ActiveIff);
            RegisterChange();
            //changes.SaveChange(ActiveIff);
            Render();
        }

        private void FolderSave(SaveFileDialog dialog)
        {
            // ༼ つ ◕_◕ ༽つ IMPEACH STAThread ༼ つ ◕_◕ ༽つ
            var wait = new AutoResetEvent(false);
            var thread = new Thread(() => {
                dialog.ShowDialog();
                wait.Set();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            wait.WaitOne();
            return;
        }

        private void TrySaveIff(IffFile file)
        {
            var dialog = new SaveFileDialog();
            dialog.FileName = file.Filename;
            dialog.Title = "Saving full IFF " + file.Filename + "...";
            FolderSave(dialog);

            Stream str;
            if ((str = dialog.OpenFile()) != null)
            {
                file.Write(str);
                str.Close();
            }
        }

        private void SaveIff_Click(object sender, EventArgs e)
        {
            TrySaveIff(ActiveObj.MainIff);
            var spr = (ActiveObj as GameObjectResource)?.Sprites;
            if (spr != null) TrySaveIff(spr);
        }
    }

    public class PIFFListItem
    {
        public PIFFEntry Entry;
        public IffChunk Replaced;
        public string Name;

        public PIFFListItem(PIFFEntry entry, IffFile target)
        {
            Entry = entry;
            if (entry.ChunkLabel != "") Name = entry.ChunkLabel;
            else
            {
                //try find the original to get name
                var chunks = target.SilentListAll();
                Replaced = chunks.FirstOrDefault(chunk => chunk.ChunkType == entry.Type && chunk.OriginalID == entry.ChunkID);
                if (Replaced == null) Name = "UNKNOWN";
                else Name = Replaced.OriginalLabel;
            }
        }

        public override string ToString()
        {
            return $"({Entry.Type} {Entry.ChunkID}) {Name} - {Entry.EntryType.ToString()}";
        }
    }
}
