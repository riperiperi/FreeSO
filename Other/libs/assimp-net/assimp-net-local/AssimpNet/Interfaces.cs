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

namespace Assimp
{
    /// <summary>
    /// Represents an object that can be marshaled to and from a native representation.
    /// </summary>
    /// <typeparam name="Managed">Managed object type</typeparam>
    /// <typeparam name="Native">Native value type</typeparam>
    public interface IMarshalable<Managed, Native>
        where Managed : class, new()
        where Native : struct
    {
        /// <summary>
        /// Gets if the native value type is blittable (that is, does not require marshaling by the runtime, e.g. has MarshalAs attributes).
        /// </summary>
        bool IsNativeBlittable { get; }

        /// <summary>
        /// Writes the managed data to the native value.
        /// </summary>
        /// <param name="thisPtr">Optional pointer to the memory that will hold the native value.</param>
        /// <param name="nativeValue">Output native value</param>
        void ToNative(IntPtr thisPtr, out Native nativeValue);

        /// <summary>
        /// Reads the unmanaged data from the native value.
        /// </summary>
        /// <param name="nativeValue">Input native value</param>
        void FromNative(ref Native nativeValue);
    }

    /// <summary>
    /// Custom marshaler for usage with the <see cref="MemoryHelper"/> for performing marshaling
    /// to-and-from unmanaged memory for non-blittable types. A type must be attributed with <see cref="NativeCustomMarshalerAttribute"/>
    /// to automatically have an instance of its marshaler be utilized.
    /// </summary>
    public interface INativeCustomMarshaler
    {
        /// <summary>
        /// Gets the native data size in bytes.
        /// </summary>
        int NativeDataSize { get; }

        /// <summary>
        /// Marshals the managed object to the unmanaged chunk of memory.
        /// </summary>
        /// <param name="managedObj">Managed object to marshal.</param>
        /// <param name="nativeData">Unmanaged chunk of memory to write to.</param>
        void MarshalManagedToNative(Object managedObj, IntPtr nativeData);

        /// <summary>
        /// Marshals the managed object from the unmanaged chunk of memory.
        /// </summary>
        /// <param name="nativeData">Unmanaged chunk of memory to read from.</param>
        /// <returns>Managed object marshaled.</returns>
        Object MarshalNativeToManaged(IntPtr nativeData);
    }
}
