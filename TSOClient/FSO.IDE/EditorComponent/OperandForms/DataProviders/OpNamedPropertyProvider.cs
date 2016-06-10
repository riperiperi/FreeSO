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
