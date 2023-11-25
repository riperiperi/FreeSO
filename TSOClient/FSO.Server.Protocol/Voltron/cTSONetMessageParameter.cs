using System;

namespace FSO.Server.Protocol.Voltron
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class cTSONetMessageParameter : Attribute
    {
        public object Value;

        public cTSONetMessageParameter(object value)
        {
            this.Value = value;
        }
    }
}
