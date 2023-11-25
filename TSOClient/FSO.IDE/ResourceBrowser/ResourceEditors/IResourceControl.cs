using FSO.Content;
using FSO.Files.Formats.IFF;

namespace FSO.IDE.ResourceBrowser.ResourceEditors
{
    public interface IResourceControl
    {
        void SetActiveResource(IffChunk chunk, GameIffResource res);
        void SetActiveObject(GameObject obj);
        void SetOBJDAttrs(OBJDSelector[] selectors);
    }
}
