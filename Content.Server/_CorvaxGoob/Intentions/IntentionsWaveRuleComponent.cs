using Content.Shared._CorvaxGoob.Intentions.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._CorvaxGoob.Intentions;

[RegisterComponent]
public sealed partial class IntentionsWaveRuleComponent : Component
{
    [DataField]
    public int MinPickIntentions = 3;

    [DataField]
    public int MaxPickIntentions = 6;

    [DataField]
    public Dictionary<ProtoId<ScenarioCategoryPrototype>, Vector2i>? IntentionsPerCategory;
}
