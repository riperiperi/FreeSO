using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSOClient.Code.Utils
{
    public class CollectionUtils
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
    }
}
