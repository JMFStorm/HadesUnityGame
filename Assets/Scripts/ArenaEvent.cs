using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum EnemyType
{
    Mook = 0,
    Grunt,
    Aero,
    ShadowMook,
    ShadowGrunt,
    ShadowAero
}

public class ArenaEvent : MonoBehaviour
{
    public GameObject Mook;
    public GameObject Grunt;
    public GameObject Aero;
    public GameObject ShadowMook;
    public GameObject ShadowGrunt;
    public GameObject ShadowAero;

    private bool _arenaTriggered = false;

    private List<ArenaEventSpawn> _arenaSpawns = new();

    private Dictionary<string, EnemyBase> _spawnPointDict = new();
    private Dictionary<string, ArenaEventSpawn> _enemyDict = new();

    private void OnEnable()
    {
        EnemyBase.OnEnemyDied += HandleEnemyDeath;
    }

    private void OnDisable()
    {
        EnemyBase.OnEnemyDied -= HandleEnemyDeath;
    }

    private void Awake()
    {
        _arenaSpawns = GetSpawnPoints();
    }

    public void TriggerArenaEvent()
    {
        _arenaTriggered = true;

        Debug.Log("TriggerArenaEvent called");

        TrySpawnNewEnemies();
    }

    void TrySpawnNewEnemies()
    {
        foreach (var spawn in _arenaSpawns)
        {
            foreach (var item in spawn.SpawnData)
            {
                TrySpawnEnemy(spawn, item);
            }
        }
    }

    void TrySpawnEnemy(ArenaEventSpawn spawnpoint, EnemySpawnData spawnItem)
    {
        var contains = _spawnPointDict.ContainsKey(spawnpoint.Id);

        if (0 < spawnItem.SpawnCount && !contains)
        {
            Debug.Log($"{spawnpoint.name}: {spawnItem.EnemyType}, {spawnItem.SpawnCount}");

            var enemyPrefab = GetEnemyByType(spawnItem.EnemyType);

            GameObject newEnemy = Instantiate(enemyPrefab, spawnpoint.transform.position, Quaternion.identity);
            var enemy = newEnemy.GetComponent<EnemyBase>();

            _spawnPointDict.Add(spawnpoint.Id, enemy);
            _enemyDict.Add(enemy.Id, spawnpoint);

            Debug.Log($"Spawned enemy: {newEnemy.name}");

            --spawnItem.SpawnCount;
        }
    }

    public List<ArenaEventSpawn> GetSpawnPoints()
    {
        List<ArenaEventSpawn> spawnPoints = new();

        foreach (Transform child in transform)
        {
            if (child.name.ToLower().Contains("spawn"))
            {
                if (child.TryGetComponent<ArenaEventSpawn>(out var spawnComponent))
                {
                    spawnPoints.Add(spawnComponent);

                    Debug.Log($"{spawnComponent.name} added to spawn points");
                }
            }
        }

        return spawnPoints;
    }

    GameObject GetEnemyByType(EnemyType type)
    {
        return type switch
        {
            EnemyType.Mook => Mook,
            EnemyType.Grunt => Grunt,
            EnemyType.Aero => Aero,
            EnemyType.ShadowMook => ShadowMook,
            EnemyType.ShadowGrunt => ShadowGrunt,
            EnemyType.ShadowAero => ShadowAero,
            _ => throw new NotImplementedException($"enum type {type} not implemented")
        };
    }

    private void HandleEnemyDeath(EnemyBase enemy)
    {
        Debug.Log($"Enemy {enemy.name} died. Spawning new one...");

        var spawnPoint = _enemyDict[enemy.Id];
        _enemyDict.Remove(enemy.Id);
        _spawnPointDict.Remove(spawnPoint.Id);

        TrySpawnNewEnemies();
    }
}
