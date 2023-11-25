namespace FSO.Common.Rendering.Framework.IO
{
    public class ClipboardHandler
    {
        public static ClipboardHandler Default = new ClipboardHandler();

        public virtual string Get() { return ""; }
        public virtual void Set(string text) { }
    }
}
