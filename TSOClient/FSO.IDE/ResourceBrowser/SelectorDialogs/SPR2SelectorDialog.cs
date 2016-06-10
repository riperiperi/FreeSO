using FSO.Content;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.IDE.ResourceBrowser
{
    public partial class SPR2SelectorDialog : Form
    {
        public ushort ChosenID;

        public SPR2SelectorDialog()
        {
            InitializeComponent();
        }

        public SPR2SelectorDialog(GameIffResource iff, GameObject srcObj) : this()
        {
            iffRes.Init(
                new Type[] { typeof(SPR2) },
                new string[] { "Sprites" },
                new OBJDSelector[][]
                {
                    new OBJDSelector[]
                    {
                        new OBJDSelector("Chosen Sprite", null, new OBJDSelector.OBJDSelectorCallback((IffChunk chunk) => {
                            var spr = (SPR2)chunk;
                            if (spr != null) {
                                ChosenID = spr.ChunkID;
                                DialogResult = DialogResult.OK;
                            }
                            Close();
                        }))
                    }
                }
                );
            iffRes.ChangeIffSource(iff);
            iffRes.ChangeActiveObject(srcObj);

            iffRes.SetAlphaOrder(false);
        }
    }
}
