/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Nicholas Roth. All Rights Reserved.

Contributor(s): Mats 'Afr0' Vederhus.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using SimsLib.FAR3;
using SimsLib.ThreeD;
using DNA;
using Microsoft.Xna.Framework;
using LogThis;

namespace TSOClient.LUI
{
    /// <summary>
    /// A UICollectionViewer is a viewer to display browseable heads (in the Create A Sim screen).
    /// </summary>
    public class UICollectionViewer : UIElement
    {
        private int myThumbSizeX, myThumbSizeY, myThumbMarginX, myThumbMarginY, myThumbImageSizeX, myThumbImageSizeY, myThumbImageOffsetX, myThumbImageOffsetY, myRows, myColumns;
        private ulong myMaleCollectionID;
        private ulong myFemaleCollectionID;
        private ulong myCurrentCollectionID;
        private UIScreen myScreen;
        private ScreenManager myScrMgr;
        private UIButton[,] myButtons;
        private List<ulong> myPurchasables;
        private List<ulong> myOutfits;
        private List<ulong[]> myAppearances;    //A appearance file contains IDs for thumbnails and bindings.
        private List<ulong[]> myBindings;       //A binding file contains IDs for meshes and textures.
        private List<ulong[]> myThumbnails;
        private List<Texture2D> myCurrentThumbnails;
        private int mySkinColor;
        private int myPageStartIdx;
        private UILabel myCountLabel;
        private UIButton myLeftButton;
        private UIButton myRightButton;
        private UITextButton[] myTextButtons;

        public int PageStartIdx { get { return myPageStartIdx; } set { myPageStartIdx = value; } }

        /// <summary>
        /// The current skincolor selected for the heads in this UICollectionViewer instance.
        /// </summary>
        public int SkinColor
        {
            get { return mySkinColor; }
        }

        public UICollectionViewer(int x, int y, int thumbSizeX, int thumbSizeY, int thumbMarginX, int thumbMarginY, int thumbImageSizeX, int thumbImageSizeY, int thumbImageOffsetX, int thumbImageOffsetY, int rows, int columns, ulong maleCollectionID, ulong femaleCollectionID, UIScreen screen, string strID, ScreenManager scrnMgr)
            : base(screen, strID, DrawLevel.AlwaysOnTop)
        {
            m_StringID = strID;

            myButtons = new UIButton[rows, columns];
            myScreen = screen;
            myScrMgr = scrnMgr;
            myMaleCollectionID = maleCollectionID;
            myFemaleCollectionID = femaleCollectionID;
            myCurrentCollectionID = femaleCollectionID;
            myThumbSizeX = thumbSizeX;
            myThumbSizeY = thumbSizeY;
            myThumbImageSizeX = thumbImageSizeX;
            myThumbImageSizeY = thumbImageSizeY;
            myThumbMarginX = thumbMarginX;
            myThumbMarginY = thumbMarginY;
            myThumbImageOffsetX = thumbImageOffsetX;
            myThumbImageOffsetY = thumbImageOffsetY;
            myPurchasables = new List<ulong>();
            myOutfits = new List<ulong>();
            myAppearances = new List<ulong[]>();
            myBindings = new List<ulong[]>();
            myThumbnails = new List<ulong[]>();
            myCurrentThumbnails = null;
            myLeftButton = addButton(0x3f500000001, 410, 275, 1, false, strID + "LeftArrow");
            myRightButton = addButton(0x3f600000001, 645, 275, 1, false, strID + "RightArrow");

            /*myLeftButton.OnButtonClick += delegate(UIButton btn) { myPageStartIdx -= myRows * myColumns; myCurrentThumbnails = null; };
            myRightButton.OnButtonClick += delegate(UIButton btn) { myPageStartIdx += myRows * myColumns; myCurrentThumbnails = null; };*/

            myTextButtons = new UITextButton[12];

            for (int i = 0, stride = 0; i < 12; i++)
            {
                myTextButtons[i] = new UITextButton(450 + stride, 270, (i + 1).ToString(), strID + "NumberButton" + i, myScreen);
                myScreen.Add(myTextButtons[i]);
                myTextButtons[i].OnButtonClick += delegate(UIElement element) { myPageStartIdx = int.Parse(element.StrID.Substring(element.StrID.LastIndexOf("NumberButton") + 12)) * myRows * myColumns; myCurrentThumbnails = null; };

                if (i < 9)
                    stride += 15;
                else
                    stride += 22;
            }

            mySkinColor = 0;
            myRows = rows;
            myColumns = columns;
            myPageStartIdx = 0;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    myButtons[i, j] = addButton(0x000003E600000001, x + thumbMarginX + (j * (thumbMarginX + thumbSizeX)), y + thumbMarginY + (i * (thumbMarginY + thumbSizeY)), 1, false, strID + '_' + i + j);
                }
            }

            loadCollection();
            myCountLabel = new UILabel(0, "CountLabel", 505, 250, myScreen);
            myCountLabel.Caption = "" + myThumbnails.Count + " Heads";
            myScreen.Add(myCountLabel);
        }

        //Goes left in the collection of head-thumbnails.
        //Called from Lua (see "personselectionedit.lua").
        public void GoLeft()
        {
            myPageStartIdx -= myRows * myColumns;
            myCurrentThumbnails = null;
        }

        //Goes right in the collection of head-thumbnails.
        //Called from Lua (see "personselectionedit.lua").
        public void GoRight()
        {
            myPageStartIdx += myRows * myColumns;
            myCurrentThumbnails = null;
        }

        public void SetPage(int page)
        {
            myPageStartIdx = page * myRows * myColumns;
            myCurrentThumbnails = null;
        }

        /// <summary>
        /// Changes the gender of the current Sim, and
        /// only displays heads of the selected gender.
        /// </summary>
        /// <param name="isMale">Is the male gender selected?</param>
        public void SetGender(bool isMale)
        {
            if (isMale)
                myCurrentCollectionID = myMaleCollectionID;
            else
                myCurrentCollectionID = myFemaleCollectionID;
            myPurchasables = new List<ulong>();
            myOutfits = new List<ulong>();
            myAppearances = new List<ulong[]>();
            myThumbnails = new List<ulong[]>();
            myCurrentThumbnails = null;
            loadCollection();
            myCurrentThumbnails = null;
            myCountLabel.Caption = "" + myThumbnails.Count + " Heads";
        }

        /// <summary>
        /// Changes the skincolor of the current Sim, and
        /// only displays heads of the selected skintone.
        /// </summary>
        /// <param name="isMale">The skintone to set.</param>
        public void SetSkinColor(int skinColor)
        {
            mySkinColor = skinColor;
            myCurrentThumbnails = null;
        }

        /// <summary>
        /// Loads the collection with the ID that was passed in the constructor of this class.
        /// </summary>
        private void loadCollection()
        {
            BinaryReader br = new BinaryReader(new MemoryStream(ContentManager.GetResourceFromLongID(myCurrentCollectionID)));

            int count = Endian.SwapInt32(br.ReadInt32());
            for (int i = 0; i < count; i++)
            {
                br.ReadInt32();
                myPurchasables.Add(Endian.SwapUInt64(br.ReadUInt64()));
            }

            foreach (ulong purchasableID in myPurchasables)
            {
                br = new BinaryReader(new MemoryStream(ContentManager.GetResourceFromLongID(purchasableID)));

                br.BaseStream.Position = 16;
                byte[] outfitID = br.ReadBytes(8);
                ulong outfit = BitConverter.ToUInt64((byte[])outfitID.Reverse().ToArray(), 0);

                myOutfits.Add(outfit);
            }

            foreach (ulong outfitID in myOutfits)
            {
                br = new BinaryReader(new MemoryStream(ContentManager.GetResourceFromLongID(outfitID)));

                br.ReadUInt32();
                br.ReadUInt32();

                ulong[] Appearances = new ulong[]
                {
                    Endian.SwapUInt64(br.ReadUInt64()),
                    Endian.SwapUInt64(br.ReadUInt64()),
                    Endian.SwapUInt64(br.ReadUInt64())
                };

                myAppearances.Add(Appearances);
            }

            foreach (ulong[] appearanceIDs in myAppearances)
            {
                ulong[] thumbnails = new ulong[3];

                for (int i = 0; i < 3; i++)
                {
                    br = new BinaryReader(new MemoryStream(ContentManager.GetResourceFromLongID(appearanceIDs[i])));

                    br.ReadInt32();

                    thumbnails[i] = Endian.SwapUInt64(br.ReadUInt64());
                }

                myThumbnails.Add(thumbnails);
            }

            br.Close();
        }

        public override void Update(GameTime GTime)
        {
            if (myPageStartIdx == 0)
                myLeftButton.Disabled = true;
            else
                myLeftButton.Disabled = false;
            if (myPageStartIdx + myColumns * myRows >= myThumbnails.Count)
                myRightButton.Disabled = true;
            else
                myRightButton.Disabled = false;

            base.Update(GTime);
        }

        public override void Draw(SpriteBatch SBatch)
        {
            bool regen = false;
            float Scale = GlobalSettings.Default.ScaleFactor;
            
            if (myCurrentThumbnails == null)
            {
                regen = true;
                myCurrentThumbnails = new List<Texture2D>();
            }

            for (int i = myPageStartIdx, r = 0; i < myPageStartIdx + myRows * myColumns && i < myThumbnails.Count; r++)
            {
                for (int j = 0; j < myColumns && i < myThumbnails.Count && i >= 0; j++, i++)
                {
                    Texture2D preview;
                    
                    if (regen)
                    {
                        preview = Texture2D.FromFile(SBatch.GraphicsDevice, new MemoryStream(ContentManager.GetResourceFromLongID(myThumbnails[i][mySkinColor])));
                        myCurrentThumbnails.Add(preview);
                    }
                    else
                    {
                        preview = myCurrentThumbnails[r * myColumns + j];
                    }

                    SBatch.Draw(preview, new Vector2((myButtons[r, j].X + myThumbImageOffsetX) * Scale,
                        (myButtons[r, j].Y + myThumbImageOffsetY) * Scale), null, Color.White, 0.0f,
                        new Vector2(0.0f, 0.0f), Scale, SpriteEffects.None, 0.0f);
                }
            }

            base.Draw(SBatch);
        }

        private UIButton addButton(ulong ID, int X, int Y, int Alpha, bool Enabled, string StrID)
        {
            MemoryStream TextureStream;
            Texture2D Texture;

            try
            {
                TextureStream = new MemoryStream(ContentManager.GetResourceFromLongID(ID));
                Texture = Texture2D.FromFile(myScrMgr.GraphicsDevice, TextureStream/*, TCP*/);
            }
            catch (FAR3Exception)
            {
                TextureStream = new MemoryStream(ContentManager.GetResourceFromLongID(ID));
                Texture = Texture2D.FromFile(myScrMgr.GraphicsDevice, TextureStream);
                TextureStream.Close();
            }

            //Why did some genius at Maxis decide it was 'ok' to operate with three masking colors?!!
            if (Alpha == 1)
                ManualTextureMask(ref Texture, new Color(255, 0, 255));
            else if (Alpha == 2)
                ManualTextureMask(ref Texture, new Color(254, 2, 254));
            else if (Alpha == 3)
                ManualTextureMask(ref Texture, new Color(255, 1, 255));

            UIButton btn = new UIButton(X, Y, Texture, Enabled, StrID, myScreen);

            myScreen.Add(btn);

            return btn;
        }

        /// <summary>
        /// Returns an ID for a appearance file based on a button's StrID.
        /// </summary>
        /// <param name="StrID">The StrID for a button (thumbnail) that was pressed.</param>
        /// <returns>An ID for a appearance file.</returns>
        public Outfit GetOutfitFromStrID(string StrID)
        {
            Log.LogThis("OutfitFromStrID: " + StrID, eloglevel.info);
            string StrIndex = StrID.Replace(m_StringID + "_", "");

            //Adding the myPageStartIdx seems to more or less select the right outfit...
            return new Outfit(ContentManager.GetResourceFromLongID(myOutfits[myPageStartIdx + int.Parse(StrIndex)]));
        }
    }
}
