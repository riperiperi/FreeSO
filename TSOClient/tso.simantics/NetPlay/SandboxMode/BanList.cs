using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.NetPlay.SandboxMode
{
    public class BanList
    {
        HashSet<string> BannedHosts;
        string Dir;

        public BanList()
        {
            Dir = "";
            //Directory.CreateDirectory(Dir);
            if (!File.Exists(Dir + "banlist.txt")) File.Create(Dir + "banlist.txt").Close();

            using (var list = File.OpenText(Dir+"banlist.txt"))
            {
                BannedHosts = new HashSet<string>();
                string line;
                while ((line = list.ReadLine()) != null)
                {
                    BannedHosts.Add(line);
                }
            }
        }

        public bool Contains(string host)
        {
            return BannedHosts.Contains(host.ToLower());
        }

        public void Add(string host)
        {
            BannedHosts.Add(host.ToLower());
            Write();
        }

        public void Remove(string host)
        {
            BannedHosts.Remove(host.ToLower());
            Write();
        }

        public List<string> List()
        {
            return BannedHosts.ToList();
        }

        public void Write()
        {
            using (var list = File.Open(Dir + "banlist.txt", FileMode.Create))
            {
                var writer = new StreamWriter(list);
                foreach (var host in BannedHosts)
                {
                    writer.WriteLine(host);
                }
            }
        }
    }
}
