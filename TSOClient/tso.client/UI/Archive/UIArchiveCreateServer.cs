using FSO.Client.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Archive
{
    internal class UIArchiveCreateServer : UIDialog
    {
        private struct ServerFlag
        {
            public string Name;
            public string Caption;
            public bool DefaultValue;
            public int Indentation;
            public Action HelpAction;

            public ServerFlag(string name, string caption, bool defaultValue, int indentation = 0, Action helpAction = null)
            {
                Name = name;
                Caption = caption;
                DefaultValue = defaultValue;
                Indentation = indentation;
                HelpAction = helpAction;
            }
        }

        private ServerFlag[] Flags = new ServerFlag[]
        {
            new ServerFlag("upnp", "Use UPnP", true, 0, UPnPHelp),
            new ServerFlag("hideNames", "Hide display names", false),
            new ServerFlag("offline", "Offline mode", false),
            default, // Gap (name is null)
            new ServerFlag("createLot", "Allow lot creation", true),
            new ServerFlag("createCharacter", "Allow character creation", true),
            new ServerFlag("lockArchived", "Lock archived characters", false),
            new ServerFlag("lockFirst", "Lock characters to first user", false),
            new ServerFlag("lockNew", "Only for new characters", false, 1),
        };

        public UIArchiveCreateServer() : base(UIDialogStyle.Close, true)
        {
            var flagsVbox = new UIVBoxContainer();

            foreach (var flag in Flags)
            {
                if (flag.Name != null)
                {
                    var flagHbox = new UIHBoxContainer();

                    var check = new UIButton(GetTexture(0x0000083600000001));
                    check.Selected = flag.DefaultValue;

                    flagHbox.Add(check);

                    flagHbox.Add(new UILabel()
                    {
                        Caption = flag.Caption,
                    });

                    if (flag.HelpAction != null)
                    {
                        // TODO: Add a help button
                    }

                    flagHbox.AutoSize();

                    flagsVbox.Add(flagHbox);
                }
            }

            flagsVbox.AutoSize();

            Add(flagsVbox);
        }

        public static void UPnPHelp()
        {
            UIAlert.Alert("UPnP", "UPnP attempts to automatically forward ports on your router to allow public access to your game server. Some routers have this disabled by default or simply don't support it, in which case you'll need to uncheck this option and manually forward the ports.", true);
        }
    }
}
