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

using System.Linq.Expressions;

namespace MaxRunSoftware.Utilities;

public sealed class MethodCaller
{
    public MethodInfo MethodInfo { get; }
    public IReadOnlyList<ParameterInfo> Parameters { get; }
    public string Name { get; }
    public bool IsStatic { get; }
    public bool IsInstance => !IsStatic;
    public bool IsVoid { get; }
    public Delegate Delegate { get; }

    public object Call(object instance, params object[] args)
    {
        args ??= Array.Empty<object>();

        if (IsVoid)
        {
            switch (args.Length)
            {
                case 0:
                    ((Action<object>)Delegate)(instance);
                    return null;
                case 1:
                    ((Action<object, object>)Delegate)(instance, args[0]);
                    return null;
                case 2:
                    ((Action<object, object, object>)Delegate)(instance, args[0], args[1]);
                    return null;
                case 3:
                    ((Action<object, object, object, object>)Delegate)(instance, args[0], args[1], args[2]);
                    return null;
                default:
                    throw new NotImplementedException("Do not support VOID with " + args.Length + " arguments");
            }
        }

        switch (args.Length)
        {
            case 0:
                return ((Func<object, object>)Delegate)(instance);
            case 1:
                return ((Func<object, object, object>)Delegate)(instance, args[0]);
            case 2:
                return ((Func<object, object, object, object>)Delegate)(instance, args[0], args[1]);
            case 3:
                ((Func<object, object, object, object, object>)Delegate)(instance, args[0], args[1], args[2]);
                break;
            default:
                throw new NotImplementedException("Do not support OBJECT with " + args.Length + " arguments");
        }

        return null;
    }

    public MethodCaller(MethodInfo info)
    {
        MethodInfo = info.CheckNotNull(nameof(info));
        Name = info.Name;
        IsStatic = info.IsStatic;

        var returnType = info.ReturnType;
        IsVoid = returnType == typeof(void);

        /*
        if (returnType == typeof(void)) DefaultNullValue = null;
        else if (returnType.IsPrimitive || returnType.IsValueType || returnType.IsEnum) { DefaultNullValue = Activator.CreateInstance(returnType); }
        else { DefaultNullValue = null; }
        */

        var declaringType = info.DeclaringType;
        // Should not happen but if it does fail here rather then later trying to call it
        if (declaringType == null) throw new NullReferenceException("Could not determine class containing method " + Name);

        var instanceParameter = Expression.Parameter(typeof(object), "instance");
        var instanceUnary = IsStatic ? null : Expression.Convert(instanceParameter, declaringType);

        Parameters = info.GetParameters().ToList().AsReadOnly();
        var argumentsParameter = new List<ParameterExpression>(Parameters.Count);
        var argumentsUnary = new List<UnaryExpression>(Parameters.Count);

        for (var i = 0; i < Parameters.Count; i++)
        {
            var methodParameter = Parameters[i];
            var rawObject = Expression.Parameter(typeof(object), "arg" + (i + 1));
            argumentsParameter.Add(rawObject);

            var convertedObject = Expression.Convert(rawObject, methodParameter.ParameterType);
            argumentsUnary.Add(convertedObject);
        }

        Expression callExpression = Expression.Call(instanceUnary, info, argumentsUnary);
        if (!IsVoid) callExpression = Expression.TypeAs(callExpression, typeof(object));
        var lambda = Expression.Lambda(callExpression, instanceParameter.Yield().Concat(argumentsParameter).ToArray());
        Delegate = lambda.Compile();
    }

    public static IReadOnlyList<MethodCaller> GetCallers(Type classType, BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static) => classType.GetMethods(flags).Select(o => new MethodCaller(o)).ToList();

    public static MethodCaller GetCaller(Type classType, string methodName, params Type[] argumentTypes)
    {
        argumentTypes ??= Array.Empty<Type>();
        foreach (var caller in GetCallers(classType))
        {
            if (caller.Name != methodName) continue;
            if (caller.Parameters.Count != argumentTypes.Length) continue;

            var allMatch = true;
            for (var i = 0; i < argumentTypes.Length; i++)
            {
                if (caller.Parameters[i].ParameterType != argumentTypes[i]) allMatch = false;
                if (!allMatch) break;
            }

            if (!allMatch) continue;

            return caller;
        }

        return null;
    }

    public static MethodCaller GetCaller<TArg1>(Type classType, string methodName) => GetCaller(classType, methodName, typeof(TArg1));
    public static MethodCaller GetCaller<TArg1, TArg2>(Type classType, string methodName) => GetCaller(classType, methodName, typeof(TArg1), typeof(TArg2));
    public static MethodCaller GetCaller<TArg1, TArg2, TArg3>(Type classType, string methodName) => GetCaller(classType, methodName, typeof(TArg1), typeof(TArg2), typeof(TArg3));
    public static MethodCaller GetCaller<TArg1, TArg2, TArg3, TArg4>(Type classType, string methodName) => GetCaller(classType, methodName, typeof(TArg1), typeof(TArg2), typeof(TArg3), typeof(TArg4));
}
