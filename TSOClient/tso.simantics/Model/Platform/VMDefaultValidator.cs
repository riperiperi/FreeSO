using FSO.SimAntics.Model.TSOPlatform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Model.Platform
{
    public class VMDefaultValidator : VMAbstractValidator
    {
        protected static HashSet<int> RoomieWhiteList = new HashSet<int>()
        {
            12, 13, 14, 15, 16, 17, 18, 19, 20
        };
        protected static HashSet<int> BuilderWhiteList = new HashSet<int>()
        {
            12, 13, 14, 15, 16, 17, 18, 19, 20,
            0, 1, 2, 3, 4, 5, 7, 8, 9 //29 is terrain tool
        };

        public VMDefaultValidator(VM vm) : base(vm)
        {
        }

        public override bool CanBuildTool(VMAvatar ava)
        {
            return ava.AvatarState.Permissions >= VMTSOAvatarPermissions.BuildBuyRoommate;
        }

        public override bool CanChangePermissions(VMAvatar ava)
        {
            return ava.AvatarState.Permissions >= VMTSOAvatarPermissions.Owner;
        }

        public override bool CanManageAdmitList(VMAvatar ava)
        {
            return ava.AvatarState.Permissions >= VMTSOAvatarPermissions.Roommate;
        }

        public override bool CanManageAsyncSale(VMAvatar ava, VMGameObject obj)
        {
            if (ava == null || obj == null || obj is VMAvatar || obj.PersistID == 0) return false;
            return ava.AvatarState.Permissions >= VMTSOAvatarPermissions.Roommate && ava.PersistID == (obj.TSOState as VMTSOObjectState).OwnerID;
        }

        public override bool CanManageEnvironment(VMAvatar ava)
        {
            return ava.AvatarState.Permissions >= VMTSOAvatarPermissions.Roommate;
        }

        public override bool CanManageLotSize(VMAvatar ava)
        {
            return ava.AvatarState.Permissions >= VMTSOAvatarPermissions.Owner;
        }

        public override bool CanMoveObject(VMAvatar ava, VMEntity obj)
        {
            return ava.AvatarState.Permissions >= VMTSOAvatarPermissions.Roommate;
        }

        public override bool CanSendbackObject(VMAvatar ava, VMGameObject obj)
        {
            return ava.AvatarState.Permissions >= VMTSOAvatarPermissions.Roommate;
        }

        public override DeleteMode GetDeleteMode(DeleteMode desired, VMAvatar ava, VMEntity obj)
        {
            if (desired > DeleteMode.Delete) return DeleteMode.Disallowed;
            if (obj == null || ava == null) return DeleteMode.Disallowed;
            if (desired == DeleteMode.Sendback)
            {
                if (obj.PersistID == 0 || obj is VMAvatar || !CanSendbackObject(ava, (VMGameObject)obj)) desired = DeleteMode.Disallowed;
            }
            else if (desired == DeleteMode.Delete)
            {
                if (obj is VMAvatar)
                    return (ava.AvatarState.Permissions < VMTSOAvatarPermissions.Admin) ? DeleteMode.Disallowed : DeleteMode.Delete;
                if (ava.AvatarState.Permissions < VMTSOAvatarPermissions.Roommate)
                    desired = DeleteMode.Disallowed;
                else if (obj.PersistID != 0 && ava.PersistID != (obj.TSOState as VMTSOObjectState).OwnerID)
                    desired = DeleteMode.Sendback;
            }
            return desired;
        }

        public override PurchaseMode GetPurchaseMode(PurchaseMode desired, VMAvatar ava, uint guid, bool fromInventory)
        {
            if (desired > PurchaseMode.Donate) return PurchaseMode.Disallowed;
            if (ava == null) return PurchaseMode.Disallowed;
            if (ava.AvatarState.Permissions < VMTSOAvatarPermissions.Roommate) return PurchaseMode.Disallowed;
            
            //todo: build/buy limits for inventory, but allow rare/non-catalog placement
            if (!vm.TS1 && !fromInventory)
            {
                var catalog = Content.Content.Get().WorldCatalog;
                var item = catalog.GetItemByGUID(guid);
                var whitelist = (ava.AvatarState.Permissions == VMTSOAvatarPermissions.Roommate) ? RoomieWhiteList : BuilderWhiteList;
                if (item == null || !whitelist.Contains(item.Value.Category))
                {
                    if (ava.AvatarState.Permissions != VMTSOAvatarPermissions.Admin) return PurchaseMode.Disallowed;
                }
            }

            return PurchaseMode.Normal;
        }
    }
}
