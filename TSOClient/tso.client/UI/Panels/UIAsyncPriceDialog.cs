using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels
{
    public class UIAsyncPriceDialog : UIDialog
    {
        public delegate void SalePriceDelegate(uint SalePrice);
        public delegate void SalePriceCanceledDelegate();
        public event SalePriceDelegate OnPriceChange;
        public event SalePriceCanceledDelegate OnPriceChangeCancel;

        public UITextEdit ForSalePrice { get; set; }
        public UITextEdit topText { get; set; }

        public UIAsyncPriceDialog(string itemName, uint originalPrice) : base(UIDialogStyle.Standard| UIDialogStyle.OK | UIDialogStyle.Close, false)
        {
            var script = RenderScript("asyncprice.uis");
            SetSize(240, 286);

            var bg = script.Create<UIImage>("OwnerPriceBack");
            AddAt(3, bg);

            topText.CurrentText = GameFacade.Strings.GetString("239", "2", new string[] { itemName });
            ForSalePrice.CurrentText = originalPrice.ToString();
            OKButton.OnButtonClick += OKClicked;
            CloseButton.OnButtonClick += CloseClicked;

            GameFacade.Screens.inputManager.SetFocus(ForSalePrice);
        }

        private void CloseClicked(UIElement button)
        {
            OnPriceChangeCancel?.Invoke();
            UIScreen.RemoveDialog(this);
        }

        private void OKClicked(UIElement button)
        {
            uint result = 0;
            if (uint.TryParse(ForSalePrice.CurrentText, out result))
            {
                OnPriceChange?.Invoke(result);
                UIScreen.RemoveDialog(this);
            } else
            {
                HIT.HITVM.Get().PlaySoundEvent(UISounds.Error);
            }
        }
    }
}
