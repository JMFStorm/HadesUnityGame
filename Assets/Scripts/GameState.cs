using System.Drawing;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    public TunnelLevel[] TunnelLevels;
    public Sprite[] Backgrounds;
    public PlayerCharacter PlayerPrefab;
    public MainCamera MainCameraPrefab;
    public Light2D GlobalLight;

    private int _currentBackgroundIndex = 0;

    private SpriteRenderer _backgroundRenderer;
    private Level _currentLevel;
    private PlayerCharacter _player;
    private MainCamera _mainCamera;

    private int _currentLevelIndex = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (_backgroundRenderer == null)
        {
            _backgroundRenderer = GetComponent<SpriteRenderer>();
        }

        _player = Instantiate(PlayerPrefab, Vector3.zero, Quaternion.identity);
        _mainCamera = Instantiate(MainCameraPrefab, new(0, 0, -10f), Quaternion.identity);
    }

    private void Start()
    {
        _mainCamera.SetFollowTarget(_player.transform);

        LoadLevel(_currentLevelIndex);

        GlobalLight.intensity = 0.4f;
    }

    private void Update()
    {
    }

    public void LoadLevel(int index)
    {
        if (index < 0 || TunnelLevels.Length <= index)
        {
            Debug.LogError("Invalid level index!");
            return;
        }

        if (_currentLevel != null)
        {
            Destroy(_currentLevel.gameObject);
            _currentLevel = null;
        }

        _currentLevel = Instantiate(TunnelLevels[index], Vector3.zero, Quaternion.identity);
        _currentLevelIndex = index;

        var (bl, tr) = _currentLevel.GetLevelBoundaries();

        _currentBackgroundIndex = Random.Range(0, 101) % TunnelLevels.Length; 

        ApplyBackground(bl, tr, _currentBackgroundIndex);

        _mainCamera.SetCameraBoundaries(bl, tr);

        var levelEnter = _currentLevel.GetLevelEntrance();

        if (levelEnter != null)
        {
            _player.transform.position = levelEnter - new Vector3(0, 0.5f, 0);
        }
        else
        {
            Debug.LogError("levelEnter not found!");
        }
    }

    public void LoadNextLevel()
    {
        int nextIndex = (_currentLevelIndex + 1) % TunnelLevels.Length;
        
        LoadLevel(nextIndex);
    }

    void ApplyBackground(Vector2 bottomLeft, Vector2 topRight, int index)
    {
        if (Backgrounds.Length == 0 || _backgroundRenderer == null)
        {
            Debug.LogError("No backgrounds assigned or missing SpriteRenderer!");
            return;
        }

        _backgroundRenderer.sprite = Backgrounds[index];

        float width = topRight.x - bottomLeft.x;
        float height = topRight.y - bottomLeft.y;

        if (_backgroundRenderer.sprite != null)
        {
            Vector2 spriteSize = _backgroundRenderer.sprite.bounds.size;

            // Use the larger scale factor to ensure full coverage
            float scaleFactor = Mathf.Max(width / spriteSize.x, height / spriteSize.y);

            _backgroundRenderer.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1);
        }

        // Position the background at the center of the boundary
        Vector2 center = (bottomLeft + topRight) / 2f;
        _backgroundRenderer.transform.position = center;
    }
}
