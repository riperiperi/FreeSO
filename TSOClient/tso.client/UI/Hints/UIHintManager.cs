using FSO.Client.UI.Framework;
using FSO.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FSO.Client.UI.Hints
{
    public class UIHintManager
    {
        public List<UIHintRef> AllHints = new List<UIHintRef>();
        public Dictionary<string, List<UIHintRef>> TriggerToHints = new Dictionary<string, List<UIHintRef>>();
        public HashSet<string> ShownGUIDs = new HashSet<string>();
        public string HintDir;
        //object land triggers, for simplicity

        public UIHintAlert HintAlert;


        public UIHintManager()
        {
            //load stuff
            //shown hints
            try
            {
                using (var str = File.Open(Path.Combine(FSOEnvironment.UserDir, "readhints.json"), FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var reader = new StreamReader(str);
                    var hints = JsonConvert.DeserializeObject<UIHintRead>(reader.ReadToEnd());
                    if (hints?.ShownGUIDs != null) ShownGUIDs = hints.ShownGUIDs;
                }
            }
            catch (Exception)
            {
                //hint read list missing or couldn't parse
            }

            var langdir = GlobalSettings.Default.CurrentLang.ToLowerInvariant() + ".dir";
            HintDir = "Content/UI/hints/"+langdir+"/";
            if (!Directory.Exists(HintDir))
            {
                HintDir = "Content/UI/hints/english.dir/";
            }

            //scan hints
            var dir = Directory.GetFiles(HintDir);
            foreach (var file in dir)
            {
                var fn = Path.GetFileName(file);
                var hint = LoadHint(fn);
                if (hint != null)
                {
                    //register this hint
                    hint.Filename = fn;
                    var r = new UIHintRef() { Filename = fn, GUID = hint.GUID };

                    AllHints.Add(r);
                    List<UIHintRef> trigger = null;
                    if (!TriggerToHints.TryGetValue(hint.Trigger, out trigger)) {
                        trigger = new List<UIHintRef>();
                        TriggerToHints[hint.Trigger] = trigger;
                    }
                    trigger.Add(r);
                }
            }
        }

        public void TriggerHint(string trigger)
        {
            List<UIHintRef> hintFiles = null;
            if (!TriggerToHints.TryGetValue(trigger, out hintFiles))
            {
                return; //no hints available
            }

            foreach (var hint in hintFiles)
            {
                TryShowHint(hint);
            }
        }

        public void TryShowHint(UIHintRef r)
        {
            if (ShownGUIDs.Contains(r.GUID)) return;
            ShownGUIDs.Add(r.GUID);
            SaveRead();
            var hint = LoadHint(r.Filename);
            if (hint != null)
            {
                ShowHint(hint);
            }
        }

        public void ShowHint(UIHint hint)
        {
            if (HintAlert == null || HintAlert.Dead)
            {
                HintAlert = new UIHintAlert(hint);
                UIScreen.GlobalShowDialog(HintAlert, true);
            } else
            {
                HintAlert.AddHint(hint);
            }
        }

        public UIHint LoadHint(string filename)
        {
            try
            {
                using (var str = File.Open(HintDir + filename, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    var reader = new StreamReader(str);
                    var hint = JsonConvert.DeserializeObject<UIHint>(reader.ReadToEnd());
                    reader.Close();
                    return hint;
                }
            } catch (Exception)
            {
                //hint missing or couldn't parse
                return null;
            }
        }

        public void MarkAllRead()
        {
            foreach (var hint in AllHints)
            {
                ShownGUIDs.Add(hint.GUID);
            }
            SaveRead();
        }

        public void SaveRead()
        {
            try
            {
                using (var str = File.Open(Path.Combine(FSOEnvironment.UserDir, "readhints.json"), FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var writer = new StreamWriter(str);
                    var read = JsonConvert.SerializeObject(new UIHintRead() { ShownGUIDs = ShownGUIDs });
                    writer.WriteLine(read);
                    writer.Close();
                }
            } catch (Exception)
            {
                //just fail silently if we can't save our hints read
            }
        }

        public List<UIHint> LoadAllHints()
        {
            return AllHints.Select(x => LoadHint(x.Filename)).Where(x => x != null).OrderBy(x => x.Order ?? 0).ToList();
        }
    }

    public class UIHintRef
    {
        public string Filename;
        public string GUID;
    }

    public class UIHintRead
    {
        public HashSet<string> ShownGUIDs;
    }
}
