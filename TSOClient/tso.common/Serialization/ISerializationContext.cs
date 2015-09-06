using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Common.Serialization
{
    public interface ISerializationContext
    {
        IKernel Kernel { get; }
        IModelSerializer ModelSerializer { get; }
    }
}
