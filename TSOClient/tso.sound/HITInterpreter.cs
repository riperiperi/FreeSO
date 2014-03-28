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
            return HITResult.CONTINUE;
        }

        public static HITResult NoteOn(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult NoteOff(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult LoadB(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult LoadL(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Set(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Call(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Return(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Wait(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult CallEntryPoint(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult WaitSamp(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult End(HITThread thread)
        {
            if (thread.Stack.Count > 0)
            {
                thread.PC = thread.Stack.Pop();
                return HITResult.CONTINUE;
            }
            else
            {
                return HITResult.KILL;
            }
        }

        public static HITResult Jump(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Test(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Add(HITThread thread) //0x10
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Sub(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Div(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Mul(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Cmp(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Less(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Greater(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Not(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Rand(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Abs(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Limit(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Error(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Assert(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult AddToGroup(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult RemoveFromGroup(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult GetVar(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Loop(HITThread thread) //0x20
        {
            return HITResult.CONTINUE;
        }

        public static HITResult SetLoop(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Callback(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult SmartAdd(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult SmartRemove(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult SmartRemoveAll(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult SmartSetCrit(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult SmartChoose(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult And(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult NAnd(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Or(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult NOr(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult XOr(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Max(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Min(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Inc(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Dec(HITThread thread) //0x30
        {
            return HITResult.CONTINUE;
        }

        public static HITResult PrintReg(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult PlayTrack(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult KillTrack(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Push(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult PushMask(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult PushVars(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult CallMask(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult CallPush(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Pop(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Test1(HITThread thread)
        {
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
            return HITResult.CONTINUE;
        }

        public static HITResult IfNotEqual(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult IfGreater(HITThread thread) //0x40
        {
            return HITResult.CONTINUE;
        }

        public static HITResult IfLess(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult IfGreatOrEq(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult IfLessOrEq(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult SmartSetList(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult SeqGroupKill(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult SeqGroupWait(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult SeqGroupReturn(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult GetSrcDataField(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult SeqGroupTrackID(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult SetLL(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult SetLT(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult SetTL(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult WaitEqual(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult WaitNotEqual(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult WaitGreater(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult WaitLess(HITThread thread) //0x50
        {
            return HITResult.CONTINUE;
        }

        public static HITResult WaitGreatOrEq(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult WaitLessOrEq(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Duck(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Unduck(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult TestX(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult SetLG(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult SetGL(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult Throw(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult SetSrcDataField(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult StopTrack(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult SetChanReg(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult PlayNote(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult StopNote(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult KillNote(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult SmartIndex(HITThread thread)
        {
            return HITResult.CONTINUE;
        }

        public static HITResult NoteOnLoop(HITThread thread) //0x60
        {
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
