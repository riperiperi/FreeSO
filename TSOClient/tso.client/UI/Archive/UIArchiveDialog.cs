using FSO.Client.UI.Controls;

namespace FSO.Client.UI.Archive
{
    public class UIArchiveDialog : UIDialog
    {
        public UIArchiveDialog(UIDialogStyle style, bool draggable) : base(style, draggable)
        {
        }

        public UIArchiveDialog(UIDialogStyle style, UIDialogExtras extras, bool draggable) : base(style, extras, draggable)
        {
        }

        protected static string GetString(string id)
        {
            return GameFacade.Strings.GetString("f128", id);
        }

        protected static string GetString(string id, params string[] args)
        {
            return GameFacade.Strings.GetString("f128", id, args);
        }
    }
}
