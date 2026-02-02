using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Sensors;

// ===== Enums =====
public enum SocketType { Floor, Wall, Corner, Ceiling }
public enum DecorCategory { Prop, Plant, Light, Furniture }
public enum PlaceOp { Place = 0, Skip = 1, End = 2 }

// ===== Domain =====
public readonly struct PlaceAction
{
    public readonly int socketIndex;
    public readonly int itemIndex;
    public readonly int rotationIndex;
    public readonly PlaceOp op;
    public PlaceAction(int s, int i, int r, PlaceOp op) { socketIndex = s; itemIndex = i; rotationIndex = r; this.op = op; }
}

public struct StepResult
{
    public bool success;
    public float rewardDelta;
    public bool endEpisode;
    public string reason;
}

public class RoomState
{
    public readonly List<DecorationSocket> sockets;
    public readonly Dictionary<string, int> itemCounts = new();
    public readonly Dictionary<DecorCategory, int> categoryCounts = new();

    public int MaxPlacements { get; private set; }
    public int RemainingPlacements { get; private set; }
    public int FailStreak { get; set; }

    public RoomState(List<DecorationSocket> sockets, int maxPlacements)
    {
        this.sockets = sockets;
        MaxPlacements = maxPlacements;
        ResetEpisode();
    }

    public void ResetEpisode()
    {
        RemainingPlacements = MaxPlacements;
        FailStreak = 0;
        itemCounts.Clear();
        categoryCounts.Clear();
    }

    public void ConsumePlacement() { if (RemainingPlacements > 0) RemainingPlacements--; }

    public void RegisterPlacement(DecorItemDefinition item)
    {
        if (item == null) return;

        if (!itemCounts.ContainsKey(item.itemId)) itemCounts[item.itemId] = 0;
        itemCounts[item.itemId]++;

        if (!categoryCounts.ContainsKey(item.category)) categoryCounts[item.category] = 0;
        categoryCounts[item.category]++;
    }

    public int GetItemCount(string id) => itemCounts.TryGetValue(id, out var c) ? c : 0;
    public int GetCategoryCount(DecorCategory cat) => categoryCounts.TryGetValue(cat, out var c) ? c : 0;
}

// ===== Interfaces =====
public interface IResetService { void ResetEpisode(); }
public interface IObservationProvider { void Collect(VectorSensor sensor); }
public interface IRoomDecorStepUseCase { StepResult Step(PlaceAction action); StepResult EndEpisode(); }
public interface ISpawner { GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot); void ClearAll(); }
public interface IPlacementService { PlacementAttempt TryPlace(PlaceAction action); }
public interface IOverlapChecker { bool HasOverlap(Vector3 center, Vector3 halfExtents, Quaternion rot); }
public interface IPlacementRule { RuleResult Evaluate(PlaceAction action, DecorationSocket socket, DecorItemDefinition item, Vector3 center, Vector3 halfExtents, Quaternion rot); }
public interface IPlacementValidator { RuleResult Validate(PlaceAction action, DecorationSocket socket, DecorItemDefinition item, Vector3 center, Vector3 halfExtents, Quaternion rot); }
public interface IScoringPolicy { float OnStep(in PlaceAction action, RoomState state); float OnPlaced(DecorItemDefinition item, RoomState state); float OnEpisodeEnd(RoomState state); }

// ===== Placement structs =====
public readonly struct RuleResult
{
    public readonly bool ok;
    public readonly float penalty;
    public readonly string reason;
    public RuleResult(bool ok, float penalty, string reason) { this.ok = ok; this.penalty = penalty; this.reason = reason; }
    public static RuleResult Ok() => new RuleResult(true, 0f, "");
}

public readonly struct PlacementAttempt
{
    public readonly bool success;
    public readonly string reason;
    public readonly float immediatePenalty;
    public readonly GameObject spawned;
    public readonly DecorItemDefinition item;
    public readonly DecorationSocket socket;

    public PlacementAttempt(bool success, string reason, float immediatePenalty, GameObject spawned, DecorItemDefinition item, DecorationSocket socket)
    {
        this.success = success; this.reason = reason; this.immediatePenalty = immediatePenalty;
        this.spawned = spawned; this.item = item; this.socket = socket;
    }
}

// ===== Implementations =====
public class PrefabSpawner : ISpawner
{
    private readonly Transform root;
    public PrefabSpawner(Transform root) { this.root = root; }

    public GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
        => UnityEngine.Object.Instantiate(prefab, pos, rot, root);

    public void ClearAll()
    {
        if (root == null) return;
        for (int i = root.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(root.GetChild(i).gameObject);
    }
}

public class UnityOverlapChecker : IOverlapChecker
{
    private readonly LayerMask layers;
    public UnityOverlapChecker(LayerMask layers) { this.layers = layers; }

    public bool HasOverlap(Vector3 center, Vector3 halfExtents, Quaternion rot)
    {
        if (layers.value == 0) return false;
        var hits = Physics.OverlapBox(center, halfExtents, rot, layers, QueryTriggerInteraction.Ignore);
        return hits != null && hits.Length > 0;
    }
}

public class CompositePlacementValidator : IPlacementValidator
{
    private readonly List<IPlacementRule> rules;
    public CompositePlacementValidator(List<IPlacementRule> rules) { this.rules = rules; }

    public RuleResult Validate(PlaceAction action, DecorationSocket socket, DecorItemDefinition item, Vector3 center, Vector3 halfExtents, Quaternion rot)
    {
        for (int i = 0; i < rules.Count; i++)
        {
            var rr = rules[i].Evaluate(action, socket, item, center, halfExtents, rot);
            if (!rr.ok) return rr;
        }
        return RuleResult.Ok();
    }
}

public class SocketNotOccupiedRule : IPlacementRule
{
    public RuleResult Evaluate(PlaceAction action, DecorationSocket socket, DecorItemDefinition item, Vector3 center, Vector3 halfExtents, Quaternion rot)
    {
        if (socket == null) return new RuleResult(false, 0.2f, "socket_null");
        if (socket.occupied) return new RuleResult(false, 0.2f, "socket_occupied");
        return RuleResult.Ok();
    }
}

public class SocketTypeRule : IPlacementRule
{
    public RuleResult Evaluate(PlaceAction action, DecorationSocket socket, DecorItemDefinition item, Vector3 center, Vector3 halfExtents, Quaternion rot)
    {
        if (socket == null || item == null) return new RuleResult(false, 0.2f, "null_ref");
        if (item.allowedSocketTypes == null || item.allowedSocketTypes.Length == 0) return RuleResult.Ok();

        for (int i = 0; i < item.allowedSocketTypes.Length; i++)
            if (item.allowedSocketTypes[i] == socket.socketType) return RuleResult.Ok();

        return new RuleResult(false, 0.2f, "socket_type_not_allowed");
    }
}

public class OverlapRule : IPlacementRule
{
    private readonly IOverlapChecker overlap;
    public OverlapRule(IOverlapChecker overlap) { this.overlap = overlap; }

    public RuleResult Evaluate(PlaceAction action, DecorationSocket socket, DecorItemDefinition item, Vector3 center, Vector3 halfExtents, Quaternion rot)
    {
        if (overlap == null) return RuleResult.Ok();
        if (overlap.HasOverlap(center, halfExtents, rot)) return new RuleResult(false, 0.2f, "overlap_blocking");
        return RuleResult.Ok();
    }
}

public class PlacementService : IPlacementService
{
    private readonly RoomState state;
    private readonly DecorCatalog catalog;
    private readonly IPlacementValidator validator;
    private readonly ISpawner spawner;
    private readonly RoomDecorSettings settings;

    public PlacementService(RoomState state, DecorCatalog catalog, IPlacementValidator validator, ISpawner spawner, RoomDecorSettings settings)
    {
        this.state = state; this.catalog = catalog; this.validator = validator; this.spawner = spawner; this.settings = settings;
    }

    public PlacementAttempt TryPlace(PlaceAction action)
    {
        if (state == null || settings == null) return new PlacementAttempt(false, "missing_dependencies", 0.2f, null, null, null);
        if (state.RemainingPlacements <= 0) return new PlacementAttempt(false, "no_budget", 0f, null, null, null);
        if (state.sockets == null || state.sockets.Count == 0) return new PlacementAttempt(false, "no_sockets", 0.2f, null, null, null);
        if (catalog == null || catalog.Count == 0) return new PlacementAttempt(false, "no_items", 0.2f, null, null, null);

        int sIdx = Mathf.Clamp(action.socketIndex, 0, state.sockets.Count - 1);
        var socket = state.sockets[sIdx];
        var item = catalog.Get(action.itemIndex);

        if (socket == null || item == null || item.prefab == null) return new PlacementAttempt(false, "null_ref", 0.2f, null, item, socket);

        Quaternion localRot = GetLocalRotation(item, action.rotationIndex, settings.rotationCount);
        Vector3 pos = socket.transform.position;
        Quaternion rot = socket.transform.rotation * localRot;

        if (!TryGetPrefabBounds(item.prefab, out var localCenter, out var localSize))
            return new PlacementAttempt(false, "missing_bounds", 0.2f, null, item, socket);

        Vector3 scale = item.prefab.transform.lossyScale;
        Vector3 worldCenter = pos + (rot * Vector3.Scale(localCenter, scale));
        Vector3 halfExtents = Vector3.Scale(localSize * 0.5f, scale) * 0.98f;

        var rr = validator != null ? validator.Validate(action, socket, item, worldCenter, halfExtents, rot) : RuleResult.Ok();
        if (!rr.ok) return new PlacementAttempt(false, rr.reason, rr.penalty, null, item, socket);

        var spawned = spawner.Spawn(item.prefab, pos, rot);
        socket.SetOccupied(spawned);
        state.ConsumePlacement();
        state.RegisterPlacement(item);
        return new PlacementAttempt(true, "", 0f, spawned, item, socket);
    }

    static Quaternion GetLocalRotation(DecorItemDefinition item, int rotIndex, int rotCount)
    {
        if (item != null && !item.allowRotateY) return Quaternion.identity;
        if (rotCount <= 1) return Quaternion.identity;
        rotIndex = Mathf.Clamp(rotIndex, 0, rotCount - 1);
        float step = 360f / rotCount;
        return Quaternion.Euler(0f, rotIndex * step, 0f);
    }

    static bool TryGetPrefabBounds(GameObject prefab, out Vector3 center, out Vector3 size)
    {
        center = Vector3.zero; size = Vector3.zero;

        var pb = prefab.GetComponentInChildren<PlacementBounds>();
        if (pb != null)
        {
            center = pb.center; size = pb.size;
            return size.sqrMagnitude > 0.0001f;
        }

        var bc = prefab.GetComponentInChildren<BoxCollider>();
        if (bc != null)
        {
            center = bc.center; size = bc.size;
            return size.sqrMagnitude > 0.0001f;
        }

        return false;
    }
}

public class SimpleScoringPolicy : IScoringPolicy
{
    private readonly RoomDecorSettings settings;
    public SimpleScoringPolicy(RoomDecorSettings settings) { this.settings = settings; }

    public float OnStep(in PlaceAction action, RoomState state) => settings != null ? settings.stepPenalty : 0f;

    public float OnPlaced(DecorItemDefinition item, RoomState state)
    {
        if (settings == null || item == null || state == null) return 0f;
        int prev = state.GetItemCount(item.itemId) - 1;
        if (prev <= 0) return 0f;
        return settings.repeatItemPenaltyBase * prev;
    }

    public float OnEpisodeEnd(RoomState state)
    {
        if (settings == null || state == null) return 0f;
        float diversity = state.itemCounts.Count / 10f;
        return Mathf.Clamp01(diversity) * settings.finalDiversityBonusMax;
    }
}

public class RoomResetService : IResetService
{
    private readonly RoomState state;
    private readonly ISpawner spawner;
    public RoomResetService(RoomState state, ISpawner spawner) { this.state = state; this.spawner = spawner; }

    public void ResetEpisode()
    {
        spawner?.ClearAll();
        if (state == null) return;
        for (int i = 0; i < state.sockets.Count; i++) state.sockets[i]?.Clear();
        state.ResetEpisode();
    }
}

public class SocketObservationProvider : IObservationProvider
{
    private readonly RoomState state;
    private readonly DecorCatalog catalog;
    private readonly Transform door;
    private readonly RoomDecorSettings settings;

    public SocketObservationProvider(RoomState state, DecorCatalog catalog, Transform door, RoomDecorSettings settings)
    {
        this.state = state; this.catalog = catalog; this.door = door; this.settings = settings;
    }

    public void Collect(VectorSensor sensor)
    {
        if (state == null || sensor == null || settings == null) return;

        sensor.AddObservation(state.RemainingPlacements / Mathf.Max(1f, state.MaxPlacements));
        sensor.AddObservation(catalog != null ? Mathf.Clamp01(catalog.Count / 50f) : 0f);

        sensor.AddObservation(NormCat(DecorCategory.Prop));
        sensor.AddObservation(NormCat(DecorCategory.Plant));
        sensor.AddObservation(NormCat(DecorCategory.Light));
        sensor.AddObservation(NormCat(DecorCategory.Furniture));

        int maxSockets = Mathf.Max(1, settings.maxSocketsForObs);
        int n = Mathf.Min(state.sockets.Count, maxSockets);

        for (int i = 0; i < maxSockets; i++)
        {
            if (i < n && state.sockets[i] != null)
            {
                var s = state.sockets[i];
                sensor.AddObservation(s.occupied ? 1f : 0f);

                sensor.AddObservation(s.socketType == SocketType.Floor ? 1f : 0f);
                sensor.AddObservation(s.socketType == SocketType.Wall ? 1f : 0f);
                sensor.AddObservation(s.socketType == SocketType.Corner ? 1f : 0f);
                sensor.AddObservation(s.socketType == SocketType.Ceiling ? 1f : 0f);

                float d = 0f;
                if (door != null && settings.maxDoorDistance > 0.001f)
                    d = Vector3.Distance(s.transform.position, door.position) / settings.maxDoorDistance;
                sensor.AddObservation(Mathf.Clamp01(d));

                sensor.AddObservation(s.isCorridorCritical ? 1f : 0f);
            }
            else
            {
                for (int k = 0; k < 7; k++) sensor.AddObservation(0f);
            }
        }
    }

    float NormCat(DecorCategory cat)
    {
        int v = state.GetCategoryCount(cat);
        return Mathf.Clamp01(v / Mathf.Max(1f, state.MaxPlacements));
    }
}

public class RoomDecorStepUseCase : IRoomDecorStepUseCase
{
    private readonly RoomState state;
    private readonly IPlacementService placement;
    private readonly IScoringPolicy scoring;
    private readonly RoomDecorSettings settings;

    public RoomDecorStepUseCase(RoomState state, IPlacementService placement, IScoringPolicy scoring, RoomDecorSettings settings)
    {
        this.state = state; this.placement = placement; this.scoring = scoring; this.settings = settings;
    }

    public StepResult Step(PlaceAction action)
    {
        if (action.op == PlaceOp.End) return EndEpisode();

        float reward = scoring != null ? scoring.OnStep(action, state) : 0f;

        if (action.op == PlaceOp.Skip)
            return new StepResult { success = true, rewardDelta = reward, endEpisode = false, reason = "" };

        var attempt = placement.TryPlace(action);

        if (!attempt.success)
        {
            state.FailStreak++;
            float baseFail = settings != null ? settings.failPenalty : -0.2f;
            float rulePenalty = (settings != null ? settings.rulePenaltyMultiplier : 1f) * attempt.immediatePenalty;
            reward += baseFail - rulePenalty;

            bool end = (settings != null && state.FailStreak >= settings.failStreakLimit);
            return new StepResult { success = false, rewardDelta = reward, endEpisode = end, reason = attempt.reason };
        }

        state.FailStreak = 0;
        reward += (settings != null ? settings.successReward : 0.1f);
        if (scoring != null) reward += scoring.OnPlaced(attempt.item, state);

        bool shouldEnd = state.RemainingPlacements <= 0;
        if (shouldEnd) reward += scoring != null ? scoring.OnEpisodeEnd(state) : 0f;

        return new StepResult { success = true, rewardDelta = reward, endEpisode = shouldEnd, reason = "" };
    }

    public StepResult EndEpisode()
    {
        float reward = scoring != null ? scoring.OnEpisodeEnd(state) : 0f;
        return new StepResult { success = true, rewardDelta = reward, endEpisode = true, reason = "" };
    }
}

public static class ActionParser
{
    public static PlaceAction Parse(int[] discreteActions, int socketCount, int itemCount, int rotationCount)
    {
        int s = discreteActions.Length > 0 ? discreteActions[0] : 0;
        int i = discreteActions.Length > 1 ? discreteActions[1] : 0;
        int r = discreteActions.Length > 2 ? discreteActions[2] : 0;
        int o = discreteActions.Length > 3 ? discreteActions[3] : 1;

        s = Clamp(s, 0, Mathf.Max(0, socketCount - 1));
        i = Clamp(i, 0, Mathf.Max(0, itemCount - 1));
        r = Clamp(r, 0, Mathf.Max(0, rotationCount - 1));
        o = Clamp(o, 0, 2);

        return new PlaceAction(s, i, r, (PlaceOp)o);
    }

    static int Clamp(int v, int min, int max)
    {
        if (max < min) return min;
        if (v < min) return min;
        if (v > max) return max;
        return v;
    }
}
