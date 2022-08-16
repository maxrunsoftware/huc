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

using JetBrains.Annotations;

namespace MaxRunSoftware.Utilities;

public static class ExtensionsStream
{
    public static long Read(this Stream stream, [InstantHandle] Action<byte[]> action, int bufferSize = (int)(Constant.Bytes_Mega * 10))
    {
        var buffer = new byte[bufferSize];
        int read;
        long totalRead = 0;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (read != buffer.Length)
            {
                var buffer2 = new byte[read];
                Array.Copy(buffer, 0, buffer2, 0, read);
                action(buffer2);
            }
            else { action(buffer); }

            totalRead += read;
        }

        return totalRead;
    }

    public static long Read(this StreamReader reader, [InstantHandle] Action<char[]> action, int bufferSize = (int)(Constant.Bytes_Mega * 10))
    {
        var buffer = new char[bufferSize];
        int read;
        long totalRead = 0;
        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (read != buffer.Length)
            {
                var buffer2 = new char[read];
                Array.Copy(buffer, 0, buffer2, 0, read);
                action(buffer2);
            }
            else { action(buffer); }

            totalRead += read;
        }

        return totalRead;
    }

    /// <summary>
    /// Reads all the bytes from the current stream and writes them to the destination stream
    /// with the specified buffer size.
    /// </summary>
    /// <param name="source">The current stream.</param>
    /// <param name="target">The stream that will contain the contents of the current stream.</param>
    /// <param name="bufferSize">The size of the buffer to use.</param>
    public static long CopyToWithCount(this Stream source, Stream target, int bufferSize = Constant.BufferSize_Optimal)
    {
        source.CheckNotNull(nameof(source));
        target.CheckNotNull(nameof(target));
        bufferSize.CheckMin(1);

        var array = new byte[bufferSize];
        long totalCount = 0;
        int count;
        while ((count = source.Read(array, 0, array.Length)) != 0)
        {
            totalCount += count;
            target.Write(array, 0, count);
        }

        return totalCount;
    }

    public static void WriteToFile(this Stream stream, string path, int bufferSize = Constant.BufferSize_Optimal)
    {
        var directoryName = Path.GetDirectoryName(path);
        if (directoryName != null) Directory.CreateDirectory(directoryName);

        //if (File.Exists(path)) File.Delete(path);

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.None);
        CopyToWithCount(stream, fs, bufferSize);
        fs.Flush();
    }

    public static bool FlushSafe(this Stream stream)
    {
        if (stream == null) return false;

        try
        {
            stream.Flush();
            return true;
        }
        catch (Exception) { return false; }
    }

    public static bool CloseSafe(this Stream stream)
    {
        if (stream == null) return false;

        try
        {
            stream.Close();
            return true;
        }
        catch (Exception) { return false; }
    }

    public static bool FlushSafe(this StreamWriter writer)
    {
        if (writer == null) return false;

        try
        {
            writer.Flush();
            return true;
        }
        catch (Exception) { return false; }
    }

    public static bool CloseSafe(this StreamWriter writer)
    {
        if (writer == null) return false;

        try
        {
            writer.Close();
            return true;
        }
        catch (Exception) { return false; }
    }

    public static bool FlushSafe(this BinaryWriter writer)
    {
        if (writer == null) return false;

        try
        {
            writer.Flush();
            return true;
        }
        catch (Exception) { return false; }
    }

    public static bool CloseSafe(this BinaryWriter writer)
    {
        if (writer == null) return false;

        try
        {
            writer.Close();
            return true;
        }
        catch (Exception) { return false; }
    }
}
