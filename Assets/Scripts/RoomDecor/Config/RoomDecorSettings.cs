using UnityEngine;

[CreateAssetMenu(menuName = "RoomDecor/Room Decor Settings")]
public class RoomDecorSettings : ScriptableObject
{
    [Header("Episode")]
    public int maxPlacements = 10;
    public int failStreakLimit = 20;

    [Header("Actions")]
    public int rotationCount = 4; // 0/90/180/270
    public int opCount = 3;       // Place/Skip/End

    [Header("Observations")]
    public int maxSocketsForObs = 32;
    public float maxDoorDistance = 12f;

    [Header("Rewards (초기 튜닝값)")]
    public float stepPenalty = -0.001f;
    public float successReward = +0.10f;
    public float failPenalty = -0.20f;
    public float rulePenaltyMultiplier = 1.0f;
    public float repeatItemPenaltyBase = -0.02f;
    public float finalDiversityBonusMax = +0.50f;

    [Header("Heuristic Demo")]
    public bool heuristicRandom = true;
}
