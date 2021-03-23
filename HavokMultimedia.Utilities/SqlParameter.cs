// /*
// Copyright (c) 2021 Steven Foster (steven.d.foster@gmail.com)
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
using System.Data;

namespace HavokMultimedia.Utilities
{
    public sealed class SqlParameter
    {
        public string Name { get; }
        public DbType Type { get; }
        public object Value { get; }

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
                if (Constant.MAP_Type_DbType.TryGetValue(value.GetType(), out var dbType))
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

        public SqlParameter(string name, object value, DbType type)
        {
            Name = name;
            Value = value;
            Type = type;
        }
    }

}
