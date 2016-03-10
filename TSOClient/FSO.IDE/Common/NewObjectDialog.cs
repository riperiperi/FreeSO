using FSO.Content;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.IDE.Common
{
    public partial class NewObjectDialog : Form
    {
        public IffFile TargetIff;
        public uint ResultGUID;
        public bool IsNew;
        public NewObjectDialog(IffFile target, bool isNew)
        {
            TargetIff = target;
            IsNew = isNew;
            InitializeComponent();
        }

        private void RandomGUID_Click(object sender, EventArgs e)
        {
            var objProvider = Content.Content.Get().WorldObjects;
            lock (objProvider.Entries)
            {
                var rand = new Random();
                var guid = (uint)rand.Next();
                //doesnt cover entire uint space, but not really a problem right now.
                while (objProvider.Entries.ContainsKey(guid))
                {
                    guid = (uint)rand.Next(); 
                    //todo: if you get really unlucky, you can get stuck here forever. I mean really unlucky...
                }
                GUIDEntry.Text = guid.ToString("x8");
            }
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            var name = ChunkLabelEntry.Text;
            var guidT = GUIDEntry.Text;
            uint guid;
            var objProvider = Content.Content.Get().WorldObjects;
            if (name == "") MessageBox.Show("Name cannot be empty!", "Invalid Object Name");
            else if (guidT == "") MessageBox.Show("GUID cannot be empty!", "Invalid GUID");
            else if (!uint.TryParse(guidT, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out guid))
                MessageBox.Show("GUID is invalid! Make sure it is a hex string of size 8. (eg. 6789ABCD)", "Invalid GUID");
            else
            {
                lock (objProvider.Entries)
                {
                    if (objProvider.Entries.ContainsKey(guid))
                    {
                        MessageBox.Show("This GUID is already being used!", "GUID is Taken!");
                        return;
                    }

                    //OK, it's valid. Now to add it to the objects system...
                    //This is a little tricky because we want to add an object that does not exist yet.
                    //There's a special function just for this! But first, we need an OBJD...

                    var obj = new OBJD()
                    {
                        GUID = guid,
                        ObjectType = OBJDType.Normal,
                        ChunkLabel = name,
                        ChunkID = 1,
                        ChunkProcessed = true,
                        ChunkType = "OBJD",
                        ChunkParent = TargetIff,
                        AnimationTableID = 128
                    };

                    Content.Content.Get().Changes.BlockingResMod(new ResAction(() =>
                    {
                        //find a free space to place the object
                        ushort id = 16807; //todo: why???
                        var list = TargetIff.List<OBJD>();
                        if (list != null)
                        {
                            foreach (var chk in list.OrderBy(x => x.ChunkID))
                            {
                                if (chk.ChunkID == id)
                                    id++;
                            }
                        }
                        obj.ChunkID = id;
                        //add it to the iff file
                        TargetIff.AddChunk(obj);
                    }, obj));
                
                    if (IsNew)
                    {
                        //add a default animation table, for quality of life reasons

                        var anim = new STR()
                        {
                            ChunkLabel = name,
                            ChunkID = 128,
                            ChunkProcessed = true,
                            ChunkType = "STR#",
                            ChunkParent = TargetIff,
                        };

                        anim.InsertString(0, new STRItem { Value = "", Comment = "" });
                        TargetIff.AddChunk(anim);

                        var filename = TargetIff.RuntimeInfo.Path;
                        Directory.CreateDirectory(Path.GetDirectoryName(filename));
                        using (var stream = new FileStream(filename, FileMode.Create))
                            TargetIff.Write(stream);
                    }

                    //add it to the provider
                    objProvider.AddObject(TargetIff, obj);

                    DialogResult = DialogResult.OK;
                    ResultGUID = guid;
                    Close();
                }
            }
        }
    }
}
