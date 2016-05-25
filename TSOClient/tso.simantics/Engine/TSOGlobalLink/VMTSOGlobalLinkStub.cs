using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.Primitives;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.NetPlay;
using FSO.SimAntics.Engine.TSOGlobalLink;
using FSO.SimAntics.Model.TSOPlatform;

namespace FSO.SimAntics.Engine.TSOTransaction
{
    /// <summary>
    /// Does not access an external database. Does the best it can with the information it currently has.
    /// Changes made do not persist to database, and can be overwritten very easily.
    /// In the final setup, should only be used for client check trees.
    /// </summary>
    public class VMTSOGlobalLinkStub : IVMTSOGlobalLink
    {
        private Queue<VMNetArchitectureCmd> ArchBuffer = new Queue<VMNetArchitectureCmd>();
        public VMTSOStandaloneDatabase Database = new VMTSOStandaloneDatabase();
        private bool WaitingOnArch;

        public void PerformTransaction(VM vm, bool testOnly, uint uid1, uint uid2, int amount, VMAsyncTransactionCallback callback)
        {
            var result = PerformTransaction(vm, testOnly, uid1, uid2, amount);
            if (callback != null)
            {
                var obj1 = vm.GetObjectByPersist(uid1);
                var obj2 = vm.GetObjectByPersist(uid2);

                new System.Threading.Thread(() =>
                {
                    //System.Threading.Thread.Sleep(250);
                    callback(result,
                        uid1, (obj1 == null) ? 0 : obj1.TSOState.Budget.Value,
                        uid2, (obj2 == null) ? 0 : obj2.TSOState.Budget.Value);
                }).Start();
            }
        }

        public bool PerformTransaction(VM vm, bool testOnly, uint uid1, uint uid2, int amount)
        {
            var obj1 = vm.GetObjectByPersist(uid1);
            var obj2 = vm.GetObjectByPersist(uid2);

            // max value ID is "maxis"
            if ((uid1 != uint.MaxValue && obj1 == null) || (uid2 != uint.MaxValue && obj2 == null)) return false;

            // fail if the target is an object and they're not on lot
            if (obj1 != null && !obj1.TSOState.Budget.CanTransact(-amount)) return false;
            if (obj2 != null && !obj2.TSOState.Budget.CanTransact(amount)) return false;

            if (!testOnly)
            {
                if (obj1 != null) obj1.TSOState.Budget.Transaction(-amount);
                if (obj2 != null) obj2.TSOState.Budget.Transaction(amount);
            }
            return true;
        }

        public void QueueArchitecture(VMNetArchitectureCmd cmd)
        {
            lock (ArchBuffer)
            {
                ArchBuffer.Enqueue(cmd);
            }
        }

        public void Tick(VM vm)
        {
            lock (ArchBuffer)
            {
                while (!WaitingOnArch && ArchBuffer.Count > 0)
                {
                    var cmd = ArchBuffer.Dequeue();
                    var cost = vm.Context.Architecture.SimulateCommands(cmd.Commands, false);
                    if (cost == 0)
                    {
                        //just send it
                        cmd.Verified = true;
                        vm.ForwardCommand(cmd);
                    }
                    else
                    {
                        uint source, target;
                        if (cost > 0) { source = cmd.ActorUID; target = uint.MaxValue; }
                        else { source = uint.MaxValue; target = cmd.ActorUID; }
                        WaitingOnArch = true;
                        PerformTransaction(vm, false, source, target, Math.Abs(cost),
                            (bool success, uint uid1, uint budget1, uint uid2, uint budget2) =>
                            {
                                lock (ArchBuffer) WaitingOnArch = false;
                                if (success)
                                {
                                    cmd.Verified = true;
                                    vm.ForwardCommand(cmd);
                                }
                                vm.SendCommand(new VMNetAsyncResponseCmd(0, new VMTransferFundsState
                                { //update budgets on clients. id of 0 means there is no target thread.
                                    Responded = true,
                                    Success = success,
                                    UID1 = uid1,
                                    Budget1 = budget1,
                                    UID2 = uid2,
                                    Budget2 = budget2
                                }));
                            });
                    }
                }
            }
        }

        public void LeaveLot(VM vm, VMAvatar avatar)
        {
            //TODO: in the global server, this will save the avatar (and possibly lot) states and send back to server.
            if (avatar.PersistID == vm.MyUID)
            {
                //stub has some functionality here. if we have left lot, disconnect.
                vm.CloseNet(VMCloseNetReason.LeaveLot);
            }
            avatar.Delete(true, vm.Context);
            vm.Context.VM.SignalChatEvent(new VMChatEvent(avatar.PersistID, VMChatEventType.Leave, avatar.Name));
        }

        public void RequestRoommate(VM vm, VMAvatar avatar)
        {
            //in final game: signal to city server persistant roommate request state.
            //right now: immedaiately add as roommate
            vm.ForwardCommand(new VMChangePermissionsCmd()
            {
                TargetUID = avatar.PersistID,
                Level = VMTSOAvatarPermissions.Roommate,
                Verified = true
            });
        }

        public void RemoveRoommate(VM vm, VMAvatar avatar)
        {
            //in final game: signal to city server persistant roommate request state.
            //right now: immedaiately remove
            vm.ForwardCommand(new VMChangePermissionsCmd()
            {
                TargetUID = avatar.PersistID,
                Level = VMTSOAvatarPermissions.Visitor,
                Verified = true
            });
        }

        public void ObtainAvatarFromTicket(VM vm, string ticket, VMAsyncAvatarCallback callback)
        {
            //gets an avatar from our stub database based on their ticket
            var uid = Database.FindOrAddAvatar(ticket);

            var permissions = VMTSOAvatarPermissions.Visitor;
            if (Database.Administrators.Contains(uid)) permissions = VMTSOAvatarPermissions.Admin;
            else if (vm.TSOState.BuildRoommates.Contains(uid)) permissions = VMTSOAvatarPermissions.BuildBuyRoommate;
            else if (vm.TSOState.Roommates.Contains(uid)) permissions = VMTSOAvatarPermissions.Roommate;

            callback(uid, permissions);
        }
    }
}
