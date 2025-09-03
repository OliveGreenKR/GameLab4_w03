using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private const string _redObjectTag = "RedObject";
    [SerializeField] private const string _blueObjectTag = "BlueObject";
    [SerializeField] private const string _grayObjectTag = "GrayObject";

    public string RedObjectTag => _redObjectTag;
    public string BlueObjectTag => _blueObjectTag;
    public string GrayObjectTag => _grayObjectTag;


    #region Singleton
    public static GameManager Instance { get; private set; }
    #endregion

    #region Serialized Fields
    [SerializeField] private PlayerController _player = null;
    [SerializeField] private PlayerColorController _playerColorController = null;


    [SerializeField] private Vector3 _respawnPosition = new Vector3(0, 0, 10);
    [SerializeField] private ObjectColorChangeManager _objectColorChangeManager = null;
    [SerializeField] private ObjectColor _playerColor = ObjectColor.Red;
    #endregion

    #region Properties
    public ObjectColor PlayerColor => _playerColor; 
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
        CollectAllColoredObjects();
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
        int nextValue = ((int)_playerColor + 1) % 2; //red,blue
        _playerColor = (ObjectColor)nextValue;
        //_playerColorController?.ChangeColor(_playerColor);
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
            _playerColorController = _player?.GetComponent<PlayerColorController>();
        }

        if (_objectColorChangeManager == null)
        {
            _objectColorChangeManager = FindFirstObjectByType<ObjectColorChangeManager>();
        }
    }

    private void CollectAllColoredObjects()
    {
        //red
        CollectColoredObjects(_redObjectTag, _objectColorChangeManager.ColorObjectsListRed);
        //blue
        CollectColoredObjects(_blueObjectTag, _objectColorChangeManager.ColorObjectsListBlue);

    }

    private void CollectColoredObjects(string tagName, List<GameObject> List)
    {
        if(List == null)
        {
            return;
        }

        List.Clear();
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tagName);
        List.AddRange(objects);
        Debug.Log($"Collected {List.Count} objects with tag {tagName}");
    }

  
    #endregion
}