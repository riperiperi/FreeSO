using System;

namespace FSO.Common.DataService.Framework.Attributes
{
    /// <summary>
    /// Properties with this attribute present will not be updated when the entity has been marked as client sourced.
    /// (eg. skills info is sourced from the current lot VM if the avatar in question is in it.)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class ClientSourced : Attribute
    {
        public ClientSourced()
        {
        }
    }
}
