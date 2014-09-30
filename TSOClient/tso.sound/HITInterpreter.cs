/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
Rhys Simpson. All Rights Reserved.

Contributor(s): Mats 'Afr0' Vederhus
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.HIT;
using TSO.Files.HIT;

namespace TSO.HIT
{
    public class HITInterpreter
    {
        public static HITInstruction[] Instructions = new HITInstruction[] {
            NOP,            Note,           NoteOn,         NoteOff,        LoadB,          LoadL,          Set,            Call, 
            Return,         Wait,           CallEntryPoint, WaitSamp,       End,            Jump,           Test,           NOP, 
            Add,            Sub,            Div,            Mul,            Cmp,            Less,           Greater,        Not, 
            Rand,           Abs,            Limit,          Error,          Assert,         AddToGroup,     RemoveFromGroup, GetVar, 
            Loop,           SetLoop,        Callback,       SmartAdd,       SmartRemove,    SmartRemoveAll, SmartSetCrit,   SmartChoose, 
            And,            NAnd,           Or,             NOr,            XOr,            Max,            Min,            Inc, 
            Dec,            PrintReg,       PlayTrack,      KillTrack,      Push,           PushMask,       PushVars,       CallMask, 
            CallPush,       Pop,            Test1,          Test2,          Test3,          Test4,          IfEqual,        IfNotEqual,
            IfGreater,      IfLess,         IfGreatOrEq,    IfLessOrEq,     SmartSetList,   SeqGroupKill,   SeqGroupWait,   SeqGroupReturn,
            GetSrcDataField, SeqGroupTrackID, SetLL,        SetLT,          SetTL,          WaitEqual,      WaitNotEqual,   WaitGreater,
            WaitLess,       WaitGreatOrEq,  WaitLessOrEq,   Duck,           Unduck,         TestX,          SetLG,          SetGL,
            Throw,          SetSrcDataField, StopTrack,     SetChanReg,     PlayNote,       StopNote,       KillNote,       SmartIndex,
            NoteOnLoop

        };

        /// <summary>
        /// Does nothing.
        /// </summary>
        public static HITResult NOP(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Note(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        /// <summary>
        /// Play a note, whose ID resides in the specified variable.
        /// </summary>
        public static HITResult NoteOn(HITThread thread)
        {
            var dest = thread.ReadByte();

            thread.WriteVar(dest, thread.NoteOn());
            
            return HITResult.CONTINUE;
        }

        public static HITResult NoteOff(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        /// <summary>
        /// Sign-extend a 1-byte constant to 4 bytes and write to a variable.
        /// </summary>
        public static HITResult LoadB(HITThread thread)
        {
            var dest = thread.ReadByte();
            var value = (sbyte)thread.ReadByte();

            thread.WriteVar(dest, value);

            thread.SetFlags(value);

            return HITResult.CONTINUE;
        }

        /// <summary>
        /// Write a 4-byte constant to a variable.
        /// </summary>
        public static HITResult LoadL(HITThread thread)
        {
            var dest = thread.ReadByte();
            var value = thread.ReadInt32();

            thread.WriteVar(dest, value);
            thread.SetFlags(value);

            return HITResult.CONTINUE;
        }

        /// <summary>
        /// Copy the contents of one variable into another.
        /// </summary>
        public static HITResult Set(HITThread thread)
        {
            var dest = thread.ReadByte();
            var src = thread.ReadVar(thread.ReadByte());

            thread.WriteVar(dest, src);

            thread.SetFlags(src);

            return HITResult.CONTINUE;
        }

        /// <summary>
        /// Call a function; push the instruction pointer and jump to the given address.
        /// </summary>
        public static HITResult Call(HITThread thread)
        {
            uint targ = thread.ReadUInt32();
            thread.Stack.Push((int)thread.PC);
            thread.PC = targ;

            return HITResult.CONTINUE;
        }

        /// <summary>
        /// Kill this thread.
        /// </summary>
        public static HITResult Return(HITThread thread)
        {
            return HITResult.KILL;
        }

        /// <summary>
        /// Wait for a length of time in milliseconds, specified by a variable.
        /// </summary>
        public static HITResult Wait(HITThread thread)
        {
            var src = thread.ReadByte();
            if (thread.WaitRemain == -1) thread.WaitRemain = thread.ReadVar(src);
            thread.WaitRemain -= 16; //assuming tick rate is 60 times a second
            if (thread.WaitRemain > 0)
            {
                thread.PC -= 2;
                return HITResult.HALT;
            }
            else
            {
                thread.WaitRemain = -1;
                return HITResult.CONTINUE;
            }
        }

        public static HITResult CallEntryPoint(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        /// <summary>
        /// Wait for the previously selected note to finish playing.
        /// </summary>
        public static HITResult WaitSamp(HITThread thread)
        {
            if (thread.NoteActive(thread.LastNote))
            {
                thread.PC--;
                return HITResult.HALT;
            }
            else
                return HITResult.HALT;
        }

        /// <summary>
        /// Return from this function; pop the instruction pointer from the stack and jump.
        /// </summary>
        public static HITResult End(HITThread thread)
        {
            if (thread.Stack.Count > 0)
            {
                thread.PC = (uint)thread.Stack.Pop();
                return HITResult.CONTINUE;
            }
            else
            {
                return HITResult.KILL;
            }
        }

        /// <summary>
        /// Jump to a given address.
        /// </summary>
        public static HITResult Jump(HITThread thread)
        {
            var read = thread.ReadByte();

            if (read > 15) //literal
            {
                thread.PC--; //backtraaackkk
                thread.PC = thread.ReadUInt32();
            }
            else //no idea if there are collisions. if there are i'm blaming fatbag. >:)
            {
                thread.PC = (uint)thread.ReadVar(read);
                if (thread.ReadByte() == 0) thread.PC += 2; //if next is no-op, the operand is 4 byte
                else thread.PC--; //operand is 1 byte (next is an instruction), backtrack
            }

            return HITResult.CONTINUE;
        }

        /// <summary>
        /// Examine a variable and set the flags.
        /// </summary>
        public static HITResult Test(HITThread thread)
        {
            var value = thread.ReadVar(thread.ReadByte());

            thread.SetFlags(value);

            return HITResult.CONTINUE;
        }

        /// <summary>
        /// Increment a "dest" variable by a "src" variable.
        /// </summary>
        public static HITResult Add(HITThread thread) //0x10
        {
            var dest = thread.ReadByte();
            var src = thread.ReadByte();

            var result = thread.ReadVar(dest) + thread.ReadVar(src);
            thread.WriteVar(dest, result);

            thread.SetFlags(result);

            return HITResult.CONTINUE;
        }

        /// <summary>
        /// Decrement a "dest" variable by a "src" variable.
        /// </summary>
        public static HITResult Sub(HITThread thread)
        {
            var dest = thread.ReadByte();
            var src = thread.ReadByte();

            var result = thread.ReadVar(dest) - thread.ReadVar(src);
            thread.WriteVar(dest, result);

            thread.SetFlags(result);

            return HITResult.CONTINUE;
        }

        /// <summary>
        /// Divide a "dest" variable by a "src" variable, and round the result towards zero (truncate).
        /// </summary>
        public static HITResult Div(HITThread thread)
        {
            var dest = thread.ReadByte();
            var src = thread.ReadByte();

            var result = thread.ReadVar(dest) / thread.ReadVar(src);
            thread.WriteVar(dest, result);

            thread.SetFlags(result);

            return HITResult.CONTINUE;
        }

        /// <summary>
        /// Multiply a "dest" variable by a "src" variable.
        /// </summary>
        public static HITResult Mul(HITThread thread)
        {
            var dest = thread.ReadByte();
            var src = thread.ReadByte();

            var result = thread.ReadVar(dest) * thread.ReadVar(src);
            thread.WriteVar(dest, result);

            thread.SetFlags(result);

            return HITResult.CONTINUE;
        }

        /// <summary>
        /// Compare two variables and set the flags.
        /// </summary>
        public static HITResult Cmp(HITThread thread) //same as sub, but does not set afterwards.
        {
            var dest = thread.ReadByte();
            var src = thread.ReadByte();

            var result = thread.ReadVar(dest) - thread.ReadVar(src);

            thread.SetFlags(result);

            return HITResult.CONTINUE;
        }

        public static HITResult Less(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult Greater(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult Not(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        /// <summary>
        /// Generate a random number between "low" and "high" variables, inclusive, 
        /// and store the result in the "dest" variable.
        /// </summary>
        public static HITResult Rand(HITThread thread)
        {
            var dest = thread.ReadByte();
            var low = thread.ReadByte();
            var high = thread.ReadByte();

            var result = (new Random()).Next(high+1-low)+low;
            thread.WriteVar(dest, result);

            thread.SetFlags(result);

            return HITResult.CONTINUE;
        }

        public static HITResult Abs(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult Limit(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult Error(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult Assert(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult AddToGroup(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult RemoveFromGroup(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult GetVar(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        /// <summary>
        /// Jump back to the loop point (start of track subroutine by default).
        /// </summary>
        public static HITResult Loop(HITThread thread) //0x20
        {
            thread.PC = (uint)thread.LoopPointer;
            return HITResult.CONTINUE;
        }

        /// <summary>
        /// Set the loop point to the current position.
        /// </summary>
        public static HITResult SetLoop(HITThread thread)
        {
            thread.LoopPointer = (int)thread.PC;
            return HITResult.CONTINUE;
        }

        public static HITResult Callback(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult SmartAdd(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult SmartRemove(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult SmartRemoveAll(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult SmartSetCrit(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        /// <summary>
        /// Set the specified variable to a random entry from the selected hitlist.
        /// </summary>
        public static HITResult SmartChoose(HITThread thread)
        {
            var dest = thread.ReadByte();
            thread.WriteVar(dest, (int)thread.HitlistChoose());
            return HITResult.CONTINUE;
        }

        public static HITResult And(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult NAnd(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult Or(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult NOr(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult XOr(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        /// <summary>
        /// Find the higher of a "dest" variable and a "src" constant and store the result in the variable.
        /// </summary>
        public static HITResult Max(HITThread thread)
        {
            var dest = thread.ReadByte();
            var src = thread.ReadInt32();

            var result = Math.Max(thread.ReadVar(dest), src);
            thread.WriteVar(dest, result);

            thread.SetFlags(result);

            return HITResult.CONTINUE;
        }

        /// <summary>
        /// Find the lower of a "dest" variable and a "src" constant and store the result in the variable.
        /// </summary>
        public static HITResult Min(HITThread thread)
        {
            var dest = thread.ReadByte();
            var src = thread.ReadInt32();

            var result = Math.Min(thread.ReadVar(dest), src);
            thread.WriteVar(dest, result);

            thread.SetFlags(result);
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult Inc(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult Dec(HITThread thread) //0x30
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult PrintReg(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        /// <summary>
        /// Play a track, whose ID resides in the specified variable.
        /// </summary>
        public static HITResult PlayTrack(HITThread thread)
        {
            var dest = thread.ReadByte();
            return HITResult.CONTINUE; //Not used in TSO.
        }

        /// <summary>
        /// Kill a track, whose ID resides in the specified variable.
        /// </summary>
        public static HITResult KillTrack(HITThread thread)
        {
            var src = thread.ReadByte();
            return HITResult.CONTINUE;
        }

        public static HITResult Push(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult PushMask(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult PushVars(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult CallMask(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult CallPush(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult Pop(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult Test1(HITThread thread)
        {
            //no idea what these do. examples?
            return HITResult.CONTINUE;
        }

        public static HITResult Test2(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Test3(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Test4(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        /// <summary>
        /// If the zero flag is set, jump to the given address.
        /// </summary>
        public static HITResult IfEqual(HITThread thread)
        {
            var loc = thread.ReadUInt32();

            if (thread.ZeroFlag) thread.PC = loc;

            return HITResult.CONTINUE;
        }

        /// <summary>
        /// If the zero flag is not set, jump to the given address.
        /// </summary>
        public static HITResult IfNotEqual(HITThread thread)
        {
            var loc = thread.ReadUInt32();

            if (!thread.ZeroFlag) thread.PC = loc;

            return HITResult.CONTINUE;
        }

        /// <summary>
        /// If the sign flag is not set and the zero flag is not set, jump to the given address
        /// </summary>
        public static HITResult IfGreater(HITThread thread) //0x40
        {
            var loc = thread.ReadUInt32();

            if (!thread.SignFlag && !thread.ZeroFlag) thread.PC = loc; //last set/compare result was > 0

            return HITResult.CONTINUE;
        }

        /// <summary>
        /// If the sign flag is set, jump to the given address.
        /// </summary>
        public static HITResult IfLess(HITThread thread)
        {
            var loc = thread.ReadUInt32();

            if (thread.SignFlag) thread.PC = loc; //last set/compare result was < 0

            return HITResult.CONTINUE;
        }

        /// <summary>
        /// If the sign flag is not set, jump to the given address.
        /// </summary>
        public static HITResult IfGreatOrEq(HITThread thread)
        {
            var loc = thread.ReadUInt32();

            if (!thread.SignFlag) thread.PC = loc; //last set/compare result was >= 0

            return HITResult.CONTINUE;
        }

        /// <summary>
        /// If the sign flag is set or the zero flag is set, jump to the given address.
        /// </summary>
        public static HITResult IfLessOrEq(HITThread thread)
        {
            var loc = thread.ReadUInt32();

            if (thread.SignFlag || thread.ZeroFlag) thread.PC = loc; //last set/compare result was <= 0

            return HITResult.CONTINUE;
        }

        /// <summary>
        /// Choose a global hitlist, or 0 for the one local to the track (source: defaultsyms.txt).
        /// </summary>
        public static HITResult SmartSetList(HITThread thread)
        { //sets the hitlist
            var src = thread.ReadByte();
            thread.LoadHitlist((uint)thread.ReadVar(src));

            return HITResult.CONTINUE;
        }

        /// <summary>
        /// Kill an actor's vocals, given a constant ID.
        /// </summary>
        public static HITResult SeqGroupKill(HITThread thread)
        {
            var src = thread.ReadByte();

            if (src == (byte)HITPerson.Instance)
                thread.KillVocals();
            else
            {
                //TODO: Implement system for keeping track of which object created a thread
                //      and kill that thread's sounds (src == ObjectID).
            }

            return HITResult.CONTINUE;
        }

        public static HITResult SeqGroupWait(HITThread thread) //unused in the sims
        {
            return HITResult.CONTINUE;
        }

        /// <summary>
        /// Kill a sequence group with the return value specified by a constant.
        /// </summary>
        public static HITResult SeqGroupReturn(HITThread thread)
        {
            var src = thread.ReadByte();
            return HITResult.CONTINUE;
        }

        public static HITResult GetSrcDataField(HITThread thread)
        {
            var dest = thread.ReadByte();
            var srcID = thread.ReadByte();
            var field = thread.ReadByte();

            //looks like this reads from object vars, though gender is apparently field 0 whereas object vars 
            //start at IsInsideViewFrustrum...
            thread.WriteVar(dest, 0); //set it to 0 for now...

            thread.SetFlags(0);

            return HITResult.CONTINUE;
        }

        public static HITResult SeqGroupTrackID(HITThread thread)
        {
            var dest = thread.ReadByte(); //uhhhh
            var src = thread.ReadByte();
            return HITResult.CONTINUE;
        }

        /// <summary>
        /// Copy the contents of one variable into another (equivalent to set and settt; 
        /// defaultsyms.txt says "ISN'T THIS THE SAME AS SET TOO?")
        /// </summary>
        public static HITResult SetLL(HITThread thread)
        {
            Set(thread);
            return HITResult.CONTINUE;
        }

        public static HITResult SetLT(HITThread thread)
        {
            //set local... to... t... yeah i don't know either
            //might be object vars

            var dest = thread.ReadByte();
            var src = thread.ReadByte();

            return HITResult.CONTINUE;
        }

        public static HITResult SetTL(HITThread thread)
        {
            var dest = thread.ReadByte();
            var src = thread.ReadByte();

            return HITResult.CONTINUE; //unused in the sims
        }

        /// <summary>
        /// Wait until two variables are equal.
        /// </summary>
        public static HITResult WaitEqual(HITThread thread)
        {
            var dest = thread.ReadByte();
            var src = thread.ReadByte();

            if (thread.ReadVar(dest) != thread.ReadVar(dest))
            {
                thread.PC -= 3;
                return HITResult.HALT;
            }
            else
                return HITResult.CONTINUE;
        }

        public static HITResult WaitNotEqual(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult WaitGreater(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult WaitLess(HITThread thread) //0x50
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult WaitGreatOrEq(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult WaitLessOrEq(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        /// <summary>
        /// Ducks all audio with priority lower than this. 
        /// Imagine all the other sounds getting quieter when the fire music plays.
        /// </summary>
        public static HITResult Duck(HITThread thread)
        {
            thread.Duck();
            return HITResult.CONTINUE;
        }

        /// <summary>
        /// Unducks all audio back to the volume before Duck() was called.
        /// </summary>
        public static HITResult Unduck(HITThread thread)
        {
            thread.Unduck();
            return HITResult.CONTINUE; //quack
        }

        public static HITResult TestX(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        /// <summary>
        /// Set global = local (source: defaultsyms.txt).
        /// </summary>
        public static HITResult SetLG(HITThread thread)
        {
            var dest = thread.ReadByte();
            var src = thread.ReadInt32(); //why is this an int32

            return HITResult.CONTINUE;
        }

        /// <summary>
        /// Read globally, set locally (source: defaultsyms.txt).
        /// </summary>
        public static HITResult SetGL(HITThread thread)
        {
            var dest = thread.ReadByte();
            var src = thread.ReadInt32(); //why is this an int32

            return HITResult.CONTINUE;
        }

        public static HITResult Throw(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult SetSrcDataField(HITThread thread)
        {
            var idk = thread.ReadByte();
            var srcID = thread.ReadByte();
            var idk2 = thread.ReadByte();

            return HITResult.CONTINUE; //you can set these??? what
        }

        /// <summary>
        /// Stop playing a track, whose ID resides in the specified variable.
        /// </summary>
        public static HITResult StopTrack(HITThread thread)
        {
            var src = thread.ReadByte();
            return HITResult.CONTINUE;
        }

        public static HITResult SetChanReg(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult PlayNote(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult StopNote(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult KillNote(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        /// <summary>
        /// Load a track ("index" variable) from a hitlist ("table" variable).
        /// </summary>
        public static HITResult SmartIndex(HITThread thread)
        {
            var table = thread.ReadVar(thread.ReadByte());
            var index = thread.ReadVar(thread.ReadByte());

            thread.LoadHitlist((byte)table);
            thread.LoadTrack(index);

            return HITResult.CONTINUE; //Appears to be unused.
        }

        public static HITResult NoteOnLoop(HITThread thread) //0x60
        {
            var dest = thread.ReadByte();
            thread.WriteVar(dest, thread.NoteLoop());

            return HITResult.CONTINUE;
        }
    }
    public delegate HITResult HITInstruction(HITThread thread);

    public enum HITResult
    {
        CONTINUE,
        HALT,
        KILL
    }
}
