using FSO.Content;
using FSO.SimAntics.JIT.Translation.CSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.IO;

namespace FSO.IDE.ContentEditors
{
    public partial class AOTGenerator : Form
    {
        public Thread ConversionThread;
        public bool Abort;
        private string DestPath;
        private bool User;

        public AOTGenerator()
        {
            InitializeComponent();
        }

        private void AOTToggle_Click(object sender, EventArgs e)
        {
            if (ConversionThread == null)
            {
                AOTToggle.Text = "Cancel";
                AOTProgress.Value = 0;
                AOTStatus.Text = $"Translating global.iff";
                DestPath = OutDir.Text;
                User = IncludeUser.Checked;
                ConversionThread = new Thread(ConversionAction);
                ConversionThread.Start();
            }
            else
            {
                Abort = true;
                AOTToggle.Enabled = false;
            }
        }

        public void ConversionAction()
        {
            int i = 0;
            while (i++ < 1)
            {
                var translator = new CSTranslator();
                var globalRes = Content.Content.Get().WorldObjectGlobals.Get("global").Resource;

                var iff = globalRes.MainIff;
                iff.Filename = "global.iff";
                translator.Context.GlobalRes = globalRes;
                var globalText = translator.TranslateIff(iff);
                using (var file = System.IO.File.Open(Path.Combine(DestPath, "Global.cs"), System.IO.FileMode.Create))
                {
                    using (var writer = new System.IO.StreamWriter(file))
                    {
                        writer.Write(globalText);
                    }
                }

                if (Abort) break;

                var globalContext = (CSTranslationContext)translator.Context;

                var compiledSG = new Dictionary<GameGlobalResource, CSTranslationContext>();
                var objs = Content.Content.Get().WorldObjects.Entries.Where(x => User || (x.Value.Source != GameObjectSource.User)).ToList();
                var fileComplete = new HashSet<string>();
                var objPct = 0;
                foreach (var obj in objs)
                {
                    var r = obj.Value;
                    if (!fileComplete.Contains(r.FileName))
                    {
                        Invoke(new Action(() => {
                            AOTProgress.Value = 10 + (objPct*90)/objs.Count;
                        }));
                        fileComplete.Add(r.FileName);
                        var objRes = r.Get();

                        CSTranslationContext sg = null;
                        if (objRes.Resource.SemiGlobal != null)
                        {
                            if (!compiledSG.TryGetValue(objRes.Resource.SemiGlobal, out sg))
                            {
                                //compile semiglobals
                                translator = new CSTranslator();
                                var sgIff = objRes.Resource.SemiGlobal.MainIff;
                                translator.Context.ObjectRes = objRes.Resource; //pass this in as occasionally *local* tuning constants are used in *semiglobal* functions.
                                translator.Context.GlobalRes = globalRes;
                                translator.Context.SemiGlobalRes = objRes.Resource.SemiGlobal;
                                translator.Context.GlobalContext = globalContext;
                                Invoke(new Action(() => {
                                    AOTStatus.Text = $"Translating Semi-Global {sgIff.Filename}";
                                }));
                                var semiglobalText = translator.TranslateIff(sgIff);
                                using (var file = System.IO.File.Open(Path.Combine(DestPath, translator.Context.Filename + ".cs"), System.IO.FileMode.Create))
                                {
                                    using (var writer = new System.IO.StreamWriter(file))
                                    {
                                        writer.Write(semiglobalText);
                                    }
                                }
                                sg = (CSTranslationContext)translator.Context;
                                compiledSG[objRes.Resource.SemiGlobal] = sg;
                            }
                        }

                        translator = new CSTranslator();
                        var objIff = objRes.Resource.MainIff;
                        translator.Context.GlobalRes = globalRes;
                        translator.Context.SemiGlobalRes = objRes.Resource.SemiGlobal;
                        translator.Context.ObjectRes = objRes.Resource;
                        translator.Context.GlobalContext = globalContext;
                        translator.Context.SemiGlobalContext = sg;
                        Invoke(new Action(() => {
                            AOTStatus.Text = $"Translating {objIff.Filename}";
                        }));
                        var objText = translator.TranslateIff(objIff);
                        using (var file = System.IO.File.Open(Path.Combine(DestPath, translator.Context.Filename + ".cs"), System.IO.FileMode.Create))
                        {
                            using (var writer = new System.IO.StreamWriter(file))
                            {
                                writer.Write(objText);
                            }
                        }
                    }
                    objPct++;
                }

                if (!Abort)
                {
                    Invoke(new Action(() => {
                        AOTStatus.Text = $"Completed! {objs.Count} objects converted.";
                        AOTProgress.Value = 100;
                    }));
                }
            }
            if (Abort)
            {
                Invoke(new Action(() => {
                    AOTStatus.Text = "Aborted.";
                }));
            }

            Invoke(new Action(() => {
                AOTToggle.Text = "Begin";
                AOTToggle.Enabled = true;
            }));
            Abort = false;
            ConversionThread = null;
        }
    }
}
