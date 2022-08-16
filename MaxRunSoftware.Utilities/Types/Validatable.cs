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

public interface IValidatable<in T>
{
    void Validate(T validationObject);
}

public interface IValidatable : IValidatable<IValidationFailureCollection> { }

public interface IValidationFailureCollection
{
    void Add(ValidationFailure failure);
}

public class ValidationFailureCollection : IValidationFailureCollection
{
    public void Add(ValidationFailure failure) => Failures.Add(failure);
    public List<ValidationFailure> Failures { get; } = new();
}

public static class ValidationFailureCollectionExtensions
{
    public static ValidationFailure Add(this IValidationFailureCollection failures, object source, string message)
    {
        var f = new ValidationFailure(source, message);
        failures.Add(f);
        return f;
    }

    public static ValidationFailure Add(this IValidationFailureCollection failures, object source, Exception exception, string message)
    {
        var f = new ValidationFailure(source, exception, message);
        failures.Add(f);
        return f;
    }
}

public class ValidationFailure
{
    public string Message { get; }
    public Exception Exception { get; }
    public object Source { get; }

    public ValidationFailure(object source, Exception exception, string message)
    {
        Source = source.CheckNotNull(nameof(source));
        Exception = exception;
        Message = message.CheckNotNullTrimmed(nameof(message));
    }

    public ValidationFailure(object source, string message) : this(source, null, message) { }
}
