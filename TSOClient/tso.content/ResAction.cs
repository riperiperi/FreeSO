using FSO.Files.Formats.IFF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FSO.Content
{
    public class ResAction
    {
        public delegate void UIResActionDelegate();
        private UIResActionDelegate Action;
        private IffChunk Chunk;
        private AutoResetEvent Signal;
        private bool CausesChange;

        public ResAction(UIResActionDelegate action, IffChunk chunk) : this(action, chunk, true, null) { }

        public ResAction(UIResActionDelegate action, IffChunk chunk, bool causesChange) : this(action, chunk, causesChange, null) { }

        public ResAction(UIResActionDelegate action, IffChunk chunk, bool causesChange, AutoResetEvent signal)
        {
            Action = action;
            Chunk = chunk;
            Signal = signal;
            CausesChange = causesChange;
        }

        public void SetSignal(AutoResetEvent signal)
        {
            Signal = signal;
        }

        public void Execute()
        {
            lock (Chunk)
            {
                try {
                    if (CausesChange)
                    {
                        Chunk.RuntimeInfo = ChunkRuntimeState.Modified;
                        //notify content system of IFF change
                        Chunk.ChunkParent.RuntimeInfo.Dirty = true;
                        Content.Get().Changes.IffChanged(Chunk.ChunkParent);
                    }
                    Action();
                } catch (Exception)
                {

                }
            }
            if (Signal != null) Signal.Set();
        }
    }
}
