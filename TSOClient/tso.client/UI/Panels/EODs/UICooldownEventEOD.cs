using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;

namespace FSO.Client.UI.Panels.EODs
{
    public class UICooldownEventEOD : UIEOD
    {
        public UICooldownEventEOD(UIEODController controller) : base(controller)
        {
            PlaintextHandlers["success"] = SuccessTest;
            PlaintextHandlers["failure"] = FailureTest;
        }
        
        public void SuccessTest(string evt, string data)
        {
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = "Success Test",
                Message = "You successfully passed the cooldown test." + System.Environment.NewLine +
                "You cannot use this item until: " + data,
                Alignment = TextAlignment.Center,
                TextEntry = false,
                Buttons = UIAlertButton.Ok((btn) =>
                {
                    UIScreen.RemoveDialog(alert);
                }),

            }, true);
            CloseInteraction();
        }
        public void FailureTest(string evt, string data)
        {
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = "Fail Test",
                Message = "You failed the cooldown test." + System.Environment.NewLine +
                "You cannot use this item until: " + data,
                Alignment = TextAlignment.Center,
                TextEntry = false,
                Buttons = UIAlertButton.Ok((btn) =>
                {
                    UIScreen.RemoveDialog(alert);
                }),

            }, true);
            CloseInteraction();
        }
    }
}
