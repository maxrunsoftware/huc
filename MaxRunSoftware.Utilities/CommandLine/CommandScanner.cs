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

namespace MaxRunSoftware.Utilities.CommandLine;

public class CommandScanner
{
    public BindingFlags FlagsPropertyInfo { get; set; } = BindingFlags.Instance | BindingFlags.Public;
    private readonly HashSet<AssemblySlim> assemblies = new();
    private readonly HashSet<TypeSlim> types = new();

    public CommandScanner AddAssembly<T>() => AddAssembly(typeof(T));
    public CommandScanner AddAssembly(Type type) => AddAssembly(type.Assembly);
    public CommandScanner AddAssembly(Assembly assembly) => AddAssembly(new AssemblySlim(assembly));
    public CommandScanner AddAssembly(AssemblySlim assembly)
    {
        assemblies.Add(assembly);
        return this;
    }

    public CommandScanner AddType<T>() => AddType(typeof(T));
    public CommandScanner AddType(Type type) => AddType(new TypeSlim(type));
    public CommandScanner AddType(TypeSlim type)
    {
        types.Add(type);
        return this;
    }


    public CommandScannerResult Scan()
    {
        var scanTypes = new HashSet<TypeSlim>(types);
        foreach (var type in assemblies.SelectMany(assembly => assembly.Assembly.ExportedTypes)) scanTypes.Add(new TypeSlim(type));
        return new CommandScannerResult(scanTypes, FlagsPropertyInfo);
    }
}

public class CommandScannerResult
{
    public IReadOnlyList<CommandScannerResultType> Commands { get; }

    public CommandScannerResult(IEnumerable<TypeSlim> types, BindingFlags flagsPropertyInfo)
    {
        var commands = new List<CommandScannerResultType>();

        foreach (var type in types)
        {
            commands.Add(new CommandScannerResultType(type, flagsPropertyInfo));
        }

        Commands = commands;
    }
}

public class CommandScannerResultType
{
    public TypeSlim Type { get; }
    public CommandAttribute? Attribute { get; }
    public bool IsCommand => Type.Type.IsAssignableTo<ICommand>();
    public IReadOnlyList<CommandScannerResultArgument> Arguments { get; }
    public IReadOnlyList<CommandScannerResultOption> Options { get; }

    public bool IsNeedsInstanceFactory => !Type.Type.HasNoArgConstructor();
    public bool IsValid =>
        IsValidType &&
        IsCommand &&
        Attribute != null &&
        Arguments.All(o => o.IsValid) &&
        Options.All(o => o.IsValid);

    public bool IsValidType =>
        Type.Type.IsClass &&
        !Type.Type.IsValueType &&
        !Type.Type.IsAbstract &&
        !Type.Type.IsInterface &&
        !Type.Type.IsArray &&
        !Type.Type.IsEnum;

    public CommandScannerResultType(TypeSlim type, BindingFlags flagsPropertyInfo)
    {
        Type = type;
        Attribute = CommandUtil.GetAttribute<CommandAttribute>(Type.Type);

        var arguments = new List<CommandScannerResultArgument>();
        var options = new List<CommandScannerResultOption>();

        foreach (var propertyInfo in Type.Type.GetProperties(flagsPropertyInfo))
        {
            var argumentAttribute = CommandUtil.GetAttribute<ArgumentAttribute>(propertyInfo);
            var optionAttribute = CommandUtil.GetAttribute<OptionAttribute>(propertyInfo);
            var containsMultiplePropertyAttributesException = new Exception($"{CommandUtil.GetPropertyName(Type, propertyInfo, true)} contains both {argumentAttribute?.GetType().NameFormatted()} and {optionAttribute?.GetType().NameFormatted()} attributes");

            if (argumentAttribute != null)
            {
                ArgumentAttributeDetail? detail = null;
                Exception? exception = null;
                try
                {
                    detail = new ArgumentAttributeDetail(Type, propertyInfo, argumentAttribute, flagsPropertyInfo.IsNonPublic());
                    if (optionAttribute != null) throw containsMultiplePropertyAttributesException;
                }
                catch (Exception e)
                {
                    exception = e;
                }
                arguments.Add(new CommandScannerResultArgument(propertyInfo, argumentAttribute, detail, exception));
            }

            if (optionAttribute != null)
            {
                OptionAttributeDetail? detail = null;
                Exception? exception = null;
                try
                {
                    detail = new OptionAttributeDetail(Type, propertyInfo, optionAttribute, flagsPropertyInfo.IsNonPublic());
                    if (argumentAttribute != null) throw containsMultiplePropertyAttributesException;
                }
                catch (Exception e)
                {
                    exception = e;
                }
                options.Add(new CommandScannerResultOption(propertyInfo, optionAttribute, detail, exception));
            }
        }

        Arguments = arguments;
        Options = options;
    }
}

public abstract class CommandScannerResultProperty<TAttribute, TDetail> where TAttribute : PropertyAttribute where TDetail : PropertyAttributeDetail<TAttribute>
{
    public PropertyInfo Info { get; }
    public TAttribute Attribute { get; }
    public TDetail? Detail { get; }
    public Exception? Exception { get; }
    public bool IsValid => Exception != null;

    protected CommandScannerResultProperty(PropertyInfo info, TAttribute attribute, TDetail? detail, Exception? exception)
    {
        Info = info;
        Attribute = attribute;
        Detail = detail;
        Exception = exception;
    }
}

public class CommandScannerResultArgument : CommandScannerResultProperty<ArgumentAttribute, ArgumentAttributeDetail>
{
    public CommandScannerResultArgument(PropertyInfo info, ArgumentAttribute attribute, ArgumentAttributeDetail? detail, Exception? exception) : base(info, attribute, detail, exception) { }
}

public class CommandScannerResultOption : CommandScannerResultProperty<OptionAttribute, OptionAttributeDetail>
{
    public CommandScannerResultOption(PropertyInfo info, OptionAttribute attribute, OptionAttributeDetail? detail, Exception? exception) : base(info, attribute, detail, exception) { }
}
