using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.IDE.EditorComponent.OperandForms.DataProviders
{
    public abstract class OpNamedPropertyProvider : OpDataProvider
    {
        public abstract Dictionary<int, string> GetNamedProperties(EditorScope scope, VMPrimitiveOperand op);
    }

    public class OpStaticNamedPropertyProvider : OpNamedPropertyProvider
    {
        private Dictionary<int, string> Map;

        public OpStaticNamedPropertyProvider(Dictionary<int, string> map)
        {
            Map = map;
        }

        public OpStaticNamedPropertyProvider(Type num)
        {
            Map = new Dictionary<int, string>();
            var vals = Enum.GetValues(num);
            var names = Enum.GetNames(num);
            var i = 0;
            foreach (var val in vals)
            {
                if (!Map.ContainsKey(Convert.ToInt32(val)))
                    Map.Add(Convert.ToInt32(val), names[i]);
                i++;
            }
        }

        public OpStaticNamedPropertyProvider(List<ScopeDataDefinition> str)
        {
            Map = new Dictionary<int, string>();
            for (int i = 0; i < str.Count; i++)
            {
                Map.Add(str[i].Value, str[i].Name);
            }
        }

        public OpStaticNamedPropertyProvider(string[] str, int startValue)
        {
            Map = new Dictionary<int, string>();
            for (int i = 0; i < str.Length; i++)
            {
                Map.Add(i + startValue, str[i]);
            }
        }

        public OpStaticNamedPropertyProvider(STR strRes, int startValue)
        {
            Map = new Dictionary<int, string>();
            for (int i=0; i<strRes.Length; i++)
            {
                Map.Add(i + startValue, strRes.GetString(i));
            }
        }

        public OpStaticNamedPropertyProvider(STR strRes) : this(strRes, 0) { }

        public override Dictionary<int, string> GetNamedProperties(EditorScope scope, VMPrimitiveOperand op)
        {
            return Map;
        }
    }
}
