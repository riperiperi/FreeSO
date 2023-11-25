using System.Reflection;

namespace FSO.Common.Utils
{
    /// <summary>
    /// Helps UI controls like lists refer to a data service value
    /// for labels such that when updates come in the labels update
    /// </summary>
    public class ValuePointer
    {
        private object Item;
        private PropertyInfo Field;

        public ValuePointer(object item, string field)
        {
            this.Item = item;
            this.Field = item.GetType().GetProperty(field);
        }

        public object Get()
        {
            return Field.GetValue(Item);
        }

        public override string ToString()
        {
            var value = Get();
            if(value != null)
            {
                return value.ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        public static T Get<T>(object value)
        {
            if(value == null)
            {
                return default(T);
            }

            if(value is ValuePointer)
            {
                return (T)((ValuePointer)value).Get();
            }

            return (T)value;
        }
    }
}
