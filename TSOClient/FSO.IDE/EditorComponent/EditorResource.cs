using FSO.Client;
using FSO.Common.Utils;
using FSO.Files;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.IDE.EditorComponent
{
    public class EditorResource
    {
        private static EditorResource _Instance;
        public static EditorResource Get()
        {
            if (_Instance == null) _Instance = new EditorResource();
            return _Instance;
        }

        public Texture2D Background;
        public Texture2D DiagTile;

        public Texture2D TrueNode;
        public Texture2D FalseNode;
        public Texture2D DoneNode;

        public Texture2D TrueReturn;
        public Texture2D FalseReturn;

        public Texture2D Node;
        public Texture2D NodeOutline;
        public Texture2D ArrowHead;
        public Texture2D ArrowHeadOutline;
        public Texture2D WhiteTex;

        public bool Ready;

        public EditorResource()
        {
        }

        public void Init(GraphicsDevice gd)
        {
            if (Ready) return;
            Background = LoadFile(gd, "IDERes/bg.png");
            DiagTile = LoadFile(gd, "IDERes/diagbg.png");

            TrueNode = LoadFile(gd, "IDERes/true.png");
            FalseNode = LoadFile(gd, "IDERes/False.png");
            DoneNode = LoadFile(gd, "IDERes/done.png");

            Node = LoadFile(gd, "IDERes/nodeFG.png");
            NodeOutline = LoadFile(gd, "IDERes/nodeBG.png");
            ArrowHead = LoadFile(gd, "IDERes/arrowFG.png");
            ArrowHeadOutline = LoadFile(gd, "IDERes/arrowBG.png");

            TrueReturn = LoadFile(gd, "IDERes/trueReturn.png");
            FalseReturn = LoadFile(gd, "IDERes/falseReturn.png");

            WhiteTex = TextureUtils.TextureFromColor(gd, Color.White);
            Ready = true;
        }

        private Texture2D LoadFile(GraphicsDevice gd, string path)
        {
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var img = ImageLoader.FromStream(gd, file);

                var data = new byte[img.Width * img.Height * 4];
                img.GetData<byte>(data);
                for (int i=0; i<data.Length; i+=4)
                {
                    var a = data[i + 3];
                    data[i] = (byte)((data[i] * a)/255);
                    data[i+1] = (byte)((data[i+1] * a) / 255);
                    data[i+2] = (byte)((data[i+2] * a) / 255);
                }
                img.SetData<byte>(data);
                return img;
            }
        }
    }
}
