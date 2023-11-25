namespace FSO.UI
{
    public interface IFSOProgram
    {
        bool InitWithArguments(string[] args);
        bool UseDX { get; set; }
    }
}
