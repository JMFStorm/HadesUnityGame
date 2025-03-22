using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemySpawnData
{
    public EnemyType EnemyType;
    public int SpawnCount;
}

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(AudioSource))]
public class ArenaEventSpawn : MonoBehaviour
{
    public List<EnemySpawnData> SpawnData = new();

    public static ArenaEventSpawn Clone(ArenaEventSpawn original)
    {
        GameObject newObject = new(original.name + "_CopyOriginal");
        ArenaEventSpawn clone = newObject.AddComponent<ArenaEventSpawn>();

        clone.SpawnData = new List<EnemySpawnData>();

        foreach (var spawn in original.SpawnData)
        {
            clone.SpawnData.Add(new EnemySpawnData
            {
                EnemyType = spawn.EnemyType,
                SpawnCount = spawn.SpawnCount
            });
        }

        return clone;
    }

    [HideInInspector]
    public string Id;

    private void Awake()
    {
        if (string.IsNullOrEmpty(Id))
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}