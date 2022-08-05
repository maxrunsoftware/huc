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

using System.Runtime.Serialization;

namespace MaxRunSoftware.Utilities.CommandLine;

public class CommandLineException : Exception
{
    public ICommand? Command { get; }
    public string? CommandName { get; }

    public CommandLineException() { }
    protected CommandLineException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    public CommandLineException(string? message) : base(message) { }
    public CommandLineException(string? message, Exception? innerException) : base(message, innerException) { }

    public CommandLineException(ICommand? command, string? message) : this(command?.CommandName, message)
    {
        Command = command;
    }
    public CommandLineException(ICommand? command, string? message, Exception? innerException) : this(command?.CommandName, message, innerException)
    {
        Command = command;
    }

    public CommandLineException(string? commandName, string? message) : this(message)
    {
        CommandName = commandName;
    }
    public CommandLineException(string? commandName, string? message, Exception? innerException) : this(message, innerException)
    {
        CommandName = commandName;
    }
}
