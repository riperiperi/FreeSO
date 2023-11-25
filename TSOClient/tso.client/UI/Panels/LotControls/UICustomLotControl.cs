using FSO.Common.Rendering.Framework.Model;

namespace FSO.Client.UI.Panels.LotControls
{
    public interface UICustomLotControl
    {
        void MouseDown(UpdateState state);
        void MouseUp(UpdateState state);
        void Update(UpdateState state, bool scrolled);

        void Release();
    }
}
