using Ninject;

namespace FSO.Common.Serialization
{
    public interface ISerializationContext
    {
        IKernel Kernel { get; }
        IModelSerializer ModelSerializer { get; }
    }
}
