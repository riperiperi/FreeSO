using FSO.SimAntics.Engine.TSOGlobalLink.Model;
using FSO.SimAntics.Entities;
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
        void RequestRoommate(VM vm, uint pid, int mode, byte permissions);
        void RemoveRoommate(VM vm, VMAvatar avatar);
        void ObtainAvatarFromTicket(VM vm, string ticket, VMAsyncAvatarCallback callback);
        void QueueArchitecture(VMNetArchitectureCmd cmd);
        void LoadPluginPersist(VM vm, uint objectPID, uint pluginID, VMAsyncPluginLoadCallback callback);
        void SavePluginPersist(VM vm, uint objectPID, uint pluginID, byte[] data);
        void RegisterNewObject(VM vm, VMEntity obj, VMAsyncPersistIDCallback callback);
        void MoveToInventory(VM vm, VMMultitileGroup obj, VMAsyncInventorySaveCallback callback);
        void ForceInInventory(VM vm, uint objectPID, VMAsyncInventorySaveCallback callback);
        void PurchaseFromOwner(VM vm, VMMultitileGroup obj, uint purchaserPID, VMAsyncInventorySaveCallback callback, VMAsyncTransactionCallback tcallback);
        void RetrieveFromInventory(VM vm, uint objectPID, uint ownerPID, VMAsyncInventoryRetrieveCallback callback);
        void ConsumeInventory(VM vm, uint ownerPID, uint guid, int mode, short num, VMAsyncInventoryConsumeCallback callback);
        void DeleteObject(VM vm, uint objectPID, VMAsyncDeleteObjectCallback callback);
        void SetSpotlightStatus(VM vm, bool on);

        //Outfits
        void StockOutfit(VM vm, VMGLOutfit outfit, VMAsyncStockOutfitCallback callback);
        void GetOutfits(VM vm, VMGLOutfitOwner owner, uint ownerPID, VMAsyncGetOutfitsCallback callback);
        void DeleteOutfit(VM vm, uint outfitPID, VMGLOutfitOwner owner, uint ownerPID, VMAsyncDeleteOutfitCallback callback);
        void UpdateOutfitSalePrice(VM vm, uint outfitPID, uint objectPID, int newSalePrice, VMAsyncUpdateOutfitSalePriceCallback callback);
        void PurchaseOutfit(VM vm, uint outfitPID, uint objectPID, uint avatarPID, VMAsyncPurchaseOutfitCallback callback);

        void Tick(VM vm);
    }

    public delegate void VMAsyncTransactionCallback(bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2);
    public delegate void VMAsyncAvatarCallback(uint persistID, VMTSOAvatarPermissions permissions); //TODO: VMPersistAvatarBlock
    public delegate void VMAsyncPluginLoadCallback(byte[] data); //null if none available
    public delegate void VMAsyncPersistIDCallback(short objectID, uint persistID);

    public delegate void VMAsyncInventorySaveCallback(bool success, uint objid); //todo: failure reasons
    public delegate void VMAsyncInventoryRetrieveCallback(uint guid, byte[] data);
    public delegate void VMAsyncInventoryConsumeCallback(bool success, int result); //todo: failure reasons
    public delegate void VMAsyncDeleteObjectCallback(bool success); //todo: failure reasons

    public delegate void VMAsyncStockOutfitCallback(bool success, uint outfit_id);
    public delegate void VMAsyncGetOutfitsCallback(VMGLOutfit[] outfits);
    public delegate void VMAsyncDeleteOutfitCallback(bool success);
    public delegate void VMAsyncUpdateOutfitSalePriceCallback(bool success);
    public delegate void VMAsyncPurchaseOutfitCallback(bool success);
}
