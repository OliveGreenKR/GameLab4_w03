using UnityEngine;

public class ClearPoint : MonoBehaviour
{
    [SerializeField] private string _playerTag = "Player";  
    [SerializeField] private Collider _trigger = null;

    private void Start()
    {
        if(_trigger == null)
        {
            _trigger = GetComponent<Collider>();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_playerTag))
        {
            GameManager.Instance.GameClear();
        }
    }
}
