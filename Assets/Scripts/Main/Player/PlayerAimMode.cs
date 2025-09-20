using Sirenix.OdinInspector;
using UnityEngine;

public class PlayerAimMode : MonoBehaviour
{
    
    [SerializeField][Required] ThirdPersonCameraController _camera = null;
    [SerializeField][Required] NewPlayerController _player = null;

    #region Unity Lifecycle
    private void OnEnable()
    {
        if (_camera == null)
        {
            _camera = Camera.main.GetComponent<ThirdPersonCameraController>();
        }

        if (_camera == null)
        {
            Debug.LogError("PlayerAimMode: _camera is null");
            return;
        }

        SubscribeAimEvents();
    }

    private void OnDisable()
    {
        UnSubscribeAimEvents();
    }  
    
    private void OnDestroy()
    {
        UnSubscribeAimEvents();
    }
    #endregion

    #region Private Methods
    private void SubscribeAimEvents()
    {
       if(_player && _camera)
        {
            _player.OnAimModeStarted -= _camera.AimModeStart;
            _player.OnAimModeStarted += _camera.AimModeStart;
            _player.OnAimModeEnded -= _camera.AImModeEnd;
            _player.OnAimModeEnded += _camera.AImModeEnd;
        }
    }

    private void UnSubscribeAimEvents()
    {
        if (_player && _camera)
        {
            _player.OnAimModeStarted -= _camera.AimModeStart;
            _player.OnAimModeEnded -= _camera.AImModeEnd;
        }
    }
    #endregion
}


