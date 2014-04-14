using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.HIT;

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

        public static HITResult NOP(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Note(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

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

        public static HITResult LoadB(HITThread thread)
        {
            var dest = thread.ReadByte();
            var value = (sbyte)thread.ReadByte();

            thread.WriteVar(dest, value);

            thread.SetFlags(value);

            return HITResult.CONTINUE;
        }

        public static HITResult LoadL(HITThread thread)
        {
            var dest = thread.ReadByte();
            var value = thread.ReadInt32();

            thread.WriteVar(dest, value);
            thread.SetFlags(value);

            return HITResult.CONTINUE;
        }

        public static HITResult Set(HITThread thread)
        {
            var dest = thread.ReadByte();
            var src = thread.ReadVar(thread.ReadByte());

            thread.WriteVar(dest, src);

            thread.SetFlags(src);

            return HITResult.CONTINUE;
        }

        public static HITResult Call(HITThread thread)
        {
            uint targ = thread.ReadUInt32();
            thread.Stack.Push((int)thread.PC);
            thread.PC = targ;

            return HITResult.CONTINUE;
        }

        public static HITResult Return(HITThread thread)
        {
            return HITResult.KILL;
        }

        public static HITResult Wait(HITThread thread)
        {
            var src = thread.ReadByte();
            if (thread.NoteActive(thread.ReadVar(src)))
            {
                thread.PC -= 2;
                return HITResult.HALT;
            }
            else
                return HITResult.HALT;
        }

        public static HITResult CallEntryPoint(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

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

        public static HITResult Test(HITThread thread)
        {
            var value = thread.ReadVar(thread.ReadByte());

            thread.SetFlags(value);

            return HITResult.CONTINUE;
        }

        public static HITResult Add(HITThread thread) //0x10
        {
            var dest = thread.ReadByte();
            var src = thread.ReadByte();

            var result = thread.ReadVar(dest) + thread.ReadVar(src);
            thread.WriteVar(dest, result);

            thread.SetFlags(result);

            return HITResult.CONTINUE;
        }

        public static HITResult Sub(HITThread thread)
        {
            var dest = thread.ReadByte();
            var src = thread.ReadByte();

            var result = thread.ReadVar(dest) - thread.ReadVar(src);
            thread.WriteVar(dest, result);

            thread.SetFlags(result);

            return HITResult.CONTINUE;
        }

        public static HITResult Div(HITThread thread)
        {
            var dest = thread.ReadByte();
            var src = thread.ReadByte();

            var result = thread.ReadVar(dest) / thread.ReadVar(src);
            thread.WriteVar(dest, result);

            thread.SetFlags(result);

            return HITResult.CONTINUE;
        }

        public static HITResult Mul(HITThread thread)
        {
            var dest = thread.ReadByte();
            var src = thread.ReadByte();

            var result = thread.ReadVar(dest) * thread.ReadVar(src);
            thread.WriteVar(dest, result);

            thread.SetFlags(result);

            return HITResult.CONTINUE;
        }

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

        public static HITResult Loop(HITThread thread) //0x20
        {
            thread.PC = thread.LoopPointer;
            return HITResult.CONTINUE;
        }

        public static HITResult SetLoop(HITThread thread)
        {
            thread.LoopPointer = thread.PC;
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

        public static HITResult Max(HITThread thread)
        {
            var dest = thread.ReadByte();
            var src = thread.ReadInt32();

            var result = Math.Max(thread.ReadVar(dest), src);
            thread.WriteVar(dest, result);

            thread.SetFlags(result);

            return HITResult.CONTINUE;
        }

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

        public static HITResult PlayTrack(HITThread thread)
        {
            //TODO: system to play tracks without setting patch (what this is)
            var src = thread.ReadByte();
            return HITResult.CONTINUE;
        }

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

        public static HITResult IfEqual(HITThread thread)
        {
            var loc = thread.ReadUInt32();

            if (thread.ZeroFlag) thread.PC = loc;

            return HITResult.CONTINUE;
        }

        public static HITResult IfNotEqual(HITThread thread)
        {
            var loc = thread.ReadUInt32();

            if (!thread.ZeroFlag) thread.PC = loc;

            return HITResult.CONTINUE;
        }

        public static HITResult IfGreater(HITThread thread) //0x40
        {
            var loc = thread.ReadUInt32();

            if (!thread.SignFlag && !thread.ZeroFlag) thread.PC = loc; //last set/compare result was > 0

            return HITResult.CONTINUE;
        }

        public static HITResult IfLess(HITThread thread)
        {
            var loc = thread.ReadUInt32();

            if (thread.SignFlag) thread.PC = loc; //last set/compare result was < 0

            return HITResult.CONTINUE;
        }

        public static HITResult IfGreatOrEq(HITThread thread)
        {
            var loc = thread.ReadUInt32();

            if (!thread.SignFlag) thread.PC = loc; //last set/compare result was >= 0

            return HITResult.CONTINUE;
        }

        public static HITResult IfLessOrEq(HITThread thread)
        {
            var loc = thread.ReadUInt32();

            if (thread.SignFlag || thread.ZeroFlag) thread.PC = loc; //last set/compare result was <= 0

            return HITResult.CONTINUE;
        }

        public static HITResult SmartSetList(HITThread thread)
        { //sets the hitlist
            var src = thread.ReadByte();
            thread.LoadHitlist((uint)thread.ReadVar(src));

            return HITResult.CONTINUE;
        }

        public static HITResult SeqGroupKill(HITThread thread)
        {
            var src = thread.ReadByte();
            return HITResult.CONTINUE;
        }

        public static HITResult SeqGroupWait(HITThread thread) //unused in the sims
        {
            return HITResult.CONTINUE;
        }

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

            //looks like this reads from object vars, though gender is apparently field 0 whereas object vars start at IsInsideViewFrustrum...
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

        public static HITResult WaitEqual(HITThread thread)
        {
            if (thread.ZeroFlag && thread.NoteActive(thread.LastNote))
            {
                thread.PC --;
                return HITResult.HALT;
            }
            else
                return HITResult.HALT;
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

        public static HITResult Duck(HITThread thread)
        {
            return HITResult.CONTINUE; //ducks all audio with priority lower than this. Imagine all the other sounds getting quieter when the fire music plays.
        }

        public static HITResult Unduck(HITThread thread)
        {
            return HITResult.CONTINUE; //quack
        }

        public static HITResult TestX(HITThread thread)
        {
            return HITResult.CONTINUE; //unused in the sims
        }

        public static HITResult SetLG(HITThread thread)
        {
            var dest = thread.ReadByte();
            var src = thread.ReadInt32(); //why is this an int32

            return HITResult.CONTINUE;
        }

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

        public static HITResult SmartIndex(HITThread thread)
        {
            var table = thread.ReadVar(thread.ReadByte());
            var index = thread.ReadVar(thread.ReadByte());

            //todo, load the track...

            return HITResult.CONTINUE;
        }

        public static HITResult NoteOnLoop(HITThread thread) //0x60
        {
            var src = thread.ReadByte();
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
