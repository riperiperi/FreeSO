using System;
using System.Reflection;

namespace FSO.Common.Utils
{
    public static class DotPath
    {
        public static PropertyInfo[] CompileDotPath(Type sourceType, string sourcePath)
        {
            //Dot path
            var path = sourcePath.Split(new char[] { '.' });
            var properties = new PropertyInfo[path.Length];

            var currentType = sourceType;
            for (int i = 0; i < path.Length; i++)
            {
                var property = currentType.GetProperty(path[i]);
                properties[i] = property;
                currentType = property.PropertyType;
            }

            return properties;
        }

        public static object GetDotPathValue(object source, PropertyInfo[] path)
        {
            if (source == null) { return null; }

            var currentValue = source;
            for (var i = 0; i < path.Length; i++)
            {
                currentValue = path[i].GetValue(currentValue, null);
                if (currentValue == null) { return null; }
            }

            return currentValue;
        }

        public static void SetDotPathValue(object source, PropertyInfo[] path, object value)
        {
            if (source == null) { return; }

            var currentValue = source;
            for (var i = 0; i < path.Length - 1; i++)
            {
                currentValue = path[i].GetValue(currentValue, null);
                if (currentValue == null) { return; }
            }

            var member = path[path.Length - 1];
            member.SetValue(currentValue, value, null);
        }
    }
}
