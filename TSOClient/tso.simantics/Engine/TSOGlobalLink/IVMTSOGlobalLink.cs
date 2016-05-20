using FSO.SimAntics.NetPlay.Model.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.Engine.TSOTransaction
{
    public interface IVMTSOGlobalLink
    {
        void LeaveLot(VM vm, VMAvatar avatar);
        void PerformTransaction(VM vm, bool testOnly, uint uid1, uint uid2, int amount, VMAsyncTransactionCallback callback);
        void QueueArchitecture(VMNetArchitectureCmd cmd);
        void Tick(VM vm);
    }

    public delegate void VMAsyncTransactionCallback(bool success, uint uid1, uint budget1, uint uid2, uint budget2);
}
