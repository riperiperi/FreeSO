using System;
using System.Collections.Generic;
using Mina.Core.Buffer;
using System.Reflection;
using FSO.Common.Utils;

namespace FSO.Common.Serialization.TypeSerializers
{
    /// <summary>
    /// Serializes / deserializes anything that implements IoBufferSerializable & IoBufferDeserializable and has a cTSOValue decoration
    /// </summary>
    public class cTSOValueDecorated : ITypeSerializer
    {
        protected Dictionary<uint, Type> ClsIdToType = new Dictionary<uint, Type>();
        protected Dictionary<Type, uint> TypeToClsId = new Dictionary<Type, uint>();

        public cTSOValueDecorated(){
            //
            //var assembly = Assembly.GetAssembly(typeof(cTSOSerializer));
            var assemblies = AssemblyUtils.GetFreeSOLibs();
            foreach(var asm in assemblies)
            {
                ScanAssembly(asm);
            }
        }

        protected virtual void ScanAssembly(Assembly assembly)
        {
            try
            {
                foreach (Type type in assembly.GetTypes())
                {
                    System.Attribute[] attributes = System.Attribute.GetCustomAttributes(type);

                    foreach (Attribute attribute in attributes)
                    {
                        if (attribute is cTSOValue)
                        {
                            foreach (uint clsid in ((cTSOValue)attribute).ClsId)
                            {
                                ClsIdToType.Add(clsid, type);
                                TypeToClsId.Add(type, clsid);
                            }
                        }
                    }
                }
            } catch (Exception)
            {

            }
        }

        public bool CanDeserialize(uint clsid)
        {
            return ClsIdToType.ContainsKey(clsid);
        }

        public bool CanSerialize(Type type)
        {
            return TypeToClsId.ContainsKey(type);
        }

        public object Deserialize(uint clsid, IoBuffer input, ISerializationContext serializer)
        {
            var instance = Activator.CreateInstance(ClsIdToType[clsid]);
            ((IoBufferDeserializable)instance).Deserialize(input, serializer);
            return instance;
        }

        public uint? GetClsid(object value)
        {
            Type type = value.GetType();
            if (TypeToClsId.ContainsKey(type))
            {
                return TypeToClsId[type];
            }
            return null;
        }

        public void Serialize(IoBuffer output, object value, ISerializationContext serializer)
        {
            ((IoBufferSerializable)value).Serialize(output, serializer);
        }
    }


    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class cTSOValue : System.Attribute
    {
        public uint[] ClsId;

        public cTSOValue(params uint[] ClsId)
        {
            this.ClsId = ClsId;
        }
    }
}
