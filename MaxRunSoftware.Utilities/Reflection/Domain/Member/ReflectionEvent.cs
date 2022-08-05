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

public interface IReflectionEvent : IReflectionObjectTypeMember<IReflectionEvent, EventInfo>, IReflectionBase<IReflectionEvent>
{
    IReflectionType? HandlerType { get; }
}

public sealed class ReflectionEvent : ReflectionObjectTypeMember<IReflectionEvent, EventInfo>, IReflectionEvent
{
    public ReflectionEvent(IReflectionType parent, EventInfo info) : base(parent, info)
    {
        handlerType = CreateLazy(HandlerType_Build);
        baseType = CreateLazy(Base_Build);
        bases = CreateLazy(Bases_Build);
    }

    public IReflectionType? HandlerType => handlerType.Value;
    private readonly Lzy<IReflectionType?> handlerType;
    private IReflectionType? HandlerType_Build() => GetType(Info.EventHandlerType);

    public IReflectionEvent? Base => baseType.Value;
    private readonly Lzy<IReflectionEvent?> baseType;
    private IReflectionEvent? Base_Build()
    {
        if (DeclarationFlags.IsNewShadowName()) return null;
        if (DeclarationFlags.IsNewShadowSignature()) return null;
        if (this.IsAbstract()) return null; // TODO: Double check this isn't possible
        if (!this.IsOverride()) return null;

        var currentType = Parent.Base;
        while (currentType != null)
        {
            var items = currentType.Events.Where(o => IsEqual(Info.Name, o.Info.Name)).Where(o => IsEqual(HandlerType, o.HandlerType)).ToList();
            if (items.Count > 0)
            {
                if (items.Count == 1) return items[0];
                if (items.Count > 1) throw new Exception($"Found multiple matching parent Events for {this} in parent class {currentType}: " + items.OrderBy(o => o).Select(o => o.ToString()).ToStringDelimited(" | "));
            }

            currentType = currentType.Base;
        }

        return null;
    }

    private readonly Lzy<IReflectionCollection<IReflectionEvent>> bases;
    public IReflectionCollection<IReflectionEvent> Bases => bases.Value;
    private IReflectionCollection<IReflectionEvent> Bases_Build() => this.Bases_Build(Domain);


    #region Overrides

    protected override string ToString_Build() => "event " + HandlerType.Name + " " + Parent.NameFull + "." + Info.Name;

    protected override int GetHashCode_Build() => Hash(
        base.GetHashCode_Build(),
        HandlerType
    );

    protected override bool Equals_Internal(IReflectionEvent other) =>
        base.Equals_Internal(other) &&
        IsEqual(HandlerType, other.HandlerType);

    protected override int CompareTo_Internal(IReflectionEvent other)
    {
        int c;
        if (0 != (c = base.CompareTo_Internal(other))) return c;
        if (0 != (c = Compare(HandlerType, other.HandlerType))) return c;
        return c;
    }

    #endregion Overrides
}

public static class ReflectionEventExtensions
{
    public static IReflectionCollection<IReflectionEvent> HandlerType<T>(this IReflectionCollection<IReflectionEvent> obj) => obj.ReturnType(o => o.HandlerType, true, typeof(T));
    public static IReflectionCollection<IReflectionEvent> HandlerType(this IReflectionCollection<IReflectionEvent> obj, Type type) => obj.ReturnType(o => o.HandlerType, true, type);
}
