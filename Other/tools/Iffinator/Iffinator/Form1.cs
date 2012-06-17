/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the Iffinator.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): Nicholas Roth & Propeng.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Iffinator.Flash;
using LogThis;

namespace Iffinator
{
    public partial class Form1 : Form
    {
        private Flash.Iff m_CurrentArchive;
        private SPRParser m_CurrentSPR;
        private SPR2Parser m_CurrentSPR2;
        private DrawGroup m_CurrentGroup;
        private int m_CurrentSPRFrame, m_CurrentSPR2Frame, m_CurrentGroupFrame = 0;

        public Form1()
        {
            InitializeComponent();

            LstSPR2s.Click += new EventHandler(LstSPR2s_Click);

            //The Log class is imported through SimsLib...
            Log.UseSensibleDefaults("Log.txt", "", eloglevel.info);
        }

        /// <summary>
        /// User clicked on the list of sprites.
        /// </summary>
        private void LstSPR2s_Click(object sender, EventArgs e)
        {
            if (m_CurrentArchive != null)
            {
                if (RdiSPR.Checked)
                {
                    string Caption = (string)LstSPR2s.SelectedItem;
                    int ID = 0;
                    string Name = "";

                    if (Caption.Contains("ID: "))
                        ID = int.Parse(Caption.Replace("ID: ", ""));
                    else
                        Name = Caption.Replace("Name: ", "");

                    foreach (SPRParser Sprite in m_CurrentArchive.SPRs)
                    {
                        if (ID != 0)
                        {
                            if (Sprite.ID == ID)
                            {
                                m_CurrentSPR = Sprite;
                                PictCurrentFrame.Image = m_CurrentSPR.GetFrame(m_CurrentSPR2Frame).BitmapData.BitMap;
                                break;
                            }
                        }
                        else
                        {
                            if (Sprite.NameString == Name)
                            {
                                m_CurrentSPR = Sprite;
                                PictCurrentFrame.Image = m_CurrentSPR.GetFrame(m_CurrentSPR2Frame).BitmapData.BitMap;
                                break;
                            }
                        }
                    }
                }
                else if (RdiSpr2.Checked)
                {
                    string Caption = (string)LstSPR2s.SelectedItem;
                    int ID = 0;
                    string Name = "";

                    if (Caption.Contains("ID: "))
                        ID = int.Parse(Caption.Replace("ID: ", ""));
                    else
                        Name = Caption.Replace("Name: ", "");

                    foreach (SPR2Parser Sprite in m_CurrentArchive.SPR2s)
                    {
                        if (ID != 0)
                        {
                            if (Sprite.ID == ID)
                            {
                                m_CurrentSPR2 = Sprite;
                                PictCurrentFrame.Image = m_CurrentSPR2.GetFrame(m_CurrentSPR2Frame).BitmapData.BitMap;
                                break;
                            }
                        }
                        else
                        {
                            if (Sprite.NameString == Name)
                            {
                                m_CurrentSPR2 = Sprite;
                                PictCurrentFrame.Image = m_CurrentSPR2.GetFrame(m_CurrentSPR2Frame).BitmapData.BitMap;
                                break;
                            }
                        }
                    }
                }
                else if (RdiStr.Checked)
                {
                    if (m_CurrentArchive.StringTables.Count > 0)
                    {
                        string Str = (string)LstSPR2s.SelectedItem;
                        Str = Str.Replace("String: ", "");
                        Str = Str.Replace("String2: ", "");

                        MessageBox.Show(Str);
                    }
                }
                else if (RdiBhavs.Checked)
                {
                    if (m_CurrentArchive.BHAVs.Count > 0)
                    {
                        BHAVEdit Editor = new BHAVEdit(m_CurrentArchive, m_CurrentArchive.BHAVs[LstSPR2s.SelectedIndex]);
                        Editor.Show();
                    }
                }
                else if (RdiDgrp.Checked)
                {
                    string Caption = (string)LstSPR2s.SelectedItem;
                    int ID = 0;
                    string Name = "";

                    if (Caption.Contains("ID: "))
                        ID = int.Parse(Caption.Replace("ID: ", ""));
                    else
                        Name = Caption.Replace("Name: ", "");

                    foreach (DrawGroup DGRP in m_CurrentArchive.DrawGroups)
                    {
                        if (ID != 0)
                        {
                            if (DGRP.ID == ID)
                            {
                                m_CurrentGroup = DGRP;
                                if (m_CurrentGroupFrame < m_CurrentGroup.ImageCount)
                                    PictCurrentFrame.Image = m_CurrentGroup.GetImage(m_CurrentGroupFrame).CompiledBitmap;
                                else
                                {
                                    m_CurrentGroupFrame = m_CurrentGroup.ImageCount - 1;
                                    PictCurrentFrame.Image = m_CurrentGroup.GetImage(m_CurrentGroupFrame).CompiledBitmap;
                                }
                                break;
                            }
                        }
                        else
                        {
                            if (DGRP.NameString == Name)
                            {
                                m_CurrentGroup = DGRP;
                                if (m_CurrentGroupFrame < m_CurrentGroup.ImageCount)
                                    PictCurrentFrame.Image = m_CurrentGroup.GetImage(m_CurrentGroupFrame).CompiledBitmap;
                                else
                                {
                                    m_CurrentGroupFrame = m_CurrentGroup.ImageCount - 1;
                                    PictCurrentFrame.Image = m_CurrentGroup.GetImage(m_CurrentGroupFrame).CompiledBitmap;
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void openiffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog OFD = new OpenFileDialog();
            OFD.Filter = "Iff files (*.iff)|*.iff|Floor files (*.flr)|*.flr|Wall files (*.wll)|*.wll|SPF files (*.spf)|*.spf";
            OFD.Title = "Open Iff Archive...";
            OFD.AddExtension = true;

            m_CurrentSPRFrame = 0;
            m_CurrentSPR2Frame = 0;
            m_CurrentGroupFrame = 0;

            if (OFD.ShowDialog() == DialogResult.OK)
            {
                m_CurrentArchive = new Iffinator.Flash.Iff(OFD.FileName);
                
                LblNumChunks.Visible = true;
                LblNumChunks.Text = "Number of chunks: " + m_CurrentArchive.Chunks.Count;

                if (m_CurrentArchive.SPR2s.Count > 0)
                {
                    m_CurrentSPR2 = m_CurrentArchive.GetSprite(0);
                    PictCurrentFrame.Image = m_CurrentSPR2.GetFrame(m_CurrentSPR2Frame).BitmapData.BitMap;
                }

                LstSPR2s.Items.Clear();

                /*foreach (Flash.IffChunk Chunk in m_CurrentArchive.Chunks)
                {
                    if (Chunk.Resource == "SPR2")
                        LstSPR2s.Items.Add("ID: " + Chunk.ID);
                }*/
            }
        }

        /// <summary>
        /// User clicked on the "Extract *.iff" menu item...
        /// </summary>
        private void extractiffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_CurrentArchive != null)
            {
                FolderBrowserDialog FBD = new FolderBrowserDialog();
                FBD.Description = "Select the folder to extract to...";
                FBD.ShowNewFolderButton = true;

                if (FBD.ShowDialog() == DialogResult.OK)
                {
                    foreach (IffChunk Chunk in m_CurrentArchive.Chunks)
                    {
                        BinaryWriter Writer = new BinaryWriter(File.Create(FBD.SelectedPath + "\\" + 
                            Chunk.ID + "." + Chunk.Resource));
                        Writer.Write(Chunk.Data);
                        Writer.Close();
                    }
                }
            }
            else
                MessageBox.Show("Please open an *.iff archive first!");
        }

        /// <summary>
        /// User clicked on the button to view the previous frame in a SPR2 or DGRP chunk.
        /// </summary>
        private void BtnPrevFrame_Click(object sender, EventArgs e)
        {
            if (m_CurrentArchive != null)
            {
                if (RdiSPR.Checked)
                {
                    m_CurrentSPRFrame--;

                    if (m_CurrentArchive.SPRs.Count > 1)
                    {
                        if (m_CurrentSPRFrame < m_CurrentSPR.FrameCount && m_CurrentSPRFrame >= 0)
                        {
                            if (m_CurrentSPR.FrameCount > 1)
                            {
                                PictCurrentFrame.Image = m_CurrentSPR.GetFrame(m_CurrentSPRFrame).BitmapData.BitMap;
                            }
                        }
                        else if (m_CurrentSPRFrame < 0)
                            m_CurrentSPRFrame = 0;
                        else if (m_CurrentSPRFrame > m_CurrentSPR.FrameCount)
                            m_CurrentSPRFrame = (int)m_CurrentSPR.FrameCount - 1;
                    }
                }
                else if (RdiSpr2.Checked)
                {
                    m_CurrentSPR2Frame--;

                    if (m_CurrentArchive.SPR2s.Count > 1)
                    {
                        if (m_CurrentSPR2Frame < m_CurrentSPR2.FrameCount && m_CurrentSPR2Frame >= 0)
                        {
                            if (m_CurrentSPR2.FrameCount > 1)
                            {
                                PictCurrentFrame.Image = m_CurrentSPR2.GetFrame(m_CurrentSPR2Frame).BitmapData.BitMap;
                            }
                        }
                        else if (m_CurrentSPR2Frame < 0)
                            m_CurrentSPR2Frame = 0;
                        else if (m_CurrentSPR2Frame > m_CurrentSPR2.FrameCount)
                            m_CurrentSPR2Frame = (int)m_CurrentSPR2.FrameCount - 1;
                    }
                }
                else if (RdiDgrp.Checked)
                {
                    m_CurrentGroupFrame--;

                    if (m_CurrentArchive.DrawGroups.Count > 1)
                    {
                        if (m_CurrentGroupFrame < m_CurrentGroup.ImageCount && m_CurrentGroupFrame >= 0 && m_CurrentGroup.ImageCount > 1)
                        {
                            PictCurrentFrame.Image = m_CurrentGroup.GetImage(m_CurrentGroupFrame).CompiledBitmap;
                        }
                    }
                    else if (m_CurrentGroupFrame < 0)
                        m_CurrentGroupFrame = 0;
                    else if (m_CurrentGroupFrame > m_CurrentGroup.ImageCount)
                        m_CurrentGroupFrame = (int)m_CurrentGroup.ImageCount - 1;
                }
            }
        }

        /// <summary>
        /// User clicked on the button to view the next frame in a SPR2 or DGRP chunk.
        /// </summary>
        private void BtnNextFrame_Click(object sender, EventArgs e)
        {
            if (m_CurrentArchive != null)
            {
                if (RdiSPR.Checked)
                {
                    m_CurrentSPRFrame++;

                    if (m_CurrentArchive.SPRs.Count > 0)
                    {
                        if (m_CurrentSPRFrame < m_CurrentSPR.FrameCount)
                        {
                            if (m_CurrentSPR.FrameCount > 0)
                            {
                                PictCurrentFrame.Image = m_CurrentSPR.GetFrame(m_CurrentSPRFrame).BitmapData.BitMap;
                            }
                        }
                        else if (m_CurrentSPR2Frame < 0)
                            m_CurrentSPRFrame = 0;
                        else if (m_CurrentSPRFrame > m_CurrentSPR.FrameCount)
                            m_CurrentSPRFrame = (int)m_CurrentSPR.FrameCount - 1;
                    }
                }
                else if (RdiSpr2.Checked)
                {
                    m_CurrentSPR2Frame++;

                    if (m_CurrentArchive.SPR2s.Count > 0)
                    {
                        if (m_CurrentGroupFrame < m_CurrentSPR2.FrameCount)
                        {
                            if (m_CurrentSPR2.FrameCount > 0)
                            {
                                PictCurrentFrame.Image = m_CurrentSPR2.GetFrame(m_CurrentSPR2Frame).BitmapData.BitMap;
                            }
                        }
                        else if (m_CurrentGroupFrame < 0)
                            m_CurrentGroupFrame = 0;
                        else if (m_CurrentGroupFrame > m_CurrentSPR2.FrameCount)
                            m_CurrentGroupFrame = (int)m_CurrentSPR2.FrameCount - 1;
                    }
                }
                else if (RdiDgrp.Checked)
                {
                    m_CurrentGroupFrame++;

                    if (m_CurrentArchive.DrawGroups.Count > 0)
                    {
                        if (m_CurrentGroup == null)
                            m_CurrentGroup = m_CurrentArchive.DrawGroups[LstSPR2s.SelectedIndex];

                        if (m_CurrentGroupFrame < m_CurrentGroup.ImageCount && m_CurrentGroup.ImageCount > 0)
                        {
                            PictCurrentFrame.Image = m_CurrentGroup.GetImage(m_CurrentGroupFrame).CompiledBitmap;
                        }
                    }
                    else if (m_CurrentGroupFrame < 0)
                        m_CurrentGroupFrame = 0;
                    else if (m_CurrentGroupFrame > m_CurrentGroup.ImageCount)
                        m_CurrentGroupFrame = (int)m_CurrentGroup.ImageCount - 1;
                }
            }
        }

        /// <summary>
        /// User clicked on the STR#s radiobutton, in order to view the stringtables
        /// in the iff.
        /// </summary>
        private void RdiStr_CheckedChanged(object sender, EventArgs e)
        {
            if (RdiStr.Checked)
            {
                LstSPR2s.Items.Clear();

                if (m_CurrentArchive != null)
                {
                    foreach (StringTable StrTb in m_CurrentArchive.StringTables)
                    {
                        LstSPR2s.Items.Add("ID: " + StrTb.ID);

                        foreach (StringSet Set in StrTb.StringSets)
                        {
                            foreach (StringTableString Str in Set.Strings)
                            {
                                LstSPR2s.Items.Add("String: " + Str.Str);

                                if(Str.Str2 != "")
                                    LstSPR2s.Items.Add("String2: " + Str.Str2);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// User clicked on the BHAVs radiobutton, in order to view the BHAVs
        /// in the iff.
        /// </summary>
        private void RdiBhavs_CheckedChanged(object sender, EventArgs e)
        {
            if (RdiBhavs.Checked)
            {
                LstSPR2s.Items.Clear();

                if (m_CurrentArchive != null)
                {
                    foreach (BHAV Behavior in m_CurrentArchive.BHAVs)
                    {
                        if (Behavior.NameString != "")
                            LstSPR2s.Items.Add("Name: " + Behavior.NameString);
                        else
                            LstSPR2s.Items.Add("ID: " + Behavior.ChunkID);
                    }
                }
            }
        }

        /// <summary>
        /// User clicked on the DGRPs radiobutton.
        /// </summary>
        private void RdiDgrp_CheckedChanged(object sender, EventArgs e)
        {
            if (RdiDgrp.Checked)
            {
                LstSPR2s.Items.Clear();

                if (m_CurrentArchive != null)
                {
                    foreach (Flash.DrawGroup DGroup in m_CurrentArchive.DrawGroups)
                    {
                        if (DGroup.NameString != "")
                            LstSPR2s.Items.Add("Name: " + DGroup.NameString);
                        else
                            LstSPR2s.Items.Add("ID: " + DGroup.ID);
                    }
                }
            }
        }

        /// <summary>
        /// User clicked on the SPR2#s radiobutton, in order to view the sprites
        /// in the iff.
        /// </summary>
        private void RdiSpr2_CheckedChanged(object sender, EventArgs e)
        {
            if (RdiSpr2.Checked)
            {
                LstSPR2s.Items.Clear();

                if (m_CurrentArchive != null)
                {
                    foreach (Flash.SPR2Parser SPR2 in m_CurrentArchive.SPR2s)
                    {
                        if (SPR2.NameString != "")
                            LstSPR2s.Items.Add("Name: " + SPR2.NameString);
                        else
                            LstSPR2s.Items.Add("ID: " + SPR2.ID);
                    }
                }
            }
        }

        /// <summary>
        /// User clicked on the SPR#s radiobutton, in order to view the sprites
        /// in the iff.
        /// </summary>
        private void RdiSPR_CheckedChanged(object sender, EventArgs e)
        {
            if (RdiSPR.Checked)
            {
                LstSPR2s.Items.Clear();

                if (m_CurrentArchive != null)
                {
                    foreach (Flash.SPRParser SPR in m_CurrentArchive.SPRs)
                    {
                        if (SPR.NameString != "")
                            LstSPR2s.Items.Add("Name: " + SPR.NameString);
                        else
                            LstSPR2s.Items.Add("ID: " + SPR.ID);
                    }
                }
            }
        }

        /// <summary>
        /// User clicked on a different SPR2 or DGRP chunk.
        /// </summary>
        private void LstSPR2s_SelectedValueChanged(object sender, EventArgs e)
        {
            if (RdiSPR.Checked)
            {
                m_CurrentSPR = m_CurrentArchive.SPRs[LstSPR2s.SelectedIndex];
                PictCurrentFrame.Image = m_CurrentSPR.GetFrame(0).BitmapData.BitMap;
                m_CurrentSPRFrame = 0;
            }
            else if (RdiSpr2.Checked)
            {
                m_CurrentSPR2 = m_CurrentArchive.SPR2s[LstSPR2s.SelectedIndex];
                PictCurrentFrame.Image = m_CurrentSPR2.GetFrame(0).BitmapData.BitMap;
                m_CurrentSPR2Frame = 0;
            }
            else if(RdiDgrp.Checked)
            {
                m_CurrentGroup = m_CurrentArchive.DrawGroups[LstSPR2s.SelectedIndex];
                PictCurrentFrame.Image = m_CurrentGroup.GetImage(0).CompiledBitmap;
                m_CurrentGroupFrame = 0;
            }
        }

        /// <summary>
        /// User clicked the "Extract Image Sprites" menu item.
        /// </summary>
        private void extractImageSpritesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_CurrentArchive != null)
            {
                FolderBrowserDialog FBD = new FolderBrowserDialog();
                FBD.Description = "Select the folder to extract to...";
                FBD.ShowNewFolderButton = true;

                if (FBD.ShowDialog() == DialogResult.OK)
                {
                    foreach (SPR2Parser Sprite in m_CurrentArchive.SPR2s)
                    {
                        Sprite.ExportToBitmaps(FBD.SelectedPath);
                    }
                }
            }
            else
                MessageBox.Show("Please open an *.iff archive first!");
        }

        /// <summary>
        /// User clicked the "About" menu item.
        /// </summary>
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Iffinator\r\n" +
                "Original implementation, user-interface and framework by Mats 'Afr0' Vederhus\r\n" +
                "Fixes, extra functionality and format research by Nicholas Roth\r\n\r\n" +
                "Created for the TSO-Restoration project (http://www.tsorestoration.com)");
        }

        /// <summary>
        /// User clicked the "Exit" menu item.
        /// </summary>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// User clicked on the "Chunk analyzer" menu option.
        /// </summary>
        private void chunkAnalyzerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChunkAnalyzer ChAnalyzer = new ChunkAnalyzer();
            ChAnalyzer.Show();
        }
    }
}
