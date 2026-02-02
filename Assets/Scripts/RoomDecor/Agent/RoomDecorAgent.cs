using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class RoomDecorAgent : Agent
{
    public RoomDecorInstaller installer;

    private IResetService resetService;
    private IObservationProvider observationProvider;
    private IRoomDecorStepUseCase stepUseCase;

    private RoomState state;
    private DecorCatalog catalog;
    private RoomDecorSettings settings;

    public void Inject(RoomState state, DecorCatalog catalog, RoomDecorSettings settings,
        IResetService resetService, IObservationProvider observationProvider, IRoomDecorStepUseCase stepUseCase)
    {
        this.state = state;
        this.catalog = catalog;
        this.settings = settings;
        this.resetService = resetService;
        this.observationProvider = observationProvider;
        this.stepUseCase = stepUseCase;
    }

    public override void Initialize()
    {
        if (installer != null) installer.BuildAndInject(this);
        else Debug.LogError("[RoomDecorAgent] installer가 비어있음. RoomDecorInstaller를 연결해줘.");
    }

    public override void OnEpisodeBegin() => resetService?.ResetEpisode();

    public override void CollectObservations(VectorSensor sensor) => observationProvider?.Collect(sensor);

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (state == null || catalog == null || settings == null || stepUseCase == null)
        {
            AddReward(-1f);
            EndEpisode();
            return;
        }

        var act = ActionParser.Parse(actions.DiscreteActions.Array, state.sockets.Count, catalog.Count, settings.rotationCount);
        var res = stepUseCase.Step(act);

        AddReward(res.rewardDelta);
        if (res.endEpisode) EndEpisode();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var a = actionsOut.DiscreteActions;
        if (a.Length < 4) return;

        int socketCount = state != null ? Mathf.Max(1, state.sockets.Count) : 1;
        int itemCount = catalog != null ? Mathf.Max(1, catalog.Count) : 1;
        int rotCount = settings != null ? Mathf.Max(1, settings.rotationCount) : 1;

        a[0] = Random.Range(0, socketCount);
        a[1] = Random.Range(0, itemCount);
        a[2] = Random.Range(0, rotCount);

        bool random = settings != null && settings.heuristicRandom;
        a[3] = (!random) ? (int)PlaceOp.Skip : ((Random.value < 0.7f) ? (int)PlaceOp.Place : (int)PlaceOp.Skip);
    }
}
