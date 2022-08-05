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
                    if (n == null) continue;

                    if (!d.TryGetValue(n, out var set)) d.Add(n, set = new HashSet<Type>());

                    foreach (var t in asm.GetTypes()) set.Add(t);
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
        try { items.Push(Assembly.GetEntryAssembly()); }
        catch (Exception) { }

        try { items.Push(Assembly.GetCallingAssembly()); }
        catch (Exception) { }

        try { items.Push(Assembly.GetExecutingAssembly()); }
        catch (Exception) { }

        try { items.Push(MethodBase.GetCurrentMethod()!.DeclaringType?.Assembly); }
        catch (Exception) { }

        try
        {
            var stackTrace = new StackTrace(); // get call stack
            var stackFrames = stackTrace.GetFrames(); // get method calls (frames)
            foreach (var stackFrame in stackFrames)
            {
                try { items.Push(stackFrame.GetMethod()?.GetType().Assembly); }
                catch (Exception) { }
            }
        }
        catch (Exception) { }

        var assemblyDictionary = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        while (items.Count > 0)
        {
            var a = items.Pop();
            if (a == null) continue;

            try
            {
                var name = a.FullName;
                if (name == null) continue;
                if (name.StartsWith("System.", StringComparison.OrdinalIgnoreCase)) continue;
                if (name.StartsWith("System,", StringComparison.OrdinalIgnoreCase)) continue;
                if (name.StartsWith("mscorlib,", StringComparison.OrdinalIgnoreCase)) continue;
                if (assemblyDictionary.ContainsKey(name)) continue;

                assemblyDictionary.Add(name, a);
                var referencedAssemblies = a.GetReferencedAssemblies();
                foreach (var referencedAssembly in referencedAssemblies)
                {
                    try
                    {
                        var referencedAssemblyLoaded = Assembly.Load(referencedAssembly);

                        var referencedAssemblyLoadedFullName = referencedAssemblyLoaded.FullName;
                        if (referencedAssemblyLoadedFullName != null && !assemblyDictionary.ContainsKey(referencedAssemblyLoadedFullName)) items.Push(referencedAssemblyLoaded);
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }

        return assemblyDictionary.Values.WhereNotNull().ToArray();
    }

    #endregion Type and Assembly Scanning

    #region Attributes

    public static TAttribute GetAssemblyAttribute<TClassInAssembly, TAttribute>() where TClassInAssembly : class where TAttribute : class => typeof(TClassInAssembly).GetTypeInfo().Assembly.GetCustomAttributes(typeof(TAttribute)).SingleOrDefault() as TAttribute;

    /// <summary>
    /// Helps in discovery of the target of an Attribute
    /// </summary>
    /// <see>https://stackoverflow.com/a/2919276</see>
    /// <typeparam name="TAttribute">An Attribute derived type</typeparam>
    /// <remarks>
    /// The .NET framework does not provide navigation from attributes back to their targets, principally for the reason that
    /// in typical usage scenarios for attributes, the attribute is discovered by a routine which already has a reference to a
    /// member type.
    /// There are, however, bona-fide cases where an attribute needs to detect it's target - an example is a localizable
    /// sub-class of the
    /// DescriptionAttribute. In order for the DescriptionAttribute to return a localized string, it requires a resource key
    /// and, ideally,
    /// a type reference as the base-key for the ResourceManager. A DescriptionAttribute could not provide this information
    /// without
    /// a reference to it's target type.
    /// Note to callers:
    /// Your Attribute-derived class must implement Equals and GetHashCode, otherwise a run-time exception will occur, since
    /// this class
    /// creates a dictionary of attributes in order to speed up target lookups.
    /// </remarks>
    public static class AttributeTargetHelper<TAttribute> where TAttribute : Attribute
    {
        /// <summary>
        /// Map of attributes and their respective targets
        /// </summary>
        private static readonly Dictionary<TAttribute, object> targetMap;

        /// <summary>
        /// List of assemblies that should not be rescanned for types.
        /// </summary>
        private static readonly List<string> skipAssemblies;

        /// <summary>
        /// Adds an attribute and it's target to the dictionary
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="item"></param>
        private static void Add(TAttribute attribute, object item) => targetMap.Add(attribute, item);

        /// <summary>
        /// Scans an assembly for all instances of the attribute.
        /// </summary>
        /// <param name="assembly"></param>
        private static void ScanAssembly(Assembly assembly)
        {
            const BindingFlags memberInfoBinding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

            if (!skipAssemblies.Contains(assembly.FullName))
            {
                skipAssemblies.Add(assembly.FullName);

                Debug.WriteLine("Loading attribute targets for " + typeof(TAttribute).Name + " from assembly " + assembly.FullName);

                foreach (TAttribute attr in assembly.GetCustomAttributes(typeof(TAttribute), false)) Add(attr, assembly);

                foreach (var type in assembly.GetTypes())
                {
                    foreach (TAttribute attr in type.GetCustomAttributes(typeof(TAttribute), false)) Add(attr, type);

                    foreach (var member in type.GetMembers(memberInfoBinding))
                    {
                        foreach (TAttribute attr in member.GetCustomAttributes(typeof(TAttribute), false)) Add(attr, member);

                        if (member.MemberType != MemberTypes.Method) continue;

                        foreach (var parameter in ((MethodInfo)member).GetParameters())
                        {
                            foreach (TAttribute attr in parameter.GetCustomAttributes(typeof(TAttribute), false)) Add(attr, parameter);
                        }
                    }
                }
            }

            foreach (var assemblyName in assembly.GetReferencedAssemblies())
            {
                if (!skipAssemblies.Contains(assemblyName.FullName)) ScanAssembly(Assembly.Load(assemblyName));
            }
        }

        /// <summary>
        /// Returns the target of an attribute.
        /// </summary>
        /// <param name="attribute">The attribute for which a target is sought</param>
        /// <returns>The target of the attribute - either an Assembly, Type or MemberInfo instance.</returns>
        public static object GetTarget(TAttribute attribute)
        {
            if (targetMap.TryGetValue(attribute, out var result)) return result;

            // Since types can be loaded at any time, recheck that all assemblies are included...
            // Walk up the stack in a last-ditch effort to find instances of the attribute.
            var stackTrace = new StackTrace(); // get call stack

            // write call stack method names
            foreach (var methodBase in stackTrace.GetFrames().OrEmpty().WhereNotNull().Select(o => o.GetMethod()).WhereNotNull()) ScanAssembly(methodBase.GetType().Assembly);

            if (!targetMap.TryGetValue(attribute, out result)) throw new InvalidProgramException("Cannot find assembly referencing attribute");
            return result;
        }

        /// <summary>
        /// Static constructor for type.
        /// </summary>
        static AttributeTargetHelper()
        {
            targetMap = new Dictionary<TAttribute, object>();

            // Do not load any assemblies reference by the assembly which declares the attribute, since they cannot possibly use the attribute
            skipAssemblies = new List<string>(typeof(TAttribute).Assembly.GetReferencedAssemblies().Select(c => c.FullName));

            // Skip common system assemblies
            skipAssemblies.Add("System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            skipAssemblies.Add("System.Security, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            skipAssemblies.Add("System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            skipAssemblies.Add("System.Data.SqlXml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            skipAssemblies.Add("System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            skipAssemblies.Add("System.Numerics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");

            // Scan the entire application
            ScanAssembly(Assembly.GetEntryAssembly());
        }
    }


    /// <summary>
    /// Extends attributes so that their targets can be discovered
    /// </summary>
    public static class AttributeTargetHelperExtension
    {
        /// <summary>
        /// Gets the target of an attribute
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="attribute">The attribute for which a target is sought</param>
        /// <returns>The target of the attribute - either an Assembly, Type or MemberInfo instance.</returns>
        public static object GetTarget<TAttribute>(TAttribute attribute) where TAttribute : Attribute => AttributeTargetHelper<TAttribute>.GetTarget(attribute);
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

        var expression = Expression.New(type);
        var converted = Expression.Convert(expression, typeof(object)); // TODO: Not sure whether to use Convert or TypeAs
        //var converted = Expression.TypeAs(expression, typeof(object));

        var c = Expression.Lambda<Func<object>>(converted).Compile();
        return c;
    }

    private static readonly IBucketReadOnly<Type, Func<object>> createInstanceCache = new BucketCacheThreadSafeCopyOnWrite<Type, Func<object>>(CreateInstanceFactory);

    /// <summary>
    /// High performance new object creation. Type must have a default constructor.
    /// </summary>
    /// <param name="type">The type of object to create</param>
    /// <returns>A new object</returns>
    public static object CreateInstance(Type type) => createInstanceCache[type]();

    public static List<T> CreateList<T, TEnumerable>(params TEnumerable[] enumerables) where TEnumerable : IEnumerable<T>
    {
        var list = new List<T>();
        foreach (var enumerable in enumerables)
        foreach (var item in enumerable)
            list.Add(item);

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
        if (method.GetParameters().Length > 0) throw new Exception("Expecting method " + method.DeclaringType.FullNameFormatted() + "." + method.Name + " containing 0 parameters");

        var input = Expression.Parameter(typeof(object), "input");
        var compiledExp = Expression.Lambda<Action<object>>(
            Expression.Call(Expression.Convert(input, method.DeclaringType ?? throw new NullReferenceException()), method), input
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
        if (method.ReturnType != typeof(T)) throw new Exception("Wrong return type specified for method " + method.DeclaringType.FullNameFormatted() + "." + method.Name + " expecting " + method.ReturnType.FullNameFormatted() + " but instead called with " + typeof(T).FullNameFormatted());

        if (method.GetParameters().Length > 0) throw new Exception("Expecting method " + method.DeclaringType.FullNameFormatted() + "." + method.Name + " containing 0 parameters");

        var input = Expression.Parameter(typeof(object), "input");
        var compiledExp = Expression.Lambda<Func<object, T>>(
            Expression.Call(Expression.Convert(input, method.DeclaringType ?? throw new NullReferenceException()), method), input
        ).Compile();

        return compiledExp;
    }

    #endregion New and Reflection
}
