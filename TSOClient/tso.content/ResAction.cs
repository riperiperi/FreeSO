using FSO.Files.Formats.IFF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Content
{
    public class ResAction
    {
        public delegate void UIResActionDelegate();
        private UIResActionDelegate Action;
        private IffChunk Chunk;

        public ResAction(UIResActionDelegate action, IffChunk chunk)
        {
            Action = action;
            Chunk = chunk;
        }

        public void Execute()
        {
            lock (Chunk)
            {
                Action();
            }
        }
    }
}
