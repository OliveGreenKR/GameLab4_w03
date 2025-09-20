using Sirenix.OdinInspector;
using UnityEngine;

public class PlayerFire : MonoBehaviour
{

    #region Serialized Fields
    [SerializeField][Required] NewPlayerController _player = null;
    [SerializeField][Required] PlayerWeaponController _playerWeaponController = null;
    #endregion

    #region Unity Lifecycle
    private void OnEnable()
    {
        if (_player == null)
        {
            _player = GetComponent<NewPlayerController>();
        }

        if (_player == null)
        {
            Debug.LogError("PlayerFire: _player is null");
            return;
        }

        if(_playerWeaponController == null)
        {
            Debug.LogError("PlayerFire: _playerWeaponController is null");
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

    #region Private Methods
    private void SubscribeFireEvents()
    {
        if (_player != null && _playerWeaponController != null)
        {
            _player.OnFire -= FireProjectile;
            _player.OnFire += FireProjectile;
        }
    }

    private void UnSubscribeFireEvents()
    {
        if (_player != null && _playerWeaponController != null)
        {
            _player.OnFire -= FireProjectile;
        }
    }

    private void FireProjectile()
    {
        _playerWeaponController.TryFire();
    }
    #endregion
}


