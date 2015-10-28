/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.SimAntics.Engine;
using FSO.Files.Utils;
using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.Engine.Utils;
using System.IO;

namespace FSO.SimAntics.Primitives
{
    public class VMTransferFunds : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMTransferFundsOperand)args;
            return VMPrimitiveExitCode.GOTO_TRUE;
            //disable for now.
            /** Bit of a legacy thing going on here so there is a helper to translate old owner values into the new scope handler **/
            /*var ammount = VMMemory.GetVariable(context, operand.GetAmmountOwner(), operand.AmmountData);

            return VMPrimitiveExitCode.GOTO_TRUE;*/
        }
    }

    public class VMTransferFundsOperand : VMPrimitiveOperand
    {
        public VMTransferFundsOldOwner OldAmmountOwner;
        public VMVariableScope AmmountOwner;
        public ushort AmmountData;
        public VMTransferFundsFlags Flags;
        public VMTransferFundsExpenseType ExpenseType;
        public VMTransferFundsType TransferType;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                OldAmmountOwner = (VMTransferFundsOldOwner)io.ReadByte();
                AmmountOwner = (VMVariableScope)io.ReadByte();

                //TODO: Not certain of the boundaries for the next 2 fields
                Flags = (VMTransferFundsFlags)io.ReadUInt32();
                ExpenseType = (VMTransferFundsExpenseType)io.ReadByte();
                TransferType = (VMTransferFundsType)io.ReadByte();
            }
        }
        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write((byte)OldAmmountOwner);
                io.Write((byte)AmmountOwner);
                io.Write((uint)Flags);
                io.Write((byte)ExpenseType);
                io.Write((byte)TransferType);
            }
        }
        #endregion

        public VMVariableScope GetAmmountOwner()
        {
            switch (OldAmmountOwner){
                case VMTransferFundsOldOwner.Literal:
                    return VMVariableScope.Literal;
                case VMTransferFundsOldOwner.Parameters:
                    return VMVariableScope.Parameters;
                case VMTransferFundsOldOwner.Local:
                    return VMVariableScope.Local;

                default:
                case VMTransferFundsOldOwner.Normal:
                    return AmmountOwner;

            }
        }
    }

    public enum VMTransferFundsExpenseType
    {
       ObjectRefillsAndMaintinance = 6
    }

    public enum VMTransferFundsType {
        DEPRICATED_ADD = 0,
        DEPRICATED_SUBTRACT = 1,
        MeToMaxis = 2,
        DEPRICATED_ME_TO_STACK_OBJECT = 3,
        DEPRICATED_ME_TO_STACK_OBJECTS_OWNER = 4,
        MaxisToMe = 5,
        MaxisToStackObjectsOwner = 6,
        StackObjectsOwnerToMaxis = 7,
        //TODO: This got cut off in edith, use a form hooking program to get full names.
        DEPRICATED_OBJECT_IN_TEMP_0 = 8,
        //TODO: This got cut off in edith, use a form hooking program to get full names.
        DEPRIACTED_FROM_OWNER_OF_OBJECT = 9,
        //TODO: This got cut off in edith, use a form hooking program to get full names.
        PUT_AMMOUNT_OF_CASH_IN_STACK_OBJECT_INTO_TEMP = 10,
        STACK_OBJECT_TO_LOT_ROOMATES = 11,
        LOT_OWNER_TO_MAXIS = 12,
        MAXIS_TO_LOT_OWNER = 13,
        UNKNOWN = 14,
        CHARACTER_TO_SHARED_BANK_ACCOUNT = 15,
        SHARED_BANK_ACCOUNT_TO_CHARACTER = 16,
        ME_TO_LOT_ROOMATES = 17
    }

    public enum VMTransferFundsOldOwner {
        Literal = 0,
        Parameters = 1,
        Local = 2,
        Normal = 3
    }

    [Flags]
    public enum VMTransferFundsFlags
    {
        JustTest = 0x1,
        Subtract = 0x2
    }
}
