using Ninject;

namespace FSO.Common.Serialization
{
    public class SerializationContext : ISerializationContext
    {
        public IKernel Kernel { get; internal set; }
        public IModelSerializer ModelSerializer { get; internal set; }

        public SerializationContext(IKernel Kernel, IModelSerializer ModelSerializer)
        {
            this.Kernel = Kernel;
            this.ModelSerializer = ModelSerializer;
        }
    }
}
