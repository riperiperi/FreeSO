﻿/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            { VMCommandType.ChatEditChan, typeof(VMNetChatEditChanCmd) }
        };
        public static Dictionary<Type, VMCommandType> ReverseMap = CmdMap.ToDictionary(x => x.Value, x => x.Key);

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
            Type cmdType = CmdMap[Type];
            Command = (VMNetCommandBodyAbstract)Activator.CreateInstance(cmdType);
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
        ChatEditChan = 40
    }
}
