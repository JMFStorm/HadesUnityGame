using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using static PlasticGui.LaunchDiffParameters;

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

    public Sprite TeleporterSprite;
    public AudioClip TeleporterStartSound;

    public bool BlockingEvent = false;
    public bool UseHadesVoice = false;
    public AnnouncerVoiceGroup AnnouncesVoiceGroup;

    private GlobalAudio _globalAudio;

    private List<ArenaEventSpawn> _arenaSpawns = new();
    private List<ArenaEventSpawn> _arenaSpawnsOriginal = new();

    private Dictionary<string, EnemyBase> _spawnPointDict = new();
    private Dictionary<string, ArenaEventSpawn> _enemyDict = new();

    private List<EnemyBase> _currentEnemies = new();
    private List<GameObject> _eventPlayerBlockers = new();

    private Coroutine _enemyTeleportCoroutine = null;

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
        _eventPlayerBlockers = GetBlockersAndDeactivate();
        _arenaSpawnsOriginal = _arenaSpawns.Select(spawn => spawn).ToList();

        _globalAudio = FindFirstObjectByType<GlobalAudio>();
    }

    public void TriggerArenaEvent()
    {
        foreach (var spawn in _arenaSpawns)
        {
            TrySpawnEnemy(spawn);
        }

        if (BlockingEvent)
        {
            foreach (var blocker in _eventPlayerBlockers)
            {
                var collider = blocker.GetComponent<BoxCollider2D>();
                var audioSource = blocker.GetComponent<AudioSource>();

                var sr = blocker.transform.Find("Sprite").GetComponent<SpriteRenderer>();
                sr.color = new Color(0.8f, 0f, 0f, 0.6f);
                sr.sortingOrder = -1;
                sr.transform.localScale = new Vector3(collider.size.x, collider.size.y, 1);
                sr.transform.position += (Vector3)collider.offset;

                var material = sr.material;
                material.SetFloat("_UVScaleX", collider.size.x);
                material.SetFloat("_UVScaleY", collider.size.y);
                material.SetFloat("_Distortion", 0.6f);
                material.SetFloat("_Speed", 0.3f);

                blocker.SetActive(true);
                StartCoroutine(FadeBlockerEffect(blocker, 0f, 1.0f, 3f, true));
            }
        }

        if (UseHadesVoice)
        {
            _globalAudio.PlayAnnouncerVoiceType(AnnouncesVoiceGroup);
        }
    }

    IEnumerator FadeBlockerEffect(GameObject blocker, float startAlpha, float endAlpha, float duration, bool setActiveAtEnd)
    {
        float elapsedTime = 0f;

        Color initialColor = new(0.0f, 0f, 0.0f, 0f);
        float initialVolume = startAlpha * 0.2f;
        float targetVolume = endAlpha * 0.2f;

        var audio = blocker.GetComponent<AudioSource>();
        var blockerRenderer = blocker.transform.Find("Sprite").GetComponent<SpriteRenderer>();

        var collider = blocker.GetComponent<BoxCollider2D>();
        collider.enabled = setActiveAtEnd;

        while (elapsedTime < duration)
        {
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);

            var newColor = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
            elapsedTime += Time.deltaTime;

            var newVolume = Mathf.Lerp(initialVolume, targetVolume, elapsedTime / duration);

            audio.volume = newVolume;
            blockerRenderer.material.SetColor("_Color", newColor);

            yield return null;
        }

        blockerRenderer.color = new Color(initialColor.r, initialColor.g, initialColor.b, endAlpha);
        blocker.SetActive(setActiveAtEnd);
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
                newEnemy.SetActive(false);
                newEnemy.transform.SetParent(this.transform);

                var enemy = newEnemy.GetComponent<EnemyBase>();

                _currentEnemies.Add(enemy);
                _spawnPointDict.Add(spawnpoint.Id, enemy);
                _enemyDict.Add(enemy.Id, spawnpoint);

                --current.SpawnCount;

                _enemyTeleportCoroutine = StartCoroutine(SpawnEnemyFromTeleport(enemy, spawnpoint));

                break;
            }
            else
            {
                spawnpoint.SpawnData.RemoveAt(0);
            }
        }
    }

    IEnumerator SpawnEnemyFromTeleport(EnemyBase enemy, ArenaEventSpawn spawn)
    {
        var spriteRenderer = spawn.GetComponent<SpriteRenderer>();
        var audioSource = spawn.GetComponent<AudioSource>();

        audioSource.clip = TeleporterStartSound;
        audioSource.loop = false;
        audioSource.volume = 0.75f;
        audioSource.Play();

        spriteRenderer.enabled = true;
        spriteRenderer.sprite = TeleporterSprite;

        yield return new WaitForSeconds(2.0f);

        enemy.gameObject.SetActive(true);

        spriteRenderer.enabled = false;
    }

    public List<GameObject> GetBlockersAndDeactivate()
    {
        List<GameObject> blockers = new();

        foreach (Transform child in transform)
        {
            if (child.name.ToLower().Contains("block"))
            {
                child.gameObject.SetActive(false);
                blockers.Add(child.gameObject);
            }
        }

        return blockers;
    }

    void DeactivateBlockers()
    {
        foreach (var blocker in _eventPlayerBlockers)
        {
            StartCoroutine(FadeBlockerEffect(blocker, 1f, 0.0f, 3f, false));
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
        if (!_enemyDict.TryGetValue(enemy.Id, out var spawnPoint))
        {
            return;
        }

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
        Debug.Log("Arena event completed");

        DeactivateBlockers();
    }

    public void ResetArenaEvent()
    {
        _arenaSpawns = _arenaSpawnsOriginal;

        foreach (var enemy in _currentEnemies)
        {
            // Destroy(enemy.gameObject);
        }

        _currentEnemies.Clear();

        StopAllCoroutines();
    }
}
