using FSO.Client.Controllers;
using FSO.Client.GameContent;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels
{
    public class UILotPurchaseDialog : UIDialog
    {
        private Regex VALIDATE_NUMERIC = new Regex(".*[0-9]+.*");
        private Regex VALIDATE_SPECIAL_CHARS = new Regex("[a-z|A-Z|-| |']*");

        public UITextEdit NameTextEdit { get; set; }
        public UIValidationMessages<string> NameTextEditValidation { get; set; }
        public UILabel MessageText { get; set; }

        public string TextTitle { get; set; }
        public string InvalidNameErrorTitle { get; set; }
        public string InvalidNameErrorShort { get; set; }
        public string InvalidNameErrorLong { get; set; }
        public string InvalidNameErrorNumeric { get; set; }
        public string InvalidNameErrorApostrophe { get; set; }
        public string InvalidNameErrorDash { get; set; }
        public string InvalidNameErrorSpace { get; set; }
        public string InvalidNameErrorSpecial { get; set; }
        public string InvalidNameErrorCensor { get; set; }
        public string CloseButtonTooltip { get; set; }
        public string AcceptButtonTooltip { get; set; }

        public event Callback<string> OnNameChosen;

        public UILotPurchaseDialog() : base(UIDialogStyle.Standard| UIDialogStyle.OK | UIDialogStyle.Close, false)
        {
            var script = RenderScript("lotpurchasedialog.uis");
            SetSize(380, 210);
            
            NameTextEdit = script.Create<UITextEdit>("NameTextEdit");
            NameTextEdit.MaxLines = 1;
            NameTextEdit.BackgroundTextureReference = UITextBox.StandardBackground;
            NameTextEdit.TextMargin = new Rectangle(8, 3, 8, 3);
            NameTextEdit.FlashOnEmpty = true;
            NameTextEdit.MaxChars = 24;
            Add(NameTextEdit);

            NameTextEditValidation = new UIValidationMessages<string>()
                .WithValidation(InvalidNameErrorShort, x => x.Length < 3)
                .WithValidation(InvalidNameErrorLong, x => x.Length > 24)
                .WithValidation(InvalidNameErrorNumeric, x => VALIDATE_NUMERIC.IsMatch(x))
                .WithValidation(InvalidNameErrorApostrophe, x => x.Split(new char[] { '\'' }).Length > 1)
                .WithValidation(InvalidNameErrorDash, x => x.Split(new char[] { '-' }).Length > 1)
                .WithValidation(InvalidNameErrorSpecial, x => !VALIDATE_SPECIAL_CHARS.IsMatch(x));

            NameTextEditValidation.ErrorPrefix = InvalidNameErrorTitle;
            NameTextEditValidation.Position = new Vector2(NameTextEdit.X, NameTextEdit.Y + NameTextEdit.Height);
            NameTextEditValidation.Width = (int)NameTextEdit.Width;
            Add(NameTextEditValidation);

            GameFacade.Screens.inputManager.SetFocus(NameTextEdit);

            NameTextEdit.OnChange += NameTextEdit_OnChange;
            RefreshValidation();

            OKButton.OnButtonClick += AcceptButton_OnButtonClick;
            CloseButton.OnButtonClick += CloseButton_OnButtonClick;
        }

        private void CloseButton_OnButtonClick(Framework.UIElement button)
        {
            //todo: special behaviour?
            UIScreen.RemoveDialog(this);
        }

        private void AcceptButton_OnButtonClick(Framework.UIElement button)
        {
            if (OnNameChosen != null)
            {
                OnNameChosen(NameTextEdit.CurrentText);
            }
            else
            {
                FindController<TerrainController>().PurchaseLot(NameTextEdit.CurrentText);
            }
        }

        private void NameTextEdit_OnChange(Framework.UIElement element)
        {
            RefreshValidation();
        }

        private void RefreshValidation()
        {
            var valid = NameTextEditValidation.Validate(NameTextEdit.CurrentText);
            OKButton.Disabled = !valid;
        }
    }
}
