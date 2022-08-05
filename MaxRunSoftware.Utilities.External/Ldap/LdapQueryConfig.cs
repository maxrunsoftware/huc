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

using System.DirectoryServices.Protocols;

namespace MaxRunSoftware.Utilities.External;

public class LdapQueryConfig : IEquatable<LdapQueryConfig>
{
    private readonly int hashCode;

    /// <summary>
    /// The default size of pages returned when making large queries.
    /// </summary>
    public const int DEFAULT_QUERY_PAGE_SIZE = 1000;

    /// <summary>
    /// The attributes that should be returned in each entry found.
    /// </summary>
    public IReadOnlyList<string> Attributes { get; }

    /// <summary>
    /// The distinguished name of the base entry where the search will begin. (Typically an OU or the base DN of the
    /// directory.)
    /// If not supplied, the default values will be used. This base is used only for the duration of this search.
    /// </summary>
    public string BaseDn { get; }

    /// <summary>
    /// The scope to use while searching. Defaults to Subtree. (Typically Base, just the object with the DN
    /// specified; OneLevel, just the child objects of the base object; or Subtree, the base object and all child objects)
    /// This scope is used only for the duration of this search.
    /// </summary>
    public SearchScope Scope { get; }

    /// <summary>
    /// The query page size to specify when making large requests. Defaults to DEFAULT_QUERY_PAGE_SIZE.
    /// </summary>
    public int QueryPageSize { get; }

    /// <summary>
    /// Whether the search should chase object referrals to other servers if necessary. Defaults to true.
    /// </summary>
    public bool ChaseReferrals { get; }

    public LdapQueryConfig(string baseDn = null, SearchScope searchScope = SearchScope.Subtree, int queryPageSize = DEFAULT_QUERY_PAGE_SIZE, bool chaseReferrals = true, IEnumerable<string> attributes = null)
    {
        BaseDn = baseDn.TrimOrNull();
        Scope = searchScope;
        QueryPageSize = queryPageSize;
        ChaseReferrals = chaseReferrals;

        Attributes = attributes.OrEmpty()
            .TrimOrNull()
            .WhereNotNull()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(o => o, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();

        hashCode = Util.GenerateHashCode(BaseDn?.ToLower(), Scope, QueryPageSize, ChaseReferrals, Util.GenerateHashCodeEnumerable(Attributes.WhereNotNull().Select(o => o.ToLower())));
    }

    public override bool Equals(object obj) => Equals(obj as LdapQueryConfig);

    public override int GetHashCode() => hashCode;

    public bool Equals(LdapQueryConfig other)
    {
        if (other == null) return false;

        if (ReferenceEquals(this, other)) return true;

        if (GetHashCode() != other.GetHashCode()) return false;

        if (!string.Equals(BaseDn, other.BaseDn, StringComparison.OrdinalIgnoreCase)) return false;

        if (!Scope.Equals(other.Scope)) return false;

        if (!QueryPageSize.Equals(other.QueryPageSize)) return false;

        if (!ChaseReferrals.Equals(other.ChaseReferrals)) return false;

        if (Attributes.Count != other.Attributes.Count) return false;

        if (!Attributes.SequenceEqual(other.Attributes, StringComparer.OrdinalIgnoreCase)) return false;

        return true;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(GetType().NameFormatted());
        sb.Append("[");
        sb.Append(nameof(BaseDn) + ": " + BaseDn);
        sb.Append(", " + nameof(Scope) + ": " + Scope);
        sb.Append(", " + nameof(QueryPageSize) + ": " + QueryPageSize);
        sb.Append(", " + nameof(ChaseReferrals) + ": " + ChaseReferrals);
        sb.Append(", " + nameof(Attributes) + ": {" + Attributes.ToStringDelimited(",") + "}");
        sb.Append("]");
        return sb.ToString();
    }
}
