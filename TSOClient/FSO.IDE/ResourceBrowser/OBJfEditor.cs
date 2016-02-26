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
using FSO.IDE.EditorComponent;

namespace FSO.IDE.ResourceBrowser
{
    public partial class OBJfEditor : UserControl
    {
        private static string[] FunctionOBJDNames = new string[]
        {
            "BHAV_Init",
            "BHAV_MainID",
            "BHAV_Load",
            "BHAV_Cleanup",
            "BHAV_QueueSkipped",
            "BHAV_AllowIntersectionID",
            "BHAV_WallAdjacencyChanged",
            "BHAV_RoomChange",
            "BHAV_DynamicMultiTileUpdate",
            "BHAV_Place",
            "BHAV_Pickup",
            "BHAV_UserPlace",
            "BHAV_UserPickup",
            "BHAV_LevelInfo",
            "BHAV_ServingSurface",
            "BHAV_Portal", //portal
            "BHAV_GardeningID",
            "BHAV_WashHandsID",
            "BHAV_PrepareFoodID",
            "BHAV_CookFoodID",
            "BHAV_PlaceSurfaceID",
            "BHAV_DisposeID",
            "BHAV_EatID",
            "BHAV_PickupFromSlotID",
            "BHAV_WashDishID",
            "BHAV_EatSurfaceID",
            "BHAV_SitID",
            "BHAV_StandID",
            "BHAV_Clean",
            "BHAV_Repair",
            "",
            "",
            ""
        };

        private GameObject ActiveObject;
        private bool IsOBJDFunc;
        private OBJf ActiveOBJf;
        private Dictionary<ListViewItem, int> itemToOffset = new Dictionary<ListViewItem, int>();

        public OBJfEditor()
        {
            InitializeComponent();
        }

        public void SetActiveObject(GameObject active)
        {
            ActiveObject = active;
            ActiveOBJf = null;
            if (active.OBJ.UsesFnTable > 0) ActiveOBJf = ActiveObject.Resource.Get<OBJf>(ActiveObject.OBJ.ChunkID);
            IsOBJDFunc = (ActiveOBJf == null);

            TableCombo.SelectedIndex = (IsOBJDFunc) ? 0 : 1;
            RefreshView();
        }

        public void RefreshView()
        {
            int oldSel = (FunctionList.SelectedIndices.Count == 0)?0:Math.Max(0, FunctionList.SelectedIndices[0]);
            FunctionList.Items.Clear();
            itemToOffset.Clear();
            var funcNames = EditorScope.Behaviour.Get<STR>(245);

            var functions = GetFunctionTable();
            int i = 0;
            foreach (var func in functions)
            {
                if (func.ActionFunction == 0 && FilterCheck.Checked)
                {
                    i++;
                    continue;
                }

                var action = GetBHAV(func.ActionFunction);
                var check = GetBHAV(func.ConditionFunction);

                var item = new ListViewItem(new string[]
                {
                    funcNames.GetString(i),
                    (func.ActionFunction == 0)?"":((action == null)?("#"+func.ActionFunction):action.ChunkLabel),
                    (func.ConditionFunction == 0)?"":((check == null)?("#"+func.ConditionFunction):check.ChunkLabel),
                });
                itemToOffset.Add(item, i++);
                FunctionList.Items.Add(item);
            }

            FunctionList.Items[Math.Min(FunctionList.Items.Count-1, oldSel)].Selected = true;
        }

        private BHAV GetBHAV(ushort id)
        {
            if (id == 0) return null;
            if (id >= 8192 && ActiveObject != null) return ActiveObject.Resource.SemiGlobal.Get<BHAV>(id); //semiglobal
            else if (id >= 4096) return ActiveObject.Resource.Get<BHAV>(id); //private
            else return EditorScope.Globals.Resource.Get<BHAV>(id); //global
        }

        public void SetAction(int function, ushort id)
        {
            if (IsOBJDFunc)
            {
                if (FunctionOBJDNames[function] == "") return;

                Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
                {
                    ActiveObject.OBJ.SetPropertyByName(FunctionOBJDNames[function], id);
                }, ActiveObject.OBJ));
            } else
            {
                Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
                {
                    ActiveOBJf.functions[function].ActionFunction = id;
                }, ActiveOBJf));
            }
            RefreshView();
        }

        public void SetCheck(int function, ushort id)
        {
            if (IsOBJDFunc)
            {
                return;
            }
            else
            {
                Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
                {
                    ActiveOBJf.functions[function].ConditionFunction = id;
                }, ActiveOBJf));
            }
            RefreshView();
        }

        public OBJfFunctionEntry[] GetFunctionTable()
        {
            if (IsOBJDFunc)
            {
                var result = new OBJfFunctionEntry[FunctionOBJDNames.Length];
                for (int i=0; i<FunctionOBJDNames.Length; i++)
                {
                    if (FunctionOBJDNames[i] == "") continue;
                    result[i] = new OBJfFunctionEntry
                    {
                        ActionFunction = ActiveObject.OBJ.GetPropertyByName<ushort>(FunctionOBJDNames[i])
                    };
                }
                return result;
            } else
            {
                return ActiveObject.Resource.Get<OBJf>(ActiveObject.OBJ.ChunkID).functions;
            }
        }

        private void ActionButton_Click(object sender, EventArgs e)
        {
            if (FunctionList.SelectedItems.Count == 0) return;
            var func = itemToOffset[FunctionList.SelectedItems[0]];

            var dialog = new SelectTreeDialog(ActiveObject.Resource);
            dialog.ShowDialog();
            if (dialog.DialogResult == DialogResult.OK) SetAction(func, dialog.ResultID);
        }

        private void CheckButton_Click(object sender, EventArgs e)
        {
            if (FunctionList.SelectedItems.Count == 0) return;
            var func = itemToOffset[FunctionList.SelectedItems[0]];

            var dialog = new SelectTreeDialog(ActiveObject.Resource);
            dialog.ShowDialog();
            if (dialog.DialogResult == DialogResult.OK) SetCheck(func, dialog.ResultID);
        }

        private void FilterCheck_CheckedChanged(object sender, EventArgs e)
        {
            RefreshView();
        }

        private void FunctionList_SelectedIndexChanged(object sender, EventArgs e)
        {
            CheckButton.Enabled = !IsOBJDFunc;
            DescLabel.Text = "No Description Available."; //todo
        }

        private void TableCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            //todo
        }
    }
}
