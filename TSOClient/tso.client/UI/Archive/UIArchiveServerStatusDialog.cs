using FSO.Client.UI.Controls;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.Server.Embedded;

namespace FSO.Client.UI.Archive
{
    internal class UIArchiveServerStatusDialog : UIArchiveDialog
    {
        private readonly UILabel InfoText;
        private bool WaitStart;
        private readonly Action OnComplete;
        private readonly EmbeddedServer Server;
        private readonly UIProgressBar ProgressBar;

        public UIArchiveServerStatusDialog(bool waitStart, EmbeddedServer server, Action onComplete) : base(UIDialogStyle.Standard, false)
        {
            WaitStart = waitStart;
            OnComplete = onComplete;
            Server = server;
            Caption = GetString("260");

            Add(InfoText = new UILabel()
            {
                Caption = waitStart ? GetString("261") : GetString("262"),
                Position = new Microsoft.Xna.Framework.Vector2(20, 45),
                Size = new Microsoft.Xna.Framework.Vector2(200, 50),
                Wrapped = true,
            });

            int ySize = 50 + 70;

            if (waitStart)
            {
                ySize += 37;

                Add(ProgressBar = new UIProgressBar()
                {
                    Position = new Microsoft.Xna.Framework.Vector2(20, 105),
                    Size = new Microsoft.Xna.Framework.Vector2(200, 27)
                });
            }

            SetSize(200 + 40, ySize);

            if (!WaitStart)
            {
                Server.Shutdown().ContinueWith((t) =>
                {
                    GameThread.NextUpdate((state) =>
                    {
                        if (onComplete != null)
                        {
                            onComplete();
                        }
                        else
                        {
                            GameFacade.Kill();
                        }
                    });
                });
            }
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);

            if (WaitStart)
            {
                if (Server.ReadyPercent != ProgressBar.Value)
                {
                    ProgressBar.Value = Server.ReadyPercent;
                }

                if (Server.Ready && OnComplete != null)
                {
                    OnComplete();
                }
            }
        }
    }
}
