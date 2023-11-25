using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.Model;
using System.Linq;

namespace FSO.Client.UI.Panels
{
    public class UISortedContainer : UIContainer
    {
        public override void Update(UpdateState state)
        {
            Sort();
            base.Update(state);
        }

        public void Sort()
        {
            Children = Children.OrderBy(x => (x as IZIndexable)?.Z ?? 0).ToList();
        }
    }

    public interface IZIndexable
    {
        float Z { get; set; }
    }
}
