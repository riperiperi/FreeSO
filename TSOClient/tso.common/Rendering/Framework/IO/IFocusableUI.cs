namespace FSO.Common.Rendering.Framework.IO
{
    public interface IFocusableUI
    {
        void OnFocusChanged(FocusEvent newFocus);
    }

    public enum FocusEvent
    {
        FocusIn,
        FocusOut
    }
}
