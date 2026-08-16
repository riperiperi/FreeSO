using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.Utils;
using FSO.Server.Clients;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Archive
{
    public readonly struct UIAddServerResult(string address, StatusCheckResult status, bool isFreeSO)
    {
        public readonly string Address = address;
        public readonly StatusCheckResult Status = status;
        public readonly bool IsFreeSO = isFreeSO;
    }

    internal class UIArchiveAddServerDialog : UIArchiveDialog
    {
        private readonly UILabel DescriptionLabel;
        private readonly UILabel AddressLabel;
        private readonly UITextBox AddressInput;
        private readonly UILabel StatusLabel;
        private readonly UIButton AddButton;

        private readonly TextStyle StatusStyle;
        private readonly UIVBoxContainer RootBox;

        private bool IsFetching;
        private readonly Action<UIAddServerResult> OnResult;

        public UIArchiveAddServerDialog(Action<UIAddServerResult> onResult) : base(UIDialogStyle.Close, true)
        {
            OnResult += onResult;

            RootBox = new UIVBoxContainer()
            {
                HorizontalAlignment = UIContainerHorizontalAlignment.Center
            };

            RootBox.Add(DescriptionLabel = new UILabel()
            {
                Caption = GetString("148"),
                Size = new Vector2(300, 50),
                Wrapped = true
            });

            StatusStyle = TextStyle.DefaultLabel.Clone();
            StatusStyle.Size = 9;
            StatusStyle.Shadow = true;

            RootBox.Add(StatusLabel = new UILabel()
            {
                CaptionStyle = StatusStyle
            });
            RootBox.Add(AddressInput = new UITextBox() { Size = new Vector2(300, 25) });

            RootBox.Add(AddButton = new UIButton() { Caption = GetString("147"), Disabled = true });

            Add(RootBox);

            AddressInput.OnChange += AddressChanged;
            AddButton.OnButtonClick += AddServer;

            CloseButton.OnButtonClick += Close;

            RootBox.AutoSize();
            RootBox.Position = new Vector2(20, 40);
            SetSize((int)RootBox.Size.X + 40, (int)RootBox.Size.Y + 60);
        }

        private void Close(UIElement button)
        {
            GameFacade.Screens.RemoveDialog(this);
        }

        private void AddressChanged(UIElement element)
        {
            AddButton.Disabled = IsFetching || AddressInput.CurrentText.Length == 0;
        }

        private void CloseWithResult(UIAddServerResult result)
        {
            GameFacade.Screens.RemoveDialog(this);
            OnResult(result);
        }

        private void Reset()
        {
            StatusStyle.Color = new Color(255, 122, 77);
            StatusLabel.Caption = GetString("151");
            AddressInput.Mode = UITextEditMode.Editor;
            IsFetching = false;
            AddressChanged(AddressInput);
        }

        private void AddServer(UIElement button)
        {
            var address = AddressInput.CurrentText;
            IsFetching = true;
            StatusStyle.Color = Color.White;
            StatusLabel.Caption = GetString("150");
            AddButton.Disabled = true;
            AddressInput.Mode = UITextEditMode.ReadOnly;

            Task.Run(async () =>
            {
                var archiveTask = Task.Run(async () => await StatusChecker.ArchiveStatus(FSOFacade.Kernel, address));
                var fsoTask = Task.Run(async () => await StatusChecker.FreeSOStatus(address));

                var first = await Task.WhenAny(archiveTask, fsoTask);

                var firstResult = first.Result;

                if (firstResult.IsOnline)
                {
                    GameThread.InUpdate(() =>
                    {
                        CloseWithResult(new UIAddServerResult(address, firstResult, first == fsoTask));
                    });
                }
                else
                {
                    var all = await Task.WhenAll(archiveTask, fsoTask);

                    int index = 0;
                    foreach (var status in all)
                    {
                        if (status.IsOnline)
                        {
                            GameThread.InUpdate(() =>
                            {
                                CloseWithResult(new UIAddServerResult(address, status, index == 1));
                            });
                            return;
                        }

                        index++;
                    }

                    GameThread.InUpdate(() =>
                    {
                        Reset();
                    });
                }
            });
        }
    }
}
