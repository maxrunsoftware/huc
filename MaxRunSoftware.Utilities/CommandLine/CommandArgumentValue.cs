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

public class CommandArgumentValue : CommandObject
{
    public Argument Argument { get; }
    public object Value { get; private set; }
    public string ValueRaw { get; private set; }
    public bool IsFound { get; private set; }
    public ICommandArgumentReaderSource Source { get; private set; }
    public Exception ParseException { get; private set; }
    public Exception ReadException { get; private set; }
    public ushort ReadIndex { get; private set; }

    public CommandArgumentValue(Argument argument)
    {
        Argument = argument.CheckNotNull(nameof(argument));
        Command = argument.Command;
    }

    public void Read(ICommandArgumentReaderSource source, ushort? index = null)
    {
        if (Source != null) throw new InvalidOperationException($"{Argument} already read once from {nameof(Source)}={Source}");
        Source = source;
        string value = null;
        if (Argument.ArgumentType == CommandArgumentType.Option)
        {
            try { IsFound = source.TryGetOption(Argument.Name, out value); }
            catch (Exception e) { ReadException = e; }

            if (!IsFound && Argument.ShortName != null)
            {
                try { IsFound = source.TryGetOption(Argument.ShortName, out value); }
                catch (Exception e) { ReadException ??= e; }
            }
        }
        else if (Argument.ArgumentType == CommandArgumentType.Parameter)
        {
            ReadIndex = index ?? Argument.Index ?? throw new NotImplementedException(); // TODO: Fix this
            try { IsFound = source.TryGetParameter(ReadIndex, out value); }
            catch (Exception e) { ReadException = e; }
        }
        else throw new NotImplementedException($"{nameof(CommandArgumentValue)}.{nameof(Argument)}.{nameof(Argument.ArgumentType)}={Argument.ArgumentType} is not yet supported");

        if (IsFound)
        {
            ValueRaw = value;
            try { Value = Argument.Parse(ValueRaw); }
            catch (Exception e) { ParseException = e; }
        }
    }

    public override void Clean()
    {
        base.Clean();
        Argument.Clean();
    }

    public override void Validate(IValidationFailureCollection failures)
    {
        base.Validate(failures);
        if (ReadException != null) failures.Add(this, ReadException, $"{this} failed to Read {Argument} from {Source}");
        if (ParseException != null) failures.Add(this, ParseException, $"{this} failed to Parse {Argument} from {Source} value '{ValueRaw}'");

        if (!IsFound || Value == null || ReadException != null || ParseException != null) return;

        if (Argument.IsComparable)
        {
            var valueComparable = (IComparable)Value;

            if (Argument.MinValue != null)
            {
                var minValue = (IComparable)Argument.Parse(Argument.MinValue);
                if (minValue != null && valueComparable.CompareTo(minValue) < 0) failures.Add(this, $"{Argument}.{nameof(Value)}={Value} cannot be less than {nameof(Argument.MinValue)}={minValue}");
            }

            if (Argument.MaxValue != null)
            {
                var maxValue = (IComparable)Argument.Parse(Argument.MaxValue);
                if (maxValue != null && valueComparable.CompareTo(maxValue) > 0) failures.Add(this, $"{Argument}.{nameof(Value)}={Value} cannot be greater than {nameof(Argument.MaxValue)}={maxValue}");
            }
        }

        if (Argument.FileOrDirectoryMustExist && Argument.ParserType is CommandArgumentParserType.File or CommandArgumentParserType.Directory or CommandArgumentParserType.FileOrDirectory)
        {
            var path = Value.ToString();
            if (path != null)
            {
                //if (!Util.FileIsAbsolutePath(path)) path = Path.Combine(Environment.WorkingDirectory, path);
                //if (Argument.ParserType is CommandArgumentParserType.File && !File.Exists(path)) failures.Add(this, $"{Argument}.{nameof(Value)}={Value} parsed path of {path} file does not exist");
            }
        }
    }
}
