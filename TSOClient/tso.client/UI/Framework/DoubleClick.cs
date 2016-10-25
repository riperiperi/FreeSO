using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Framework
{
    public class DoubleClick
    {
        private long LastClick;

        public bool TryDoubleClick(UIMouseEventType type, UpdateState update)
        {
            if(type == UIMouseEventType.MouseUp)
            {
                var now = update.Time.ElapsedGameTime.Ticks;
                if(now - LastClick < 1000)
                {
                    LastClick = now;
                    return true;
                }
                LastClick = now;
            }

            return false;
        }
    }
}
