using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Wave;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Threading;

namespace TSOClient.LUI
{
    public class IsometricView : UIScreen
    {
        private int myX, myY, mySize;
        private bool m_Disabled;

        private IWavePlayer m_WaveOutDevice = new DirectSoundOut();

        private bool m_Clicking = false;

        public event ButtonClickDelegate OnButtonClick;

        private Floor[,] myFloors;
        private Stack<GameObject>[,] myWallsAndHangings;
        private Stack<GameObject>[,] myGameObjects;

        private Floor[,] myFloors_tempbuff;
        private Stack<GameObject>[,] myWallsAndHangings_tempbuff;
        private Stack<GameObject>[,] myGameObjects_tempbuff;

        private bool myShouldDoPicking;

        private Color[,] myPickingTexture = new Color[800,600];

        private List<GameObject> myObjects;                             // The index of any drawable in this list is its unique ID for picking

        private BuyBuildTool myCurrentTool;

        public BuyBuildTool CurrentTool { get { return myCurrentTool; } set { myCurrentTool = value; } }

        public delegate void SetPickingPixel(Color c, int x, int y);

        public bool Disabled
        {
            get { return m_Disabled; }
            set { m_Disabled = value; }
        }

        /// <summary>
        /// Gets or sets the x-coordinate for where to render this button.
        /// </summary>
        public int X
        {
            get { return myX; }
            set { myY = value; }
        }

        /// <summary>
        /// Gets or sets the y-coordinate for where to render this button.
        /// </summary>
        public int Y
        {
            get { return myY; }
            set { myY = value; }
        }

        public int Size
        {
            get { return mySize; }
        }

        public IsometricView (ScreenManager ScreenMgr)
            : base(ScreenMgr)
        {
            GameObject.GlobalXTranslation = -507;
            GameObject.GlobalYTranslation = 177;
            GameObject.GlobalRotation++;
            GameObject.GlobalRotation++;
            GameObject.DrawSize = 3;
            myX = 0;
            myY = 0;
            mySize = 12;
            myFloors = new Floor[55, 48];
            myWallsAndHangings = new Stack<GameObject>[55, 48];
            myGameObjects = new Stack<GameObject>[55, 48];
            myObjects = new List<GameObject>();

            InitFloorsDebug();
        }

        public IsometricView(int Size, ScreenManager ScreenMgr)
            : base(ScreenMgr)
        {
            GameObject.GlobalXTranslation = -507;
            GameObject.GlobalYTranslation = 177;
            GameObject.GlobalRotation++;
            GameObject.GlobalRotation++;
            GameObject.DrawSize = 3;
            myX = X;
            myY = Y;
            mySize = Size;
            myFloors = new Floor[55, 48];
            myWallsAndHangings = new Stack<GameObject>[55, 48];
            myGameObjects = new Stack<GameObject>[55, 48];

            InitFloorsDebug();
        }

        public void InitFloorsDebug()
        {
            GameObject.DrawSize = 0;
            GameObject.GlobalRotation = 0;
            int x = 0;
            int y = 0;
            int numDrawn = 1;
            for (; y < 48; y++)
            {
                if (numDrawn++ < 4)
                    AddFloor(ContentManager.Floors[212], x, y);
                else
                {
                    AddFloor(ContentManager.Floors[211], x, y);
                    numDrawn = 0;
                }
            }
            x = 1;
            for (y = 0; x < 5; y++)
            {
                if (y >= 48)
                {
                    x++;
                    y = 0;
                }
                AddFloor(ContentManager.Floors[211], x, y);
            }
            for (y = 0; y < 48; y++)
            {
                AddFloor(ContentManager.Floors[175], x, y);
            }
            x++;
            for (y = 0; y < 48; y++)
            {
                AddFloor(ContentManager.Floors[214], x, y);
            }
            x++;
            for (; x < 55; x++)
            {
                for (y = 0; y < 48; y++)
                {
                    AddFloor(ContentManager.Floors[175], x, y);
                }
            }
            x = 0;
            y = 0;
        }

        public void PlaceObjectInWorld(GameObject obj, int x, int y)
        {
            obj.Position = new int[] { x, y };
            if (myGameObjects[x, y] == null)
                myGameObjects[x, y] = new Stack<GameObject>();
            myGameObjects[x, y].Push(obj);
            myObjects.Add(obj);
            myShouldDoPicking = true;
        }

        public void PlaceWallItemInWorld(GameObject obj, int x, int y)
        {
            obj.Position = new int[] { x, y };
            if (myWallsAndHangings[x, y] == null)
                myWallsAndHangings[x, y] = new Stack<GameObject>();
            myWallsAndHangings[x, y].Push(obj);
            myObjects.Add(obj);
            myShouldDoPicking = true;
        }

        public void AddFloor(Floor flr, int x, int y)
        {
            Floor floor = (Floor)flr.AddToWorld(x, y, 0);
            myFloors[x, y] = floor;
            myObjects.Add(floor);
            myShouldDoPicking = true;
        }

        public void AddFloorTemporary(Floor flr, int x, int y)
        {
            if (myFloors_tempbuff == null)
            {
                myFloors_tempbuff = new Floor[55, 48];
                for (int i = 0; i < 55; i++)
                    for (int j = 0; j < 48; j++)
                        myFloors_tempbuff[i, j] = myFloors[i, j];
            }

            Floor floor = (Floor)flr.AddToWorld(x, y, 0);
            myFloors_tempbuff[x, y] = floor;
            //myShouldDoPicking = true;
        }

        public int MouseX { get { return Mouse.GetState().X; } }
        public int MouseY { get { return Mouse.GetState().Y; } }

        public void MoveLeft()
        {
            if (myCurrentTool == null || myCurrentTool.CanChangePosition || true)
            {
                GameObject.GlobalXTranslation--;
                myShouldDoPicking = true;
            }
        }

        public void MoveRight()
        {
            if (myCurrentTool == null || myCurrentTool.CanChangePosition || true)
            {
                GameObject.GlobalXTranslation++;
                myShouldDoPicking = true;
            }
        }

        public void MoveUp()
        {
            if (myCurrentTool == null || myCurrentTool.CanChangePosition || true)
            {
                GameObject.GlobalYTranslation++;
                myShouldDoPicking = true;
            }
        }

        public void MoveDown()
        {
            if (myCurrentTool == null || myCurrentTool.CanChangePosition || true)
            {
                GameObject.GlobalYTranslation--;
                myShouldDoPicking = true;
            }
        }

        static bool lastnight = false;
        static bool lastnight2 = false;
        static bool lastnight3 = false;
        static bool lastnight4 = false;
        private bool bRot = true;
        private bool bZoom = true;
        public override void Update(GameTime GTime)
        {
            if (myCurrentTool != null)
            {
                bRot = myCurrentTool.CanRotate;
                bZoom = myCurrentTool.CanZoom;
                myCurrentTool.Update();
            }


            if (bRot)
            {
                if (Keyboard.GetState().IsKeyDown(Keys.Right))
                {
                    if (lastnight)
                    {
                        GameObject.GlobalRotation++;
                        myShouldDoPicking = true;
                        Thread.Sleep(75);
                    }
                    lastnight = !lastnight;
                }
                if (Keyboard.GetState().IsKeyDown(Keys.Left))
                {
                    if (lastnight2)
                    {
                        GameObject.GlobalRotation--;
                        myShouldDoPicking = true;
                        Thread.Sleep(75);
                    }
                    lastnight2 = !lastnight2;
                }
            }
            if (bZoom)
            {
                if (Keyboard.GetState().IsKeyDown(Keys.Add))
                {
                    if (lastnight3)
                    {
                        GameObject.DrawSize++;
                        myShouldDoPicking = true;
                        Thread.Sleep(75);
                    }
                    lastnight3 = !lastnight3;
                }
                if (Keyboard.GetState().IsKeyDown(Keys.Subtract))
                {
                    if (lastnight4)
                    {
                        GameObject.DrawSize--;
                        myShouldDoPicking = true;
                        Thread.Sleep(75);
                    }
                    lastnight4 = !lastnight4;
                }
            }
            base.Update(GTime);
        }

        public void CreateCatalog(uint bg_0, uint bg_1, int x, int y, int catalogSize, string catalogType, string StrID)
        {
            CatalogChooser catalog = new CatalogChooser(this, StrID, bg_0, bg_1, x, y, catalogType);

            Add(catalog);
        }

        // draw the walls and hangings for the current tile
        private void DrawWallsAndHangingsForTile(int x, int y, SpriteBatch SBatch)
        {
            Stack<GameObject> temp = new Stack<GameObject>();
            if (myWallsAndHangings[x, y] != null)
            {
                // reverse the stack so we can draw it
                for (int i = 0; i < myWallsAndHangings[x, y].Count; i++)
                    temp.Push(myWallsAndHangings[x, y].Pop());

                // draw the stack and reverse it again for storage (It is stored like this for a good reason)
                for (int i = 0; i < temp.Count; i++)
                {
                    GameObject obj = temp.Pop();
                    obj.Draw(SBatch);
                    myWallsAndHangings[x, y].Push(obj);
                }
            }
        }

        // draw the walls and hangings for the current tile for picking
        private void DrawWallsAndHangingsForTileForPicking(int x, int y)
        {
            Stack<GameObject> temp = new Stack<GameObject>();
            if (myWallsAndHangings[x, y] != null)
            {
                // reverse the stack so we can draw it
                for (int i = 0; i < myWallsAndHangings[x, y].Count; i++)
                    temp.Push(myWallsAndHangings[x, y].Pop());

                // draw the stack and reverse it again for storage (It is stored like this for a good reason)
                for (int i = 0; i < temp.Count; i++)
                {
                    GameObject obj = temp.Pop();
                    obj.DrawForPicking(delegate(Color c, int X, int Y) { if (c.R + c.G != 510 || myPickingTexture[X, Y].PackedValue == 0) { myPickingTexture[X, Y] = c; } }, (ushort)myObjects.IndexOf(obj));
                    myWallsAndHangings[x, y].Push(obj);
                }
            }
        }

        // draw the game objects for the current tile
        private void DrawGameObjectsForTile(int x, int y, SpriteBatch SBatch)
        {
            Stack<GameObject> temp = new Stack<GameObject>();
            if (myGameObjects[x, y] != null)
            {
                // reverse the stack so we can draw it
                for (int i = 0; i < myGameObjects[x, y].Count; i++)
                    temp.Push(myGameObjects[x, y].Pop());

                // draw the stack and reverse it again for storage (It is stored like this for a good reason)
                for (int i = 0; i < temp.Count; i++)
                {
                    GameObject obj = temp.Pop();
                    obj.Draw(SBatch);
                    myGameObjects[x, y].Push(obj);
                }
            }
        }

        /// <summary>
        /// Draw the game objects for the current tile for picking.
        /// </summary>
        /// <param name="x">The x-coordinate of the current tile.</param>
        /// <param name="y">The y-coordinate of the current tile.</param>
        private void DrawGameObjectsForTileForPicking(int x, int y)
        {
            Stack<GameObject> temp = new Stack<GameObject>();
            if (myGameObjects[x, y] != null)
            {
                // reverse the stack so we can draw it
                for (int i = 0; i < myGameObjects[x, y].Count; i++)
                    temp.Push(myGameObjects[x, y].Pop());

                // draw the stack and reverse it again for storage (It is stored like this for a good reason)
                for (int i = 0; i < temp.Count; i++)
                {
                    GameObject obj = temp.Pop();
                    obj.DrawForPicking(delegate(Color c, int X, int Y) { if (c.R + c.G != 510 || myPickingTexture[X, Y].PackedValue == 0) { myPickingTexture[X, Y] = c; } }, (ushort)myObjects.IndexOf(obj));
                    myGameObjects[x, y].Push(obj);
                }
            }
        }

        public override UIButton CreateButton(uint id_0, uint id_1, float X, float Y, int Alpha, bool Disabled, string StrID)
        {
            return base.CreateButton(id_0, id_1, X, Y+390, Alpha, Disabled, StrID);
        }

        private void DrawToPickingSurface()
        {
            foreach (Floor f in myFloors)
            {
                ushort idx = (ushort)myObjects.IndexOf(f);
                f.DrawForPicking(delegate(Color c, int X, int Y) { if (c.R + c.G != 510 || myPickingTexture[X, Y].PackedValue == 0) { myPickingTexture[X, Y] = c; } }, idx);
            }
            switch (GameObject.GlobalRotation)
            {
                case 0:
                    for (int x = 0; x < 55; x++)
                    {
                        for (int y = 0; y < 48; y++)
                        {
                            DrawWallsAndHangingsForTileForPicking(x, y);
                            DrawGameObjectsForTileForPicking(x, y);
                        }
                    }
                    break;
                case 1:
                    for (int x = 0; x < 55; x++)
                    {
                        for (int y = 0; y < 48; y++)
                        {
                            DrawWallsAndHangingsForTileForPicking(x, y);
                            DrawGameObjectsForTileForPicking(x, y);
                        }
                    }
                    break;
                case 2:
                    for (int x = 55 - 1; x >= 0; x--)
                    {
                        for (int y = 47; y >= 0; y--)
                        {
                            DrawWallsAndHangingsForTileForPicking(x, y);
                            DrawGameObjectsForTileForPicking(x, y);
                        }
                    }
                    break;
                case 3:
                    for (int x = 55 - 1; x >= 0; x--)
                    {
                        for (int y = 47; y >= 0; y--)
                        {
                            DrawWallsAndHangingsForTileForPicking(x, y);
                            DrawGameObjectsForTileForPicking(x, y);
                        }
                    }
                    break;
            }
        }

        public override void Draw(SpriteBatch SBatch)
        {
            if (myShouldDoPicking)
            {
                //myPickingTexture = new Color[800, 600];
                //DrawToPickingSurface();
                //myShouldDoPicking = false;
            }
            if (myFloors_tempbuff != null)
            {
                foreach (Floor f in myFloors_tempbuff)
                {
                    f.Draw(SBatch);
                }
            }
            else
            {
                foreach (Floor f in myFloors)
                {
                    f.Draw(SBatch);
                }
            }
            switch (GameObject.GlobalRotation)
            {
                case 0:
                for (int x = 0; x < 55; x++)
                {
                    for (int y = 0; y < 48; y++)
                    {
                        DrawWallsAndHangingsForTile(x, y, SBatch);
                        DrawGameObjectsForTile(x, y, SBatch);
                    }
                }
            break;
                case 1:
                for (int x = 0; x < 55; x++)
                {
                    for (int y = 0; y < 48; y++)
                    {
                        DrawWallsAndHangingsForTile(x, y, SBatch);
                        DrawGameObjectsForTile(x, y, SBatch);
                    }
                }
            break;
                case 2:
                for (int x = 55 - 1; x >= 0; x--)
                {
                    for (int y = 47; y >= 0; y--)
                    {
                        DrawWallsAndHangingsForTile(x, y, SBatch);
                        DrawGameObjectsForTile(x, y, SBatch);
                    }
                }
            break;
                case 3:
                for (int x = 55 - 1; x >= 0; x--)
                {
                    for (int y = 47; y >= 0; y--)
                    {
                        DrawWallsAndHangingsForTile(x, y, SBatch);
                        DrawGameObjectsForTile(x, y, SBatch);
                    }
                }
            break;
            }

            myFloors_tempbuff = null;


            base.Draw(SBatch);
        }

        public void DumpPickingBuffer_DEBUG(string path, ImageFileFormat format)
        {
            Texture2D tex = new Texture2D(base.ScreenMgr.GraphicsDevice, 800, 600);
            Color[] clrs = new Color[800 * 600];
            for (int i = 0; i < 800; i++)
                for (int j = 0; j < 600; j++)
                    clrs[i * 600 + j] = myPickingTexture[i, j];
            tex.SetData<Color>(clrs);
            tex.Save(path, format);
        }

        public override void MouseDown(int x, int y)
        {
            if (x >= 0 && x < 800 && y >= 0 && y < 600)
            {
                if (myShouldDoPicking)
                {
                    myPickingTexture = new Color[800, 600];
                    for (int i = 0; i < 800; i++)
                        for (int j = 0; j < 600; j++)
                            myPickingTexture[i, j] = new Color(255, 255, 0, 0);
                    DrawToPickingSurface();
                    myShouldDoPicking = false;
                }

                Color objectPicked = myPickingTexture[x, y];

                if (objectPicked.R + objectPicked.G != 510)
                {
                    int X = objectPicked.R;
                    int Y = objectPicked.G;
                    int uid = objectPicked.B | objectPicked.A << 8;
                    GameObject objPicked = myObjects[uid];
                    if (myCurrentTool != null)
                        myCurrentTool.MouseDown(X, Y);
                }
            }

            base.MouseDown(x, y);
        }

        public override void MouseUp(int x, int y)
        {
            if (x >= 0 && x < 800 && y >= 0 && y < 600)
            {
                if (myShouldDoPicking)
                {
                    myPickingTexture = new Color[800, 600];
                    for (int i = 0; i < 800; i++)
                        for (int j = 0; j < 600; j++)
                            myPickingTexture[i, j] = new Color(255, 255, 0, 0);
                    DrawToPickingSurface();
                    myShouldDoPicking = false;
                }

                Color objectPicked = myPickingTexture[x, y];

                if (objectPicked.R + objectPicked.G != 510)
                {
                    int X = objectPicked.R;
                    int Y = objectPicked.G;
                    int uid = objectPicked.B | objectPicked.A << 8;
                    GameObject objPicked = myObjects[uid];
                    if (myCurrentTool != null)
                        myCurrentTool.MouseUp(X, Y);
                }
            }

            

            base.MouseUp(x, y);
        }
    }
}
