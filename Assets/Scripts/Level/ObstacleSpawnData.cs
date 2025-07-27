using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ObstacleSpawnData", menuName = "ScriptableObjects/ObstacleSpawnData")]
public class ObstacleSpawnData : ScriptableObject
{
    public GameObject ObstaclePrefab;
    [Tooltip("If not 0 then use this coordinate to spawn obstacle at exact point")]
    public Vector3 PredefinedSpawnPosition = Vector3.zero;
    [Tooltip("If not 0 then use this coordinate to spawn obstacle with this exact rotation")]
    public float PredefinedSpawnRotationY = 0;
    [Tooltip("If SpawnPosition is 0 then use random zone from this array and " +
        "spawn obstacle at random position inside this zone")]
    public List<LevelZoneData> RandomSpawnZones = new();
    [Min(1)]
    public int ObstaclesAmount = 1;

    public List<float> SpawnRotationsY
    {
        get
        {
            List<float> result = new();
            if (PredefinedSpawnRotationY != 0)
            {
                result.Add(PredefinedSpawnRotationY);
                return result;
            }
            for (int i = 0; i < ObstaclesAmount; i++)
            {
                result.Add(Random.Range(0f, 360f));
            }
            return result;
        }
    }
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

            for (int i = 0; i < ObstaclesAmount; i++)
            {
                LevelZoneData randomZone = RandomSpawnZones[Random.Range(0, RandomSpawnZones.Count)];
                result.Add(randomZone.GetRandomPointInZone());
            }
            return result;
        }
    }
}
