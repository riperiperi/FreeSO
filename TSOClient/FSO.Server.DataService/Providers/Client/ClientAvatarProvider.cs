using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Server.DataService.Providers.Client
{
    public class ClientAvatarProvider : LazyDataServiceProvider<uint, Avatar>
    {
        protected override Avatar LazyLoad(uint key)
        {
            var result = base.LazyLoad(key);
            Thread.Sleep(15000);
            return result;
        }
    }
}
