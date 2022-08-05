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

namespace MaxRunSoftware.Utilities;

public static class Reflection
{
    private static IReflectionDomain Domain { get; } = new ReflectionDomain();

    public static IReflectionAssembly GetAssembly(Assembly assembly) => Domain.GetAssembly(assembly);
    public static IReflectionAssembly GetAssembly(AssemblySlim assemblySlim) => Domain.GetAssembly(assemblySlim);
    public static IReflectionType GetType<T>() => Domain.GetType<T>();
    public static IReflectionType GetType(Type type) => Domain.GetType(type);
    public static IReflectionType GetType(TypeSlim typeSlim) => Domain.GetType(typeSlim);

    #region Helpers

    internal static bool InternalIsEqual<T>(T x, T y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;
        return x.Equals(y);
    }
    internal static bool InternalIsEqual(string x, string y) => StringComparer.Ordinal.Equals(x, y);

    #endregion Helpers
}
