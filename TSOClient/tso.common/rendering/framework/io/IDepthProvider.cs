using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSO.Common.rendering.framework.io
{
    /// <summary>
    /// Represents an object that has depth
    /// </summary>
    public interface IDepthProvider
    {
        float Depth { get; }
    }
}
