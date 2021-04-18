/*
Copyright (c) 2020 Steven Foster (steven.d.foster@gmail.com)

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
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;

namespace HavokMultimedia.Utilities.Console.External
{
    public class LdapEntryAttributeCollection : IBucketReadOnly<string, IReadOnlyList<LdapEntryAttributeValue>>
    {
        private static readonly ILogger log = Program.LogFactory.GetLogger<LdapEntryAttributeCollection>();
        private static readonly List<LdapEntryAttributeValue> EMPTY = new List<LdapEntryAttributeValue>();
        private readonly Dictionary<string, List<LdapEntryAttributeValue>> dictionary = new Dictionary<string, List<LdapEntryAttributeValue>>(StringComparer.OrdinalIgnoreCase);
        public Guid ObjectGUID { get; }
        public string DistinguishedName { get; }

        public LdapEntryAttributeCollection(SearchResultEntry entry) : this(entry, null)
        {
        }

        public LdapEntryAttributeCollection(SearchResultEntry entry, Ldap ldap)
        {
            entry.CheckNotNull(nameof(entry));

            var attributes = entry.Attributes;
            if (attributes == null) return;

            foreach (DictionaryEntry da in attributes)
            {
                if (da.Value is DirectoryAttribute directoryAttribute)
                {
                    var name = directoryAttribute.Name.TrimOrNull();
                    if (name == null) continue;

                    AddItems(name, directoryAttribute);
                }
            }

            ObjectGUID = Ldap.Bytes2Guid(GetByteArray("objectGUID"));
            DistinguishedName = GetString("distinguishedName");

            foreach (var attributeName in dictionary.Keys.ToList())
            {
                var range = ParseRange(attributeName);
                if (range.name == null) continue;
                if (ldap == null)
                {
                    log.Debug("LDAP connection not provided so skipping ranged attribute [" + range.name + "] : " + attributeName);
                    dictionary.Remove(attributeName);
                    dictionary.Remove(range.name);
                    continue;
                }

                var blockSize = 0;
                if (!dictionary.TryGetValue(range.name, out var values)) dictionary.Add(range.name, values = new List<LdapEntryAttributeValue>());
                values.AddRange(dictionary[attributeName]);

                while (range.rangeEnd != null)
                {
                    if (blockSize == 0) blockSize = 1 + range.rangeEnd.Value - range.rangeStart;
                    var start = range.rangeStart + blockSize;
                    var end = range.rangeEnd.Value + blockSize;
                    var attributeFilter = range.name + ";range=" + start + "-" + end;

                    if (ObjectGUID != null) entry = ldap.SearchResultEntryGetByObjectGuid(ObjectGUID, new LdapQueryConfig(attributes: attributeFilter.Yield()));
                    else if (DistinguishedName != null) entry = ldap.SearchResultEntryGetByDistinguishedName(DistinguishedName, new LdapQueryConfig(attributes: attributeFilter.Yield()));
                    else break;
                    attributes = entry.Attributes;
                    if (attributes == null) break;

                    var valuesBefore = values.Count;
                    foreach (DictionaryEntry da in attributes)
                    {
                        if (da.Value is DirectoryAttribute directoryAttribute)
                        {
                            var name = directoryAttribute.Name.TrimOrNull();
                            if (name == null) continue;
                            if (!name.StartsWith(range.name + ";range=", StringComparison.OrdinalIgnoreCase)) continue;

                            if (!dictionary.TryGetValue(name, out var rangeList)) dictionary.Add(name, rangeList = new List<LdapEntryAttributeValue>());
                            foreach (var obj in directoryAttribute)
                            {
                                var sreav = LdapEntryAttributeValue.Parse(obj);
                                if (sreav != null) rangeList.Add(sreav);
                            }

                            values.AddRange(rangeList);

                            range = ParseRange(name);
                        }
                    }
                    if (values.Count == valuesBefore) break; // sanity check
                }

                dictionary[range.name] = values;
            }
        }

        private static (string name, int rangeStart, int? rangeEnd) ParseRange(string name)
        {
            (string name, int rangeStart, int? rangeEnd) def = (null, -1, null);
            if (name.IndexOf(";range=", StringComparison.OrdinalIgnoreCase) <= 0) return def;

            var nameParts = name.Split(new string[] { ";range=" }, StringSplitOptions.RemoveEmptyEntries).TrimOrNull().WhereNotNull();
            if (nameParts.Length != 2) return def;
            var n = nameParts[0].TrimOrNull();
            if (n == null) return def;

            var rangeParts = nameParts[1].Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries).TrimOrNull().WhereNotNull();
            if (rangeParts.Length != 2) return def;

            if (!rangeParts[0].ToIntTry(out var rangeStart)) return def;

            int? rangeEnd = null;
            if (rangeParts[1].ToIntTry(out var rangeEndd)) rangeEnd = rangeEndd;

            return (n, rangeStart, rangeEnd);
        }

        private void AddItems(string name, DirectoryAttribute directoryAttribute)
        {
            var values = new List<LdapEntryAttributeValue>();
            foreach (var obj in directoryAttribute)
            {
                var sreav = LdapEntryAttributeValue.Parse(obj);
                if (sreav != null) values.Add(sreav);
            }

            if (dictionary.TryGetValue(name, out var list))
            {
                list.AddRange(values);
            }
            else
            {
                dictionary.Add(name, values.ToList());
            }
        }

        #region Get

        private List<LdapEntryAttributeValue> GetList(string name)
        {
            name = name.TrimOrNull();
            if (name == null) return EMPTY;
            if (dictionary.TryGetValue(name, out var list)) return list;
            return EMPTY;
        }

        public IEnumerable<string> GetStrings(string name) => GetList(name).Select(o => o.String).WhereNotNull();

        public string GetString(string name) => GetStrings(name).FirstOrDefault();

        public IEnumerable<byte[]> GetByteArrays(string name) => GetList(name).Select(o => o.Bytes).WhereNotNull();

        public byte[] GetByteArray(string name) => GetByteArrays(name).FirstOrDefault();

        public IEnumerable<long> GetLongs(string name) => GetList(name).Where(o => o.Long.HasValue).Select(o => o.Long.Value);

        public long? GetLong(string name)
        {
            foreach (var item in GetList(name))
            {
                if (item.Long.HasValue)
                {
                    return item.Long;
                }
            }
            return null;
        }

        public IEnumerable<int> GetInts(string name) => GetList(name).Where(o => o.Int.HasValue).Select(o => o.Int.Value);

        public int? GetInt(string name)
        {
            foreach (var item in GetList(name))
            {
                if (item.Int.HasValue)
                {
                    return item.Int;
                }
            }
            return null;
        }

        public IEnumerable<uint> GetUInts(string name) => GetList(name).Where(o => o.UInt.HasValue).Select(o => o.UInt.Value);

        public uint? GetUInt(string name)
        {
            foreach (var item in GetList(name))
            {
                if (item.UInt.HasValue)
                {
                    return item.UInt;
                }
            }
            return null;
        }

        public IEnumerable<bool> GetBools(string name) => GetList(name).Where(o => o.Bool.HasValue).Select(o => o.Bool.Value);

        public bool? GetBool(string name)
        {
            foreach (var item in GetList(name))
            {
                if (item.Bool.HasValue)
                {
                    return item.Bool;
                }
            }
            return null;
        }

        public IEnumerable<DateTime> GetDateTimeUTCs(string name) => GetList(name).Where(o => o.DateTimeUtc.HasValue).Select(o => o.DateTimeUtc.Value);

        public DateTime? GetDateTimeUTC(string name)
        {
            foreach (var item in GetList(name))
            {
                if (item.DateTimeUtc.HasValue)
                {
                    return item.DateTimeUtc;
                }
            }
            return null;
        }

        #endregion Get

        #region IBucketReadOnly

        public IEnumerable<string> Keys => dictionary.Keys;
        public IReadOnlyList<LdapEntryAttributeValue> this[string key] => GetList(key).AsReadOnly();

        #endregion IBucketReadOnly
    }











}
















