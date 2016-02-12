using FSO.Content;
using FSO.Files.Formats.IFF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.IDE.ResourceBrowser.ResourceEditors
{
    public interface IResourceControl
    {
        void SetActiveResource(IffChunk chunk, GameIffResource res);
        void SetActiveObject(GameObject obj);
        void SetOBJDAttrs(OBJDSelector[] selectors);
    }
}
