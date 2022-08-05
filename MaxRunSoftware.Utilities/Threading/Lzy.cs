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
using System.Threading;

namespace MaxRunSoftware.Utilities;

/// <summary>
/// LazyWithNoExceptionCaching: Basically the same as Lazy with LazyThreadSafetyMode of ExecutionAndPublication, BUT
/// exceptions are not cached
/// https://stackoverflow.com/a/42567351
/// </summary>
public sealed class Lzy<T>
{
    private Func<T> valueFactory;
    private T? value;
    private readonly object locker = new();
    private bool initialized;

    // ReSharper disable once InconsistentNaming
    private static readonly Func<T> EMPTY_VALUE_FACTORY = () => default!;

    public Lzy(Func<T> valueFactory)
    {
        this.valueFactory = valueFactory;
    }

    public bool IsValueCreated => initialized;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public T Value
    {
        get
        {
            //Mimic LazyInitializer.EnsureInitialized()'s double-checked locking, whilst allowing control flow to clear valueFactory on successful initialisation
            if (Volatile.Read(ref initialized)) return value!;

            lock (locker)
            {
                if (Volatile.Read(ref initialized)) return value!;

                value = valueFactory();
                Volatile.Write(ref initialized, true);
            }

            valueFactory = EMPTY_VALUE_FACTORY;
            return value;
        }
    }

    public override string? ToString() => IsValueCreated ? Value!.ToString() : "Value is not created."; // Throws NullReferenceException as if caller called ToString on the value itself
}
