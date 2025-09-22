using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HPBar : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private TMP_Text _healthText;
    [SerializeField] private PlayerBattleEntity _playerBattleEntity;
    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        if (_playerBattleEntity != null && _playerBattleEntity.BattleStat != null)
        {
            _playerBattleEntity.BattleStat.OnStatChanged -= OnHealthChanged;
            _playerBattleEntity.BattleStat.OnStatChanged += OnHealthChanged;
            UpdateHealthUI(
                _playerBattleEntity.BattleStat.CurrentHealth,
                _playerBattleEntity.BattleStat.MaxHealth
            );
        }
    }
    private void OnEnable()
    {
        if (_playerBattleEntity != null && _playerBattleEntity.BattleStat != null)
        {
            _playerBattleEntity.BattleStat.OnStatChanged -= OnHealthChanged;
            _playerBattleEntity.BattleStat.OnStatChanged += OnHealthChanged;
            UpdateHealthUI(
                _playerBattleEntity.BattleStat.CurrentHealth,
                _playerBattleEntity.BattleStat.MaxHealth
            );
        }
    }

    private void OnDisable()
    {
        if (_playerBattleEntity != null && _playerBattleEntity.BattleStat != null)
        {
            _playerBattleEntity.BattleStat.OnStatChanged -= OnHealthChanged;
        }
    }
    #endregion

    #region Private Methods - UI Update
    /// <summary>
    /// 배틀 스탯 변경 이벤트 처리 - 체력 변경시에만 UI 업데이트
    /// </summary>
    /// <param name="statType">변경된 스탯 타입</param>
    /// <param name="oldValue">이전 값</param>
    /// <param name="newValue">새로운 값</param>
    private void OnHealthChanged(BattleStatType statType, float oldValue, float newValue)
    {
        if (statType == BattleStatType.Health || statType == BattleStatType.MaxHealth)
        {
            UpdateHealthUI(
                (int)_playerBattleEntity.BattleStat.CurrentHealth,
                (int)_playerBattleEntity.BattleStat.MaxHealth
            );
        }
    }

    /// <summary>
    /// 체력바와 텍스트 UI 업데이트
    /// </summary>
    /// <param name="currentHealth">현재 체력</param>
    /// <param name="maxHealth">최대 체력</param>
    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (_healthSlider != null && maxHealth > 0)
        {
            _healthSlider.value = (float)currentHealth / maxHealth;
        }

        if (_healthText != null)
        {
            _healthText.text = $"{(int)currentHealth}/{(int)maxHealth}";
        }
    }
    #endregion
}