using Content.Shared.Interaction;
using Content.Shared.Tools.Components;
using Robust.Shared.Map.Components;

namespace Content.Server._CorvaxGoob.BSA;
public sealed partial class BSASystem : EntitySystem
{
    private Dictionary<BSAPartType, Vector2i> _partsOffsets = new();

    private void OnInteractUsing(Entity<BSAControlPanelComponent> entity, ref InteractUsingEvent ev)
    {
        if (!TryComp<ToolComponent>(ev.Used, out var toolComp))
            return;

        if (!_tool.HasQuality(ev.Used, "Pulsing"))
            return;

        if (ev.Handled)
            return;

        var xform = Transform(entity);

        if (!xform.Anchored)
            return;

        var (result, parts) = ScanCostruction(entity);

        if (!result || parts is null)
            return;

        var mapPos = _transform.GetMapCoordinates(entity);

        foreach (var part in parts)
            QueueDel(part);

        QueueDel(entity);

        Spawn("AdminInstantEffectSmoke3", mapPos);
        Spawn("MobHuman", mapPos);

        ev.Handled = true;
    }

    private (bool Result, List<EntityUid>? Parts) ScanCostruction(Entity<BSAControlPanelComponent> entity)
    {
/*        var xform = Transform(entity);
        if (!_transform.TryGetGridTilePosition((entity, xform), out var indices))
            return (false, null);

        if (!_partsOffsets.TryGetValue(entity.Comp.PartType, out var offsets))
            return (false, null);

        if (!TryComp<MapGridComponent>(xform.GridUid, out var gridComp))
            return (false, null);

        var partRotation = xform.LocalRotation;
        var corrects = 0;
        var required = offsets.Count;
        var parts = new List<EntityUid>();

        foreach (var offset in offsets)
        {
            var offsetRot = offset.Key.Rotate(Angle.FromDegrees(partRotation.Degrees - 90));
            var c = offsetRot + indices;

            foreach (var item in _mapSystem.GetAnchoredEntities(
                xform.GridUid.Value,
                gridComp,
                offset.Key.Rotate(partRotation) + indices))
            {
                if (!TryComp<BSAPartComponent>(item, out var itemPart))
                    continue;

                if (itemPart.PartType != offset.Value)
                    continue;

                if (Transform(item).LocalRotation != xform.LocalRotation)
                    continue;

                corrects++;
                parts.Add(item);
            }

            if (corrects == required)
                return (true, parts);
        }*/

        return (false, null);
    }
}
