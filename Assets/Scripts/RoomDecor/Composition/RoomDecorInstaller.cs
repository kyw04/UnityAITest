using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoomDecorInstaller : MonoBehaviour
{
    public DecorCatalog catalog;
    public RoomDecorSettings settings;
    public Transform door;
    public Transform spawnedRoot;

    [Tooltip("처음엔 Decor 레이어만 추천(바닥/벽 레이어 포함 시 overlap 실패 가능)")]
    public LayerMask blockingLayers;

    public bool autoFindSockets = true;
    public List<DecorationSocket> sockets = new();

    public void BuildAndInject(RoomDecorAgent agent)
    {
        if (agent == null) return;

        if (spawnedRoot == null)
        {
            var go = new GameObject("SpawnedDecorRoot");
            spawnedRoot = go.transform;
        }

        if (autoFindSockets)
        {
#if UNITY_2022_2_OR_NEWER || UNITY_2023_1_OR_NEWER
            sockets = Object.FindObjectsByType<DecorationSocket>(FindObjectsSortMode.None)
                .OrderBy(s => s.name).ToList();
#else
            sockets = Object.FindObjectsOfType<DecorationSocket>()
                .OrderBy(s => s.name).ToList();
#endif
        }

        if (settings == null)
        {
            Debug.LogError("[RoomDecorInstaller] settings가 비어있음. RoomDecorSettings 에셋 만들어서 연결해줘.");
            return;
        }

        var state = new RoomState(sockets, settings.maxPlacements);

        ISpawner spawner = new PrefabSpawner(spawnedRoot);
        IOverlapChecker overlap = new UnityOverlapChecker(blockingLayers);

        var rules = new List<IPlacementRule>
        {
            new SocketNotOccupiedRule(),
            new SocketTypeRule(),
            new OverlapRule(overlap),
        };
        IPlacementValidator validator = new CompositePlacementValidator(rules);

        IPlacementService placement = new PlacementService(state, catalog, validator, spawner, settings);
        IScoringPolicy scoring = new SimpleScoringPolicy(settings);
        IRoomDecorStepUseCase usecase = new RoomDecorStepUseCase(state, placement, scoring, settings);
        IObservationProvider obs = new SocketObservationProvider(state, catalog, door, settings);
        IResetService reset = new RoomResetService(state, spawner);

        agent.Inject(state, catalog, settings, reset, obs, usecase);

        Debug.Log($"[RoomDecorInstaller] sockets={sockets.Count}, items={(catalog!=null?catalog.Count:0)}, VectorObsSize={RoomDecorSetupInfo.CalcVectorObsSize(settings.maxSocketsForObs)}");
    }
}
