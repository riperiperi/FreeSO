using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Model;

namespace TSOClient.Code.UI.Framework
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
