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

        public ResAction(UIResActionDelegate action, IffChunk chunk) : this(action, chunk, null) { }

        public ResAction(UIResActionDelegate action, IffChunk chunk, AutoResetEvent signal)
        {
            Action = action;
            Chunk = chunk;
            Signal = signal;
        }

        public void Execute()
        {
            lock (Chunk)
            {
                Action();
            }
            if (Signal != null) Signal.Set();
        }
    }
}
