using System.Text.RegularExpressions;

namespace FSO.Patcher.Unix
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var path = UpdatePath();
            //console only application
            var patcher = new CLIPatcher(path, args);
            patcher.Begin();
        }

        static List<string> UpdatePath()
        {
            try
            {
                var files = Directory.GetFiles("PatchFiles/");
                return files.Where(x => x.EndsWith(".zip") && !x.EndsWith("patch.zip")).OrderBy(x => {
                    var match = Regex.Match(x, @"\d+").Value ?? "200";
                    if (match == "") match = "200";
                    return int.Parse(match);
                }
                ).ToList();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }
    }
}
