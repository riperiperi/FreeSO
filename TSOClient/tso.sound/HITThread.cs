using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Files.HIT;

namespace TSO.HIT
{
    public class HITThread
    {
        public int PC; //program counter
        public HITFile Src;
        public HITVM VM;
        private int[] LocalVar; //includes args, vars, whatever "h" is up to 0xf
        public Stack<int> Stack;

        public void Tick()
        {
            while (true)
            {
                var result = HITInterpreter.Instructions[Src.Data[PC++]](this);
                if (result == HITResult.HALT) break;
                else if (result == HITResult.KILL)
                {
                    //todo, remove thread from vm
                    return;
                }
            }
        }

        public HITThread(HITFile Src, HITVM VM)
        {
            this.Src = Src;
            this.VM = VM;
            LocalVar = new int[16];
        }

        public void JumpToEntryPoint(int TrackID) {
            PC = (int)Src.EntryPointByTrackID[(uint)TrackID];
        }
    }
}
