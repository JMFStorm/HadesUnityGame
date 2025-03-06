using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    public TunnelLevel[] TunnelLevels;
    public PlayerCharacter PlayerPrefab;
    public MainCamera MainCameraPrefab;
    public Light2D GlobalLight;

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
    }

    private void Start()
    {
        _player = Instantiate(PlayerPrefab, Vector3.zero, Quaternion.identity);

        _mainCamera = Instantiate(MainCameraPrefab, new(0, 0, -10f), Quaternion.identity);
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

        // _mainCamera.SetCameraBoundaries(bl, tr);

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
}
