using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FSO.Common.DataService;

namespace FSO.Server.DataService.Providers.Client
{
    public class ClientAvatarProvider : ReceiveOnlyServiceProvider<uint, Avatar>
    {
        protected override Avatar CreateInstance(uint key)
        {
            var avatar = base.CreateInstance(key);
            avatar.RequestDefaultData = true;

            //TODO: Use the string tables
            avatar.Avatar_Id = key;
            avatar.Avatar_Name = "Retrieving...";
            avatar.Avatar_Description = "Retrieving...";

            //mab000_xy__proxy
            avatar.Avatar_Appearance.AvatarAppearance_BodyOutfitID = 2525440770061;
            //mah000_proxy
            avatar.Avatar_Appearance.AvatarAppearance_HeadOutfitID = 3985729650701;
            return avatar;
        }
    }
}
