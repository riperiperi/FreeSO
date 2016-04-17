using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.Engine.TSOTransaction
{
    /// <summary>
    /// Does not access an external database. Does the best it can with the information it currently has.
    /// Changes made do not persist to database, and can be overwritten very easily.
    /// In the final setup, should only be used for client check trees.
    /// </summary>
    public class VMTSOGlobalLinkStub : IVMTSOGlobalLink
    {
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
    }
}
