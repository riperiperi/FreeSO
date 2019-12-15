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
using FSO.Files.Formats.IFF.Chunks;
using FSO.Vitaboy;
using System.IO;
using FSO.SimAntics.Model;

namespace FSO.SimAntics.Primitives
{
    public class VMChangeSuitOrAccessory : VMPrimitiveHandler {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMChangeSuitOrAccessoryOperand)args;
            var avatar = (VMAvatar)context.Caller;

            var outfitType = GetOutfitType(operand);

            if (operand.SuitScope == VMSuitScope.Object && (operand.Flags & VMChangeSuitOrAccessoryFlags.Update) == VMChangeSuitOrAccessoryFlags.Update)
            { //update default outfit with outfit in stringset 304 with index in temp 0
                avatar.DefaultSuits.Daywear = VMOutfitReference.Parse(context.Callee.Object.Resource.Get<STR>(304).GetString((context.Thread.TempRegisters[0])), context.VM.TS1);
                avatar.BodyOutfit = avatar.DefaultSuits.Daywear;
            } 
            else 
            {
                var data = operand.SuitData;
                if (operand.Flags.HasFlag(VMChangeSuitOrAccessoryFlags.UseTemp)) data = (byte)context.Thread.TempRegisters[data];
                var suit = VMSuitProvider.GetSuit(context, operand.SuitScope, data);
                if (suit == null){
                    return VMPrimitiveExitCode.GOTO_TRUE;
                }

                if (suit is string)
                {
                    var suitFile = (string)suit;
                    var apr = (VM.UseWorld) ? FSO.Content.Content.Get().AvatarAppearances.Get(suitFile) : null;
                    if ((operand.Flags & VMChangeSuitOrAccessoryFlags.Remove) == VMChangeSuitOrAccessoryFlags.Remove)
                    {
                        avatar.BoundAppearances.Remove(suitFile);
                        if (VM.UseWorld && apr != null) avatar.Avatar.RemoveAccessory(apr);
                    }
                    else
                    {
                        avatar.BoundAppearances.Add(suitFile);
                        if (VM.UseWorld && apr != null) avatar.Avatar.AddAccessory(apr);
                    }
                } else if (suit is VMOutfitReference)
                {
                    avatar.SetPersonData(Model.VMPersonDataVariable.CurrentOutfit, operand.SuitData);
                    avatar.BodyOutfit = (VMOutfitReference)suit;
                } else if (suit is ulong)
                {
                    if (outfitType == OutfitType.BODY)
                    {
                        avatar.SetPersonData(Model.VMPersonDataVariable.CurrentOutfit, operand.SuitData);
                        avatar.BodyOutfit = new VMOutfitReference((ulong)suit);
                    }else if(outfitType == OutfitType.ACCESSORY){
                        if (VM.UseWorld){
                            var outfit = Content.Content.Get().AvatarOutfits?.Get((ulong)suit);

                            if ((operand.Flags & VMChangeSuitOrAccessoryFlags.Remove) == VMChangeSuitOrAccessoryFlags.Remove)
                            {
                                avatar.Avatar.RemoveAccessory(outfit);
                            }
                            else
                            {
                                //The clothing rack does not seem to have any way to remove accessories so I have implemented as a toggle
                                //until we know better
                                switch ((VMPersonSuits)operand.SuitData)
                                {
                                    case VMPersonSuits.DecorationHead:
                                        if(avatar.Avatar.DecorationHead == outfit){
                                            //Remove it
                                            avatar.Avatar.DecorationHead = null;
                                        }else{
                                            //Add it
                                            avatar.Avatar.DecorationHead = outfit;
                                        }
                                        break;
                                    case VMPersonSuits.DecorationBack:
                                        if (avatar.Avatar.DecorationBack == outfit){
                                            //Remove it
                                            avatar.Avatar.DecorationBack = null;
                                        }else{
                                            //Add it
                                            avatar.Avatar.DecorationBack = outfit;
                                        }
                                        break;
                                    case VMPersonSuits.DecorationShoes:
                                        if (avatar.Avatar.DecorationShoes == outfit){
                                            //Remove it
                                            avatar.Avatar.DecorationShoes = null;
                                        }else{
                                            //Add it
                                            avatar.Avatar.DecorationShoes = outfit;
                                        }
                                        break;
                                    case VMPersonSuits.DecorationTail:
                                        if (avatar.Avatar.DecorationTail == outfit){
                                            //Remove it
                                            avatar.Avatar.DecorationTail = null;
                                        }else{
                                            //Add it
                                            avatar.Avatar.DecorationTail = outfit;
                                        }
                                        break;
                                }
                                
                            }
                        }
                    }
                }
            }

            return VMPrimitiveExitCode.GOTO_TRUE;
        }


        private OutfitType GetOutfitType(VMChangeSuitOrAccessoryOperand operand)
        {
            switch (operand.SuitScope)
            {
                case VMSuitScope.Global:
                case VMSuitScope.Object:
                    return OutfitType.ACCESSORY;
                case VMSuitScope.Person:
                    switch ((VMPersonSuits)operand.SuitData)
                    {
                        case VMPersonSuits.DefaultDaywear:
                        case VMPersonSuits.DefaultSleepwear:
                        case VMPersonSuits.DefaultSwimwear:
                        case VMPersonSuits.DynamicCostume:
                        case VMPersonSuits.DynamicDaywear:
                        case VMPersonSuits.DynamicSleepwear:
                        case VMPersonSuits.DynamicSwimwear:
                        case VMPersonSuits.JobOutfit:
                        case VMPersonSuits.Naked:
                        case VMPersonSuits.SkeletonMinus:
                        case VMPersonSuits.SkeletonPlus:
                        case VMPersonSuits.TeleporterMishap:
                            return OutfitType.BODY;

                        default:
                            return OutfitType.ACCESSORY;
                    }
                default:
                    return OutfitType.ACCESSORY;
            }
        }
        
    }

    public enum OutfitType
    {
        HEAD,
        BODY,
        ACCESSORY
    }

    public class VMChangeSuitOrAccessoryOperand : VMPrimitiveOperand {

        public byte SuitData { get; set; }
        public VMSuitScope SuitScope { get; set; }
        public VMChangeSuitOrAccessoryFlags Flags { get; set; }

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                SuitData = io.ReadByte();
                SuitScope = (VMSuitScope)io.ReadByte();
                Flags = (VMChangeSuitOrAccessoryFlags)io.ReadUInt16();
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(SuitData);
                io.Write((byte)SuitScope);
                io.Write((ushort)Flags);
            }
        }
        #endregion
    }

    [Flags]
    public enum VMChangeSuitOrAccessoryFlags
    {
        Remove = 1,
        UseTemp = 2,
        Update = 4
    }
}
