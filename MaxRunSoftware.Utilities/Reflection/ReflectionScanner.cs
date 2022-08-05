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

public class ReflectionScannerResult
{
    public AssemblySlim Assembly { get; }
    public HashSet<TypeSlim> TypesInThisAssembly { get; }
    public HashSet<TypeSlim> TypesInOtherAssemblies { get; }

    public ReflectionScannerResult(AssemblySlim assembly) : this(assembly, new HashSet<TypeSlim>(), new HashSet<TypeSlim>()) { }
    public ReflectionScannerResult(AssemblySlim assembly, HashSet<TypeSlim> typesInThisAssembly, HashSet<TypeSlim> typesInOtherAssemblies)
    {
        Assembly = assembly;
        TypesInThisAssembly = typesInThisAssembly;
        TypesInOtherAssemblies = typesInOtherAssemblies;
    }
}

public class ReflectionScanner
{
    private AssemblySlim Assembly { get; }

    private HashSet<TypeSlim> TypesInThisAssembly { get; } = new();
    private HashSet<TypeSlim> TypesInOtherAssemblies { get; } = new();
    private HashSet<TypeSlim> TypesScanned { get; } = new();

    private HashSet<AssemblySlim> SystemAssemblies { get; } = new();
    private HashSet<AssemblySlim> NonSystemAssemblies { get; } = new();


    public BindingFlags Flags { get; set; } = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private ReflectionScanner(AssemblySlim assembly)
    {
        Assembly = assembly;
    }


    public static ReflectionScannerResult Scan(AssemblySlim assembly)
    {
        var scanner = new ReflectionScanner(assembly);

        var typesToScan = GetTypes(assembly.Assembly).Select(o => new TypeSlim(o)).ToList();

        while (typesToScan.Count > 0)
        {
            var typesScanned = new HashSet<TypeSlim>(scanner.TypesInThisAssembly);

            scanner.Scan(typesToScan);

            typesToScan.Clear();
            if (typesScanned.Count < scanner.TypesInThisAssembly.Count)
            {
                typesToScan = scanner.TypesInThisAssembly.Except(typesScanned).ToList();
            }
        }

        return new ReflectionScannerResult(scanner.Assembly, scanner.TypesInThisAssembly, scanner.TypesInOtherAssemblies);
    }

    private static IEnumerable<Type> GetTypes(Assembly assembly) =>
        /*
        // https://stackoverflow.com/a/7889272
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.WhereNotNull().Where(o => o.TypeInitializer != null);
        }
        */
        assembly.GetTypes();

    private void Add(params Type?[]? types)
    {
        foreach (var type in types.OrEmpty().WhereNotNull())
        {
            var typeSlim = new TypeSlim(type);
            Add(typeSlim);
        }
    }

    private void Add(TypeSlim typeSlim)
    {
        if (typeSlim.Assembly.Equals(Assembly)) TypesInThisAssembly.Add(typeSlim);
        else TypesInOtherAssemblies.Add(typeSlim);
    }

    private void Scan(IEnumerable<TypeSlim> typesToScan)
    {
        foreach (var typeSlim in typesToScan)
        {
            if (TypesScanned.Contains(typeSlim)) continue;
            TypesScanned.Add(typeSlim);
            Add(typeSlim);
            var shouldSkip = SystemAssemblies.Contains(typeSlim.Assembly);
            if (!shouldSkip)
            {
                if (!NonSystemAssemblies.Contains(typeSlim.Assembly))
                {
                    var isSystemAssembly = typeSlim.Assembly.Assembly.IsSystemAssembly();
                    if (isSystemAssembly)
                    {
                        SystemAssemblies.Add(typeSlim.Assembly);
                        shouldSkip = true;
                    }
                    else
                    {
                        NonSystemAssemblies.Add(typeSlim.Assembly);
                    }
                }
            }

            if (shouldSkip) continue;

            ScanType(typeSlim.Type);
            Scan(typeSlim.Type.BaseTypes().Select(o => new TypeSlim(o)));
        }
    }

    private void ScanType(Type type)
    {
        ScanMemberInfo(type);
        Add(type.GenericTypeArguments);
        Add(type.GetInterfaces());

        var flags = Flags;
        ScanConstructorInfo(type.GetConstructors(flags));
        ScanMemberInfo(type.GetMembers(flags));
        ScanMethodInfo(type.GetMethods(flags));
        ScanPropertyInfo(type.GetProperties(flags));
        ScanFieldInfo(type.GetFields(flags));
        ScanEventInfo(type.GetEvents(flags));
        ScanMemberInfo(type.GetDefaultMembers());
        Add(type.GetElementType());
        Add(type.GetNestedTypes());

        if (type.IsEnum) Add(type.GetEnumUnderlyingType());
        if (type.IsGenericParameter) Add(type.GetGenericParameterConstraints());
        if (type.IsGenericType) Add(type.GetGenericTypeDefinition());
    }


    private void ScanConstructorInfo(params ConstructorInfo?[]? infos)
    {
        foreach (var info in infos.OrEmpty().WhereNotNull())
        {
            ScanMethodBase(info);
        }
    }

    private void ScanEventInfo(params EventInfo?[]? infos)
    {
        foreach (var info in infos.OrEmpty().WhereNotNull())
        {
            ScanMemberInfo(info);
            ScanMethodInfo(info.AddMethod);
            ScanMethodInfo(info.GetAddMethod());

            ScanMethodInfo(info.RaiseMethod);
            ScanMethodInfo(info.GetRaiseMethod());

            ScanMethodInfo(info.RemoveMethod);
            ScanMethodInfo(info.GetRemoveMethod());

            ScanMethodInfo(info.GetOtherMethods());

            Add(info.EventHandlerType);
        }
    }

    private void ScanFieldInfo(params FieldInfo?[]? infos)
    {
        foreach (var info in infos.OrEmpty().WhereNotNull())
        {
            ScanMemberInfo(info);
            foreach (var t in info.GetOptionalCustomModifiers()) Add(t);
            foreach (var t in info.GetRequiredCustomModifiers()) Add(t);
            Add(info.FieldType);
        }
    }

    private void ScanMethodInfo(params MethodInfo?[]? infos)
    {
        foreach (var info in infos.OrEmpty().WhereNotNull())
        {
            //if (info.IsAnonymous()) return;
            //if (info.IsInner()) return;
            ScanMethodBase(info);
            ScanParameterInfo(info.ReturnParameter);
            ScanCustomAttributeData(info.GetCustomAttributesData());
            Add(info.ReturnType);
        }
    }

    private void ScanPropertyInfo(params PropertyInfo?[]? infos)
    {
        foreach (var info in infos.OrEmpty().WhereNotNull())
        {
            ScanMemberInfo(info);
            ScanParameterInfo(info.GetIndexParameters());
            ScanMethodInfo(info.GetMethod);
            ScanMethodInfo(info.SetMethod);
            Add(info.PropertyType);
        }
    }

    private void ScanMemberInfo(params MemberInfo?[]? infos)
    {
        foreach (var info in infos.OrEmpty().WhereNotNull())
        {
            ScanCustomAttributes(info);
            ScanCustomAttributeData(info.GetCustomAttributesData());
        }
    }

    private void ScanMethodBase(params MethodBase?[]? infos)
    {
        foreach (var info in infos.OrEmpty().WhereNotNull())
        {
            if (!info.IsConstructor && !StringComparer.Ordinal.Equals(info.Name, ".cctor"))
            {
                var reflectedType = info.ReflectedType;

                if (reflectedType != null && reflectedType.IsRealType())
                {
                    try
                    {
                        Add(info.GetGenericArguments());
                    }
                    catch (Exception e)
                    {
                        throw new TargetInvocationException($"Error {info}.GetGenericArguments()  {nameof(info.Name)}={info.Name}  {nameof(info.ReflectedType)}={info.ReflectedType?.FullNameFormatted()}", e);
                    }
                }
            }

            ScanParameterInfo(info.GetParameters());

            ScanMemberInfo(info);
        }
    }

    private void ScanParameterInfo(params ParameterInfo?[]? infos)
    {
        foreach (var info in infos.OrEmpty().WhereNotNull())
        {
            Add(info.ParameterType);
            foreach (var attr in info.GetCustomAttributes(true)) Add(attr.GetType());
            ScanCustomAttributes(info);
            ScanCustomAttributeData(info.GetCustomAttributesData());
        }
    }

    private void ScanCustomAttributes(ICustomAttributeProvider? customAttributeProvider)
    {
        if (customAttributeProvider == null) return;
        foreach (var attr in customAttributeProvider.GetCustomAttributes(true).WhereNotNull()) Add(attr.GetType());
    }

    private void ScanCustomAttributeData(IEnumerable<CustomAttributeData?>? info)
    {
        foreach (var attr in info.OrEmpty()) ScanCustomAttributeData(attr);
    }

    private void ScanCustomAttributeData(params CustomAttributeData?[]? infos)
    {
        foreach (var info in infos.OrEmpty().WhereNotNull())
        {
            Add(info.AttributeType);
            ScanConstructorInfo(info.Constructor);
            foreach (var a in info.ConstructorArguments) Add(a.ArgumentType);
            foreach (var a in info.NamedArguments.OrEmpty()) Add(a.TypedValue.ArgumentType);
        }
    }
}
