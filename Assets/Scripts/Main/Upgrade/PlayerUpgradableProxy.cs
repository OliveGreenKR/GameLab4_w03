using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 업그레이드 프록시 - IUpgradable 구현체
/// 업그레이드 명령을 적절한 컴포넌트로 라우팅하고 생명주기 관리
/// </summary>
public class PlayerUpgradableProxy : MonoBehaviour, IUpgradable
{
    #region Serialized Fields
    [TabGroup("References")]
    [Header("Core Components")]
    [Required]
    [SerializeField] private PlayerWeaponController _playerWeaponController;

    [TabGroup("References")]
    [Required]
    [SerializeField] private PlayerBattleEntity _playerBattleEntity;

    [TabGroup("References")]
    [Required]
    [SerializeField] private TemporaryUpgradeManager _temporaryUpgradeManager;

    [TabGroup("Settings")]
    [Header("Debug Settings")]
    [SerializeField] private bool _enableDebugLogs = true;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int PermanentUpgradeCount => _permanentUpgrades?.Count ?? 0;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int ActiveTemporaryUpgradeCount => _temporaryUpgradeManager?.ActiveUpgradeCount ?? 0;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsInitialized { get; private set; }7

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Dictionary<UpgradeType, float> PermanentUpgrades => _permanentUpgrades;
    #endregion

    #region Private Fields
    private Dictionary<UpgradeType, float> _permanentUpgrades = new Dictionary<UpgradeType, float>();
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        _permanentUpgrades = new Dictionary<UpgradeType, float>();
        IsInitialized = false;

        if (!ValidateReferences())
        {
            Debug.LogError("[PlayerUpgradableProxy] Reference validation failed", this);
            return;
        }

        if (_enableDebugLogs)
        {
            Debug.Log("[PlayerUpgradableProxy] Initialized", this);
        }
    }

    private void Start()
    {
        if (_temporaryUpgradeManager != null)
        {
            _temporaryUpgradeManager.OnUpgradeExpired -= OnTemporaryUpgradeExpired;
            _temporaryUpgradeManager.OnUpgradeExpired += OnTemporaryUpgradeExpired;
        }

        IsInitialized = true;

        if (_enableDebugLogs)
        {
            Debug.Log("[PlayerUpgradableProxy] Started and subscribed to temporary upgrade events", this);
        }
    }

    private void OnDestroy()
    {
        if (_temporaryUpgradeManager != null)
        {
            _temporaryUpgradeManager.OnUpgradeExpired -= OnTemporaryUpgradeExpired;
        }

        _permanentUpgrades?.Clear();
        IsInitialized = false;

        if (_enableDebugLogs)
        {
            Debug.Log("[PlayerUpgradableProxy] Destroyed and cleaned up", this);
        }
    }
    #endregion

    #region IUpgradable Implementation
    /// <summary>특정 타입의 업그레이드를 받을 수 있는지 확인</summary>
    /// <param name="upgradeType">업그레이드 타입</param>
    /// <returns>업그레이드 가능 여부</returns>
    public bool CanReceiveUpgrade(UpgradeType upgradeType)
    {
        if (!IsInitialized)
            return false;

        switch (upgradeType)
        {
            // 무기 스탯
            case UpgradeType.WeaponDamage:
            case UpgradeType.WeaponFireRate:
            case UpgradeType.WeaponAccuracy:
            case UpgradeType.WeaponRecoil:
            case UpgradeType.WeaponProjectileSpeed:
            case UpgradeType.WeaponProjectileLifetime:
                return _playerWeaponController != null;

            // 무기 특수 효과
            case UpgradeType.WeaponPiercing:
            case UpgradeType.WeaponSplit:
            case UpgradeType.WeaponConcentration:
            case UpgradeType.WeaponBurst:
                return _playerWeaponController != null;

            // 플레이어 스탯
            case UpgradeType.PlayerHealth:
            case UpgradeType.PlayerMaxHealth:
            case UpgradeType.PlayerMoveSpeed:
                return _playerBattleEntity != null;

            default:
                return false;
        }
    }

    /// <summary>업그레이드를 적용 (영구/임시 구분하여 처리)</summary>
    /// <param name="upgradeType">업그레이드 타입</param>
    /// <param name="value">업그레이드 수치</param>
    /// <param name="applicationType">적용 방식 (영구/임시)</param>
    /// <param name="durationSeconds">임시 적용 시 지속 시간 (초)</param>
    /// <returns>임시 적용 시 업그레이드 ID, 영구 적용 시 null</returns>
    public string ApplyUpgrade(UpgradeType upgradeType, float value, UpgradeApplicationType applicationType, float durationSeconds = 0f)
    {
        if (!CanReceiveUpgrade(upgradeType))
        {
            LogUpgradeAction($"Cannot Apply {applicationType}", upgradeType, value);
            return null;
        }

        if (applicationType == UpgradeApplicationType.Permanent)
        {
            bool success = ApplyPermanentUpgrade(upgradeType, value);
            LogUpgradeAction(success ? "Applied Permanent" : "Failed Permanent", upgradeType, value);
            return success ? null : null;
        }
        else
        {
            if (_temporaryUpgradeManager == null)
            {
                LogUpgradeAction("Failed Temporary (No Manager)", upgradeType, value);
                return null;
            }

            // 임시 업그레이드 적용
            if (RouteUpgradeToComponent(upgradeType, value, false))
            {
                string upgradeId = _temporaryUpgradeManager.AddTemporaryUpgrade(upgradeType, value, durationSeconds);
                LogUpgradeAction("Applied Temporary", upgradeType, value, upgradeId);
                return upgradeId;
            }
            else
            {
                LogUpgradeAction("Failed Temporary (Routing)", upgradeType, value);
                return null;
            }
        }
    }

    /// <summary>업그레이드를 제거</summary>
    /// <param name="upgradeType">업그레이드 타입</param>
    /// <param name="value">제거할 업그레이드 수치</param>
    /// <param name="upgradeId">임시 업그레이드 ID (임시 제거 시 필요)</param>
    /// <returns>제거 성공 여부</returns>
    public bool RemoveUpgrade(UpgradeType upgradeType, float value, string upgradeId = null)
    {
        if (!IsInitialized)
            return false;

        // 임시 업그레이드 제거 (ID 제공)
        if (!string.IsNullOrEmpty(upgradeId))
        {
            if (_temporaryUpgradeManager != null && _temporaryUpgradeManager.HasUpgrade(upgradeId))
            {
                bool success = _temporaryUpgradeManager.RemoveTemporaryUpgrade(upgradeId);
                if (success)
                {
                    RouteUpgradeToComponent(upgradeType, value, true);
                }
                LogUpgradeAction(success ? "Removed Temporary" : "Failed Remove Temporary", upgradeType, value, upgradeId);
                return success;
            }
            return false;
        }

        // 영구 업그레이드 제거
        bool removed = RemovePermanentUpgrade(upgradeType, value);
        LogUpgradeAction(removed ? "Removed Permanent" : "Failed Remove Permanent", upgradeType, value);
        return removed;
    }

    /// <summary>현재 적용된 영구 업그레이드 수치 조회</summary>
    /// <param name="upgradeType">업그레이드 타입</param>
    /// <returns>현재 적용된 수치</returns>
    public float GetCurrentUpgradeValue(UpgradeType upgradeType)
    {
        if (_permanentUpgrades != null && _permanentUpgrades.ContainsKey(upgradeType))
        {
            return _permanentUpgrades[upgradeType];
        }
        return 0f;
    }

    /// <summary>현재 활성화된 임시 업그레이드 개수 조회</summary>
    /// <param name="upgradeType">업그레이드 타입</param>
    /// <returns>활성 임시 업그레이드 개수</returns>
    public int GetActiveTemporaryUpgradeCount(UpgradeType upgradeType)
    {
        if (_temporaryUpgradeManager == null)
            return 0;

        return _temporaryUpgradeManager.GetActiveUpgradeCount(upgradeType);
    }
    #endregion

    #region Private Methods - Routing
    private bool ApplyPermanentUpgrade(UpgradeType upgradeType, float value)
    {
        if (!RouteUpgradeToComponent(upgradeType, value, false))
        {
            return false;
        }

        // 영구 업그레이드 누적 저장
        if (_permanentUpgrades.ContainsKey(upgradeType))
        {
            _permanentUpgrades[upgradeType] += value;
        }
        else
        {
            _permanentUpgrades[upgradeType] = value;
        }

        return true;
    }

    private bool RemovePermanentUpgrade(UpgradeType upgradeType, float value)
    {
        if (!_permanentUpgrades.ContainsKey(upgradeType))
        {
            return false;
        }

        float currentValue = _permanentUpgrades[upgradeType];
        float newValue = currentValue - value;

        // 음수 방지
        if (newValue < 0f)
        {
            value = currentValue; // 실제 제거할 수 있는 값으로 조정
            newValue = 0f;
        }

        if (!RouteUpgradeToComponent(upgradeType, value, true))
        {
            return false;
        }

        // 값 업데이트 또는 제거
        if (newValue <= 0f)
        {
            _permanentUpgrades.Remove(upgradeType);
        }
        else
        {
            _permanentUpgrades[upgradeType] = newValue;
        }

        return true;
    }

    private bool RouteUpgradeToComponent(UpgradeType upgradeType, float value, bool isRemoving = false)
    {
        switch (upgradeType)
        {
            // 무기 스탯 및 무기 특수 효과
            case UpgradeType.WeaponDamage:
            case UpgradeType.WeaponFireRate:
            case UpgradeType.WeaponAccuracy:
            case UpgradeType.WeaponRecoil:
            case UpgradeType.WeaponProjectileSpeed:
            case UpgradeType.WeaponProjectileLifetime:
            case UpgradeType.WeaponPiercing:
            case UpgradeType.WeaponSplit:
            case UpgradeType.WeaponConcentration:
            case UpgradeType.WeaponBurst:
                return ApplyWeaponUpgrade(upgradeType, value, isRemoving);

            // 플레이어 스탯
            case UpgradeType.PlayerHealth:
            case UpgradeType.PlayerMaxHealth:
            case UpgradeType.PlayerMoveSpeed:
                return ApplyPlayerUpgrade(upgradeType, value, isRemoving);

            default:
                Debug.LogWarning($"[PlayerUpgradableProxy] Unknown upgrade type: {upgradeType}", this);
                return false;
        }
    }
    #endregion

    #region Private Methods - Component Routing
    private bool ApplyWeaponUpgrade(UpgradeType upgradeType, float value, bool isRemoving = false)
    {
        if (_playerWeaponController == null)
            return false;

        float finalValue = isRemoving ? -value : value;

        switch (upgradeType)
        {
            case UpgradeType.WeaponDamage:
                float currentDamage = _playerWeaponController.ProjectileLauncher.GetProjectileDamage();
                float newDamage = Mathf.Max(0f, currentDamage + finalValue);
                _playerWeaponController.ProjectileLauncher.SetProjectileDamage(newDamage);
                return true;

            case UpgradeType.WeaponFireRate:
                float currentFireRate = _playerWeaponController.ProjectileLauncher.GetFireRate();
                float newFireRate = Mathf.Max(0.1f, currentFireRate + finalValue);
                _playerWeaponController.ProjectileLauncher.SetFireRate(newFireRate);
                return true;

            case UpgradeType.WeaponProjectileSpeed:
                float currentSpeed = _playerWeaponController.ProjectileLauncher.GetProjectileSpeed();
                float newSpeed = Mathf.Max(1f, currentSpeed + finalValue);
                _playerWeaponController.ProjectileLauncher.SetProjectileSpeed(newSpeed);
                return true;

            case UpgradeType.WeaponProjectileLifetime:
                float currentLifetime = _playerWeaponController.ProjectileLauncher.GetProjectileLifetime();
                float newLifetime = Mathf.Max(0.1f, currentLifetime + finalValue);
                _playerWeaponController.ProjectileLauncher.SetProjectileLifetime(newLifetime);
                return true;

            case UpgradeType.WeaponAccuracy:
            case UpgradeType.WeaponRecoil:
                // 정확도와 반동은 WeaponEffectSO를 통해 처리해야 함
                Debug.LogWarning($"[PlayerUpgradableProxy] {upgradeType} requires WeaponEffectSO implementation", this);
                return false;

            case UpgradeType.WeaponPiercing:
            case UpgradeType.WeaponSplit:
            case UpgradeType.WeaponConcentration:
            case UpgradeType.WeaponBurst:
                // 특수 효과는 WeaponEffectSO나 ProjectileEffectSO를 통해 처리해야 함
                Debug.LogWarning($"[PlayerUpgradableProxy] {upgradeType} requires Effect SO implementation", this);
                return false;

            default:
                return false;
        }
    }

    private bool ApplyPlayerUpgrade(UpgradeType upgradeType, float value, bool isRemoving = false)
    {
        if (_playerBattleEntity == null)
            return false;

        float finalValue = isRemoving ? -value : value;

        switch (upgradeType)
        {
            case UpgradeType.PlayerHealth:
                if (isRemoving)
                {
                    // 체력 감소는 데미지로 처리
                    _playerBattleEntity.TakeDamage(null, Mathf.Abs(finalValue));
                }
                else
                {
                    // 체력 증가는 힐로 처리
                    _playerBattleEntity.HealPlayer(value);
                }
                return true;

            case UpgradeType.PlayerMaxHealth:
                float currentMaxHealth = _playerBattleEntity.GetCurrentStat(BattleStatType.MaxHealth);
                float newMaxHealth = Mathf.Max(1f, currentMaxHealth + finalValue);
                _playerBattleEntity.SetMaxHealth(newMaxHealth);
                return true;

            case UpgradeType.PlayerMoveSpeed:
                var controller = _playerBattleEntity.GetComponent<NewPlayerController>();
                if(controller != null)
                {
                    float currentSpeed = controller.MoveSpeed;
                    float newSpeed = Mathf.Max(0f, currentSpeed + finalValue);
                    controller.SetMoveSpeed(newSpeed);
                    return true;
                }
                Debug.LogWarning($"[PlayerUpgradableProxy] {upgradeType} requires NewPlayerController integration", this);
                return false;

            default:
                return false;
        }
    }
    #endregion

    #region Private Methods - Temporary Upgrade Callbacks
    private void OnTemporaryUpgradeExpired(string upgradeId, UpgradeType upgradeType, float value)
    {
        // 임시 업그레이드 만료시 해당 효과를 컴포넌트에서 제거
        bool success = RouteUpgradeToComponent(upgradeType, value, true);

        LogUpgradeAction(success ? "Expired and Removed" : "Expired but Failed to Remove",
                        upgradeType, value, upgradeId);
    }
    #endregion

    #region Private Methods - Validation
    private bool ValidateReferences()
    {
        bool isValid = true;

        if (_playerWeaponController == null)
        {
            Debug.LogError("[PlayerUpgradableProxy] PlayerWeaponController reference required!", this);
            isValid = false;
        }

        if (_playerBattleEntity == null)
        {
            Debug.LogError("[PlayerUpgradableProxy] PlayerBattleEntity reference required!", this);
            isValid = false;
        }

        if (_temporaryUpgradeManager == null)
        {
            Debug.LogError("[PlayerUpgradableProxy] TemporaryUpgradeManager reference required!", this);
            isValid = false;
        }

        return isValid;
    }

    private void LogUpgradeAction(string action, UpgradeType upgradeType, float value, string upgradeId = null)
    {
        if (!_enableDebugLogs)
            return;

        string message = $"[PlayerUpgradableProxy] {action} - Type:{upgradeType} Value:{value:F2}";

        if (!string.IsNullOrEmpty(upgradeId))
            message += $" ID:{upgradeId}";

        Debug.Log(message, this);
    }
    #endregion
}