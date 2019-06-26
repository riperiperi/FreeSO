using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Model.Platform
{
    public abstract class VMAbstractValidator
    {
        protected VM vm;
        public VMAbstractValidator(VM vm)
        {
            this.vm = vm;
        }

        public abstract DeleteMode GetDeleteMode(DeleteMode desired, VMAvatar ava, VMEntity obj);
        public abstract PurchaseMode GetPurchaseMode(PurchaseMode desired, VMAvatar ava, uint guid, bool fromInventory);
        public abstract bool CanBuildTool(VMAvatar ava);
        public abstract bool CanMoveObject(VMAvatar ava, VMEntity obj);
        public abstract bool CanSendbackObject(VMAvatar ava, VMGameObject obj);
        public abstract bool CanManageAsyncSale(VMAvatar ava, VMGameObject obj);

        public abstract bool CanManageAdmitList(VMAvatar ava);
        public abstract bool CanManageEnvironment(VMAvatar ava);
        public abstract bool CanManageLotSize(VMAvatar ava);
        public abstract bool CanChangePermissions(VMAvatar ava);
    }

    public enum DeleteMode : byte
    {
        Disallowed = 0,
        Sendback,
        Delete
    }

    public enum PurchaseMode : byte
    {
        Disallowed = 0,
        Normal,
        Donate
    }
}
