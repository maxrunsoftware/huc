/*
Copyright (c) 2021 Steven Foster (steven.d.foster@gmail.com)

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

using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace HavokMultimedia.Utilities
{
    public class JsonWriter : IDisposable
    {
        public class ObjectToken : IDisposable
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

        private MemoryStream stream;
        private Utf8JsonWriter writer;
        private string toString;
        private SingleUse isDisposed = new SingleUse();
        public JsonWriter(bool formatted = false)
        {
            stream = new MemoryStream();
            writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = formatted });
        }

        public void Dispose()
        {
            if (!isDisposed.TryUse()) return;
            ToString();
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

        public ObjectToken Object() => Object(null);
        public ObjectToken Object(string objectName)
        {
            if (objectName == null) writer.WriteStartObject();
            else writer.WriteStartObject(objectName);
            return new ObjectToken(this);
        }
        public void EndObject() => writer.WriteEndObject();

        public void Property(string propertyName, string propertyValue) => writer.WriteString(propertyName, propertyValue);
        public void Property(string propertyName, bool propertyValue) => writer.WriteBoolean(propertyName, propertyValue);


    }
}
