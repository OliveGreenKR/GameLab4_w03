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
    [Header("Weighted Distribution")]
    [InfoBox("중앙 집중도 제어 (높을수록 중앙 집중)")]
    [SuffixLabel("weight")]
    [PropertyRange(0.1f, 5f)]
    [SerializeField] private float _centerWeightMultiplier = 2f;

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
    public float BaseAccuracy => _currentWeaponStats.Accuracy;
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
        return GetRandomDirectionInCone(baseDirection, spreadAngle);
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
        _maxSpreadAngle = Mathf.Max(maxSpreadAngle, 0.01f);
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
        float baseAccuracy = _currentWeaponStats.Accuracy;
        float effectivePenalty = CalculateEffectivePenalty(baseAccuracy, CurrentAccuracyPenalty);
        CurrentAccuracy = Mathf.Max(0f, baseAccuracy - effectivePenalty);
        CurrentSpreadAngle = CalculateSpreadAngle(CurrentAccuracy);
    }

    private float CalculateEffectivePenalty(float baseAccuracy, float rawPenalty)
    {
        if (baseAccuracy <= 100f)
            return rawPenalty;

        // 100 초과 부분을 페널티 버퍼로 사용
        float excessAccuracy = baseAccuracy - 100f;
        float bufferedPenalty = Mathf.Max(0f, rawPenalty - excessAccuracy);

        return bufferedPenalty;
    }

    private float CalculateSpreadAngle(float accuracy)
    {
        // 100 기준으로 정규화하여 100 이상은 완전 정확도
        float clampedAccuracy = Mathf.Min(accuracy, 100f);
        float normalizedAccuracy = Mathf.Clamp01(clampedAccuracy / 100f);
        return _maxSpreadAngle * (1f - normalizedAccuracy);
    }

    /// <summary>
    /// 구면 좌표계 기반으로 원뿔 내부의 가중 분포 방향 벡터 생성
    /// </summary>
    /// <param name="baseDirection">기준 방향 벡터</param>
    /// <param name="coneAngleDegrees">원뿔 각도 (도)</param>
    /// <returns>원뿔 내부의 가중 랜덤 방향 벡터</returns>
    private Vector3 GetRandomDirectionInCone(Vector3 baseDirection, float coneAngleDegrees)
    {
        if (coneAngleDegrees <= 0f)
            return baseDirection;

        // 정확도 기반 중앙 집중도 계산
        float accuracyRatio = Mathf.Clamp01(CurrentAccuracy / 100f);
        float centerWeight = Mathf.Lerp(1.0f, _centerWeightMultiplier, accuracyRatio);

        // 구면 좌표계에서 가중 분포 샘플링
        float coneAngleRadians = coneAngleDegrees * Mathf.Deg2Rad;
        float cosTheta = Mathf.Cos(coneAngleRadians);

        // Power Law Distribution으로 중앙 집중
        float randomValue = Random.Range(0f, 1f);
        float weightedRandom = Mathf.Pow(randomValue, centerWeight);
        float randomCosTheta = Mathf.Lerp(1f, cosTheta, weightedRandom);
        float theta = Mathf.Acos(randomCosTheta);

        // 방위각은 균등 분포 유지
        float phi = Random.Range(0f, 2f * Mathf.PI);

        // 구면 좌표를 직교 좌표로 변환
        float sinTheta = Mathf.Sin(theta);
        Vector3 localDirection = new Vector3(
            sinTheta * Mathf.Cos(phi),
            sinTheta * Mathf.Sin(phi),
            Mathf.Cos(theta)
        );

        return TransformDirectionToWorldSpace(localDirection, baseDirection);
    }

    /// <summary>
    /// 로컬 방향 벡터를 월드 공간의 기준 방향 기준으로 변환
    /// </summary>
    /// <param name="localDirection">로컬 공간 방향 벡터 (Z축이 전방)</param>
    /// <param name="worldForward">월드 공간의 기준 방향</param>
    /// <returns>변환된 월드 공간 방향 벡터</returns>
    private Vector3 TransformDirectionToWorldSpace(Vector3 localDirection, Vector3 worldForward)
    {
        // 월드 전방 벡터에서 직교 기저 생성
        Vector3 forward = worldForward.normalized;

        // Up 벡터 선택 (forward와 평행하지 않은 벡터)
        Vector3 tempUp = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) < 0.9f
            ? Vector3.up
            : Vector3.forward;

        // 직교 기저 생성
        Vector3 right = Vector3.Cross(forward, tempUp).normalized;
        Vector3 up = Vector3.Cross(right, forward).normalized;

        // 로컬 방향을 월드 공간으로 변환
        return localDirection.x * right +
               localDirection.y * up +
               localDirection.z * forward;
    }
    #endregion
}