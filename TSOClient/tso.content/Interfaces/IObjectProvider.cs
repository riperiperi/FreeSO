using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content.Interfaces
{
    public interface IObjectProvider
    {
        void AddObject(GameObject obj);
        void AddObject(IffFile iff, OBJD obj);
        void RemoveObject(uint GUID);
    }
}
