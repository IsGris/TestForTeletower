using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Assertions;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] GameObject _floor;
    [SerializeField] Camera _camera;

    private void Start()
    {
        if (_camera == null)
            _camera = Camera.main;
        Assert.IsNotNull(_camera);
    }

    public void GenerateLevel(LevelSpawnData levelData, int seed)
    {
        Random.InitState(seed);

        _floor.transform.localScale = new Vector3(levelData.LevelSize, 1, levelData.LevelSize);
        _floor.GetComponent<NavMeshSurface>().BuildNavMesh();
        AdjustCamera(levelData.LevelSize);
        foreach (var obstacleData in levelData.ObstacleSpawnDatas)
        {
            for (int i = 0; i < obstacleData.SpawnPositions.Count; i++)
            {
                Instantiate(
                    obstacleData.ObstaclePrefab,
                    obstacleData.SpawnPositions[i], 
                    Quaternion.Euler(0, obstacleData.SpawnRotationsY[i], 0));
            }
        }
        foreach (var unitData in levelData.UnitSpawnDatas)
        {
            for (int i = 0; i < unitData.SpawnPositions.Count; i++)
            {
                var unit = Instantiate(
                    unitData.UnitPrefab,
                    unitData.SpawnPositions[i],
                    Quaternion.identity);
                unit.name = $"Unit{Random.Range(10000000000000, 100000000000000).ToString("0")}";
            }
        }
    }
    private void AdjustCamera(float levelSize)
    {
        _camera.orthographicSize = levelSize * 5;
        _camera.gameObject.transform.position = new Vector3(0, 10, 0);
        _camera.gameObject.transform.rotation = Quaternion.Euler(90, 0, 0);
    }
}
