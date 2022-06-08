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

namespace MaxRunSoftware.Utilities;

/// <summary>
/// Parameter used for calling SQL stored procedure and functions
/// </summary>
public sealed class SqlParameter
{
    /// <summary>
    /// Parameter name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Parameter type
    /// </summary>
    public DbType Type { get; }

    /// <summary>
    /// Parameter value
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// Constructs a new SqlParameter attempting to figure out the Type based on the value supplied
    /// </summary>
    /// <param name="name">Name</param>
    /// <param name="value">Value</param>
    public SqlParameter(string name, object value)
    {
        Name = name;

        if (value == null)
        {
            Type = DbType.String;
            Value = value;
        }
        else
        {
            if (Constant.Type_DbType.TryGetValue(value.GetType(), out var dbType))
            {
                Type = dbType;
                Value = value;
            }
            else
            {
                Type = DbType.String;
                Value = value.ToStringGuessFormat();
            }
        }
    }

    /// <summary>
    /// Constructs a new SqlParameter
    /// </summary>
    /// <param name="name">Name</param>
    /// <param name="value">Value</param>
    /// <param name="type"></param>
    public SqlParameter(string name, object value, DbType type)
    {
        Name = name;
        Value = value;
        Type = type;
    }
}

