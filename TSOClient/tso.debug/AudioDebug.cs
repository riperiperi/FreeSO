using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using tso.sound;
using tso.content;
using tso.content.model;

namespace tso.debug
{
    public partial class AudioDebug : Form
    {
        private AudioEngine Engine = new AudioEngine();


        public AudioDebug()
        {
            InitializeComponent();
        }

        private void AudioDebug_Load(object sender, EventArgs e)
        {
            RefreshTrackList();
        }


        private void RefreshTrackList()
        {
            var files = Content.Get().Audio.List();
            var showStations = radioStationsToolStripMenuItem.Checked;

            var items = new List<AudioReference>();
            foreach (var file in files){
                switch (file.Type)
                {
                    case tso.content.model.AudioType.RADIO_STATION:
                        if (!showStations) { continue; }
                        break;
                }
                items.Add(file);
            }
            trackGrid.DataSource = items;
        }











        private void radioStationsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void btnPlayTrack_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow item in trackGrid.SelectedRows)
            {
                var audioItem = (AudioReference)item.DataBoundItem;
                Engine.PlayMusic(audioItem);
            }
        }
    }
}
