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

using System.Diagnostics.CodeAnalysis;

namespace MaxRunSoftware.Utilities.CommandLine;

public enum CommandArgumentType
{
    Option,
    Parameter
}

public enum CommandArgumentParserType
{
    String,
    Enum,
    Bool,
    Byte,
    SByte,
    Short,
    UShort,
    Int,
    UInt,
    Long,
    ULong,
    Float,
    Double,
    Decimal,
    DateTime,
    DateOnly,
    TimeOnly,
    File,
    Directory,
    FileOrDirectory
}

public class Argument : CommandObject, IValidatable
{
    // ReSharper disable once InconsistentNaming
    public static readonly ImmutableHashSet<CommandArgumentParserType> ParserTypes_Comparable = ImmutableHashSet.Create(
        CommandArgumentParserType.Byte,
        CommandArgumentParserType.SByte,
        CommandArgumentParserType.Short,
        CommandArgumentParserType.UShort,
        CommandArgumentParserType.Int,
        CommandArgumentParserType.UInt,
        CommandArgumentParserType.Long,
        CommandArgumentParserType.ULong,
        CommandArgumentParserType.Float,
        CommandArgumentParserType.Double,
        CommandArgumentParserType.Decimal,
        CommandArgumentParserType.DateTime,
        CommandArgumentParserType.DateOnly,
        CommandArgumentParserType.TimeOnly,
        CommandArgumentParserType.Enum
    );

    public static CommandArgumentParserType GetParserType(Type type)
    {
        type.CheckNotNull(nameof(type));

        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type.IsEnum) return CommandArgumentParserType.Enum;

        if (type.Is<string>()) return CommandArgumentParserType.String;

        if (type.Is<bool>()) return CommandArgumentParserType.Bool;

        if (type.Is<byte>()) return CommandArgumentParserType.Byte;
        if (type.Is<sbyte>()) return CommandArgumentParserType.SByte;

        if (type.Is<short>()) return CommandArgumentParserType.Short;
        if (type.Is<ushort>()) return CommandArgumentParserType.UShort;

        if (type.Is<int>()) return CommandArgumentParserType.Int;
        if (type.Is<uint>()) return CommandArgumentParserType.UInt;

        if (type.Is<long>()) return CommandArgumentParserType.Long;
        if (type.Is<ulong>()) return CommandArgumentParserType.ULong;

        if (type.Is<float>()) return CommandArgumentParserType.Float;
        if (type.Is<double>()) return CommandArgumentParserType.Double;
        if (type.Is<decimal>()) return CommandArgumentParserType.Decimal;

        if (type.Is<DateTime>()) return CommandArgumentParserType.DateTime;
        if (type.Is<DateOnly>()) return CommandArgumentParserType.DateOnly;
        if (type.Is<TimeOnly>()) return CommandArgumentParserType.TimeOnly;

        if (type.Is<FileSystemFile>()) return CommandArgumentParserType.File;
        if (type.Is<FileSystemDirectory>()) return CommandArgumentParserType.Directory;
        if (type.Is<FileSystemObject>()) return CommandArgumentParserType.FileOrDirectory;

        throw new NotImplementedException($"No {nameof(CommandArgumentParserType)} for {type.FullNameFormatted()}");
    }


    public ICommandBuilderArgument BuilderArgument { get; }
    public CommandArgumentType ArgumentType { get; set; }
    public CommandArgumentParserType ParserType { get; set; }

    public Argument(ICommand command, ICommandBuilderArgument builderArgument)
    {
        Command = command.CheckNotNull(nameof(command));
        BuilderArgument = builderArgument.CheckNotNull(nameof(builderArgument));
    }

    public string Name { get; set; }
    public string ShortName { get; set; }
    public string Description { get; set; }
    public object DefaultValue { get; set; }
    public bool IsRequired { get; set; }
    public bool AllSourcesIncluded { get; set; }
    public bool FileOrDirectoryMustExist { get; set; }
    public bool AllowWildcard { get; set; }
    public bool IsParameterMultiple { get; set; }
    public ushort? Index { get; set; }

    public ushort ParameterIndex
    {
        get
        {
            if (ArgumentType is not CommandArgumentType.Parameter) throw new InvalidOperationException($"{this}.{nameof(ArgumentType)}={ArgumentType} is not {CommandArgumentType.Parameter}");
            if (Index == null) throw new InvalidOperationException($"{this}.{nameof(Index)} has not been set");
            return Index.Value;
        }
    }

    public ushort ParameterMinRequired { get; set; }
    public object MinValue { get; set; }
    public object MaxValue { get; set; }
    public Type EnumType { get; set; }

    public override void Clean()
    {
        base.Clean();
        Name = Name.TrimOrNull();
        ShortName = ShortName.TrimOrNull();
        if (ShortName.EqualsIgnoreCase(Name)) ShortName = null;
        Description = Description.TrimOrNull();
        if (IsRequired && ParameterMinRequired < 1) ParameterMinRequired = 1;
        if (ParameterMinRequired > 0) IsRequired = true;
    }

    public bool IsComparable => ParserTypes_Comparable.Contains(ParserType);

    public override void Validate(IValidationFailureCollection failures)
    {
        base.Validate(failures);

        if (DefaultValue != null && IsRequired) failures.Add(this, $"{this} cannot both {nameof(IsRequired)}={IsRequired} and have a {nameof(DefaultValue)}={DefaultValue}");

        if (Name == null) failures.Add(this, $"{ToStringDetailed()} does not have a {nameof(Name)} assigned");

        if (ArgumentType == CommandArgumentType.Parameter && ShortName != null) failures.Add(this, $"{this}.{ArgumentType} does not support {nameof(ShortName)}={ShortName} value being assigned");
        if (ArgumentType == CommandArgumentType.Option && Index > 0) failures.Add(this, $"{this}.{ArgumentType} does not support {nameof(Index)}={Index} value being assigned");

        if (Description == null) failures.Add(this, $"{this} does not have a {nameof(Description)} assigned");

        var parseEnum = true;
        if (ParserType == CommandArgumentParserType.Enum)
        {
            if (EnumType == null)
            {
                failures.Add(this, $"{this}.{nameof(ParserType)}={ParserType} but {nameof(EnumType)} is null");
                parseEnum = false;
            }
            else if (!EnumType.IsEnum)
            {
                failures.Add(this, $"{this}.{nameof(EnumType)}={EnumType.NameFormatted()} but that Type is not an Enum");
                parseEnum = false;
            }
        }

        if (!parseEnum) return; // we are ParserType=Enum but have invalid values so we can't test others

        var convertErrorMsg = $"{this}.{{0}} could not convert {{1}} to " + (ParserType == CommandArgumentParserType.Enum ? $"Enum type {EnumType.NameFormatted()} valid values are: " + EnumType.GetEnumNames(", ", true) : $"type {ParserType}");

        object defaultValueConverted = null;
        try { defaultValueConverted = Convert(DefaultValue); }
        catch (Exception e) { failures.Add(this, e, string.Format(convertErrorMsg, nameof(DefaultValue), DefaultValue)); }

        var defaultValueComparable = (IsComparable ? defaultValueConverted : null) as IComparable;


        object minValueConverted = null;
        try { minValueConverted = Convert(MinValue); }
        catch (Exception e) { failures.Add(this, e, string.Format(convertErrorMsg, nameof(MinValue), MinValue)); }

        if (MinValue != null && !IsComparable)
        {
            failures.Add(this, $"{this}.{nameof(MinValue)}={MinValue} is not a valid range type for {nameof(ParserType)}={ParserType}");
            minValueConverted = null;
        }

        var minValueComparable = (IsComparable ? minValueConverted : null) as IComparable;


        object maxValueConverted = null;
        try { maxValueConverted = Convert(MaxValue); }
        catch (Exception e) { failures.Add(this, e, string.Format(convertErrorMsg, nameof(MaxValue), MaxValue)); }

        if (MaxValue != null && !IsComparable)
        {
            failures.Add(this, $"{this}.{nameof(MaxValue)}={MaxValue} is not a valid range type for {nameof(ParserType)}={ParserType}");
            maxValueConverted = null;
        }

        // ReSharper disable once UsePatternMatching
        var maxValueComparable = (IsComparable ? maxValueConverted : null) as IComparable;


        if (minValueComparable != null && maxValueComparable != null && minValueComparable.CompareTo(maxValueComparable) > 0) failures.Add(this, $"{this}.{nameof(MinValue)}={minValueConverted} cannot be greater than {nameof(MaxValue)}={maxValueConverted}");

        if (defaultValueComparable != null)
        {
            if (minValueConverted != null && defaultValueComparable.CompareTo(minValueComparable) < 0) failures.Add(this, $"{this}.{nameof(DefaultValue)}={defaultValueConverted} cannot be less than {nameof(MinValue)}={minValueConverted}");
            if (maxValueConverted != null && defaultValueComparable.CompareTo(maxValueComparable) > 0) failures.Add(this, $"{this}.{nameof(DefaultValue)}={defaultValueConverted} cannot be greater than {nameof(MaxValue)}={maxValueConverted}");
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append(ArgumentType);
        sb.Append('[');
        if (ArgumentType == CommandArgumentType.Parameter) sb.Append(Index + ":");
        sb.Append(Name.TrimOrNull() ?? ShortName.TrimOrNull() ?? string.Empty);
        sb.Append(']');

        return sb.ToString();
    }

    public string ToStringDetailed() => base.ToString();

    private object Convert([AllowNull] object value) => Convert(value?.ToString());

    private object Convert([AllowNull] string value) =>
        ParserType switch
        {
            CommandArgumentParserType.Bool => value.TrimOrNull().ToBoolNullable(),
            CommandArgumentParserType.Byte => value.TrimOrNull().ToByteNullable(),
            CommandArgumentParserType.SByte => value.TrimOrNull().ToSByte(),
            CommandArgumentParserType.String => value,
            CommandArgumentParserType.Short => value.TrimOrNull().ToShortNullable(),
            CommandArgumentParserType.UShort => value.TrimOrNull().ToUShortNullable(),
            CommandArgumentParserType.Int => value.TrimOrNull().ToIntNullable(),
            CommandArgumentParserType.UInt => value.TrimOrNull().ToUIntNullable(),
            CommandArgumentParserType.Long => value.TrimOrNull().ToLongNullable(),
            CommandArgumentParserType.ULong => value.TrimOrNull().ToULongNullable(),
            CommandArgumentParserType.Float => value.TrimOrNull().ToFloatNullable(),
            CommandArgumentParserType.Double => value.TrimOrNull().ToDoubleNullable(),
            CommandArgumentParserType.Decimal => value.TrimOrNull().ToDecimalNullable(),
            CommandArgumentParserType.DateTime => value.TrimOrNull().ToDateTimeNullable(),
            CommandArgumentParserType.DateOnly => value.TrimOrNull().ToDateOnlyNullable(),
            CommandArgumentParserType.TimeOnly => value.TrimOrNull().ToTimeOnlyNullable(),
            CommandArgumentParserType.File => value,
            CommandArgumentParserType.Directory => value,
            CommandArgumentParserType.FileOrDirectory => value,
            CommandArgumentParserType.Enum => value.TrimOrNull().ToEnumNullable(EnumType),
            _ => throw new NotImplementedException($"{this}.Convert({value}, {ParserType}) has no parser available")
        };

    public object Parse([AllowNull] object value) => Convert(value?.ToString());
    public object Parse([AllowNull] string value) => value == null ? null : Convert(value);
}
