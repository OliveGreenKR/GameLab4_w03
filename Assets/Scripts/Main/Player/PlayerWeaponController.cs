using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 무기 시스템 통합 관리자
/// 무기 스탯, 효과, 정확도, 발사를 총괄 제어
/// </summary>
public class PlayerWeaponController : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("References")]
    [Header("Core References")]
    [Required]
    [SerializeField] private WeaponStatSO _baseWeaponStats;

    [TabGroup("References")]
    [Required]
    [SerializeField] private AccuracySystem _accuracySystem;

    [TabGroup("References")]
    [Required]
    [SerializeField] private RecoilSystem _recoilSystem;

    [TabGroup("References")]
    [SerializeField] private RecoilConverterForCamera _recoilConverter;

    [TabGroup("References")]
    [Required]
    [SerializeField] private ProjectileLauncher _projectileLauncher;

    [TabGroup("References")]
    [Required]
    [SerializeField] private AimPointManager _aimPointManager;

    [TabGroup("Effects",TextColor ="cyan")]
    [Header("Active Weapon Effects")]
    [SerializeField] private List<WeaponEffectSO> _activeEffects = new List<WeaponEffectSO>();
    #endregion

    #region Properties

    [TabGroup("Debug", "Default")]
    [ShowInInspector, ReadOnly]
    public bool IsInitialized => _isInitialized;

    [TabGroup("Debug", "Default")]
    [ShowInInspector, ReadOnly]
    public Vector3 CurrentAimPoint => _aimPointManager?.AimPoint ?? Vector3.zero;

    [TabGroup("Debug","Weapon System")]
    [ShowInInspector, ReadOnly]
    public bool CanFire => CanFireInternal();

    [TabGroup("Debug","Weapon System")]
    [ShowInInspector, ReadOnly]
    public int ActiveEffectCount => _activeEffects?.Count ?? 0;

    [TabGroup("Debug","Weapon System")]
    [ShowInInspector, ReadOnly]
    public float CurrentAccuracy => _accuracySystem?.CurrentAccuracy ?? 0f;

    [TabGroup("Debug","Weapon System")]
    [ShowInInspector, ReadOnly]
    public float CurrentRecoilIntensity => _recoilSystem?.GetRecoilIntensityRatio() ?? 0f;

    [TabGroup("Debug","Weapon System")]
    [ShowInInspector, ReadOnly]
    public Vector3 CurrentRecoilVector => _recoilSystem?.GetCurrentRecoilVector() ?? Vector3.zero;

    public WeaponStatData FinalStats { get; private set; }

    [TabGroup("Debug","Weapon Stat")]
    [ShowInInspector, ReadOnly]
    [InfoBox("최종 적용된 무기 스탯")]
    public float FinalFireRate => FinalStats.CurrentFireRate;

    [TabGroup("Debug","Weapon Stat")]
    [ShowInInspector, ReadOnly]
    public float FinalDamage => FinalStats.CurrentDamage;

    [TabGroup("Debug","Weapon Stat")]
    [ShowInInspector, ReadOnly]
    public float FinalProjectileSpeed => FinalStats.CurrentProjectileSpeed;

    [TabGroup("Debug","Weapon Stat")]
    [ShowInInspector, ReadOnly]
    public float FinalProjectileLifetime => FinalStats.CurrentProjectileLifetime;

    [TabGroup("Debug","Weapon Stat")]
    [ShowInInspector, ReadOnly]
    public float FinalAccuracy => FinalStats.CurrentAccuracy;

    [TabGroup("Debug","Weapon Stat")]
    [ShowInInspector, ReadOnly]
    public float FinalRecoil => FinalStats.CurrentRecoil;

    [TabGroup("Debug","Weapon Stat")]
    [ShowInInspector, ReadOnly]
    [InfoBox("기본 스탯 대비 배율")]
    public string StatMultipliers => _baseWeaponStats != null ?
        $"Fire: {(FinalStats.CurrentFireRate / _baseWeaponStats.BaseFireRate):F2}x, " +
        $"Dmg: {(FinalStats.CurrentDamage / _baseWeaponStats.BaseDamage):F2}x, " +
        $"Acc: {(FinalStats.CurrentAccuracy / _baseWeaponStats.BaseAccuracy):F2}x" : "N/A";

    public ProjectileLauncher ProjectileLauncher => _projectileLauncher;
    public AccuracySystem AccuracySystem => _accuracySystem;
    public RecoilSystem RecoilSystem => _recoilSystem;
    #endregion

    #region Private Fields
    private bool _isInitialized = false;
    private Vector3 _lastBaseDirection = Vector3.forward;
    private Vector3 _lastAccurateDirection = Vector3.forward;
    private bool _hasDebugDirections = false;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        ValidateReferences();
    }

    private void Start()
    {
        InitializeWeaponSystem();
    }

    private void Update()
    {
    }

    private void OnValidate()
    {
        if (Application.isPlaying && _isInitialized)
        {
            CalculateFinalStats();
            UpdateLauncherSettings();
            UpdateAccuracySystem();
            
        }
    }

    private void OnDrawGizmos()
    {
        //if (_projectileLauncher == null) return;

        Vector3 shootPosition = _projectileLauncher.transform.position;

        float rayLength = 5f;

        // 기본 조준 방향 (초록색)
        Gizmos.color = Color.green;
        Gizmos.DrawRay(shootPosition, _lastBaseDirection * rayLength);

        // 정확도 적용 방향 (파란색)
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(shootPosition, _lastAccurateDirection * rayLength);

        // 스탯 텍스트 표시
        if (_isInitialized && _baseWeaponStats != null)
        {
            Vector3 basePosition = transform.position;
            float offsetY = 0.5f;
            float spacing = 0.3f;

#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(basePosition + Vector3.up * (offsetY + spacing * 0),
                $"Fire Rate: {FinalStats.CurrentFireRate:F1} ({(FinalStats.CurrentFireRate / _baseWeaponStats.BaseFireRate):F2}x)");

            UnityEditor.Handles.Label(basePosition + Vector3.up * (offsetY + spacing * 1),
                $"Damage: {FinalStats.CurrentDamage:F1} ({(FinalStats.CurrentDamage / _baseWeaponStats.BaseDamage):F2}x)");

            UnityEditor.Handles.Label(basePosition + Vector3.up * (offsetY + spacing * 2),
                $"Accuracy: {FinalStats.CurrentAccuracy:F1}% ({(FinalStats.CurrentAccuracy / _baseWeaponStats.BaseAccuracy):F2}x)");

            UnityEditor.Handles.Label(basePosition + Vector3.up * (offsetY + spacing * 3),
                $"Recoil: {FinalStats.CurrentRecoil:F1} ({(FinalStats.CurrentRecoil / _baseWeaponStats.BaseRecoil):F2}x)");

            UnityEditor.Handles.Label(basePosition + Vector3.up * (offsetY + spacing * 4),
                $"Effects: {ActiveEffectCount}");
#endif
        }
    }
    #endregion

    #region Public Methods - Weapon Effect Management
    /// <summary>무기 효과 추가</summary>
    /// <param name="effect">추가할 효과</param>
    public void ApplyWeaponEffect(WeaponEffectSO effect)
    {
        if (effect == null)
        {
            Debug.LogWarning("[PlayerWeaponController] Cannot apply null weapon effect", this);
            return;
        }

        if (_activeEffects == null)
        {
            _activeEffects = new List<WeaponEffectSO>();
        }

        if (_activeEffects.Contains(effect))
        {
            Debug.LogWarning($"[PlayerWeaponController] Effect {effect.EffectName} already applied", this);
            return;
        }

        _activeEffects.Add(effect);
        CalculateFinalStats();
        UpdateLauncherSettings();
        UpdateAccuracySystem();
        

        Debug.Log($"[PlayerWeaponController] Applied weapon effect: {effect.EffectName}", this);
    }

    /// <summary>무기 효과 제거</summary>
    /// <param name="effect">제거할 효과</param>
    /// <returns>제거 성공 여부</returns>
    public bool RemoveWeaponEffect(WeaponEffectSO effect)
    {
        if (effect == null || _activeEffects == null)
            return false;

        bool removed = _activeEffects.Remove(effect);

        if (removed)
        {
            CalculateFinalStats();
            UpdateLauncherSettings();
            UpdateAccuracySystem();
            

            Debug.Log($"[PlayerWeaponController] Removed weapon effect: {effect.EffectName}", this);
        }

        return removed;
    }

    /// <summary>모든 무기 효과 제거</summary>
    public void ClearAllEffects()
    {
        if (_activeEffects == null || _activeEffects.Count == 0)
            return;

        int removedCount = _activeEffects.Count;
        _activeEffects.Clear();

        CalculateFinalStats();
        UpdateLauncherSettings();
        UpdateAccuracySystem();
        

        Debug.Log($"[PlayerWeaponController] Cleared {removedCount} weapon effects", this);
    }

    /// <summary>특정 효과 적용 여부 확인</summary>
    /// <param name="effect">확인할 효과</param>
    /// <returns>적용 중이면 true</returns>
    public bool HasWeaponEffect(WeaponEffectSO effect)
    {
        return effect != null && _activeEffects != null && _activeEffects.Contains(effect);
    }
    #endregion

    #region Public Methods - Fire Control
    /// <summary>발사 시도</summary>
    /// <returns>발사 성공 여부</returns>
    public bool TryFire()
    {
        if (!CanFireInternal())
            return false;

        Vector3 baseDirection = CalculateBaseDirection();
        Vector3 accurateDirection = (_accuracySystem != null)
            ? _accuracySystem.ApplySpreadToDirection(baseDirection)
            : baseDirection;

        // 디버그 정보 저장
        _lastBaseDirection = baseDirection;
        _lastAccurateDirection = accurateDirection;
        _hasDebugDirections = true;

        bool success = _projectileLauncher.Fire(accurateDirection);

        if (success)
        {
            // 정확도: 연사 페널티 적용
            if (_accuracySystem != null)
            {
                _accuracySystem.AddAccuracyPenalty();
            }

            // 반동: 시각적 피드백용
            if (_recoilSystem != null)
            {
                float recoilAmount = FinalRecoil;
                _recoilSystem.AddRecoil(recoilAmount);
                Debug.Log($"[PlayerWeaponController] Recoil added {recoilAmount}. Current Intensity: {_recoilSystem.CurrentRecoilIntensity:F2}", this);
                // 반동을 카메라에 전달
                if (_recoilConverter != null)
                {
                    Vector3 recoilVector = _recoilSystem.GetCurrentRecoilVector();
                    _recoilConverter.ApplyRecoil(new Vector2(recoilVector.x, recoilVector.y));
                }
            }
        }

        return success;
    }

    /// <summary>현재 조준 방향 조회</summary>
    /// <returns>정확도가 적용된 발사 방향</returns>
    public Vector3 GetAccurateDirection()
    {
        Vector3 baseDirection = CalculateBaseDirection();

        if (_accuracySystem == null)
            return baseDirection;

        return _accuracySystem.ApplySpreadToDirection(baseDirection);
    }
    #endregion

    #region Private Methods - Stat Calculation
    private void InitializeWeaponSystem()
    {
        if (_baseWeaponStats == null)
        {
            Debug.LogError("[PlayerWeaponController] Base weapon stats required!", this);
            return;
        }

        CalculateFinalStats();
        UpdateLauncherSettings();
        UpdateAccuracySystem();
        

        _isInitialized = true;
        Debug.Log("[PlayerWeaponController] Weapon system initialized", this);
    }

    private void CalculateFinalStats()
    {
        if (_baseWeaponStats == null) return;

        WeaponStatData baseStats = _baseWeaponStats.CreateRuntimeStats();
        WeaponStatData finalStats = baseStats;

        // 효과들을 우선순위 순으로 정렬 후 적용
        if (_activeEffects != null && _activeEffects.Count > 0)
        {
            var sortedEffects = new List<WeaponEffectSO>(_activeEffects);
            sortedEffects.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            foreach (var effect in sortedEffects)
            {
                if (effect != null && effect.CanApplyToWeapon(finalStats))
                {
                    finalStats = effect.ApplyToWeapon(finalStats);
                }
            }
        }

        FinalStats = finalStats;
    }

    private void UpdateLauncherSettings()
    {
        if (_projectileLauncher == null) return;

        _projectileLauncher.SetFireRate(FinalStats.CurrentFireRate);
        _projectileLauncher.SetProjectileSpeed(FinalStats.CurrentProjectileSpeed);
        _projectileLauncher.SetProjectileLifetime(FinalStats.CurrentProjectileLifetime);
        _projectileLauncher.SetProjectileDamage(FinalStats.CurrentDamage);
    }

    private void UpdateAccuracySystem()
    {
        if (_accuracySystem == null) return;

        _accuracySystem.SetWeaponStats(FinalStats);
    }
    private void ValidateReferences()
    {
        if (_baseWeaponStats == null)
            Debug.LogError("[PlayerWeaponController] Base weapon stats not assigned!", this);

        if (_accuracySystem == null)
            Debug.LogError("[PlayerWeaponController] Accuracy system not assigned!", this);

        if (_recoilSystem == null)
            Debug.LogError("[PlayerWeaponController] Recoil system not assigned!", this);

        if(_recoilConverter == null)
            Debug.LogWarning("[PlayerWeaponController] Recoil converter not assigned! Recoil will not affect camera.", this);

        if (_projectileLauncher == null)
            Debug.LogError("[PlayerWeaponController] Projectile launcher not assigned!", this);

        if (_aimPointManager == null)
            Debug.LogError("[PlayerWeaponController] Aim point manager not assigned!", this);
    }
    #endregion

    #region Private Methods - Fire Logic
    private bool CanFireInternal()
    {
        if (!_isInitialized || _projectileLauncher == null)
            return false;

        return FinalStats.CurrentFireRate > 0f && _projectileLauncher.CanFire;
    }

    private Vector3 CalculateBaseDirection()
    {
        if (_aimPointManager == null || _projectileLauncher == null)
            return transform.forward;

        Vector3 shootPosition = _projectileLauncher.transform.position;
        Vector3 targetPosition = _aimPointManager.AimPoint;

        Vector3 direction = (targetPosition - shootPosition).normalized;

        return direction.sqrMagnitude > 0.1f ? direction : transform.forward;
    }
    #endregion
}