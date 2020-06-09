using FSO.Client;
using FSO.Client.UI.Framework;
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

        public Texture2D Breakpoint;
        public Texture2D CurrentArrow;

        public Texture2D ViewBG;

        public Texture2D CommentBox;
        public Texture2D CommentBubble;
        public Texture2D GotoBox;
        public Texture2D LabelBox;

        public TextStyle CommentStyle;
        public TextStyle TitleStyle;

        public string[] IndexedLoad = new string[]
        {
            "btns/play.png",
            "btns/stepin.png",
            "btns/stepover.png",
            "btns/stepout.png",

            "btns/returntrue.png",
            "btns/returnfalse.png",
            "btns/reset.png",

            "btns/pause.png",
        };

        public Texture2D[] Indexed;

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
            FalseNode = LoadFile(gd, "IDERes/false.png");
            DoneNode = LoadFile(gd, "IDERes/done.png");

            Node = LoadFile(gd, "IDERes/nodeFG.png");
            NodeOutline = LoadFile(gd, "IDERes/nodeBG.png");
            ArrowHead = LoadFile(gd, "IDERes/arrowFG.png");
            ArrowHeadOutline = LoadFile(gd, "IDERes/arrowBG.png");

            TrueReturn = LoadFile(gd, "IDERes/trueReturn.png");
            FalseReturn = LoadFile(gd, "IDERes/falseReturn.png");

            Breakpoint = LoadFile(gd, "IDERes/breakpoint.png");
            CurrentArrow = LoadFile(gd, "IDERes/current.png");

            ViewBG = LoadFile(gd, "IDERes/viewBG.png");

            CommentBox = LoadFile(gd, "IDERes/tree/comment_box.png");
            CommentBubble = LoadFile(gd, "IDERes/tree/comment_bubble.png");
            GotoBox = LoadFile(gd, "IDERes/tree/goto_box.png");
            LabelBox = LoadFile(gd, "IDERes/tree/label_box.png");

            Indexed = new Texture2D[IndexedLoad.Length];
            for (int i=0; i<IndexedLoad.Length; i++)
            {
                Indexed[i] = LoadFile(gd, "IDERes/"+IndexedLoad[i]);
            }

            WhiteTex = TextureUtils.TextureFromColor(gd, Color.White);

            //prepare fonts
            var style = TextStyle.Create(new Color(70, 70, 55), 12);
            style.Font = FSO.Client.GameFacade.EdithFont;
            style.VFont = FSO.Client.GameFacade.EdithVectorFont;
            style.Size = 12;
            style.HighlightedColor = new Color(221, 221, 221);
            style.SelectionBoxColor = style.HighlightedColor;
            style.SelectedColor = style.Color;
            style.CursorColor = style.Color;
            CommentStyle = style;

            style = style.Clone();
            style.Color = Color.White;
            style.HighlightedColor = new Color(102, 102, 102);
            style.SelectionBoxColor = style.HighlightedColor;
            style.SelectedColor = style.Color;
            style.CursorColor = style.Color;
            style.Size = 14;
            TitleStyle = style;

            Ready = true;
        }

        private Texture2D LoadFile(GraphicsDevice gd, string path)
        {
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var img = ImageLoader.FromStream(gd, file);

                /*
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
                */
                return img;
            }
        }
    }
}
