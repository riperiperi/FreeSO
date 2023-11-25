using System;
using Newtonsoft.Json;

namespace FSO.Common.Utils
{
    public class Base64JsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            if (typeof(byte[]).IsAssignableFrom(objectType))
            {
                return true;
            }
            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string data = (string)reader.Value;
            return Convert.FromBase64String(data);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            byte[] bytes = (byte[])value;
            writer.WriteValue(Convert.ToBase64String(bytes));
        }
    }
}
