using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using tso.simantics;
using tso.files.formats.iff.chunks;
using tso.debug.content.preview;

namespace tso.debug
{
    public partial class Simantics : Form
    {
        private VM vm;
        private VMEntity ActiveEntity;


        public Simantics(VM vm)
        {
            this.vm = vm;
            InitializeComponent();

            ActiveEntity = vm.Entities.FirstOrDefault(x => x is VMAvatar);

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
        private void SetSelected(VMEntity entity){
            SelectedEntity = entity;
            propertyGrid.SelectedObject = entity;

            bhavList.Items.Clear();
            var resource = entity.Object;
            var bhavs = resource.Resource.List<BHAV>();
            foreach (var bhav in bhavs){
                bhavList.Items.Add(bhav);
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
                ActiveEntity.Thread.EnqueueAction(new tso.simantics.engine.VMQueuedAction() {
                    Routine = vm.Assemble(bhav),
                    Callee = SelectedEntity
                });
            }
        }
    }
}
