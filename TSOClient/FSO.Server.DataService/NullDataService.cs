using FSO.Common.Serialization;
using Ninject;

namespace FSO.Common.DataService
{
    public class NullDataService : DataService
    {
        public NullDataService(IModelSerializer serializer,
                                FSO.Content.Content content,
                                IKernel kernel) : base(serializer, content)
        {
        }
    }
}
