using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.Server.Embedded;
using System;

namespace FSO.Client.UI.Archive
{
    internal class UIArchiveServerStatusDialog : UIDialog
    {
        private UILabel InfoText;
        private bool WaitStart;
        private Action OnComplete;
        private EmbeddedServer Server;

        public UIArchiveServerStatusDialog(bool waitStart, EmbeddedServer server, Action onComplete) : base(UIDialogStyle.Standard, false)
        {
            WaitStart = waitStart;
            OnComplete = onComplete;
            Server = server;
            Caption = "Archive Server";

            Add(InfoText = new UILabel()
            {
                Caption = waitStart ? "Starting archive server. Please wait..." : "Safely shutting down archive server before closing.",
                Position = new Microsoft.Xna.Framework.Vector2(20, 45),
                Size = new Microsoft.Xna.Framework.Vector2(200, 50),
                Wrapped = true,
            });

            SetSize(200 + 40, 50 + 70);

            if (!WaitStart)
            {
                Server.Shutdown().ContinueWith((t) =>
                {
                    GameThread.NextUpdate((state) =>
                    {
                        GameFacade.Kill();
                    });
                });
            }
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);

            if (WaitStart)
            {
                if (Server.Ready && OnComplete != null)
                {
                    OnComplete();
                }
            }
        }
    }
}
