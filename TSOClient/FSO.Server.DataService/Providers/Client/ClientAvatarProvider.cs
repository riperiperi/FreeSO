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
    public class ClientAvatarProvider : ReceiveOnlyServiceProvider<uint, Avatar>
    {
        protected override Avatar CreateInstance(uint key)
        {
            var avatar = base.CreateInstance(key);
            //TODO: Use the string tables
            avatar.Avatar_Name = "Retrieving...";
            avatar.Avatar_Description = "Retrieving...";
            return avatar;
        }
    }
}
