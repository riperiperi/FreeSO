using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace FSO.Client.UI.Panels
{
    public class UINetStatusTray : UIContainer
    {
        public UILabel DCLabel;

        public UINetStatusTray()
        {
            DCLabel = new UILabel();
            DCLabel.CaptionStyle = DCLabel.CaptionStyle.Clone();
            DCLabel.CaptionStyle.Shadow = true;
            DCLabel.Visible = false;
            DCLabel.Alignment = TextAlignment.Top | TextAlignment.Right;
            DCLabel.Size = Vector2.One;
            Add(DCLabel);
        }

        public override void Update(UpdateState state)
        {
            var status = FSOFacade.NetStatus;
            if (status.Any)
            {
                DCLabel.Visible = true;
                var messages = new List<string>();
                if (status.CityReconnectAttempt > 0)
                {
                    messages.Add(GameFacade.Strings.GetString("f100", "4", new string[] { status.CityReconnectAttempt.ToString() }));
                }
                if (status.LotReconnectAttempt > 0)
                {
                    messages.Add(GameFacade.Strings.GetString("f100", "5", new string[] { status.LotReconnectAttempt.ToString() }));
                }
                if (status.RemeshesInProgress > 0)
                {
                    messages.Add(GameFacade.Strings.GetString("f100", "6", new string[] { status.RemeshesInProgress.ToString() }));
                }
                DCLabel.Caption = string.Join(", ", messages);
                DCLabel.CaptionStyle.Color = status.Severe ? new Color(255, 122, 77) : Color.White;
                var screen = UIScreen.Current;
                DCLabel.Position = new Vector2(screen.ScreenWidth - 10, 10);
            }
            else
            {
                DCLabel.Visible = false;
            }
            base.Update(state);
        }
    }
}
