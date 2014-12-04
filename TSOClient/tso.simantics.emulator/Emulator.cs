using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using TSO.Files.FAR3;
using TSO.Files.FAR1;
using TSO.Content;

namespace TSO.Simantics.emulator
{
    public partial class Emulator : Form
    {
        public Emulator()
        {
            InitializeComponent();

            //VMContext context = new VMContext();
            
            ///** Globals **/
            //var tsoBase = @"C:\Program Files\Maxis\The Sims Online\TSOClient\";
            //var behavior = new Iff(File.ReadAllBytes(Path.Combine(tsoBase, "objectdata/globals/behavior.iff")));
            //var globals = new Iff(File.ReadAllBytes(Path.Combine(tsoBase, "objectdata/globals/global.iff")));

            //context.Globals.Import(behavior);
            //context.Globals.Import(globals);

            //VM vm = new VM(context);

            ///*var content = Content.Get();
            //FARArchive archive = new FARArchive(Path.Combine(tsoBase, "objectdata/objects/objiff.far"));
            //foreach (FarEntry entry in archive.GetAllFarEntries())
            //{
            //    System.Diagnostics.Debug.WriteLine(entry.Filename);
            //    if (entry.Filename == "christmastree.iff" || entry.Filename == "aquarium1.iff" || entry.Filename == "bubblemaker.iff")
            //    {
            //        byte[] data = archive.GetEntry(entry);
            //        content.Objects.Import(new Iff(data));
            //    }
            //    //System.Diagnostics.Debug.WriteLine(entry.Filename);
            //}*/


            /** Aquarium = 0xF8BD0081 **/
            /** Xmas tree = 0x2AE3C9DA **/
            //vm.AddEntity(new VMGameObject(Content.Get().Objects.Get(0x2AE3C9DA)));


        }
    }
}
