using Sirenix.OdinInspector;
using UnityEngine;

public class AccuracySystem : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("Settings")]
    [Header("Accuracy Settings")]
    [SuffixLabel("degrees")]
    [PropertyRange(0.1f, 45f)]
    [SerializeField] private float _maxSpreadAngle = 15f;

    [TabGroup("Settings")]
    [Header("Recoil Recovery")]
    [SuffixLabel("units/sec")]
    [PropertyRange(1f, 20f)]
    [SerializeField] private float _recoilRecoveryRate = 5f;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float CurrentSpreadAngle { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float CurrentAccuracy { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public RecoilState CurrentRecoilState { get; private set; }
    #endregion

    #region Private Fields
    private WeaponStatData _currentWeaponStats;
    private WeaponMode _currentWeaponMode;
    #endregion

    #region Unity Lifecycle
    private void Update()
    {
        UpdateRecoilRecovery();
        UpdateCurrentAccuracy();
    }
    #endregion

    #region Public Methods - Accuracy Calculation
    public float CalculateFinalAccuracy(WeaponStatData weaponStats, WeaponMode mode, RecoilState recoilState)
    {
        float baseAccuracy = weaponStats.CurrentAccuracy;
        WeaponModeModifiers modifiers = mode.GetModifiers();

        // 반동으로 인한 정확도 감소
        float recoilPenalty = baseAccuracy * recoilState.RecoilRatio * 0.5f;
        float accuracyWithRecoil = baseAccuracy - recoilPenalty;

        // 무기 모드 배율 적용
        float finalAccuracy = accuracyWithRecoil * modifiers.accuracyMultiplier;

        return Mathf.Clamp(finalAccuracy, 0f, 100f);
    }

    public Vector3 ApplySpreadToDirection(Vector3 baseDirection, float accuracy)
    {
        if (accuracy >= 100f)
            return baseDirection;

        float spreadAngle = CalculateSpreadAngle(accuracy);
        Vector2 spreadOffset = GetRandomSpreadOffset(spreadAngle);

        // 기본 방향에 수직인 두 벡터 계산
        Vector3 right = Vector3.Cross(baseDirection, Vector3.up).normalized;
        if (right.sqrMagnitude < 0.1f)
            right = Vector3.Cross(baseDirection, Vector3.forward).normalized;

        Vector3 up = Vector3.Cross(right, baseDirection).normalized;

        // 확산 적용
        Vector3 spreadDirection = baseDirection + (right * spreadOffset.x) + (up * spreadOffset.y);
        return spreadDirection.normalized;
    }

    public float GetCrosshairSpread(float accuracy)
    {
        float spreadAngle = CalculateSpreadAngle(accuracy);
        return spreadAngle / _maxSpreadAngle; // 0-1 정규화
    }
    #endregion

    #region Public Methods - Recoil Management
    public void AddRecoil(float amount)
    {
        CurrentRecoilState = CurrentRecoilState.AddRecoil(amount);
    }

    public void UpdateRecoilRecovery()
    {
        CurrentRecoilState = CurrentRecoilState.UpdateRecovery(Time.deltaTime);
    }

    public void ResetRecoil()
    {
        float maxRecoil = _currentWeaponStats.CurrentRecoil;
        CurrentRecoilState = new RecoilState(maxRecoil, _recoilRecoveryRate);
    }
    #endregion

    #region Public Methods - Configuration
    public void SetWeaponStats(WeaponStatData weaponStats)
    {
        _currentWeaponStats = weaponStats;

        // 무기 변경시 반동 상태 리셋
        float maxRecoil = weaponStats.CurrentRecoil;
        CurrentRecoilState = new RecoilState(maxRecoil, _recoilRecoveryRate);
    }

    public void SetWeaponMode(WeaponMode mode)
    {
        _currentWeaponMode = mode;
    }
    #endregion

    #region Private Methods - Accuracy Logic
    private float CalculateSpreadAngle(float accuracy)
    {
        float normalizedAccuracy = Mathf.Clamp01(accuracy / 100f);
        return _maxSpreadAngle * (1f - normalizedAccuracy);
    }

    private Vector2 GetRandomSpreadOffset(float spreadAngle)
    {
        Vector2 randomPoint = Random.insideUnitCircle;
        float spreadRadians = spreadAngle * Mathf.Deg2Rad;
        return randomPoint * Mathf.Tan(spreadRadians);
    }

    private void UpdateCurrentAccuracy()
    {
        CurrentAccuracy = CalculateFinalAccuracy(_currentWeaponStats, _currentWeaponMode, CurrentRecoilState);
        CurrentSpreadAngle = CalculateSpreadAngle(CurrentAccuracy);
    }
    #endregion
}