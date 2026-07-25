namespace FSO.Client.Utils.GameLocator
{
    public interface ILocator
    {
        static bool ValidPath(string path)
        {
            return File.Exists(Path.Combine(path, "tuning.dat"));
        }

        string FindTheSimsOnline();
    }
}
