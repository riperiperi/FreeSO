using FSO.Common.Rendering.Framework.Model;

namespace FSO.Client.UI.Framework
{
    /// <summary>
    /// Non-visual UI component. For example, an animation library that needs to be involved
    /// with the update loop
    /// </summary>
    public interface IUIProcess
    {
        void Update(UpdateState state);
    }
}
