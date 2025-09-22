using Sirenix.OdinInspector;
using UnityEngine;

public class RecoilConverterForCamera : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("References")]
    [Header("Target Controller")]
    [Required]
    [SerializeField] private GameObject _angleControllerGameObject;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool HasValidController => _angleController != null;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    private Vector2 _lastRecoil;
    #endregion

    #region Private Fields
    private IAngleController _angleController;
    
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeAngleController();
    }
    #endregion

    #region Public Methods - Recoil Application
    /// <summary>
    /// 리코일 벡터를 각도로 변환하여 적용
    /// </summary>
    /// <param name="recoilVector">x: Yaw 리코일, y: Pitch 리코일</param>
    public void ApplyRecoil(Vector2 recoilVector)
    {
        if (!HasValidController)
            return;

        ConvertAndApplyRecoil(recoilVector);
    }
    #endregion

    #region Private Methods - Initialization
    private void InitializeAngleController()
    {
        if (_angleControllerGameObject != null)
        {
            _angleController = _angleControllerGameObject.GetComponent<IAngleController>();

            if (_angleController == null)
            {
                Debug.LogError("[RecoilController] IAngleController not found on assigned GameObject!", this);
            }
        }
        else
        {
            Debug.LogError("[RecoilController] Angle Controller GameObject not assigned!", this);
        }
    }
    #endregion

    #region Private Methods - Conversion
    private void ConvertAndApplyRecoil(Vector2 recoilVector)
    {
        if (_angleController == null)
            return;

        _lastRecoil = recoilVector;
        // 리코일 벡터를 각도 델타로 직접 적용
        // y축은 Pitch로 반전
        //Debug.Log($"Applying Recoil - Yaw: {recoilVector.x}, Pitch: {-recoilVector.y}", this);
        _angleController.AdjustAngles(recoilVector.x, -recoilVector.y);
    }
    #endregion
}
