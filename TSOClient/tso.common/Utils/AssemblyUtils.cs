using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FSO.Common.Utils
{
    public class AssemblyUtils
    {
        public static Assembly Entry;
        public static List<Assembly> GetFreeSOLibs()
        {
            var map = new Dictionary<string, Assembly>();
            if (Entry == null) Entry = Assembly.GetEntryAssembly();
            RecurseAssembly(Entry, map);
            return map.Values.ToList();
        }

        private static void RecurseAssembly(Assembly assembly, Dictionary<string, Assembly> map)
        {
            var refs = assembly.GetReferencedAssemblies();
            foreach (var refAsm in refs)
            {
                if ((refAsm.Name.StartsWith("FSO.") || refAsm.Name.Equals("FreeSO") || refAsm.Name.Equals("server")) && !map.ContainsKey(refAsm.Name))
                {
                    var loadedAssembly = Assembly.Load(refAsm);
                    map.Add(refAsm.Name, loadedAssembly);
                    RecurseAssembly(loadedAssembly, map);
                }
            };
        }
    }
}
