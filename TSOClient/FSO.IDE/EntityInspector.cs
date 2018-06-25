using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FSO.SimAntics;
using FSO.Common.Utils;
using System.Threading;
using System.Diagnostics;

namespace FSO.IDE
{
    public partial class EntityInspector : UserControl
    {
        private VM HookedVM;
        private List<InspectorEntityMeta> EntityList;
        private Dictionary<ListViewItem, InspectorEntityMeta> ItemToEnt;

        public EntityInspector()
        {
            InitializeComponent();
        }

        public void ChangeVM(VM target)
        {
            HookedVM = target;
            RefreshView();
        }

        public void RefreshView()
        {
            if (HookedVM == null) return; //?

            EntityList = new List<InspectorEntityMeta>();
            ItemToEnt = new Dictionary<ListViewItem, InspectorEntityMeta>();
            if (!Program.MainThread.IsAlive)
            {
                Application.Exit();
                return;
            }
            Content.Content.Get().Changes.Invoke((Action<List<InspectorEntityMeta>>)GetEntityList, EntityList);
            EntityView.Items.Clear();

            foreach (var ent in EntityList)
            {
                var item = new ListViewItem(
                    new string[] { ent.ID.ToString(), ent.Name, ent.MultitileLead.ToString(),
                        (ent.Container>0)?ent.Container.ToString():"", (ent.Container>0)?ent.Slot.ToString():""
                });
                EntityView.Items.Add(item);
                ItemToEnt.Add(item, ent);
            }

        }

        public void GetEntityList(List<InspectorEntityMeta> list)
        {
            if (HookedVM == null) return;
            foreach (var entity in HookedVM.Entities)
            {
                list.Add(new InspectorEntityMeta
                {
                    Entity = entity,
                    ID = entity.ObjectID,
                    Name = entity.ToString(),
                    MultitileLead = entity.MultitileGroup.BaseObject.ObjectID,
                    Container = (entity.Container == null) ? (short)0 : entity.Container.ObjectID,
                    Slot = entity.ContainerSlot
                });
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            RefreshView();
        }

        private void EntityView_DoubleClick(object sender, EventArgs e)
        {
            if (EntityView.SelectedIndices == null || EntityView.SelectedIndices.Count == 0) return;
            var item = ItemToEnt[EntityView.SelectedItems[0]];

            Content.Content.Get().Changes.Invoke((Action<VMEntity>)SetBreak, item.Entity);
        }

        private void SetBreak(VMEntity entity)
        {
            entity.Thread.ThreadBreak = SimAntics.Engine.VMThreadBreakMode.Immediate;
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (EntityView.SelectedIndices == null || EntityView.SelectedIndices.Count == 0) return;
            var item = ItemToEnt[EntityView.SelectedItems[0]];
            Content.Content.Get().Changes.Invoke((Action<bool, VMContext>)item.Entity.Delete, true, item.Entity.Thread.Context);
            RefreshView();
        }
    }

    public class InspectorEntityMeta {
        public VMEntity Entity;
        public short ID;
        public string Name;
        public short MultitileLead;
        public short Container;
        public short Slot;
    }
}
