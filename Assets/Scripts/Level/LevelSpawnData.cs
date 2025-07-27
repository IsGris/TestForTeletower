using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelSpawnData", menuName = "ScriptableObjects/LevelSpawnData")]
public class LevelSpawnData : ScriptableObject
{
    public float LevelSize = 1;
    public List<UnitSpawnData> UnitSpawnDatas = new List<UnitSpawnData>();
    public List<ObstacleSpawnData> ObstacleSpawnDatas = new List<ObstacleSpawnData>();
}
