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

public class CommandScannerResult
{
    public HashSet<TypeSlim> Types { get; }
    public List<CommandDetail> Commands { get; } = new();
    public List<CommandDetailWrapped> CommandsInvalid { get; } = new();

    public CommandScannerResult(IEnumerable<TypeSlim> types)
    {
        Types = new HashSet<TypeSlim>(types);

        var all = CommandDetailWrapped.Scan(Types, BindingFlags.Public | BindingFlags.Instance);
        foreach (var cdw in all)
        {
            if (IsValid(cdw)) Commands.Add(cdw.Detail!);
            else CommandsInvalid.Add(cdw);
        }
    }

    private bool IsValid(CommandDetailWrapped c) =>
        c.Detail != null &&
        c.Exception == null &&
        c.Options.All(o => o.Detail != null && o.Exception == null) &&
        c.Arguments.All(o => o.Detail != null && o.Exception == null);
}

public class CommandScanner
{
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
        return new CommandScannerResult(scanTypes);
    }
}
