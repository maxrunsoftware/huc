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

using System.Net;
using System.Net.Mail;
using System.Net.Mime;

namespace MaxRunSoftware.Utilities;

public static class ExtensionsNet
{
    public static Attachment AddAttachment(this MailMessage mailMessage, string fileName)
    {
        var fi = new FileInfo(fileName);
        var attachment = new Attachment(fileName, MediaTypeNames.Application.Octet);
        var disposition = attachment.ContentDisposition;
        if (disposition != null)
        {
            disposition.CreationDate = fi.CreationTime;
            disposition.ModificationDate = fi.LastWriteTime;
            disposition.ReadDate = fi.LastAccessTime;
            disposition.FileName = fi.Name;
            disposition.Size = fi.Length;
            disposition.DispositionType = DispositionTypeNames.Attachment;
        }

        mailMessage.Attachments.Add(attachment);

        return attachment;
    }

    public static uint ToUInt(this IPAddress ipAddress)
    {
        var ip = ipAddress.ToString().Split('.').Select(byte.Parse).ToArray();
        if (BitConverter.IsLittleEndian) Array.Reverse(ip);

        var num = BitConverter.ToUInt32(ip, 0);
        return num;
    }

    public static long ToLong(this IPAddress ipaddress) => ToUInt(ipaddress);

    public static IPAddress ToIPAddress(this uint ipAddress)
    {
        var ipBytes = BitConverter.GetBytes(ipAddress);
        if (BitConverter.IsLittleEndian) Array.Reverse(ipBytes);

        var address = string.Join(".", ipBytes.Select(n => n.ToString()));
        return IPAddress.Parse(address);
    }

    public static IPAddress ToIPAddress(this long ip) => ToIPAddress((uint)ip);

    public static IEnumerable<IPAddress> Range(this IPAddress startAddressInclusive, IPAddress endAddressInclusive)
    {
        var ui1 = startAddressInclusive.ToUInt();
        var ui2 = endAddressInclusive.ToUInt();

        if (ui2 >= ui1)
            for (var i = ui1; i <= ui2; i++)
                yield return i.ToIPAddress();
        else
            for (var i = ui1; i >= ui2; i--)
                yield return i.ToIPAddress();
    }
}
