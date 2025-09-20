using Sirenix.OdinInspector;
using UnityEngine;

public class AccuracySystem : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("Settings")]
    [Header("Accuracy Settings")]
    [SuffixLabel("degrees")]
    [SerializeField] private float _maxSpreadAngle = 15f;

    [TabGroup("Settings")]
    [Header("Accuracy Penalty")]
    [InfoBox("연사 시 정확도 저하")]
    [SuffixLabel("penalty/shot")]
    [SerializeField] private float _accuracyPenaltyPerShot = 5f;

    [TabGroup("Settings")]
    [SuffixLabel("penalty/sec")]
    [SerializeField] private float _accuracyRecoveryRate = 10f;

    [TabGroup("Settings")]
    [SuffixLabel("penalty")]
    [SerializeField] private float _maxAccuracyPenalty = 50f;
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
    public float CurrentAccuracyPenalty { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float BaseAccuracy => _currentWeaponStats.CurrentAccuracy;
    #endregion

    #region Private Fields
    private WeaponStatData _currentWeaponStats;
    #endregion

    #region Unity Lifecycle
    private void Update()
    {
        UpdateAccuracyRecovery();
        UpdateCurrentAccuracy();
    }
    #endregion

    #region Public Methods - Accuracy Calculation
    /// <summary>
    /// 방향에 스프레드 적용
    /// </summary>
    /// <param name="baseDirection">기본 방향</param>
    /// <returns>스프레드가 적용된 방향</returns>
    public Vector3 ApplySpreadToDirection(Vector3 baseDirection)
    {
        if (CurrentAccuracy >= 100f)
            return baseDirection;

        float spreadAngle = CalculateSpreadAngle(CurrentAccuracy);
        Vector2 spreadOffset = GetRandomSpreadOffset(spreadAngle);

        Vector3 right = Vector3.Cross(baseDirection, Vector3.up).normalized;
        if (right.sqrMagnitude < 0.1f)
            right = Vector3.Cross(baseDirection, Vector3.forward).normalized;

        Vector3 up = Vector3.Cross(right, baseDirection).normalized;
        Vector3 spreadDirection = baseDirection + (right * spreadOffset.x) + (up * spreadOffset.y);
        return spreadDirection.normalized;
    }

    /// <summary>
    /// 크로스헤어 스프레드 비율 계산
    /// </summary>
    /// <returns>0~1 스프레드 비율</returns>
    public float GetCrosshairSpread()
    {
        float spreadAngle = CalculateSpreadAngle(CurrentAccuracy);
        return spreadAngle / _maxSpreadAngle;
    }

    /// <summary>
    /// 현재 정확도 반환
    /// </summary>
    /// <returns>현재 정확도 (0~100)</returns>
    public float GetCurrentAccuracy()
    {
        return CurrentAccuracy;
    }
    #endregion

    #region Public Methods - Accuracy Penalty
    /// <summary>
    /// 연사 시 정확도 페널티 추가
    /// </summary>
    /// <param name="penaltyAmount">페널티 양 (기본값: 설정된 페널티)</param>
    public void AddAccuracyPenalty(float penaltyAmount = -1f)
    {
        if (penaltyAmount < 0f)
            penaltyAmount = _accuracyPenaltyPerShot;

        CurrentAccuracyPenalty = Mathf.Min(CurrentAccuracyPenalty + penaltyAmount, _maxAccuracyPenalty);
    }

    /// <summary>
    /// 정확도 페널티 초기화
    /// </summary>
    public void ResetAccuracyPenalty()
    {
        CurrentAccuracyPenalty = 0f;
    }
    #endregion

    #region Public Methods - Configuration
    /// <summary>
    /// 무기 스탯 설정
    /// </summary>
    /// <param name="weaponStats">무기 스탯 데이터</param>
    public void SetWeaponStats(WeaponStatData weaponStats)
    {
        _currentWeaponStats = weaponStats;
    }

    /// <summary>
    /// 최대 스프레드 각도 설정
    /// </summary>
    /// <param name="maxSpreadAngle">최대 스프레드 각도</param>
    public void SetMaxSpreadAngle(float maxSpreadAngle)
    {
        _maxSpreadAngle = Mathf.Clamp(maxSpreadAngle, 1f, 45f);
    }
    #endregion

    #region Private Methods
    private void UpdateAccuracyRecovery()
    {
        if (CurrentAccuracyPenalty > 0f)
        {
            CurrentAccuracyPenalty = Mathf.Max(0f, CurrentAccuracyPenalty - _accuracyRecoveryRate * Time.deltaTime);
        }
    }

    private void UpdateCurrentAccuracy()
    {
        float baseAccuracy = _currentWeaponStats.CurrentAccuracy;
        CurrentAccuracy = Mathf.Clamp(baseAccuracy - CurrentAccuracyPenalty, 0f, 100f);
        CurrentSpreadAngle = CalculateSpreadAngle(CurrentAccuracy);
    }

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
    #endregion
}