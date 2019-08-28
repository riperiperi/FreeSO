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
using System.Collections;
using System.Collections.Generic;
using Assimp.Unmanaged;
using System.Globalization;

namespace Assimp
{
    /// <summary>
    /// Represents a container for holding metadata, representing as key-value pairs.
    /// </summary>
    [Serializable]
    public sealed class Metadata : Dictionary<String, Metadata.Entry>, IMarshalable<Metadata, AiMetadata>
    {
        /// <summary>
        /// Constructs a new instance of the <see cref="Metadata"/> class.
        /// </summary>
        public Metadata() { }

        #region IMarshalable Implementation

        /// <summary>
        /// Gets if the native value type is blittable (that is, does not require marshaling by the runtime, e.g. has MarshalAs attributes).
        /// </summary>
        bool IMarshalable<Metadata, AiMetadata>.IsNativeBlittable
        {
            get { return true; }
        }

        /// <summary>
        /// Writes the managed data to the native value.
        /// </summary>
        /// <param name="thisPtr">Optional pointer to the memory that will hold the native value.</param>
        /// <param name="nativeValue">Output native value</param>
        void IMarshalable<Metadata, AiMetadata>.ToNative(IntPtr thisPtr, out AiMetadata nativeValue)
        {
            nativeValue = new AiMetadata();
            nativeValue.NumProperties = (uint) Count;

            AiString[] keys = new AiString[Count];
            AiMetadataEntry[] entries = new AiMetadataEntry[Count];
            int index = 0;
            foreach(KeyValuePair<String, Entry> kv in this)
            {
                AiMetadataEntry entry = new AiMetadataEntry();
                entry.DataType = kv.Value.DataType;

                switch(kv.Value.DataType)
                {
                    case MetaDataType.Bool:
                        entry.Data = MemoryHelper.AllocateMemory(sizeof(bool));
                        bool boolValue = (bool) kv.Value.Data;
                        MemoryHelper.Write<bool>(entry.Data, ref boolValue);
                        break;
                    case MetaDataType.Float:
                        entry.Data = MemoryHelper.AllocateMemory(sizeof(float));
                        float floatValue = (float) kv.Value.Data;
                        MemoryHelper.Write<float>(entry.Data, ref floatValue);
                        break;
                    case MetaDataType.Int:
                        entry.Data = MemoryHelper.AllocateMemory(sizeof(int));
                        int intValue = (int) kv.Value.Data;
                        MemoryHelper.Write<int>(entry.Data, ref intValue);
                        break;
                    case MetaDataType.String:
                        entry.Data = MemoryHelper.AllocateMemory(MemoryHelper.SizeOf<AiString>());
                        AiString aiStringValue = new AiString(kv.Value.Data as String);
                        MemoryHelper.Write<AiString>(entry.Data, ref aiStringValue);
                        break;
                    case MetaDataType.UInt64:
                        entry.Data = MemoryHelper.AllocateMemory(sizeof(UInt64));
                        UInt64 uint64Value = (UInt64) kv.Value.Data;
                        MemoryHelper.Write<UInt64>(entry.Data, ref uint64Value);
                        break;
                    case MetaDataType.Vector3D:
                        entry.Data = MemoryHelper.AllocateMemory(MemoryHelper.SizeOf<Vector3D>());
                        Vector3D vectorValue = (Vector3D) kv.Value.Data;
                        MemoryHelper.Write<Vector3D>(entry.Data, ref vectorValue);
                        break;
                }

                keys[index] = new AiString(kv.Key);
                entries[index] = entry;
                index++;
            }

            nativeValue.keys = MemoryHelper.ToNativeArray<AiString>(keys);
            nativeValue.Values = MemoryHelper.ToNativeArray<AiMetadataEntry>(entries);
        }

        /// <summary>
        /// Reads the unmanaged data from the native value.
        /// </summary>
        /// <param name="nativeValue">Input native value</param>
        void IMarshalable<Metadata, AiMetadata>.FromNative(ref AiMetadata nativeValue)
        {
            Clear();

            if(nativeValue.NumProperties == 0 || nativeValue.keys == IntPtr.Zero || nativeValue.Values == IntPtr.Zero)
                return;

            AiString[] keys = MemoryHelper.FromNativeArray<AiString>(nativeValue.keys, (int) nativeValue.NumProperties);
            AiMetadataEntry[] entries = MemoryHelper.FromNativeArray<AiMetadataEntry>(nativeValue.Values, (int) nativeValue.NumProperties);

            for(int i = 0; i < nativeValue.NumProperties; i++)
            {
                String key = keys[i].GetString();
                AiMetadataEntry entry = entries[i];

                if(String.IsNullOrEmpty(key) || entry.Data == IntPtr.Zero)
                    continue;

                Object data = null;
                switch(entry.DataType)
                {
                    case MetaDataType.Bool:
                        data = MemoryHelper.Read<bool>(entry.Data);
                        break;
                    case MetaDataType.Float:
                        data = MemoryHelper.Read<float>(entry.Data);
                        break;
                    case MetaDataType.Int:
                        data = MemoryHelper.Read<int>(entry.Data);
                        break;
                    case MetaDataType.String:
                        AiString aiString = MemoryHelper.Read<AiString>(entry.Data);
                        data = aiString.GetString();
                        break;
                    case MetaDataType.UInt64:
                        data = MemoryHelper.Read<UInt64>(entry.Data);
                        break;
                    case MetaDataType.Vector3D:
                        data = MemoryHelper.Read<Vector3D>(entry.Data);
                        break;
                }

                if(data != null)
                    Add(key, new Entry(entry.DataType, data));
            }
        }

        /// <summary>
        /// Frees unmanaged memory created by <see cref="IMarshalable{Metadata, AiMetadata}.ToNative"/>.
        /// </summary>
        /// <param name="nativeValue">Native value to free</param>
        /// <param name="freeNative">True if the unmanaged memory should be freed, false otherwise.</param>
        public static void FreeNative(IntPtr nativeValue, bool freeNative)
        {
            if(nativeValue == IntPtr.Zero)
                return;

            AiMetadata aiMetadata = MemoryHelper.MarshalStructure<AiMetadata>(nativeValue);

            if(aiMetadata.keys != IntPtr.Zero)
                MemoryHelper.FreeMemory(aiMetadata.keys);

            if(aiMetadata.Values != IntPtr.Zero)
            {
                AiMetadataEntry[] entries = MemoryHelper.FromNativeArray<AiMetadataEntry>(aiMetadata.Values, (int) aiMetadata.NumProperties);

                foreach(AiMetadataEntry entry in entries)
                {
                    if(entry.Data != IntPtr.Zero)
                        MemoryHelper.FreeMemory(entry.Data);
                }

                MemoryHelper.FreeMemory(aiMetadata.Values);
            }

            if(freeNative)
                MemoryHelper.FreeMemory(nativeValue);
        }

        #endregion

        /// <summary>
        /// Represents an entry in a metadata container.
        /// </summary>
        public struct Entry : IEquatable<Entry>
        {
            private MetaDataType m_dataType;
            private Object m_data;

            /// <summary>
            /// Gets the type of metadata.
            /// </summary>
            public MetaDataType DataType
            {
                get
                {
                    return m_dataType;
                }
            }

            /// <summary>
            /// Gets the metadata data stored in this entry.
            /// </summary>
            public Object Data
            {
                get
                {
                    return m_data;
                }
            }

            /// <summary>
            /// Constructs a new instance of the <see cref="Entry"/> struct.
            /// </summary>
            /// <param name="dataType">Type of the data.</param>
            /// <param name="data">The data.</param>
            public Entry(MetaDataType dataType, Object data)
            {
                m_dataType = dataType;
                m_data = data;
            }

            /// <summary>
            /// Tests equality between two entries.
            /// </summary>
            /// <param name="a">First entry</param>
            /// <param name="b">Second entry</param>
            /// <returns>True if the entries are equal, false otherwise</returns>
            public static bool operator ==(Entry a, Entry b)
            {
                return a.Equals(b);
            }

            /// <summary>
            /// Tests inequality between two entries.
            /// </summary>
            /// <param name="a">First entry</param>
            /// <param name="b">Second entry</param>
            /// <returns>True if the entries are not equal, false otherwise</returns>
            public static bool operator !=(Entry a, Entry b)
            {
                return !a.Equals(b);
            }

            /// <summary>
            /// Gets the data as the specified type. If it cannot be casted to the type, then null is returned.
            /// </summary>
            /// <typeparam name="T">Type to cast the data to.</typeparam>
            /// <returns>Casted data or null.</returns>
            public T? DataAs<T>() where T : struct
            {
                Type dataTypeType = null;
                switch(m_dataType)
                {
                    case MetaDataType.Bool:
                        dataTypeType = typeof(bool);
                        break;
                    case MetaDataType.Float:
                        dataTypeType = typeof(float);
                        break;
                    case MetaDataType.Int:
                        dataTypeType = typeof(int);
                        break;
                    case MetaDataType.String:
                        dataTypeType = typeof(String);
                        break;
                    case MetaDataType.UInt64:
                        dataTypeType = typeof(UInt64);
                        break;
                    case MetaDataType.Vector3D:
                        dataTypeType = typeof(Vector3D);
                        break;
                }

                if(dataTypeType == typeof(T))
                    return (T) m_data;

                return null;
            }

            /// <summary>
            /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
            /// </summary>
            /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
            /// <returns>True if the specified <see cref="System.Object" /> is equal to this instance; otherwise, false.</returns>
            public override bool Equals(object obj)
            {
                if(obj is Entry)
                    return Equals((Entry) obj);

                return false;
            }

            /// <summary>
            /// Indicates whether the current object is equal to another object of the same type.
            /// </summary>
            /// <param name="other">An object to compare with this object.</param>
            /// <returns>True if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
            public bool Equals(Entry other)
            {
                if(other.DataType != DataType)
                    return false;

                return Object.Equals(other.Data, Data);
            }

            /// <summary>
            /// Returns a hash code for this instance.
            /// </summary>
            /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. </returns>
            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = (hash * 31) + m_data.GetHashCode();
                    hash = (hash * 31) + ((m_data == null) ? 0 : m_data.GetHashCode());

                    return hash;
                }
            }

            /// <summary>
            /// Returns the fully qualified type name of this instance.
            /// </summary>
            /// <returns>A <see cref="T:System.String" /> containing a fully qualified type name.</returns>
            public override String ToString()
            {
                return String.Format(CultureInfo.CurrentCulture, "DataType: {0}, Data: {1}", new Object[] { m_dataType.ToString(), (m_data == null) ? "null" : m_data.ToString() });
            }
        }
    }
}
