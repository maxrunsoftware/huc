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

using System.Diagnostics;
using System.Linq.Expressions;

namespace MaxRunSoftware.Utilities;

public static partial class Util
{
    #region Type and Assembly Scanning

    /// <summary>Gets all non-system types in all visible assemblies.</summary>
    /// <returns>All non-system types in all visible assemblies</returns>
    public static Type[] GetTypes()
    {
        var d = new Dictionary<string, HashSet<Type>>();
        try
        {
            foreach (var asm in GetAssemblies())
            {
                try
                {
                    var n = asm.FullName;
                    if (n == null)
                    {
                        continue;
                    }

                    if (!d.TryGetValue(n, out var set))
                    {
                        d.Add(n, set = new HashSet<Type>());
                    }

                    foreach (var t in asm.GetTypes())
                    {
                        if (t != null)
                        {
                            set.Add(t);
                        }
                    }
                }
                catch (Exception) { }
            }
        }
        catch (Exception) { }

        return d.Values.SelectMany(o => o).WhereNotNull().ToArray();
    }

    /// <summary>Gets all non-system assemblies currently visible.</summary>
    /// <returns>All non-system assemblies currently visible</returns>
    public static Assembly[] GetAssemblies()
    {
        var items = new Stack<Assembly>();
        try
        {
            items.Push(Assembly.GetEntryAssembly());
        }
        catch (Exception) { }

        try
        {
            items.Push(Assembly.GetCallingAssembly());
        }
        catch (Exception) { }

        try
        {
            items.Push(Assembly.GetExecutingAssembly());
        }
        catch (Exception) { }

        try
        {
            items.Push(MethodBase.GetCurrentMethod().DeclaringType?.Assembly);
        }
        catch (Exception) { }

        try
        {
            var stackTrace = new StackTrace(); // get call stack
            var stackFrames = stackTrace.GetFrames(); // get method calls (frames)
            foreach (var stackFrame in stackFrames)
            {
                try
                {
                    items.Push(stackFrame?.GetMethod()?.GetType()?.Assembly);
                }
                catch (Exception) { }
            }
        }
        catch (Exception) { }

        var asms = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        while (items.Count > 0)
        {
            var a = items.Pop();
            if (a == null)
            {
                continue;
            }

            try
            {
                var name = a.FullName;
                if (name == null)
                {
                    continue;
                }

                if (name.StartsWith("System.", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (name.StartsWith("System,", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (name.StartsWith("mscorlib,", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (asms.ContainsKey(name))
                {
                    continue;
                }

                asms.Add(name, a);
                var asmsNames = a.GetReferencedAssemblies();
                if (asmsNames != null)
                {
                    foreach (var asmsName in asmsNames)
                    {
                        try
                        {
                            var aa = Assembly.Load(asmsName);
                            if (aa != null)
                            {
                                var aaName = aa.FullName;
                                if (aaName != null)
                                {
                                    if (!asms.ContainsKey(aaName))
                                    {
                                        items.Push(aa);
                                    }
                                }
                            }
                        }
                        catch (Exception) { }
                    }
                }
            }
            catch (Exception) { }
        }

        return asms.Values.WhereNotNull().ToArray();
    }

    #endregion Type and Assembly Scanning

    #region Attributes

    public static TAttribute GetAssemblyAttribute<TClassInAssembly, TAttribute>() where TClassInAssembly : class where TAttribute : class
    {
        return typeof(TClassInAssembly).GetTypeInfo().Assembly.GetCustomAttributes(typeof(TAttribute)).SingleOrDefault() as TAttribute;
    }

    #endregion Attributes

    #region New and Reflection

    /// <summary>
    /// High performance new object creation. Type must have a default constructor.
    /// </summary>
    /// <param name="type">The type of object to create</param>
    /// <returns>A factory for creating new objects</returns>
    public static Func<object> CreateInstanceFactory(Type type)
    {
        // https://stackoverflow.com/a/29972767
        //public static readonly Func<T> New = Expression.Lambda<Func<T>>(Expression.New(typeof(T).GetConstructor(Type.EmptyTypes))).Compile();
        //public static readonly Func<T> New = Expression.Lambda<Func<T>>(Expression.New(typeof(T))).Compile();
        //private static readonly ParameterExpression YCreator_Arg_Param = Expression.Parameter(typeof(int), "z");
        //private static readonly Func<int, X> YCreator_Arg = Expression.Lambda<Func<int, X>>(Expression.New(typeof(Y).GetConstructor(new[] { typeof(int), }), new[] { YCreator_Arg_Param, }), YCreator_Arg_Param).Compile();
        var c = Expression.Lambda<Func<object>>(Expression.New(type)).Compile();
        return c;
    }

    private static readonly IBucketReadOnly<Type, Func<object>> CreateInstanceCache = new BucketCacheThreadSafeCopyOnWrite<Type, Func<object>>(o => CreateInstanceFactory(o));

    /// <summary>
    /// High performance new object creation. Type must have a default constructor.
    /// </summary>
    /// <param name="type">The type of object to create</param>
    /// <returns>A new object</returns>
    public static object CreateInstance(Type type)
    {
        return CreateInstanceCache[type]();
    }

    public static List<T> CreateList<T, TEnumerable>(params TEnumerable[] enumerables) where TEnumerable : IEnumerable<T>
    {
        var list = new List<T>();
        foreach (var enumerable in enumerables)
        {
            foreach (var item in enumerable)
            {
                list.Add(item);
            }
        }

        return list;
    }

    /// <summary>
    /// Creates an Action from a MethodInfo. The method provided must have 0 parameters.
    /// </summary>
    /// <param name="method">The method, man</param>
    /// <returns>An Action delegate to calling that method, man</returns>
    public static Action<object> CreateAction(MethodInfo method)
    {
        method.CheckNotNull(nameof(method));

        // https://stackoverflow.com/a/2933227
        if (method.GetParameters().Length > 0)
        {
            throw new Exception("Expecting method " + method.DeclaringType.FullNameFormatted() + "." + method.Name + " containing 0 parameters");
        }

        var input = Expression.Parameter(typeof(object), "input");
        var compiledExp = Expression.Lambda<Action<object>>(
            Expression.Call(Expression.Convert(input, method.DeclaringType), method), input
        ).Compile();

        Action<object> func = o => compiledExp(o);
        return func;
    }

    /// <summary>
    /// Creates an Func from a MethodInfo. The method provided must have 0 parameters. The return types must match.
    /// </summary>
    /// <param name="method">The method, man</param>
    /// <returns>A Func delegate to calling that method, man</returns>
    public static Func<object, T> CreateFunc<T>(MethodInfo method)
    {
        method.CheckNotNull(nameof(method));

        // https://stackoverflow.com/a/2933227
        if (!method.ReturnType.Equals(typeof(T)))
        {
            throw new Exception("Wrong return type specified for method " + method.DeclaringType.FullNameFormatted() + "." + method.Name + " expecting " + method.ReturnType.FullNameFormatted() + " but instead called with " + typeof(T).FullNameFormatted());
        }

        if (method.GetParameters().Length > 0)
        {
            throw new Exception("Expecting method " + method.DeclaringType.FullNameFormatted() + "." + method.Name + " containing 0 parameters");
        }

        var input = Expression.Parameter(typeof(object), "input");
        var compiledExp = Expression.Lambda<Func<object, T>>(
            Expression.Call(Expression.Convert(input, method.DeclaringType), method), input
        ).Compile();

        return compiledExp;
    }

    #endregion New and Reflection
}
