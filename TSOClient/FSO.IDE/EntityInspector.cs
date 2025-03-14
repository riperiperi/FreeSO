using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FSO.SimAntics;
using FSO.SimAntics.NetPlay.Model.Commands;

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

            EntityView.BeginUpdate();
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

            EntityView.EndUpdate();
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
                    MultitileLead = entity.MultitileGroup.BaseObject?.ObjectID ?? 0,
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
            HookedVM.Scheduler.RescheduleInterrupt(entity);
            entity.Thread.ThreadBreak = SimAntics.Engine.VMThreadBreakMode.Immediate;
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (EntityView.SelectedIndices == null || EntityView.SelectedIndices.Count == 0) return;
            foreach (ListViewItem item in EntityView.SelectedItems)
            {
                // Send a delete command in addition to deleting the object locally to allow deletion by admin clients.
                HookedVM.SendCommand(new VMNetDeleteObjectCmd()
                {
                    ObjectID = ItemToEnt[item].ID,
                    CleanupAll = true
                });
                var ent = ItemToEnt[item];
                Content.Content.Get().Changes.Invoke((Action<bool, VMContext>)ent.Entity.Delete, true, ent.Entity.Thread.Context);
            }
            RefreshView();
        }

        private void OpenResource_Click(object sender, EventArgs e)
        {
            if (EntityView.SelectedIndices == null || EntityView.SelectedIndices.Count == 0) return;
            foreach (ListViewItem item in EntityView.SelectedItems)
            {
                MainWindow.Instance.IffManager.OpenResourceWindow(ItemToEnt[item].Entity.Object);
            }
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
