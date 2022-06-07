// /*
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
// */

namespace MaxRunSoftware.Utilities;

public static partial class Util
{
    // https://stackoverflow.com/a/49372627

    public static Guid GuidFromLongs(long a, long b)
    {
        var guidData = new byte[16];
        Array.Copy(BitConverter.GetBytes(a), guidData, 8);
        Array.Copy(BitConverter.GetBytes(b), 0, guidData, 8, 8);
        return new Guid(guidData);
    }

    public static (long, long) ToLongs(this Guid guid)
    {
        var bytes = guid.ToByteArray();
        var long1 = BitConverter.ToInt64(bytes, 0);
        var long2 = BitConverter.ToInt64(bytes, 8);
        return (long1, long2);
    }

    public static Guid GuidFromULongs(ulong a, ulong b)
    {
        var guidData = new byte[16];
        Array.Copy(BitConverter.GetBytes(a), guidData, 8);
        Array.Copy(BitConverter.GetBytes(b), 0, guidData, 8, 8);
        return new Guid(guidData);
    }

    public static (ulong, ulong) ToULongs(this Guid guid)
    {
        var bytes = guid.ToByteArray();
        var ulong1 = BitConverter.ToUInt64(bytes, 0);
        var ulong2 = BitConverter.ToUInt64(bytes, 8);
        return (ulong1, ulong2);
    }

    public static Guid GuidFromInts(int a, int b, int c, int d)
    {
        var guidData = new byte[16];
        Array.Copy(BitConverter.GetBytes(a), guidData, 4);
        Array.Copy(BitConverter.GetBytes(b), 0, guidData, 4, 4);
        Array.Copy(BitConverter.GetBytes(c), 0, guidData, 8, 4);
        Array.Copy(BitConverter.GetBytes(d), 0, guidData, 12, 4);
        return new Guid(guidData);
    }

    public static (int, int, int, int) ToInts(this Guid guid)
    {
        var bytes = guid.ToByteArray();
        var a = BitConverter.ToInt32(bytes, 0);
        var b = BitConverter.ToInt32(bytes, 4);
        var c = BitConverter.ToInt32(bytes, 8);
        var d = BitConverter.ToInt32(bytes, 12);
        return (a, b, c, d);
    }

    public static Guid GuidFromUInts(uint a, uint b, uint c, uint d)
    {
        var guidData = new byte[16];
        Array.Copy(BitConverter.GetBytes(a), guidData, 4);
        Array.Copy(BitConverter.GetBytes(b), 0, guidData, 4, 4);
        Array.Copy(BitConverter.GetBytes(c), 0, guidData, 8, 4);
        Array.Copy(BitConverter.GetBytes(d), 0, guidData, 12, 4);
        return new Guid(guidData);
    }

    public static (uint, uint, uint, uint) ToUInts(this Guid guid)
    {
        var bytes = guid.ToByteArray();
        var a = BitConverter.ToUInt32(bytes, 0);
        var b = BitConverter.ToUInt32(bytes, 4);
        var c = BitConverter.ToUInt32(bytes, 8);
        var d = BitConverter.ToUInt32(bytes, 12);
        return (a, b, c, d);
    }

}
