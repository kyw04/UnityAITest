using UnityEngine;

[ExecuteAlways]
public class RoomDecorSetupInfo : MonoBehaviour
{
    public RoomDecorInstaller installer;
    public DecorCatalog catalog;
    public RoomDecorSettings settings;

    public static int CalcVectorObsSize(int maxSocketsForObs)
    {
        return 6 + Mathf.Max(1, maxSocketsForObs) * 7;
    }

    private void Awake() => Print();
    private void OnValidate() => Print();

    void Print()
    {
        if (installer == null) installer = GetComponent<RoomDecorInstaller>();
        if (catalog == null && installer != null) catalog = installer.catalog;
        if (settings == null && installer != null) settings = installer.settings;

        int sockets = (installer != null && installer.sockets != null) ? installer.sockets.Count : 0;
        int items = (catalog != null) ? catalog.Count : 0;

        int maxSockets = (settings != null) ? settings.maxSocketsForObs : 32;
        int obsSize = CalcVectorObsSize(maxSockets);
        int rot = (settings != null) ? settings.rotationCount : 4;

        Debug.Log($"[RoomDecorSetupInfo] Recommended Discrete Branches = [{Mathf.Max(1,sockets)},{Mathf.Max(1,items)},{rot},3], VectorObsSize={obsSize} (maxSocketsForObs={maxSockets})");
    }
}
