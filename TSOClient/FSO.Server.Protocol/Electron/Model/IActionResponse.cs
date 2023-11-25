namespace FSO.Server.Protocol.Electron.Model
{
    public interface IActionResponse
    {
        bool Success { get; }
        object OCode { get; }
    }
}
