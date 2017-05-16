using FSO.Client.Regulators;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Server.Protocol.Electron.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Rendering.Framework;

namespace FSO.Client.UI.Panels
{
    public class UIJoinLotProgress : UIDialog
    {
        private UIProgressBar m_ProgressBar;
        private UILabel m_ProgressLabel;

        public UIJoinLotProgress() : base(UIDialogStyle.Standard, false)
        {
            this.SetSize(400, 180);
            this.Caption = GameFacade.Strings.GetString("211", "1");


            /**
             * Label background
             */
            var bgImg = new UIImage(UITextBox.StandardBackground)
            {
                X = 20,
                Y = 120
            };
            bgImg.SetSize(360, 27);
            this.Add(bgImg);


            m_ProgressBar = new UIProgressBar()
            {
                X = 20,
                Y = 66,
                Value = 0
            };
            m_ProgressBar.SetSize(360, 27);
            this.Add(m_ProgressBar);

            this.Add(new UILabel
            {
                Caption = GameFacade.Strings.GetString("211", "2"),
                X = 20,
                Y = 44
            });

            this.Add(new UILabel
            {
                Caption = GameFacade.Strings.GetString("211", "3"),
                X = 20,
                Y = 97
            });

            m_ProgressLabel = new UILabel
            {
                Caption = GameFacade.Strings.GetString("211", "4"),
                X = 31,
                Y = 122
            };
            this.Add(m_ProgressLabel);
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (Visible) GameFacade.Cursor.SetCursor(CursorType.Hourglass);
        }

        public float Progress
        {
            set
            {
                m_ProgressBar.Value = value;
            }
        }

        public string ProgressCaption
        {
            set
            {
                m_ProgressLabel.Caption = value;
            }
        }
    }
}
