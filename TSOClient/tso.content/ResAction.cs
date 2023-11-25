using FSO.Files.Formats.IFF;
using System;
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

        public ResAction(UIResActionDelegate action) : this(action, null, true, null) { }
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
            if (Chunk != null)
            {
                lock (Chunk)
                {
                    try
                    {
                        if (CausesChange)
                            Content.Get().Changes.ChunkChanged(Chunk);
                        Action();
                    }
                    catch (Exception)
                    {}
                }
            } else
            {
                try
                { Action(); }
                catch (Exception)
                { }
            }
            if (Signal != null) Signal.Set();
        }
    }
}
