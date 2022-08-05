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

namespace MaxRunSoftware.Utilities.Tests.Reflection;

public abstract class ReflectionTestBase : TestBase
{
    protected ReflectionTestBase(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    protected IReflectionDomain Domain { get; private set; }
    protected override void SetUp()
    {
        base.SetUp();
        Domain = new ReflectionDomain();
    }
    protected IReflectionType T(Type type) => Domain.GetType(type);
    protected IReflectionType T(object o) => Domain.GetType(o.GetType());
    protected IReflectionType T<TType>() => T(typeof(TType));

    protected IReflectionCollection<IReflectionMethod> M(object o, string name) => T(o.GetType()).Methods.Named(name);
    protected IReflectionCollection<IReflectionMethod> M<T>(string name) => M<T>().Named(name);
    protected IReflectionCollection<IReflectionMethod> M<T>() => T<T>().Methods;
    protected IReflectionField F(object o, string name) => T(o.GetType()).Fields.Named(name).First();
    protected IReflectionProperty P(object o, string name) => T(o.GetType()).Properties.Named(name).First();
}
