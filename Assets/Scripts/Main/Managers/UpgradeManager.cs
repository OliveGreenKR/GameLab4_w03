using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct TemporaryBuff
{
    public string buffId;
    public float deltaValue;  // 변화량 저장
    public System.Action<float> revertAction;  // 변화량을 받는 액션
    public float endTime;
}

/// <summary>
/// 업그레이드 시스템 중앙 관리자
/// 기존 컴포넌트들을 직접 호출하여 업그레이드 처리
/// </summary>
public class UpgradeManager : MonoBehaviour
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
    [SerializeField] private NewPlayerController _playerController;

    [TabGroup("References")]
    [Required]
    [SerializeField] private ProjectileLauncher _projectileLauncher;

    [TabGroup("Temporary Buffs")]
    [Header("Active Temporary Buffs")]
    [SerializeField][ReadOnly] private List<TemporaryBuff> _activeTemporaryBuffs = new List<TemporaryBuff>();

    [TabGroup("Temporary Buffs")]
    [SerializeField][ReadOnly] private int _nextBuffId = 1;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int ActiveBuffCount => _activeTemporaryBuffs?.Count ?? 0;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsInitialized { get; private set; }
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        if (!ValidateReferences())
        {
            Debug.LogError("[UpgradeManager] Required references not assigned!", this);
            enabled = false;
            return;
        }

        InitializeSystem();
        Debug.Log("[UpgradeManager] Initialized successfully", this);
    }

    private void Update()
    {
        if (!IsInitialized)
            return;

        ProcessTemporaryBuffs();
    }
    #endregion

    #region Public Methods - Weapon Upgrades
    /// <summary>무기 데미지 영구 증가</summary>
    /// <param name="value">증가할 데미지</param>
    public void UpgradeWeaponDamage(float value)
    {
        if (!IsInitialized || value <= 0f) return;

        float currentDamage = _projectileLauncher.GetProjectileDamage();
        float newDamage = currentDamage + value;
        _projectileLauncher.SetProjectileDamage(newDamage);

        Debug.Log($"[UpgradeManager] Weapon damage upgraded: {currentDamage:F1} → {newDamage:F1} (+{value:F1})", this);
    }

    /// <summary>무기 발사속도 영구 증가</summary>
    /// <param name="value">증가할 발사속도</param>
    public void UpgradeWeaponFireRate(float value)
    {
        if (!IsInitialized || value <= 0f) return;

        float currentFireRate = _projectileLauncher.GetFireRate();
        float newFireRate = currentFireRate + value;
        _projectileLauncher.SetFireRate(newFireRate);

        Debug.Log($"[UpgradeManager] Weapon fire rate upgraded: {currentFireRate:F1} → {newFireRate:F1} (+{value:F1})", this);
    }

    /// <summary>투사체 속도 영구 증가</summary>
    /// <param name="value">증가할 속도</param>
    public void UpgradeProjectileSpeed(float value)
    {
        if (!IsInitialized || value <= 0f) return;

        float currentSpeed = _projectileLauncher.GetProjectileSpeed();
        float newSpeed = currentSpeed + value;
        _projectileLauncher.SetProjectileSpeed(newSpeed);

        Debug.Log($"[UpgradeManager] Projectile speed upgraded: {currentSpeed:F1} → {newSpeed:F1} (+{value:F1})", this);
    }

    /// <summary>투사체 생존시간 영구 증가</summary>
    /// <param name="value">증가할 생존시간 (초)</param>
    public void UpgradeProjectileLifetime(float value)
    {
        if (!IsInitialized || value <= 0f) return;

        float currentLifetime = _projectileLauncher.GetProjectileLifetime();
        float newLifetime = currentLifetime + value;
        _projectileLauncher.SetProjectileLifetime(newLifetime);

        Debug.Log($"[UpgradeManager] Projectile lifetime upgraded: {currentLifetime:F1}s → {newLifetime:F1}s (+{value:F1}s)", this);
    }
    #endregion

    #region Public Methods - Player Upgrades
    /// <summary>플레이어 체력 즉시 회복</summary>
    /// <param name="value">회복할 체력</param>
    public void HealPlayer(float value)
    {
        if (!IsInitialized || value <= 0f) return;

        float actualHealed = _playerBattleEntity.HealPlayer(value);

        Debug.Log($"[UpgradeManager] Player healed: {actualHealed:F1} HP restored", this);
    }

    /// <summary>플레이어 최대 체력 영구 증가</summary>
    /// <param name="value">증가할 최대 체력</param>
    public void UpgradePlayerMaxHealth(float value)
    {
        if (!IsInitialized || value <= 0f) return;

        float currentMaxHealth = _playerBattleEntity.GetCurrentStat(BattleStatType.MaxHealth);
        float newMaxHealth = currentMaxHealth + value;
        _playerBattleEntity.SetMaxHealth(newMaxHealth);

        Debug.Log($"[UpgradeManager] Player max health upgraded: {currentMaxHealth:F1} → {newMaxHealth:F1} (+{value:F1})", this);
    }

    /// <summary>플레이어 이동속도 영구 증가</summary>
    /// <param name="value">증가할 이동속도</param>
    public void UpgradePlayerMoveSpeed(float value)
    {
        if (!IsInitialized || value <= 0f) return;

        // NewPlayerController에서 현재 이동속도를 가져올 수 있는 public 메서드가 없으므로
        float currentMoveSpeed = _playerController.MoveSpeed;
        float newMoveSpeed = currentMoveSpeed + value;
        _playerController.SetMoveSpeed(newMoveSpeed);
        Debug.Log($"[UpgradeManager] Player move speed upgraded: +{value:F1} units/sec", this);
    }
    #endregion

    #region Public Methods - Effect Upgrades
    /// <summary>무기 효과 추가</summary>
    /// <param name="effectAsset">추가할 무기 효과 SO</param>
    public void AddWeaponEffect(WeaponEffectSO effectAsset)
    {
        if (!IsInitialized || effectAsset == null) return;

        _playerWeaponController.ApplyWeaponEffect(effectAsset);

        Debug.Log($"[UpgradeManager] Weapon effect added: {effectAsset.EffectName}", this);
    }

    /// <summary>투사체 효과 추가</summary>
    /// <param name="effectAsset">추가할 투사체 효과 SO</param>
    public void AddProjectileEffect(ProjectileEffectSO effectAsset)
    {
        if (!IsInitialized || effectAsset == null) return;

        _projectileLauncher.AddEffect(effectAsset);

        Debug.Log($"[UpgradeManager] Projectile effect added: {effectAsset.name}", this);
    }
    #endregion

    /// <summary>임시 버프 데이터</summary>
    [System.Serializable]
    public struct TemporaryBuff
    {
        public string buffId;
        public float deltaValue;  // 변화량
        public System.Action<float> revertAction;  // 변화량을 받는 해제 액션
        public float endTime;
    }

    #region Public Methods - Temporary Buffs
    /// <summary>임시 무기 데미지 버프</summary>
    /// <param name="value">증가할 데미지</param>
    /// <param name="durationSeconds">지속 시간 (초)</param>
    /// <returns>버프 ID</returns>
    public string ApplyTemporaryWeaponDamageBuff(float value, float durationSeconds)
    {
        if (!IsInitialized || value <= 0f || durationSeconds <= 0f) return null;

        string buffId = GenerateBuffId();
        float currentDamage = _projectileLauncher.GetProjectileDamage();
        float newDamage = currentDamage + value;

        _projectileLauncher.SetProjectileDamage(newDamage);

        System.Action<float> revertAction = (delta) =>
        {
            float currentValue = _projectileLauncher.GetProjectileDamage();
            _projectileLauncher.SetProjectileDamage(Mathf.Max(0f, currentValue - delta));
        };

        TemporaryBuff buff = CreateTemporaryBuff(buffId, value, revertAction, durationSeconds);
        _activeTemporaryBuffs.Add(buff);

        Debug.Log($"[UpgradeManager] Temporary weapon damage buff applied: +{value:F1} for {durationSeconds:F1}s (ID: {buffId})", this);
        return buffId;
    }

    /// <summary>임시 이동속도 버프</summary>
    /// <param name="value">증가할 이동속도</param>
    /// <param name="durationSeconds">지속 시간 (초)</param>
    /// <returns>버프 ID</returns>
    public string ApplyTemporaryMoveSpeedBuff(float value, float durationSeconds)
    {
        if (!IsInitialized || value <= 0f || durationSeconds <= 0f) return null;

        string buffId = GenerateBuffId();
        float currentSpeed = _playerController.MoveSpeed;
        float newSpeed = currentSpeed + value;

        _playerController.SetMoveSpeed(newSpeed);

        System.Action<float> revertAction = (delta) =>
        {
            float currentValue = _playerController.MoveSpeed;
            _playerController.SetMoveSpeed(Mathf.Max(0.1f, currentValue - delta));
        };

        TemporaryBuff buff = CreateTemporaryBuff(buffId, value, revertAction, durationSeconds);
        _activeTemporaryBuffs.Add(buff);

        Debug.Log($"[UpgradeManager] Temporary move speed buff applied: +{value:F1} for {durationSeconds:F1}s (ID: {buffId})", this);
        return buffId;
    }

    /// <summary>임시 버프 제거</summary>
    /// <param name="buffId">제거할 버프 ID</param>
    /// <returns>제거 성공 여부</returns>
    public bool RemoveTemporaryBuff(string buffId)
    {
        if (!IsInitialized || string.IsNullOrEmpty(buffId)) return false;

        for (int i = 0; i < _activeTemporaryBuffs.Count; i++)
        {
            if (_activeTemporaryBuffs[i].buffId == buffId)
            {
                TemporaryBuff buff = _activeTemporaryBuffs[i];
                buff.revertAction?.Invoke(buff.deltaValue);
                _activeTemporaryBuffs.RemoveAt(i);

                Debug.Log($"[UpgradeManager] Temporary buff manually removed: {buffId}", this);
                return true;
            }
        }

        Debug.LogWarning($"[UpgradeManager] Temporary buff not found: {buffId}", this);
        return false;
    }
    #endregion

    #region Private Methods - Initialization
    private bool ValidateReferences()
    {
        if (_playerWeaponController == null)
        {
            Debug.LogError("[UpgradeManager] PlayerWeaponController not assigned!", this);
            return false;
        }

        if (_playerBattleEntity == null)
        {
            Debug.LogError("[UpgradeManager] PlayerBattleEntity not assigned!", this);
            return false;
        }

        if (_playerController == null)
        {
            Debug.LogError("[UpgradeManager] NewPlayerController not assigned!", this);
            return false;
        }

        if (_projectileLauncher == null)
        {
            Debug.LogError("[UpgradeManager] ProjectileLauncher not assigned!", this);
            return false;
        }

        return true;
    }

    private void InitializeSystem()
    {
        _activeTemporaryBuffs.Clear();
        _nextBuffId = 1;
        IsInitialized = true;
    }
    #endregion

    #region Private Methods - Temporary Buff Management
    private void ProcessTemporaryBuffs()
    {
        float currentTime = Time.time;

        for (int i = _activeTemporaryBuffs.Count - 1; i >= 0; i--)
        {
            if (currentTime >= _activeTemporaryBuffs[i].endTime)
            {
                TemporaryBuff expiredBuff = _activeTemporaryBuffs[i];
                expiredBuff.revertAction?.Invoke(expiredBuff.deltaValue);
                _activeTemporaryBuffs.RemoveAt(i);

                Debug.Log($"[UpgradeManager] Temporary buff {expiredBuff.buffId} expired and removed", this);
            }
        }
    }

    private string GenerateBuffId()
    {
        return $"BUFF_{_nextBuffId++}";
    }

    private TemporaryBuff CreateTemporaryBuff(string buffId, float deltaValue, System.Action<float> revertAction, float durationSeconds)
    {
        return new TemporaryBuff
        {
            buffId = buffId,
            deltaValue = deltaValue,
            revertAction = revertAction,
            endTime = Time.time + durationSeconds
        };
    }
    #endregion
}