using FSO.SimAntics.Engine;
using System;

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
                if (value.GetType() != property.PropertyType && property.PropertyType == typeof(UInt32))
                {
                    finalType = unchecked((uint)Convert.ToInt32(value));
                }
                else
                    finalType = Convert.ChangeType(value, property.PropertyType);
            }

            property.SetValue(op, finalType, new object[0]);
        }

        public static object GetOperandProperty(VMPrimitiveOperand op, string propertyN)
        {
            var property = op.GetType().GetProperty(propertyN);
            return(property.GetValue(op, new object[0]));
        }
    }
}
