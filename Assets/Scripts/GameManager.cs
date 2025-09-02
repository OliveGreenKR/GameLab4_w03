using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private PlayerController _player = null;

    [SerializeField] private Vector3 _respawnPosition = new Vector3(0,0,10);


    private void Awake()
    {
        if(_player == null )
        {
            _player = FindObjectOfType<PlayerController>();
        }
    }

    private void Start()
    {
        GameStart();
    }
    public void GameStart()
    {
        Debug.Log("Game Start");
    }

    public void GameOver()
    {
        Debug.Log("Game Over");
    }
    
    public void RespawnPlayer()
    {

    }
}