namespace FSO.Common.Rendering.Framework.IO
{
    public interface IFocusableUI
    {
        bool IsFocused { get; set; }
        int TabIndex { get; }
        void OnFocusChanged(FocusEvent newFocus) { }
    }

    public enum FocusEvent
    {
        FocusIn,
        FocusOut
    }
}
