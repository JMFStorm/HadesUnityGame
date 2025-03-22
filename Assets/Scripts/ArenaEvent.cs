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

    private bool _arenaEventOngoing = false;
    private bool _arenaEnded = false;

    private List<ArenaEventSpawn> _arenaSpawns = new();
    private List<ArenaEventSpawn> _arenaSpawnsOriginal = new();

    private Dictionary<string, EnemyBase> _spawnPointDict = new();
    private Dictionary<string, ArenaEventSpawn> _enemyDict = new();

    private List<EnemyBase> _currentEnemies = new();

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
        _arenaSpawnsOriginal = _arenaSpawns.Select(spawn => ArenaEventSpawn.Clone(spawn)).ToList();
    }

    public void TriggerArenaEvent()
    {
        _arenaEventOngoing = true;

        foreach (var spawn in _arenaSpawns)
        {
            TrySpawnEnemy(spawn);
        }
    }

    void TrySpawnEnemy(ArenaEventSpawn spawnpoint)
    {
        while (0 < spawnpoint.SpawnData.Count)
        {
            var current = spawnpoint.SpawnData[0];

            if (0 < current.SpawnCount)
            {
                var enemyPrefab = GetEnemyByType(current.EnemyType);

                GameObject newEnemy = Instantiate(enemyPrefab, spawnpoint.transform.position, Quaternion.identity);
                var enemy = newEnemy.GetComponent<EnemyBase>();

                _currentEnemies.Add(enemy);
                _spawnPointDict.Add(spawnpoint.Id, enemy);
                _enemyDict.Add(enemy.Id, spawnpoint);

                --current.SpawnCount;

                break;
            }
            else
            {
                spawnpoint.SpawnData.RemoveAt(0);
            }
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

        _currentEnemies.Remove(enemy);

        _enemyDict.Remove(enemy.Id);
        _spawnPointDict.Remove(spawnPoint.Id);

        TrySpawnEnemy(spawnPoint);

        if (_arenaSpawns.All(x => x.SpawnData.Count <= 0))
        {
            CompleteArenaEvent();
        }
    }

    void CompleteArenaEvent()
    {
        _arenaEventOngoing = false;
        _arenaEnded = true;
    }

    public void ResetArenaEvent()
    {
        _arenaSpawns = _arenaSpawnsOriginal;

        foreach (var enemy in _currentEnemies)
        {
            Destroy(enemy.gameObject);
        }

        _currentEnemies.Clear();

        _arenaEventOngoing = false;
        _arenaEnded = false;
    }
}
