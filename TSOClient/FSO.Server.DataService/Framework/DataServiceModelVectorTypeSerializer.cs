using FSO.Common.Serialization;
using FSO.Common.Serialization.Primitives;
using FSO.Files.Formats.tsodata;
using Mina.Core.Buffer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Framework
{
    public class DataServiceModelVectorTypeSerializer : DataServiceModelTypeSerializer
    {
        //0xA97384A3: cTSOValueVector<class cRZAutoRefCount<class cITSOProperty> >
        private readonly uint CLSID = 0xA97384A3;

        public DataServiceModelVectorTypeSerializer(TSODataDefinition model) : base(model)
        {
        }

        public override bool CanDeserialize(uint clsid)
        {
            return clsid == CLSID;
        }

        public override bool CanSerialize(Type type)
        {
            if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableList<>)){
                var genericArgs = type.GetGenericArguments();
                if(genericArgs.Length == 1)
                {
                    return StructsByName.ContainsKey(genericArgs[0].Name);
                }
            }

            return false;
        }

        public override object Deserialize(uint clsid, IoBuffer input, ISerializationContext serializer)
        {
            //note: currently an array full with null values will just return null.
            object result = null;
            var count = input.GetUInt32();
            int leadingNulls = 0;
            for (int i=0; i<count; i++)
            {
                var property = new cTSOProperty();
                property.Deserialize(input, serializer);
                //Convert to instance
                var item = ConvertFromProperty(property, serializer);
                if (item == null) leadingNulls++;
                else if (result == null)
                {
                    //still need to make the list!
                    var listType = typeof(ImmutableList);
                    var consMethod = listType.GetMethods().Where(method => method.IsGenericMethod).FirstOrDefault();
                    var generic = consMethod.MakeGenericMethod(item.GetType());

                    result = generic.Invoke(null, new object[] { }); //listType.MakeGenericType(item.GetType());

                    //result = Activator.CreateInstance(constructedListType);
                    for (int j=0; j<leadingNulls; j++)
                        result = result.GetType().GetMethod("Add").Invoke(result, new object[] { null });
                }
                if (result != null) result = result.GetType().GetMethod("Add").Invoke(result, new object[] { item });
            }

            return result;
        }

        public override void Serialize(IoBuffer output, object value, ISerializationContext context)
        {
            IList list = (IList)value;
            var genericType = value.GetType().GetGenericArguments()[0];

            var _struct = GetStruct(genericType);
            if (_struct == null)
            {
                throw new Exception("Unable to map " + genericType + " to a tso struct");
            }

            output.PutUInt32((uint)list.Count);

            foreach(var item in list)
            {
                var property = ConvertToProperty(_struct, item, context);
                property.Serialize(output, context);
            }
        }

        public override uint? GetClsid(object value)
        {
            return CLSID;
        }
    }
}
