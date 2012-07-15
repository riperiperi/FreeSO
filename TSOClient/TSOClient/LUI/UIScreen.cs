/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using SimsLib.FAR3;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace TSOClient.LUI
{
    public class UIScreen : GameScreen
    {
        private MouseState m_CurrentMouseState;
        private MouseState m_PreviousMouseState;

        private List<UIElement> m_UIElements = new List<UIElement>();
        private List<NetworkedUIElement> m_NetUIElements = new List<NetworkedUIElement>();
        private List<Texture2D> m_Backgrounds = new List<Texture2D>();
        private List<ImgInfoPopup> m_Popups = new List<ImgInfoPopup>();
        private List<UIButton> m_ButtonsClicked = new List<UIButton>();
        private List<UINetworkButton> m_NetworkButtonsClicked = new List<UINetworkButton>();

        private bool m_IsLeftMouseAwaitingRelease = false;

        public UIScreen(ScreenManager ScreenMgr) : 
            base(ScreenMgr)
        {
            m_ScreenMgr = ScreenMgr;
        }

        public void HighlightButton(string strID)
        {
            UIElement elem = m_UIElements.Find(delegate(UIElement element) { return element.StrID.CompareTo(strID) == 0; });
            if (elem is UIButton)
                ((UIButton)elem).Highlight = true;
        }

        #region Construction methods

        /// <summary>
        /// Loads a background for this UIScreen instance.
        /// </summary>
        /// <param name="id_0">The TypeID of the background.</param>
        /// <param name="id_1">The GroupID of the background.</param>
        /// <param name="TextureName">The name of the texture for the background (if no ID is provided).</param>
        public void LoadBackground(uint id_0, uint id_1, string TextureName)
        {
            ulong ID = (ulong)(((ulong)id_0)<<32 | ((ulong)(id_1 >> 32)));
            if (ID != 0x00)
            {
                MemoryStream TextureStream = new MemoryStream(ContentManager.GetResourceFromLongID(ID));
                m_Backgrounds.Add(Texture2D.FromFile(m_ScreenMgr.GraphicsDevice, TextureStream));
            }
            else
            {
                Texture2D Background = m_ScreenMgr.GameComponent.Content.Load<Texture2D>(TextureName);
                m_Backgrounds.Add(Background);
            }
        }

        /// <summary>
        /// Creates a UIButton instance, and adds it to this UIScreen instance's list of UIButtons.
        /// This function is called from Lua.
        /// </summary>
        /// <param name="id_0">The first part of the button's texture's ID.</param>
        /// <param name="id_1">The second part of the button's texture's ID.</param>
        /// <param name="X">The X position of the button.</param>
        /// <param name="Y">The Y position of the button.</param>
        /// <param name="Alpha">The masking color for the button's graphic.</param>
        /// <param name="StrID">The button's string ID.</param>
        public virtual UIButton CreateButton(uint id_0, uint id_1, int X, int Y, int Alpha, bool Disabled, string StrID)
        {
            ulong ID = (ulong)(((ulong)id_0) << 32 | ((ulong)(id_1 >> 32)));
            MemoryStream TextureStream;
            Texture2D Texture;

            try
            {
                TextureStream = new MemoryStream(ContentManager.GetResourceFromLongID(ID));
                Texture = Texture2D.FromFile(m_ScreenMgr.GraphicsDevice, TextureStream/*, TCP*/);
            }
            catch (FAR3Exception)
            {
                TextureStream = new MemoryStream(ContentManager.GetResourceFromLongID(ID));
                Texture = Texture2D.FromFile(m_ScreenMgr.GraphicsDevice, TextureStream);
                TextureStream.Close();
            }

            //Why did some genius at Maxis decide it was 'ok' to operate with three masking colors?!!
            if (Alpha == 1)
                ManualTextureMask(ref Texture, new Color(255, 0, 255));
            else if (Alpha == 2)
                ManualTextureMask(ref Texture, new Color(254, 2, 254));
            else if (Alpha == 3)
                ManualTextureMask(ref Texture, new Color(255, 1, 255));

            UIButton btn = new UIButton(X, Y, Texture, Disabled, StrID, this);

            m_UIElements.Add(btn);

            return btn;
        }

        /// <summary>
        /// Creates a UIButton instance with a texture and a caption, 
        /// and adds it to this UIScreen instance's list of UIButtons.
        /// This function is called from Lua.
        /// </summary>
        /// <param name="id_0">The TypeID of the button's texture.</param>
        /// <param name="id_1">The GroupID of the button's texture.</param>
        /// <param name="X">The X position of the button.</param>
        /// <param name="Y">The Y position of the button.</param>
        /// <param name="Alpha">The masking color for the button's graphic.</param>
        /// <param name="StrID">The button's string ID.</param>
        public virtual UIButton CreateTextButton(uint id_0, uint id_1, int X, int Y, int CaptionID, int Alpha, string StrID)
        {
            ulong ID = (ulong)(((ulong)id_0) << 32 | ((ulong)(id_1 >> 32)));
            MemoryStream TextureStream;
            Texture2D Texture;

            try
            {
                TextureStream = new MemoryStream(ContentManager.GetResourceFromLongID(ID));
                Texture = Texture2D.FromFile(m_ScreenMgr.GraphicsDevice, TextureStream/*, TCP*/);
            }
            catch (FAR3Exception)
            {
                TextureStream = new MemoryStream(ContentManager.GetResourceFromLongID(ID));
                Texture = Texture2D.FromFile(m_ScreenMgr.GraphicsDevice, TextureStream);
                TextureStream.Close();
            }

            //Why did some genius at Maxis decide it was 'ok' to operate with three masking colors?!!
            if (Alpha == 1)
                ManualTextureMask(ref Texture, new Color(255, 0, 255));
            else if (Alpha == 2)
                ManualTextureMask(ref Texture, new Color(254, 2, 254));
            else if (Alpha == 3)
                ManualTextureMask(ref Texture, new Color(255, 1, 255));

            UIButton btn = new UIButton(X, Y, Texture, CaptionID, StrID, this);

            m_UIElements.Add(btn);

            return btn;
        }

        /// <summary>
        /// Creates a network button that can interact with the network subsystem when clicked.
        /// </summary>
        /// <param name="id_0">The TypeID of the button's texture.</param>
        /// <param name="id_1">The GroupID of the button's texture.</param>
        /// <param name="X">The X position of the button.</param>
        /// <param name="Y">The Y position of the button.</param>
        /// <param name="Alpha">The alphacolor of the button.</param>
        /// <param name="Disabled">Is this button disabled?</param>
        /// <param name="StrID">The string ID of this button.</param>
        /// <returns>A UINetworkButton instance.</returns>
        public UINetworkButton CreateNetworkButton(uint id_0, uint id_1, int X, int Y, int Alpha, bool Disabled, 
            string StrID)
        {
            ulong ID = (ulong)(((ulong)id_0) << 32 | ((ulong)(id_1 >> 32)));
            MemoryStream TextureStream;
            Texture2D Texture;

            try
            {
                TextureStream = new MemoryStream(ContentManager.GetResourceFromLongID(ID));
                Texture = Texture2D.FromFile(m_ScreenMgr.GraphicsDevice, TextureStream/*, TCP*/);
            }
            catch (FAR3Exception)
            {
                TextureStream = new MemoryStream(ContentManager.GetResourceFromLongID(ID));
                Texture = Texture2D.FromFile(m_ScreenMgr.GraphicsDevice, TextureStream);
                TextureStream.Close();
            }

            //Why did some genius at Maxis decide it was 'ok' to operate with three masking colors?!!
            if (Alpha == 1)
                ManualTextureMask(ref Texture, new Color(255, 0, 255));
            else if (Alpha == 2)
                ManualTextureMask(ref Texture, new Color(254, 2, 254));
            else if (Alpha == 3)
                ManualTextureMask(ref Texture, new Color(255, 1, 255));

            UINetworkButton Btn = new UINetworkButton(X, Y, Texture, PlayerAccount.Client, this, StrID);

            m_NetUIElements.Add(Btn);

            return Btn;
        }

        public UINetworkButton CreateNetworkButtonWithCaption(uint id_0, uint id_1, int X, int Y, int Alpha, bool Disabled,
            string Caption, string StrID)
        {
            ulong ID = (ulong)(((ulong)id_0) << 32 | ((ulong)(id_1 >> 32)));
            MemoryStream TextureStream;
            Texture2D Texture;

            try
            {
                TextureStream = new MemoryStream(ContentManager.GetResourceFromLongID(ID));
                Texture = Texture2D.FromFile(m_ScreenMgr.GraphicsDevice, TextureStream/*, TCP*/);
            }
            catch (FAR3Exception)
            {
                TextureStream = new MemoryStream(ContentManager.GetResourceFromLongID(ID));
                Texture = Texture2D.FromFile(m_ScreenMgr.GraphicsDevice, TextureStream);
                TextureStream.Close();
            }

            //Why did some genius at Maxis decide it was 'ok' to operate with three masking colors?!!
            if (Alpha == 1)
                ManualTextureMask(ref Texture, new Color(255, 0, 255));
            else if (Alpha == 2)
                ManualTextureMask(ref Texture, new Color(254, 2, 254));
            else if (Alpha == 3)
                ManualTextureMask(ref Texture, new Color(255, 1, 255));

            UINetworkButton Btn = new UINetworkButton(X, Y, Texture, Caption, PlayerAccount.Client, this, StrID);

            m_NetUIElements.Add(Btn);

            return Btn;
        }

        public void RegisterClick(UIButton btn)
        {
            m_ButtonsClicked.Add(btn);
        }

        public void RegisterClick(UINetworkButton btn)
        {
            m_NetworkButtonsClicked.Add(btn);
        }

        public void Add(UIElement toAdd)
        {
            m_UIElements.Add(toAdd);
        }

        /// <summary>
        /// Creates a HeadCatalogBrowser. Only used by the 
        /// CAS (Create A Sim) screen.
        /// </summary>
        /// <param name="X">The X-coordinate of the HeadCatalogBrowser.</param>
        /// <param name="Y">The Y-coordinate of the HeadCatalogBrowser.</param>
        /// <param name="strID">The string ID of the HeadCatalogBrowser.</param>
        /// <returns>A new UICollectionViewer instance.</returns>
        public UICollectionViewer CreateHeadCatalogBrowser(int X, int Y, string strID)
        {
            //0x100000010  ./avatardata/heads/collections/ea_male_heads.col
            //0x200000010  ./avatardata/heads/collections/ea_female_heads.col
            UICollectionViewer viewer = new UICollectionViewer(X, Y, 39, 44, 6, 8, 33, 33, 2, 2, 3, 7, 0x100000010, 0x200000010, this, strID, m_ScreenMgr);

            m_UIElements.Add(viewer);

            return viewer;
        }

        /// <summary>
        /// Creates a BodyCatalogBrowser. Only used by the 
        /// CAS (Create A Sim) screen.
        /// </summary>
        /// <param name="X">The X-coordinate of the BodyCatalogBrowser.</param>
        /// <param name="Y">The Y-coordinate of the BodyCatalogBrowser.</param>
        /// <param name="strID">The string ID of the BodyCatalogBrowser.</param>
        /// <returns>A new UICollectionViewer instance.</returns>
        public UICollectionViewerOutfits CreateBodyCatalogBrowser(int X, int Y, string strID)
        {
	        //0x1d00000010  ./avatardata/bodies/collections/ea_male.col
	        //0x1e00000010  ./avatardata/bodies/collections/ea_female.col
            UICollectionViewerOutfits viewer = new UICollectionViewerOutfits(X, Y, 39, 78, 6, 6, 33, 70, 2, 2, 2, 8, 0x1d00000010, 0x1e00000010, this, strID, m_ScreenMgr);

            m_UIElements.Add(viewer);

            return viewer;
        }

        /// <summary>
        /// Creates a UILabel instance and adds it to this UIScreen's list of UILabels.
        /// This function is called from Lua.
        /// </summary>
        /// <param name="CaptionID">The ID of the caption for the label.</param>
        /// <param name="StrID">The string ID of the label.</param>
        /// <param name="X">The X position of the label.</param>
        /// <param name="Y">The Y position of the label.</param>
        public void CreateLabel(int CaptionID, string StrID, int X, int Y)
        {
            m_UIElements.Add(new UILabel(CaptionID, StrID, X, Y, this));
        }

        public void CreateTextLabel(string Caption, string StrID, int X, int Y)
        {
            m_UIElements.Add(new UILabel(Caption, StrID, X, Y, this));
        }

        /// <summary>
        /// Creates a UIImage instance and adds it to this UISceen's list of UIImages.
        /// </summary>
        /// <param name="id_0">The first ID for the image resource.</param>
        /// <param name="id_1">The second ID for the image resource.</param>
        /// <param name="X">The position on the x-axis where the image is to be drawn.</param>
        /// <param name="Y">The position on the y-axis where the image is to be drawn.</param>
        /// <param name="Alpha">The alpha (masking-color) of the image.</param>
        /// <param name="StrID">The string ID for this UIImage instance.</param>
        public void CreateImage(uint id_0, uint id_1, int X, int Y, int Alpha, string StrID)
        {
            ulong ID = (ulong)(((ulong)id_0) << 32 | ((ulong)(id_1 >> 32)));
            MemoryStream TextureStream = new MemoryStream(ContentManager.GetResourceFromLongID(ID));
            Texture2D Texture = Texture2D.FromFile(m_ScreenMgr.GraphicsDevice, TextureStream);
            

            if (Alpha == 1)
                ManualTextureMask(ref Texture, new Color(255, 0, 255));
            else if (Alpha == 2)
                ManualTextureMask(ref Texture, new Color(254, 2, 254));

            m_UIElements.Add(new UIImage(X, Y, StrID, Texture, this));
        }

        public void CreateTextEdit(int X, int Y, int Width, int Height, bool ReadOnly, int Capacity)
        {
            m_UIElements.Add(new UITextEdit(X, Y, Width, Height, ReadOnly, Capacity, "", this));
        }

        public void CreateInfoPopup(int X, int Y, int ID, string Filename, int TextID)
        {
            ImgInfoPopup Popup = new ImgInfoPopup(X, Y, ID, Filename, TextID, this);
            m_Popups.Add(Popup);
        }

        public void CreateLoginDialog(int X, int Y)
        {
            MemoryStream TexStream = new MemoryStream(ContentManager.GetResourceFromLongID(0xe500000002));
            Texture2D DiagTexture = Texture2D.FromFile(m_ScreenMgr.GraphicsDevice, TexStream);

            string IP = GlobalSettings.Default.LoginServerIP;
            int Port = GlobalSettings.Default.LoginServerPort;

            UILoginDialog LoginDiag = new UILoginDialog(IP, Port, X, Y, DiagTexture, this, "LoginDialog");
            m_NetUIElements.Add(LoginDiag);
        }

        public void CreateMsgBox(int X, int Y, string Message)
        {
            UIMessageBox MsgBox = new UIMessageBox(X, Y, Message, this, "MsgBox");

            m_UIElements.Add(MsgBox);
        }

        #endregion

        /// <summary>
        /// Adds a scalefactor to a button. This means the button will be drawn as big as the
        /// size of the button's texture, plus the scalefactor. To scale down, use a negative
        /// value (untested).
        /// </summary>
        /// <param name="StrID">The string ID of the button.</param>
        /// <param name="ScaleX">The scalefactor on the x-axis.</param>
        /// <param name="ScaleY">The scalefactor on the y-axis.</param>
        public void AddScalefactorToButton(string StrID, int ScaleX, int ScaleY)
        {
            for (int i = 0; i < m_UIElements.Count; i++)
            {
                if (StrID == m_UIElements[i].StrID)
                {
                    UIButton Button = (UIButton)m_UIElements[i];
                    Button.ScaleX = ScaleX;
                    Button.ScaleY = ScaleY;
                }
            }
        }

        /// <summary>
        /// Updates the caption of a specific label.
        /// This function is called from Lua.
        /// </summary>
        /// <param name="StrID">The string ID of the label.</param>
        /// <param name="Caption">The new caption of the label.</param>
        public void UpdateLabel(string StrID, string Caption)
        {
            for (int i = 0; i < m_UIElements.Count; i++)
            {
                if (StrID == m_UIElements[i].StrID)
                {
                    //Not really sure why this works...
                    //The variable Label seems to act like a pointer to m_UIElements[i].
                    UILabel Label = (UILabel)m_UIElements[i];
                    Label.Caption = Caption;
                }
            }
        }

        /// <summary>
        /// Updates the caption of a specific label,
        /// using an ID to reference a specific caption.
        /// This funtion is called from Lua.
        /// </summary>
        /// <param name="StrID">The string ID of the label.</param>
        /// <param name="Caption">The ID of the new caption of the label.</param>
        public void UpdateLabelWithID(string StrID, int Caption)
        {
            for (int i = 0; i < m_UIElements.Count; i++)
            {
                if (StrID == m_UIElements[i].StrID)
                {
                    UILabel Label = (UILabel)m_UIElements[i];
                    Label.Caption = m_ScreenMgr.TextDict[Caption];
                }
            }
        }

        /// <summary>
        /// Removes an information popup dialog.
        /// </summary>
        /// <param name="ID">The ID of the dialog.</param>
        public void RemoveInfoPopup(int ID)
        {
            for (int i = 0; i < m_Popups.Count; i++)
            {
                if (ID == m_Popups[i].ID)
                    m_Popups.Remove(m_Popups[i]);
            }
        }

        public override void Update(GameTime GTime)
        {
            LuaInterfaceManager.CallFunction("Update");
            
            foreach (UIButton b in m_ButtonsClicked)
            {
                LuaInterfaceManager.CallFunction("ButtonHandler", b);
            }

            if (m_ButtonsClicked.Count == 0)
            {
                if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                {
                    m_IsLeftMouseAwaitingRelease = true;
                    int screen_x = 0, screen_y = 0;
                    int world_x = 0, world_y = 0;
                    MouseState mSt = Mouse.GetState();
                    screen_x = mSt.X;
                    screen_y = mSt.Y;
                    MouseDown(screen_x, screen_y);
                }
            }

            if (m_IsLeftMouseAwaitingRelease && Mouse.GetState().LeftButton == ButtonState.Released)
            {
                m_IsLeftMouseAwaitingRelease = false;
                int screen_x = 0, screen_y = 0;
                int world_x = 0, world_y = 0;
                MouseState mSt = Mouse.GetState();
                screen_x = mSt.X;
                screen_y = mSt.Y;
                MouseUp(screen_x, screen_y);
            }

            m_ButtonsClicked = new List<UIButton>();
            m_NetworkButtonsClicked = new List<UINetworkButton>();

            m_CurrentMouseState = Mouse.GetState();

            foreach (UIElement Element in m_UIElements)
            {
                Element.Update(GTime, ref m_CurrentMouseState, ref m_PreviousMouseState);
                Element.Update(GTime);
            }

            foreach (NetworkedUIElement Element in m_NetUIElements)
            {
                Element.Update(GTime, ref m_CurrentMouseState, ref m_PreviousMouseState);
                Element.Update(GTime);
            }

            for (int i = 0; i < m_Popups.Count; i++ )
                m_Popups[i].Update(GTime, ref m_CurrentMouseState, ref m_PreviousMouseState);

            m_PreviousMouseState = Mouse.GetState();
        }

        public override void Draw(SpriteBatch SBatch)
        {
            foreach (Texture2D Background in m_Backgrounds)
            {
                if (Background != null)
                {
                    //Usually a screen only has 1 background, it should be drawn at 0, 0...
                    SBatch.Draw(Background, new Rectangle(0, 0, GlobalSettings.Default.GraphicsWidth, 
                        GlobalSettings.Default.GraphicsHeight), Color.White);
                }
            }

            foreach (NetworkedUIElement Element in m_NetUIElements)
            {
                if(Element.DrawingLevel == DrawLevel.DontGiveAFuck)
                    Element.Draw(SBatch);
            }

            foreach (UIElement Element in m_UIElements)
            {
                if (Element.DrawingLevel == DrawLevel.DontGiveAFuck)
                    Element.Draw(SBatch);
            }

            foreach (NetworkedUIElement Element in m_NetUIElements)
            {
                if (Element.DrawingLevel == DrawLevel.AlwaysOnTop)
                    Element.Draw(SBatch);
            }

            foreach (UIElement Element in m_UIElements)
            {
                if (Element.DrawingLevel == DrawLevel.AlwaysOnTop)
                    Element.Draw(SBatch);
            }

            for (int i = 0; i < m_Popups.Count; i++)
                m_Popups[i].Draw(SBatch);
        }

        /// <summary>
        /// Removes a UIElement from this UIScene.
        /// </summary>
        /// <param name="ID">The ID of the element to remove.</param>
        public void RemoveElement(string ID)
        {
            if (m_UIElements.Exists(delegate(UIElement element) { return ID.CompareTo(element.StrID) == 0; }))
            {
                m_UIElements.RemoveAt(m_UIElements.FindIndex(delegate(UIElement element) 
                { return ID.CompareTo(element.StrID) == 0; }));
            }

            if (m_UIElements.Exists(delegate (UIElement element) { return ID.CompareTo(element.StrID) == 0; }))
            {
                int index = m_UIElements.FindIndex(delegate(UIElement element) { return ID.CompareTo(element.StrID) == 0; });
                if (index != -1 && index < m_UIElements.Count)
                {
                    if (m_UIElements[index] is CatalogChooser)
                    {
                        ((CatalogChooser)m_UIElements[index]).Dispose();
                    }
                    m_UIElements.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// Manually replaces a specified color in a texture with transparent black,
        /// thereby masking it.
        /// </summary>
        /// <param name="Texture">The texture on which to apply the mask.</param>
        /// <param name="ColorFrom">The color to mask away.</param>
        public static void ManualTextureMask(ref Texture2D Texture, Color ColorFrom)
        {
            Color ColorTo = Color.TransparentBlack;

            Color[] data = new Color[Texture.Width * Texture.Height];
            Texture.GetData(data);

            for (int i = 0; i < data.Length; i++)
            {
                data[i].A = 255;
                if (data[i] == ColorFrom)
                    data[i] = ColorTo;
            }

            if (Texture.Format != SurfaceFormat.Color)
                Texture = new Texture2D(Texture.GraphicsDevice, Texture.Width, Texture.Height, 4, TextureUsage.Linear, SurfaceFormat.Color);

            Texture.SetData(data);
        }

        public void RemoveAllCatalogs()
        {
            for (int i = 0; i < m_UIElements.Count; i++)
            {
                if (m_UIElements[i] is CatalogChooser)
                {
                    ((CatalogChooser)m_UIElements[i]).Dispose();
                    m_UIElements.RemoveAt(i);
                }
            }
        }

        public UIElement this[string index]
        {
            get
            {
                return m_UIElements.Find(delegate(UIElement elem) { return elem.StrID.CompareTo(index) == 0; });
            }
        }

        public virtual void MouseDown(int x, int y)
        {
            
        }

        public virtual void MouseUp(int x, int y)
        {

        }
    }
}
