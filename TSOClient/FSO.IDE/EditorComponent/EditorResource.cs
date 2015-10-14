using FSO.Common.Utils;
using Microsoft.Xna.Framework;
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

        public Texture2D ImageBackground;
        public Texture2D DiagTile;

        public Texture2D TrueNode;
        public Texture2D FalseNode;
        public Texture2D DoneNode;

        public Texture2D Node;
        public Texture2D NodeOutline;
        public Texture2D ArrowHead;
        public Texture2D ArrowHeadOutline;
        public Texture2D WhiteTex;

        public EditorResource()
        {

        }

        public void Init(GraphicsDevice gd)
        {
            DiagTile = LoadFile(gd, "IDERes/diagbg.png");

            TrueNode = LoadFile(gd, "IDERes/true.png");
            FalseNode = LoadFile(gd, "IDERes/False.png");
            DoneNode = LoadFile(gd, "IDERes/done.png");

            Node = LoadFile(gd, "IDERes/nodeFG.png");
            NodeOutline = LoadFile(gd, "IDERes/nodeBG.png");
            ArrowHead = LoadFile(gd, "IDERes/arrowFG.png");
            ArrowHeadOutline = LoadFile(gd, "IDERes/arrowBG.png");

            WhiteTex = TextureUtils.TextureFromColor(gd, Color.White);
        }

        private Texture2D LoadFile(GraphicsDevice gd, string path)
        {
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Texture2D.FromStream(gd, file);
            }
        }
    }
}
