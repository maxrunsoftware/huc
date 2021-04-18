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
using System.Text;

namespace HavokMultimedia.Utilities.Console.External
{
    public class LdapEntryAttributeValue
    {
        public byte[] Bytes { get; }
        public string String { get; }
        public uint? UInt => String != null && uint.TryParse(String, out var o) ? o : (uint?)null;
        public int? Int => String != null && int.TryParse(String, out var o) ? o : (int?)null;
        public long? Long => String != null && long.TryParse(String, out var o) ? o : (long?)null;
        public bool? Bool => String != null && String.ToBoolNullableTry(out var o) ? o : null;

        public DateTime? DateTimeUtc
        {
            get
            {
                var s = String;
                if (s == null) return null;
                if (s.EndsWith("Z") || s.EndsWith("z"))
                {
                    if (DateTime.TryParseExact(s, "yyyyMMddHHmmss.0Z", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt))
                    {
                        return dt.ToUniversalTime();
                    }
                }
                if (Long != null)
                {
                    var l = Long.Value;
                    try
                    {
                        if (l == long.MaxValue)
                        {
                            return DateTime.MaxValue.ToUniversalTime();
                        }

                        if (Long.Value == 0)
                        {
                            return DateTime.MinValue.ToUniversalTime();
                        }

                        var ft = DateTime.FromFileTimeUtc(Long.Value);
                        return ft;
                    }
                    catch { }
                }
                return null;
            }
        }

        private LdapEntryAttributeValue(byte[] bytes, string str)
        {
            Bytes = bytes;
            String = str;
        }

        public static LdapEntryAttributeValue Parse(object obj)
        {
            if (obj == null) return null;
            if (obj is string str)
            {
                var s = str.TrimOrNull();
                if (s == null) return null;
                var bytes = Encoding.UTF8.GetBytes(s);
                return new LdapEntryAttributeValue(bytes, s);
            }
            else if (obj is Uri uri)
            {
                var s = uri.ToString().TrimOrNull();
                if (s == null) return null;
                var bytes = Encoding.UTF8.GetBytes(s);
                return new LdapEntryAttributeValue(bytes, s);
            }
            else if (obj is byte[] b)
            {
                string s = null;
                if (Util.IsValidUTF8(b)) s = Encoding.UTF8.GetString(b);
                return new LdapEntryAttributeValue(b, s);
            }
            else
            {
                throw new ArgumentException("Unable to parse type: " + obj.GetType().FullNameFormatted());
            }
        }

        public override string ToString()
        {
            if (DateTimeUtc != null && DateTimeUtc != DateTime.MinValue.ToUniversalTime()) return DateTimeUtc.Value.ToStringISO8601();
            if (Int != null) return Int.ToString();
            if (UInt != null) return UInt.ToString();
            if (Long != null) return Long.ToString();
            if (String != null) return String;
            return Bytes.ToStringGuessFormat();
        }
    }







}
















