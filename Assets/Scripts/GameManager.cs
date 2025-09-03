using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager Instance { get; private set; }
    #endregion

    #region Serialized Fields
    [SerializeField] private PlayerController _player = null;
    [SerializeField] private Vector3 _respawnPosition = new Vector3(0, 0, 10);
    [SerializeField] private ObjectColorChangeManager _objectColorChangeManager = null;
    [SerializeField] private ObjectColor _playerColor = ObjectColor.Red;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // ΩÃ±€≈Ê ¿ŒΩ∫≈œΩ∫ ∞¸∏Æ
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeReferences();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        GameStart();
    }
    #endregion

    #region Public Methods
    public void GameStart()
    {
        Debug.Log("Game Start");
    }

    public void GameOver()
    {
        Debug.Log("Game Over");
    }

    public void OnPlayerClicked()
    {
        int nextValue = ((int)_playerColor + 1) % Enum.GetValues(typeof(ObjectColor)).Length;
        _playerColor = (ObjectColor)nextValue;
        _objectColorChangeManager?.ObjectColorChange(_playerColor);
    }

    public void RespawnPlayer()
    {
        (_player as IReSpawnable)?.ReSpawn(_respawnPosition, Quaternion.identity);  
    }
    #endregion

    #region Private Methods
    private void InitializeReferences()
    {
        if (_player == null)
        {
            _player = FindFirstObjectByType<PlayerController>();
        }

        if (_objectColorChangeManager == null)
        {
            _objectColorChangeManager = FindFirstObjectByType<ObjectColorChangeManager>();
        }
    }
    #endregion
}