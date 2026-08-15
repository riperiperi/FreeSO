using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FSO.SimAntics.NetPlay.Model.Commands;

namespace FSO.SimAntics.NetPlay.Model
{
    public class VMNetCommand : VMSerializable
    {
        public static Dictionary<VMCommandType, Type> CmdMap = new Dictionary<VMCommandType, Type> {
            { VMCommandType.SimJoin, typeof(VMNetSimJoinCmd) },
            { VMCommandType.Interaction, typeof(VMNetInteractionCmd) },
            { VMCommandType.Architecture, typeof(VMNetArchitectureCmd) },
            { VMCommandType.BuyObject, typeof(VMNetBuyObjectCmd) },
            { VMCommandType.Chat, typeof(VMNetChatCmd) },
            { VMCommandType.BlueprintRestore, typeof(VMBlueprintRestoreCmd) },
            { VMCommandType.SimLeave, typeof(VMNetSimLeaveCmd) },
            { VMCommandType.InteractionCancel, typeof(VMNetInteractionCancelCmd) },
            { VMCommandType.MoveObject, typeof(VMNetMoveObjectCmd) },
            { VMCommandType.DeleteObject, typeof(VMNetDeleteObjectCmd) },
            { VMCommandType.Goto, typeof(VMNetGotoCmd) },
            { VMCommandType.DialogResponse, typeof(VMNetDialogResponseCmd) },
            { VMCommandType.StateSync, typeof(VMStateSyncCmd) },
            { VMCommandType.RequestResync, typeof(VMRequestResyncCmd) },
            { VMCommandType.GenericDialog, typeof(VMGenericDialogCommand) },
            { VMCommandType.AsyncResponse, typeof(VMNetAsyncResponseCmd) },
            { VMCommandType.ChangePermissions, typeof(VMChangePermissionsCmd) },
            { VMCommandType.EODObjEvent, typeof(VMNetEODEventCmd) },
            { VMCommandType.EODMessage, typeof(VMNetEODMessageCmd) },
            { VMCommandType.UpdatePersistState, typeof(VMNetUpdatePersistStateCmd) },
            { VMCommandType.AdjHollowSync, typeof(VMNetAdjHollowSyncCmd) },
            { VMCommandType.SendToInventory, typeof(VMNetSendToInventoryCmd) },
            { VMCommandType.PlaceInventory, typeof(VMNetPlaceInventoryCmd) },
            { VMCommandType.UpdateInventory, typeof(VMNetUpdateInventoryCmd) },
            { VMCommandType.ChangeEnvironment, typeof(VMNetChangeEnvironmentCmd) },
            { VMCommandType.ChangeLotSize, typeof(VMNetChangeLotSizeCmd) },
            { VMCommandType.InteractionResult, typeof(VMNetInteractionResultCmd) },
            { VMCommandType.AsyncPrice, typeof(VMNetAsyncPriceCmd) },
            { VMCommandType.AsyncSale, typeof(VMNetAsyncSaleCmd) },
            { VMCommandType.LockObject, typeof(VMNetLockCmd) },
            { VMCommandType.SkillLock, typeof(VMNetSkillLockCmd) },
            { VMCommandType.SetIgnore, typeof(VMNetSetIgnoreCmd) },
            { VMCommandType.SetRoof, typeof(VMNetSetRoofCmd) },
			{ VMCommandType.SetOutfit, typeof(VMNetSetOutfitCmd) },
            { VMCommandType.TimeoutNotify, typeof(VMNetTimeoutNotifyCmd) },
            { VMCommandType.ChangeControl, typeof(VMNetChangeControlCmd) },
            { VMCommandType.SetTime, typeof(VMNetSetTimeCmd) },
            { VMCommandType.Tuning, typeof(VMNetTuningCmd) },
            { VMCommandType.BatchGraphic, typeof(VMNetBatchGraphicCmd) },
            { VMCommandType.ChatParameters, typeof(VMNetChatParamCmd) },
            { VMCommandType.ChatEditChan, typeof(VMNetChatEditChanCmd) },
            { VMCommandType.Ping, typeof(VMNetPingCmd) },
            { VMCommandType.Upgrade, typeof(VMNetUpgradeCmd) },
            { VMCommandType.Cheat, typeof(VMNetCheatCmd) },
            { VMCommandType.DirectControl, typeof(VMNetDirectControlCommand) },
            { VMCommandType.DirectControlToggle, typeof(VMNetDirectControlToggleCommand) },
            { VMCommandType.SM64Position, typeof(VMNetSM64PositionCmd) },
            { VMCommandType.SM64Event, typeof(VMNetSM64EventCmd) },
            { VMCommandType.SM64AnimData, typeof(VMNetSM64AnimDataCmd) },

            { VMCommandType.BeginFreeRoam, typeof(VMNetBeginFreeRoamCmd) },
            { VMCommandType.GotoLot, typeof(VMNetGotoLotCmd) },
            { VMCommandType.LeaveBuildBuy, typeof(VMNetLeaveBuildBuyCmd) },
        };
        public static Dictionary<Type, VMCommandType> ReverseMap = CmdMap.ToDictionary(x => x.Value, x => x.Key);

        /// <summary>
        /// Compile-time constructors mirroring CmdMap: Mono on WASM fails
        /// Activator.CreateInstance (Arg_NoDefCTor) for types never directly
        /// constructed in the compiled app — deserializing the first server tick
        /// would throw. Types missing here fall back to Activator.
        /// </summary>
        public static Dictionary<VMCommandType, Func<VMNetCommandBodyAbstract>> CmdFactories = new Dictionary<VMCommandType, Func<VMNetCommandBodyAbstract>> {
            { VMCommandType.SimJoin, () => new VMNetSimJoinCmd() },
            { VMCommandType.Interaction, () => new VMNetInteractionCmd() },
            { VMCommandType.Architecture, () => new VMNetArchitectureCmd() },
            { VMCommandType.BuyObject, () => new VMNetBuyObjectCmd() },
            { VMCommandType.Chat, () => new VMNetChatCmd() },
            { VMCommandType.BlueprintRestore, () => new VMBlueprintRestoreCmd() },
            { VMCommandType.SimLeave, () => new VMNetSimLeaveCmd() },
            { VMCommandType.InteractionCancel, () => new VMNetInteractionCancelCmd() },
            { VMCommandType.MoveObject, () => new VMNetMoveObjectCmd() },
            { VMCommandType.DeleteObject, () => new VMNetDeleteObjectCmd() },
            { VMCommandType.Goto, () => new VMNetGotoCmd() },
            { VMCommandType.DialogResponse, () => new VMNetDialogResponseCmd() },
            { VMCommandType.StateSync, () => new VMStateSyncCmd() },
            { VMCommandType.RequestResync, () => new VMRequestResyncCmd() },
            { VMCommandType.GenericDialog, () => new VMGenericDialogCommand() },
            { VMCommandType.AsyncResponse, () => new VMNetAsyncResponseCmd() },
            { VMCommandType.ChangePermissions, () => new VMChangePermissionsCmd() },
            { VMCommandType.EODObjEvent, () => new VMNetEODEventCmd() },
            { VMCommandType.EODMessage, () => new VMNetEODMessageCmd() },
            { VMCommandType.UpdatePersistState, () => new VMNetUpdatePersistStateCmd() },
            { VMCommandType.AdjHollowSync, () => new VMNetAdjHollowSyncCmd() },
            { VMCommandType.SendToInventory, () => new VMNetSendToInventoryCmd() },
            { VMCommandType.PlaceInventory, () => new VMNetPlaceInventoryCmd() },
            { VMCommandType.UpdateInventory, () => new VMNetUpdateInventoryCmd() },
            { VMCommandType.ChangeEnvironment, () => new VMNetChangeEnvironmentCmd() },
            { VMCommandType.ChangeLotSize, () => new VMNetChangeLotSizeCmd() },
            { VMCommandType.InteractionResult, () => new VMNetInteractionResultCmd() },
            { VMCommandType.AsyncPrice, () => new VMNetAsyncPriceCmd() },
            { VMCommandType.AsyncSale, () => new VMNetAsyncSaleCmd() },
            { VMCommandType.LockObject, () => new VMNetLockCmd() },
            { VMCommandType.SkillLock, () => new VMNetSkillLockCmd() },
            { VMCommandType.SetIgnore, () => new VMNetSetIgnoreCmd() },
            { VMCommandType.SetRoof, () => new VMNetSetRoofCmd() },
            { VMCommandType.SetOutfit, () => new VMNetSetOutfitCmd() },
            { VMCommandType.TimeoutNotify, () => new VMNetTimeoutNotifyCmd() },
            { VMCommandType.ChangeControl, () => new VMNetChangeControlCmd() },
            { VMCommandType.SetTime, () => new VMNetSetTimeCmd() },
            { VMCommandType.Tuning, () => new VMNetTuningCmd() },
            { VMCommandType.BatchGraphic, () => new VMNetBatchGraphicCmd() },
            { VMCommandType.ChatParameters, () => new VMNetChatParamCmd() },
            { VMCommandType.ChatEditChan, () => new VMNetChatEditChanCmd() },
            { VMCommandType.Ping, () => new VMNetPingCmd() },
            { VMCommandType.Upgrade, () => new VMNetUpgradeCmd() },
            { VMCommandType.Cheat, () => new VMNetCheatCmd() },
            { VMCommandType.DirectControl, () => new VMNetDirectControlCommand() },
            { VMCommandType.DirectControlToggle, () => new VMNetDirectControlToggleCommand() },
            { VMCommandType.SM64Position, () => new VMNetSM64PositionCmd() },
            { VMCommandType.SM64Event, () => new VMNetSM64EventCmd() },
            { VMCommandType.SM64AnimData, () => new VMNetSM64AnimDataCmd() },
            { VMCommandType.BeginFreeRoam, () => new VMNetBeginFreeRoamCmd() },
            { VMCommandType.GotoLot, () => new VMNetGotoLotCmd() },
            { VMCommandType.LeaveBuildBuy, () => new VMNetLeaveBuildBuyCmd() },
        };

        public VMCommandType Type;
        public VMNetCommandBodyAbstract Command;

        public VMNetCommand()
        {
        }

        public VMNetCommand(VMNetCommandBodyAbstract cmd)
        {
            SetCommand(cmd);
        }

        public void SetCommand(VMNetCommandBodyAbstract cmd)
        {
            Type = ReverseMap[cmd.GetType()];
            Command = cmd;
        }

        #region VMSerializable Members

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            Command.SerializeInto(writer);
        }

        public void Deserialize(BinaryReader reader)
        {
            TryDeserialize(reader, true);
        }

        public bool TryDeserialize(BinaryReader reader, bool isClient)
        {
            Type = (VMCommandType)reader.ReadByte();
            Command = CmdFactories.TryGetValue(Type, out var factory)
                ? factory()
                : (VMNetCommandBodyAbstract)Activator.CreateInstance(CmdMap[Type]);
            if (Command.AcceptFromClient || isClient)
            {
                Command.Deserialize(reader);
                return true;
            }
            return false;
        }

        #endregion

    }

    public enum VMCommandType : byte
    {
        SimJoin = 0,
        Interaction = 1,
        Architecture = 2,
        BuyObject = 3,
        Chat = 4,
        BlueprintRestore = 5,
        SimLeave = 6,
        InteractionCancel = 7,
        MoveObject = 8,
        DeleteObject = 9,
        Goto = 10,
        DialogResponse = 11,
        StateSync = 12,
        RequestResync = 13,
        GenericDialog = 14,
        AsyncResponse = 15,
        ChangePermissions = 16,
        EODObjEvent = 17,
        EODMessage = 18,
        UpdatePersistState = 19,
        AdjHollowSync = 20,

        //inventory
        SendToInventory = 21,
        PlaceInventory = 22,
        UpdateInventory = 23,

        //housemode
        ChangeEnvironment = 24,
        ChangeLotSize = 25,

        InteractionResult = 26,

        AsyncPrice = 27,
        AsyncSale = 28,
        LockObject = 29,
        SkillLock = 30,
        SetIgnore = 31,
        SetRoof = 32,
        SetOutfit = 33,

        TimeoutNotify = 34,
        ChangeControl = 35,
        SetTime = 36,
        Tuning = 37,

        BatchGraphic = 38,

        ChatParameters = 39,
        ChatEditChan = 40,
        Ping = 41,
        Upgrade = 42,

        //ts1 cheat
        Cheat = 43,
        DirectControl = 44,
        DirectControlToggle = 45,
        SM64Position = 46,
        SM64Event = 47,
        SM64AnimData = 48,

        // Archive
        BeginFreeRoam = 49,
        GotoLot = 50,
        LeaveBuildBuy = 51,
    }
}
