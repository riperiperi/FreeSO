using System.IO;

namespace FSO.Server.Common
{
    public class ServerVersion
    {
        public string Name;
        public string Number;
        public int? UpdateID;

        public static ServerVersion Get()
        {
            var result = new ServerVersion()
            {
                Name = "unknown",
                Number = "0"
            };

            if (File.Exists("version.txt"))
            {
                using (StreamReader Reader = new StreamReader(File.Open("version.txt", FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    var str = Reader.ReadLine();
                    var split = str.LastIndexOf('-');

                    result.Name = str;
                    if (split != -1)
                    {
                        result.Name = str.Substring(0, split);
                        result.Number = str.Substring(split + 1);
                    }
                }
            }

            if (File.Exists("updateID.txt"))
            {
                var stringID = File.ReadAllText("updateID.txt");
                int id;
                if (int.TryParse(stringID, out id))
                {
                    result.UpdateID = id;
                }
            }

            return result;
        }
    }
}
