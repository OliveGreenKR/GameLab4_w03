using Sirenix.OdinInspector;
using UnityEngine;

public class PlayerFire : MonoBehaviour
{
    
    [SerializeField][Required] NewPlayerController _player = null;
    [SerializeField][Required] ProjectileLauncher _playerLauncher = null;
    [SerializeField] ProjectileType _projectileType = ProjectileType.BasicProjectile;
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    #region Unity Lifecycle
    private void OnEnable()
    {
        if (_player == null)
        {
            _player = GetComponent<NewPlayerController>();
        }

        if (_player == null)
        {
            Debug.LogError("PlayerFire: PlayerEvents is null");
            return;
        }

        if(_playerLauncher == null)
        {
            Debug.LogError("PlayerFire: PlayerLauncher is null");
            return;
        }

        SubscribeFireEvents();
    }

    private void OnDisable()
    {
        UnSubscribeFireEvents();
    }  
    
    private void OnDestroy()
    {
        UnSubscribeFireEvents();
    }
    #endregion

    #region Public Methods
    public void SetProjectileType(ProjectileType type)
    {
        _projectileType = type;
    }
    #endregion

    #region Private Methods
    private void SubscribeFireEvents()
    {
        if (_player != null && _playerLauncher != null)
        {
            _player.OnFire -= FireProjectile;
            _player.OnFire += FireProjectile;
        }
    }

    private void UnSubscribeFireEvents()
    {
        if (_player != null && _playerLauncher != null)
        {
            _player.OnFire -= FireProjectile;
        }
    }

    private void FireProjectile()
    {
        _playerLauncher.Fire(_projectileType, _playerLauncher.transform.forward);
    }
    #endregion
}


