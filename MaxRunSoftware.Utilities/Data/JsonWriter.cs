// Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Text.Json;

namespace MaxRunSoftware.Utilities;

public class JsonWriter : IDisposable
{
    private class ObjectToken : IDisposable
    {
        private readonly JsonWriter writer;

        public ObjectToken(JsonWriter writer)
        {
            this.writer = writer;
        }

        public void Dispose()
        {
            writer.EndObject();
        }
    }

    private class ArrayToken : IDisposable
    {
        private readonly JsonWriter writer;

        public ArrayToken(JsonWriter writer)
        {
            this.writer = writer;
        }

        public void Dispose()
        {
            writer.EndArray();
        }
    }

    private readonly MemoryStream stream;
    private readonly Utf8JsonWriter writer;
    private string toString;
    private readonly SingleUse isDisposed = new();

    public JsonWriter(bool formatted = false)
    {
        stream = new MemoryStream();
        writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = formatted });
    }

    public void Dispose()
    {
        if (!isDisposed.TryUse())
        {
            return;
        }

        var _ = ToString();
        writer.Dispose();
        stream.Dispose();
    }

    public override string ToString()
    {
        if (!isDisposed.IsUsed)
        {
            writer.Flush();
            toString = Encoding.UTF8.GetString(stream.ToArray());
        }

        return toString;
    }

    public IDisposable Object(string objectName = null)
    {
        if (objectName == null)
        {
            writer.WriteStartObject();
        }
        else
        {
            writer.WriteStartObject(objectName);
        }

        return new ObjectToken(this);
    }

    public IDisposable Array(string arrayPropertyName = null)
    {
        if (arrayPropertyName == null)
        {
            writer.WriteStartArray();
        }
        else
        {
            writer.WriteStartArray(arrayPropertyName);
        }

        return new ArrayToken(this);
    }

    public void Array(string arrayPropertyName, IEnumerable<object> enumerable)
    {
        using (Array(arrayPropertyName))
        {
            foreach (var o in enumerable)
            {
                Value(o);
            }
        }
    }


    public void EndObject()
    {
        writer.WriteEndObject();
    }

    public void EndArray()
    {
        writer.WriteEndArray();
    }

    public void Property(string propertyName, object propertyValue)
    {
        writer.WriteString(propertyName, propertyValue.ToStringGuessFormat());
    }

    public void Property(string propertyName, bool propertyValue)
    {
        writer.WriteBoolean(propertyName, propertyValue);
    }

    public void Value(object value)
    {
        writer.WriteStringValue(value.ToStringGuessFormat());
    }
}
