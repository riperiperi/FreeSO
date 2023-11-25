namespace FSO.Server.Protocol.Electron.Model
{
    public interface IActionRequest
    {
        object OType { get; }
        bool NeedsValidation { get; }
    }
}
