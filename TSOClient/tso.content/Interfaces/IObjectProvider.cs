using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.Content.Interfaces
{
    public interface IObjectProvider
    {
        void AddObject(GameObject obj);
        void AddObject(IffFile iff, OBJD obj);
        void RemoveObject(uint GUID);
    }
}
