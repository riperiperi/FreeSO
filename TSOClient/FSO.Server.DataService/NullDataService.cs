using FSO.Common.Serialization;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
