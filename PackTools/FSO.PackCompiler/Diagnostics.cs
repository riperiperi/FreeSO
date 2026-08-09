using System.Collections.Generic;

namespace FSO.PackCompiler
{
    public class Diagnostics
    {
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();

        public void Error(string path, string message)
        {
            Errors.Add(path + ": " + message);
        }

        public void Warn(string path, string message)
        {
            Warnings.Add(path + ": " + message);
        }

        public bool HasErrors => Errors.Count > 0;
    }
}
