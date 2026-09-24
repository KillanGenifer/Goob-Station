using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxGoob.Intentions.Prototypes;

/// <summary>
/// Content prototype that describes a scenario distributed as a bundle of intention slots.
/// </summary>
[Prototype, DataDefinition]
public sealed partial class IntentionPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    /// <summary>
    /// Unique identifier used during validation, wave selection, and runtime assignment tracking.
    /// </summary>
    public string ID { get; private set; } = default!;

    [DataField]
    /// <summary>
    /// Human-readable content name used by tooling and debug output.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    /// Optional Fluent localization key for category description text.
    /// </summary>
    public string? Description;

    [DataField]
    /// <summary>
    /// Category that controls quotas, UI fallbacks, and primary ownership limits.
    /// </summary>  
    public ProtoId<ScenarioCategoryPrototype> Category;

    [DataField]
    /// <summary>
    /// Relative selection weight inside the category pool.
    /// </summary>
    public int Weight = 1;

    [DataField]
    public bool RequireAll = false;

    /// <summary>
    /// Ordered slot declarations that are validated into a deterministic build order.
    /// </summary>
    public List<PredicateDefinition> Entries = new();

    [DataField]
    /// <summary>
    /// Ordered slot declarations that are validated into a deterministic build order.
    /// </summary>
    public List<Predicators>? Actors;
}

[DataDefinition]
public sealed partial class Predicators
{
    [DataField(required: true)]
    /// <summary>
    /// Human-readable content name used by tooling and debug output.
    /// </summary>
    public string Key = string.Empty;

    [DataField]
    /// <summary>
    /// Human-readable content name used by tooling and debug output.
    /// </summary>
    public string Name = string.Empty;

    [DataField]
    /// <summary>
    /// Optional Fluent localization key for category description text.
    /// </summary>
    public string? Description;

    [DataField]
    public bool RequireAll = false;

    [DataField]
    public List<PredicateDefinition>? Predicates;
}

/// <summary>
/// Generic predicate declaration shared by global and candidate checks.
/// </summary>
[DataDefinition]
public sealed partial class PredicateDefinition
{
    [DataField(required: true)]
    /// <summary>
    /// Predicate scope, such as round or candidate.
    /// </summary>
    public PredicateDefinitionScope Scope;

    [DataField(required: true)]
    /// <summary>
    /// Logical field name resolved by the predicate schema for the chosen scope.
    /// </summary>
    public PredicateDefinitionField Field;

    [DataField(required: true)]
    /// <summary>
    /// Comparison operator applied to the resolved field value.
    /// </summary>
    public PredicateDefinitionOperator Operator;

    [DataField(required: true)]
    /// <summary>
    /// Single expected value used by unary and comparison operators.
    /// </summary>
    public string Key = String.Empty;
}

[NetSerializable, Serializable]
public struct IntentionData(string name, string? description, ProtoId<IntentionPrototype>? origIntention = null)
{
    public string Name = name;

    public string? Description = description;

    public ProtoId<IntentionPrototype>? OrigIntention = origIntention;
}

public enum PredicateDefinitionOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    GraterThanOrEquals,
    LessThan,
    LessThanOrEquals,
    Contains,
    NotContains,
}

public enum PredicateDefinitionScope
{
    Candidate,
    Round,
}

public enum PredicateDefinitionField
{
    Job,
    Department,
    Age,
    Species,
    Sex,
    Traits,
    HasMindshield,
    AntagRole,
    AntagObjective,
    Component,
}
