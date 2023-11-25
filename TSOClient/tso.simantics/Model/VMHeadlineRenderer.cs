using FSO.LotView;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.SimAntics.Model
{
    public class VMHeadlineRenderer
    {
        protected VMRuntimeHeadline Headline;
        public virtual bool IsMoney { get => false; }

        public VMHeadlineRenderer(VMRuntimeHeadline headline) {
            Headline = headline;
        }

        public virtual Texture2D DrawFrame(World world)
        {
            return null;
        }

        public virtual void Dispose()
        {

        }

        /// <summary>
        /// Returns true if the headline should be killed.
        /// </summary>
        /// <returns></returns>
        public bool Update() {
            Headline.Anim++;
            if (Headline.Duration < 0) return false;
            return (--Headline.Duration <= 0);
        }
    }

    public interface VMHeadlineRendererProvider
    {
        VMHeadlineRenderer Get(VMRuntimeHeadline headline);
    }

    public class VMNullHeadlineProvider : VMHeadlineRendererProvider
    {
        public VMHeadlineRenderer Get(VMRuntimeHeadline headline)
        {
            return new VMHeadlineRenderer(headline);
        }
    }

}
