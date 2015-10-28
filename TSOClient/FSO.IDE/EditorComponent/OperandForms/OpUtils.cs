using FSO.SimAntics.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.IDE.EditorComponent.OperandForms
{
    public static class OpUtils
    {
        public static void SetOperandProperty(VMPrimitiveOperand op, string propertyN, object value)
        {
            var property = op.GetType().GetProperty(propertyN);
            object finalType;
            try
            {
                finalType = Enum.ToObject(property.PropertyType, value);
            }
            catch (Exception)
            {
                finalType = Convert.ChangeType(value, property.PropertyType);
            }

            property.SetValue(op, finalType);
        }

        public static object GetOperandProperty(VMPrimitiveOperand op, string propertyN)
        {
            var property = op.GetType().GetProperty(propertyN);
            return(property.GetValue(op));
        }
    }
}
