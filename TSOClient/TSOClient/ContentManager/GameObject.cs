using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TSOClient.LUI;

namespace TSOClient
{
    public abstract class GameObject
    {
        private static int ourGlobalXTranslation, ourGlobalYTranslation, ourDrawSize, ourRotation;
        protected int[] myPosition = new int[2];
        protected int myRotation = 0;

        public static int GlobalRotation { get { return ourRotation; } set
        { 
            int newRotation = (value < 4 && value >= 0) ? value : (ourRotation == 0 && value < 0) ? 3 : 0;
            int oldRot = ourRotation;
            int diff = newRotation - oldRot;
            ourRotation = newRotation;
            if (diff != 0)
            {
                switch (oldRot)
                {
                    case 0:
                        if (diff > 0 && diff != 3)
                        {
                            ourGlobalXTranslation -= Floor.Width * 55;
                            ourGlobalYTranslation -= Floor.Height * 12;
                        }
                        else if (diff == 3)
                        {
                            ourGlobalXTranslation -= Floor.Width * 55;
                            ourGlobalYTranslation += Floor.Height * 12;
                        }
                        else
                        {
                            ourGlobalXTranslation += Floor.Width * 55;
                            ourGlobalYTranslation += Floor.Height * 12;
                        }
                        break;
                    case 1:
                        if (diff > 0)
                        {
                            ourGlobalXTranslation -= Floor.Width * 55;
                            ourGlobalYTranslation += Floor.Height * 12;
                        }
                        else
                        {
                            ourGlobalXTranslation += Floor.Width * 55;
                            ourGlobalYTranslation += Floor.Height * 12;
                        }
                        break;
                    case 2:
                        if (diff > 0)
                        {
                            ourGlobalXTranslation += Floor.Width * 55;
                            ourGlobalYTranslation += Floor.Height * 12;
                        }
                        else
                        {
                            ourGlobalXTranslation += Floor.Width * 55;
                            ourGlobalYTranslation -= Floor.Height * 12;
                        }
                        break;
                    case 3:
                        if (diff > 0)
                        {
                            ourGlobalXTranslation += Floor.Width * 55;
                            ourGlobalYTranslation += Floor.Height * 12;
                        }
                        else if (newRotation == 0)
                        {
                            ourGlobalXTranslation += Floor.Width * 55;
                            ourGlobalYTranslation -= Floor.Height * 12;
                        }
                        else
                        {
                            ourGlobalXTranslation -= Floor.Width * 55;
                            ourGlobalYTranslation -= Floor.Height * 12;
                        }
                        break;
                }
            }
        } }
        public static int DrawSize { get { return ourDrawSize; } set { ourDrawSize = (value < 3 && value >= 0) ? value : 0; } }
        public static int GlobalXTranslation { get { return ourGlobalXTranslation; } set { ourGlobalXTranslation = value; } }
        public static int GlobalYTranslation { get { return ourGlobalYTranslation; } set { ourGlobalYTranslation = value; } }

        public int[] Position
        {
            get
            {
                return myPosition;
            }
            set
            {
                myPosition = value;
            }
        }

        public int Rotation
        {
            get
            {
                return myRotation;
            }
            set
            {
                myRotation = value;
            }
        }

        public virtual int[] ScreenPosition
        {
            get
            {
                int x = 0, y = 0;

                int v1 = myPosition[0] * ((DrawSize == 0) ? 15 : (DrawSize == 1) ? 31 : (DrawSize == 2) ? 63 : 0);
                int v2 = myPosition[0] * ((DrawSize == 0) ? 8 : (DrawSize == 1) ? 16 : (DrawSize == 2) ? 32 : 0);

                int v3 = myPosition[1] * ((DrawSize == 0) ? 16 : (DrawSize == 1) ? 32 : (DrawSize == 2) ? 64 : 0);
                int v4 = myPosition[1] * ((DrawSize == 0) ? 8 : (DrawSize == 1) ? 16 : (DrawSize == 2) ? 32 : 0);

                switch(ourRotation)
                {
                    case 0:
                    x = -v1;
                    y = v2;

                    x += -v3;
                    y += -v4;
                break;
                    case 1:
                    x = v1;
                    y = v2;

                    x += -v3;
                    y += v4;
                break;
                    case 2:
                    x = v1;
                    y = -v2;

                    x += v3;
                    y += v4;
                break;
                    case 3:
                    x = -v1;
                    y = -v2;

                    x += v3;
                    y += -v4;
                break;
                }

                return new int[] { x, y };
            }
        }

        public abstract GameObject AddToWorld(int x, int y, int rotation);

        public abstract void Draw(SpriteBatch SBatch);

        public abstract void DrawForPicking(IsometricView.SetPickingPixel setPixelDelegate, ushort UId);
    }
}
