using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitSpawnData", menuName = "ScriptableObjects/UnitSpawnData")]
public class UnitSpawnData : ScriptableObject
{
    public GameObject UnitPrefab;
    [Tooltip("If not 0 then use this coordinate to spawn unit at exact point")] 
    public Vector3 PredefinedSpawnPosition = Vector3.zero;
    [Tooltip("If SpawnPosition is 0 then use random zone from this array and " +
        "spawn unit at random position inside this zone")] 
    public List<LevelZoneData> RandomSpawnZones = new();
    [Tooltip("How much units must be spawned if RandomSpawnZones is used")]
    [Min(1)]
    public int UnitsAmount = 1;
    public List<Vector3> SpawnPositions
    {
        get
        {
            List<Vector3> result = new();
            if (PredefinedSpawnPosition != Vector3.zero)
            {
                result.Add(PredefinedSpawnPosition);
                return result;
            }

            for (int i = 0; i < UnitsAmount; i++)
            {
                LevelZoneData randomZone = RandomSpawnZones[Random.Range(0, RandomSpawnZones.Count)];
                result.Add(randomZone.GetRandomPointInZone());
            }
            return result;
        }
    }
}
