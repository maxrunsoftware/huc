/*
Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace MaxRunSoftware.Utilities;

public static partial class Util
{

    /// <summary>
    /// Deserializes an object from binary data
    /// </summary>
    /// <param name="data">The serialized binary data</param>
    /// <returns>The deserialized object</returns>
    public static object DeserializeBinary(byte[] data)
    {
        IFormatter formatter = new BinaryFormatter();

        using (var stream = new MemoryStream(data))
        {
#pragma warning disable SYSLIB0011 // Type or member is obsolete
            return formatter.Deserialize(stream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
        }
    }

    /// <summary>
    /// Serializes an object to binary data
    /// </summary>
    /// <param name="obj">The object to serialize</param>
    /// <returns>The serialized binary data</returns>
    public static byte[] SerializeBinary(object obj)
    {
        IFormatter formatter = new BinaryFormatter();

        using (var stream = new MemoryStream())
        {
#pragma warning disable SYSLIB0011 // Type or member is obsolete
            formatter.Serialize(stream, obj);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            stream.Flush();
            stream.Close();
            return stream.ToArray();
        }
    }

    /// <summary>
    /// Deep clones a serializable object
    /// </summary>
    /// <typeparam name="T">The type</typeparam>
    /// <param name="obj">The object to clonse</param>
    /// <returns>The clone</returns>
    public static T Clone<T>(T obj)
    {
        // http://stackoverflow.com/a/78612
        if (!typeof(T).IsSerializable)
        {
            throw new ArgumentException("The type must be serializable.", "obj");
        }

        // Don't serialize a null object, simply return the default for that object
        if (ReferenceEquals(obj, null))
        {
            return default;
        }

        IFormatter formatter = new BinaryFormatter();
        using (Stream stream = new MemoryStream())
        {
            using (stream)
            {
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                formatter.Serialize(stream, obj);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            }
        }
    }

}
