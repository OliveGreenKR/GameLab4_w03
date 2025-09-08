using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 싱글톤 게임 매니저 - 게임 시작/종료 및 생존 시간 관리
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager Instance { get; private set; }
    #endregion

    #region Serialized Fields
    [Header("Scene Names")]
    [SerializeField] private string _gamePlayingSceneName = "GameScene";
    [SerializeField] private string _gameStartSceneName = "GameScene";
    [SerializeField] private string _gameOverSceneName = "GameOverScene";
    #endregion

    #region Private Fields
    private bool _isGamePlaying = false;
    private float _survivalTimeSeconds = 0f;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (_isGamePlaying)
        {
            _survivalTimeSeconds += Time.deltaTime;
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 게임 시작 - 게임 씬으로 전환
    /// </summary>
    public void StartGame()
    {
        _isGamePlaying = false;
        _survivalTimeSeconds = 0f;
        Debug.Log("[GameManager] Starting Game - Loading Game Scene");
        SceneManager.LoadScene(_gameStartSceneName);
    }

    /// <summary>
    /// 게임 종료 - 게임 종료 씬으로 전환
    /// </summary>
    public void EndGame()
    {
        _isGamePlaying = false;
        string formattedTime = FormatSurvivalTime(_survivalTimeSeconds);
        Debug.Log($"[GameManager] Game Ended - Survival Time: {formattedTime}");
        SceneManager.LoadScene(_gameOverSceneName);
    }

    public void LoadPlayingScene()
    {
        _isGamePlaying = true;
        Debug.Log("[GameManager] Loading Playing Scene");
        SceneManager.LoadScene(_gamePlayingSceneName);
    }

    /// <summary>
    /// 현재 생존 시간 조회
    /// </summary>
    /// <returns>생존 시간 (초)</returns>
    public float GetSurvivalTime()
    {
        return _survivalTimeSeconds;
    }

    /// <summary>
    /// 포맷팅된 생존 시간 조회
    /// </summary>
    /// <returns>MM:SS 형태의 시간 문자열</returns>
    public string GetFormattedSurvivalTime()
    {
        return FormatSurvivalTime(_survivalTimeSeconds);
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// 생존 시간을 MM:SS 형태로 포맷팅
    /// </summary>
    /// <param name="seconds">총 초</param>
    /// <returns>포맷팅된 시간 문자열</returns>
    private string FormatSurvivalTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int remainingSeconds = Mathf.FloorToInt(seconds % 60f);
        return string.Format("{0:00}:{1:00}", minutes, remainingSeconds);
    }


    #endregion
}