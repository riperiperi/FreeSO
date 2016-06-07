using FSO.SimAntics.Model.TSOPlatform;
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
        void RequestRoommate(VM vm, VMAvatar avatar);
        void RemoveRoommate(VM vm, VMAvatar avatar);
        void ObtainAvatarFromTicket(VM vm, string ticket, VMAsyncAvatarCallback callback);
        void QueueArchitecture(VMNetArchitectureCmd cmd);
        void LoadPluginPersist(VM vm, uint objectPID, uint pluginID, VMAsyncPluginLoadCallback callback);
        void SavePluginPersist(VM vm, uint objectPID, uint pluginID, byte[] data);

        void Tick(VM vm);
    }

    public delegate void VMAsyncTransactionCallback(bool success, uint uid1, uint budget1, uint uid2, uint budget2);
    public delegate void VMAsyncAvatarCallback(uint persistID, VMTSOAvatarPermissions permissions); //TODO: VMPersistAvatarBlock
    public delegate void VMAsyncPluginLoadCallback(byte[] data); //null if none available
}
