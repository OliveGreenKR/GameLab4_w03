using UnityEngine;
using TMPro;

/// <summary>플레이어 무기 스탯을 UI에 표시하는 컴포넌트</summary>
public class WeaponStatsUI : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] private TextMeshProUGUI _statsText;
    [SerializeField] private TextMeshProUGUI _projStatsTxt;
    [SerializeField] private PlayerWeaponController _playerWeaponController;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        if (_playerWeaponController != null)
        {
            _playerWeaponController.OnWeaponStatChanged -= UpdateStatsDisplay;
            _playerWeaponController.OnWeaponStatChanged += UpdateStatsDisplay;
        }
    }

    private void OnDestroy()
    {
        if (_playerWeaponController != null)
        {
            _playerWeaponController.OnWeaponStatChanged -= UpdateStatsDisplay;
        }
    }
    #endregion

    #region Private Methods - UI Updates
    /// <summary>무기 스탯 변경 시 UI 텍스트 업데이트</summary>
    /// <param name="weaponStatData">변경된 무기 스탯 데이터</param>
    private void UpdateStatsDisplay(WeaponStatData weaponStatData)
    {
        if (_statsText != null && _playerWeaponController != null)
        {
            _statsText.text = _playerWeaponController.GetWeaponStatsString();
        }
        if(_projStatsTxt != null && _playerWeaponController != null)
        {
            _projStatsTxt.text = _playerWeaponController.GetProjectileStatsString();
        }
    }
    #endregion
}