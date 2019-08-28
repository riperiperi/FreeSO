/*
* Copyright (c) 2012-2014 AssimpNet - Nicholas Woodfield
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/

using System;
using System.Diagnostics;

namespace Assimp
{
    /// <summary>
    /// Attribute for assocating a type with an <see cref="INativeCustomMarshaler"/> instance.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class NativeCustomMarshalerAttribute : Attribute
    {
        private INativeCustomMarshaler m_marshaler;

        /// <summary>
        /// Gets the associated marshaler.
        /// </summary>
        public INativeCustomMarshaler Marshaler
        {
            get
            {
                return m_marshaler;
            }
        }

        /// <summary>
        /// Constructs a new instance of the <see cref="NativeCustomMarshalerAttribute"/> class.
        /// </summary>
        /// <param name="type">Type that implements <see cref="INativeCustomMarshaler"/></param>
        /// <exception cref="System.NullReferenceException">Thrown if the type is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if the type does not implement <see cref="INativeCustomMarshaler"/>.</exception>
        public NativeCustomMarshalerAttribute(Type type)
        {
            if (type == null)
                throw new NullReferenceException("type");

            if (!typeof(INativeCustomMarshaler).IsAssignableFrom(type))
                throw new ArgumentException(String.Format("{0} does not implement INativeCustomMarshaler.", type.FullName));

            m_marshaler = Activator.CreateInstance(type) as INativeCustomMarshaler;
        }
    }
}
