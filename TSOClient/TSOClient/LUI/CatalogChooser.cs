/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Nicholas Roth. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace TSOClient.LUI
{
    class CatalogChooser : UIElement
    {
        UIScreen myScreen;
        UIButton myNextPgBtn;
        UIButton myPrevPgBtn;
        string myCatalogType;
        int currentItemIndex = 0;

        public CatalogChooser(UIScreen scr, string StrID, uint FileID, uint TypeID, int x, int y, string catalogType)
            : base(scr, StrID, DrawLevel.DontGiveAFuck)
        {
            m_Screen.CreateImage(FileID, TypeID, 336+177, 5+96+390, 1, StrID+"SubtoolsBackground");
            myCatalogType = catalogType;

            //buildpanel_scrolleftbtn
            m_Screen.CreateButton(0x00000423, 1, 348 + 177, 30 + 96, 1, false, StrID + "PreviousPageButton");
            //buildpanel_scrollrightbtn
            m_Screen.CreateButton(0x00000424, 1, 597 + 177, 30 + 96, 1, false, StrID + "NextPageButton");
            myScreen.CreateImage(FileID, TypeID, 336 + 177, 5 + 96 + 390, 1, StrID + "SubtoolsBackground");

            //buildpanel_scrolleftbtn
            myPrevPgBtn = myScreen.CreateButton(0x00000423, 1, 348 + 177, 30 + 96, 1, false, StrID + "PreviousPageButton");
            //buildpanel_scrollrightbtn
            myNextPgBtn = myScreen.CreateButton(0x00000424, 1, 597 + 177, 30 + 96, 1, false, StrID + "NextPageButton");

            myPrevPgBtn.Disabled = true;
            myPrevPgBtn.OnButtonClick += new ButtonClickDelegate(delegate(UIButton b) { if (currentItemIndex != 0) { currentItemIndex -= 10; } RefreshFloorCatalog(); if (currentItemIndex == 0) { myPrevPgBtn.Disabled = true; } if (currentItemIndex <= ContentManager.Floors.Count) { myNextPgBtn.Disabled = false; } });
            myNextPgBtn.OnButtonClick += new ButtonClickDelegate(delegate(UIButton b) { if (currentItemIndex <= ContentManager.Floors.Count) { currentItemIndex += 10; } RefreshFloorCatalog(); if (currentItemIndex + 10 >= ContentManager.Floors.Count) { myNextPgBtn.Disabled = true; } if (currentItemIndex != 0) { myPrevPgBtn.Disabled = false; } });

            InitCatalog();
        }

        private void InitCatalog()
        {
            int current = 0;
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (ContentManager.Floors.Count > currentItemIndex + current)
                    {
                        UIButton btn = new UIButton(462 + 86 - 7 + (44 * j), 500 - 7 + (44 * i), ContentManager.Floors[currentItemIndex + current++].CatalogImage, false, StrID + myCatalogType + '_' + i + '_' + j, myScreen);
                        btn.OnButtonClick += new ButtonClickDelegate(delegate (UIButton button) {
                                    if (myScreen is IsometricView)
                                    {
                                        string[] nameParts = button.StrID.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                                        int row = int.Parse(nameParts[1]);
                                        int col = int.Parse(nameParts[2]);
                                        ((IsometricView)myScreen).CurrentTool = new FloorTool((IsometricView)myScreen, currentItemIndex + row * 5 + col);
                                    }
                                });
                        myScreen.Add(btn);
                    }
                }
            }
        }

        private void RefreshFloorCatalog()
        {
            int current = 0;
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (ContentManager.Floors.Count > currentItemIndex + current)
                    {
                        ((UIButton)myScreen[StrID + "Floor_" + i + '_' + j]).Texture = ContentManager.Floors[currentItemIndex + current++].CatalogImage;
                        ((UIButton)myScreen[StrID + "Floor_" + i + '_' + j]).Invisible = false;
                    }
                    else
                    {
                        ((UIButton)myScreen[StrID + "Floor_" + i + '_' + j]).Invisible = true;
                    }
                }
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    myScreen.RemoveElement(StrID + myCatalogType + '_' + i + '_' + j);
                }
            }
            myScreen.RemoveElement(base.StrID + "SubtoolsBackground");
            myScreen.RemoveElement(base.StrID + "PreviousPageButton");
            myScreen.RemoveElement(base.StrID + "NextPageButton");
        }

        ~CatalogChooser()
        {
            
        }
    }
}
