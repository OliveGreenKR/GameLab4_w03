using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private InputSystem_Actions _inputActions;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // VSync 끄기
        QualitySettings.vSyncCount = 0;
        // 프레임 제한
        Application.targetFrameRate = 120;
    }

    private void OnEnable()
    {
        InitializeInput();
        EnableInput();
    }
    private void OnDisable()
    {
        DisableInput();
    }



    #region Input System Initialization
    private void InitializeInput()
    {
        _inputActions = new InputSystem_Actions();
        if(_inputActions == null)
        {
            Debug.LogWarning("InputActions for Debug in GM is null");
            return;
        }
    }

    private void EnableInput()
    {
        _inputActions.Debug.Enable();
    }
    private void DisableInput()
    {
        _inputActions.Debug.Disable();
    }
    #endregion
    #region Input Event CallBacks

    private void OnDebug01Pressed()
    {

    }
    #endregion

}
