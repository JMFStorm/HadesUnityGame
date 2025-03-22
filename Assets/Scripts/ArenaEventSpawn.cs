using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemySpawnData
{
    public EnemyType EnemyType;
    public int SpawnCount;
}

public class ArenaEventSpawn : MonoBehaviour
{
    public List<EnemySpawnData> SpawnData = new();

    public string Id;

    private void Awake()
    {
        if (string.IsNullOrEmpty(Id))
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
