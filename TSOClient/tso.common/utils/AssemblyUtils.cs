using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Utils
{
    public class AssemblyUtils
    {
        public static List<Assembly> GetFreeSOLibs()
        {
            var map = new Dictionary<string, Assembly>();
            var entry = Assembly.GetEntryAssembly();
            RecurseAssembly(entry, map);
            return map.Values.ToList();
        }

        private static void RecurseAssembly(Assembly assembly, Dictionary<string, Assembly> map)
        {
            var refs = assembly.GetReferencedAssemblies();
            foreach (var refAsm in refs)
            {
                if ((refAsm.Name.StartsWith("FSO.") || refAsm.Name.Equals("FreeSO")) && !map.ContainsKey(refAsm.Name))
                {
                    var loadedAssembly = Assembly.Load(refAsm);
                    map.Add(refAsm.Name, loadedAssembly);
                    RecurseAssembly(loadedAssembly, map);
                }
            };
        }
    }
}
