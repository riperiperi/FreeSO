using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FSO.SimAntics;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Debug.content.preview;
using FSO.SimAntics.Engine;

namespace FSO.Debug
{
    public partial class Simantics : Form
    {
        private VM vm;
        private VMEntity ActiveEntity;
        private ActionQueue aq;

        public Simantics(VM vm)
        {
            this.vm = vm;
            InitializeComponent();

            ActiveEntity = vm.Entities.FirstOrDefault(x => x is VMAvatar);

            aq = new ActionQueue();
            aq.Show();
            aq.target = ActiveEntity;
        }

        public void UpdateAQLocation() {
            aq.Location = new System.Drawing.Point(Location.X, Location.Y + Height);
        }

        private void RefreshEntityList(){
            var items = new List<VMEntity>();
            var all = vm.Entities;

            var showSims = menuShowSims.Checked;
            var showObjects = menuShowObjects.Checked;
            var search = textBox1.Text.ToLower();

            foreach (var entity in all){
                if (entity is VMAvatar){
                    if (!showSims) { continue; }
                }else if (entity is VMGameObject){
                    if (!showObjects) { continue; }
                }

                if (search.Length > 0){
                    var toString = entity.ToString().ToLower();
                    if (toString.IndexOf(search) == -1){
                        continue;
                    }
                }
                items.Add(entity);
            }

            entityList.Items.Clear();
            foreach(var item in items){
                entityList.Items.Add(item);
            }
        }

        private void Simantics_Load(object sender, EventArgs e)
        {
            RefreshEntityList();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            RefreshEntityList();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            RefreshEntityList();
        }

        private void entityList_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetSelected((VMEntity)entityList.SelectedItem);
        }

        private VMEntity SelectedEntity;
        private TTAB TreeTableSel;
        private void SetSelected(VMEntity entity){
            SelectedEntity = entity;
            propertyGrid.SelectedObject = entity;

            bhavList.Items.Clear();
            var resource = entity.Object;
            var bhavs = resource.Resource.List<BHAV>();
            if (bhavs != null)
            {
                foreach (var bhav in bhavs)
                {
                    bhavList.Items.Add(bhav);
                }
            }

            if (entity.SemiGlobal != null)
            {
                var sglobbhavs = entity.SemiGlobal.List<BHAV>();
                if (bhavs != null)
                {
                    foreach (var bhav in sglobbhavs)
                    {
                        bhavList.Items.Add(bhav);
                    }
                }
            }

            interactionList.Items.Clear();
            if (entity.TreeTable != null)
            {
                TreeTableSel = entity.TreeTable;
                foreach (var interaction in entity.TreeTable.Interactions)
                {
                    interactionList.Items.Add(entity.TreeTableStrings.GetString((int)interaction.TTAIndex));
                }
            }
        }

        private void menuShowSims_Click(object sender, EventArgs e)
        {
            menuShowSims.Checked = !menuShowSims.Checked;
            RefreshEntityList();
        }

        private void menuShowObjects_Click(object sender, EventArgs e)
        {
            menuShowObjects.Checked = !menuShowObjects.Checked;
            RefreshEntityList();
        }

        private void btnInspectBhav_Click(object sender, EventArgs e)
        {
            var bhav = (BHAV)bhavList.SelectedItem;
            if (bhav != null)
            {
                new VMRoutineInspector(vm.Assemble(bhav)).Show();
            }
        }

        private void bhavExecuteBtn_Click(object sender, EventArgs e){
            var bhav = (BHAV)bhavList.SelectedItem;
            if (bhav != null)
            {
                ActiveEntity.Thread.EnqueueAction(new FSO.SimAntics.Engine.VMQueuedAction() {
                    Routine = vm.Assemble(bhav),
                    Callee = SelectedEntity,
                    StackObject = SelectedEntity,
                    CodeOwner = SelectedEntity.Object,
                    Priority = VMQueuePriority.UserDriven
                });
            }
        }

        private void TTABExecute_Click(object sender, EventArgs e)
        {
            var interaction = TreeTableSel.Interactions[interactionList.SelectedIndex];
            var ActionID = interaction.ActionFunction;
            BHAV bhav;
            FSO.Content.GameIffResource CodeOwner;

            if (ActionID < 4096)
            { //global
                bhav = null;
                //unimp as it has to access the context to get this.
            }
            else if (ActionID < 8192)
            { //local
                bhav = SelectedEntity.Object.Resource.Get<BHAV>(ActionID);
                CodeOwner = SelectedEntity.Object.Resource;
            }
            else
            { //semi-global
                bhav = SelectedEntity.SemiGlobal.Get<BHAV>(ActionID);
                CodeOwner = SelectedEntity.SemiGlobal;
            }

            if (bhav != null)
            {
                ActiveEntity.Thread.EnqueueAction(new VMQueuedAction()
                {
                    Routine = vm.Assemble(bhav),
                    Callee = SelectedEntity,
                    StackObject = SelectedEntity,
                    CodeOwner = SelectedEntity.Object,
                    Name = (string)interactionList.SelectedItem,
                    InteractionNumber = (int)interaction.TTAIndex, //interactions are referenced by their tta index
                    Priority = VMQueuePriority.UserDriven
                });
            }
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            ActiveEntity = SelectedEntity;
            aq.target = ActiveEntity;
        }
    }
}
