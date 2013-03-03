using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSOClient.Code.Utils
{
    public static class CollectionUtils
    {
        public static Dictionary<TKey, TValue> Clone<TKey, TValue>(Dictionary<TKey, TValue> input)
        {
            var result = new Dictionary<TKey, TValue>();
            foreach (var val in input)
            {
                result.Add(val.Key, val.Value);
            }
            return result;
        }


        private static Random RAND = new Random();
        public static T RandomItem<T>(this T[] items)
        {
            var index = RAND.Next(items.Length);
            return items[index];
        }
    }
}
