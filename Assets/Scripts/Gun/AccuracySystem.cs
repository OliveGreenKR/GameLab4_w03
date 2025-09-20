using Sirenix.OdinInspector;
using UnityEngine;

public class AccuracySystem : MonoBehaviour
{
    #region Nested Types
    [System.Serializable]
    public struct RecoilState
    {
        #region Fields
        private readonly float _currentRecoil;
        private readonly float _maxRecoil;
        private readonly float _recoveryRate;
        #endregion

        #region Properties
        public float CurrentRecoil => _currentRecoil;
        public float RecoilRatio => _maxRecoil > 0f ? _currentRecoil / _maxRecoil : 0f;
        #endregion

        #region Constructor
        public RecoilState(float maxRecoil, float recoveryRate)
        {
            _currentRecoil = 0f;
            _maxRecoil = Mathf.Max(0.1f, maxRecoil);
            _recoveryRate = Mathf.Max(0f, recoveryRate);
        }

        private RecoilState(float currentRecoil, float maxRecoil, float recoveryRate)
        {
            _currentRecoil = currentRecoil;
            _maxRecoil = maxRecoil;
            _recoveryRate = recoveryRate;
        }
        #endregion

        #region Public Methods
        public RecoilState AddRecoil(float amount)
        {
            float newRecoil = Mathf.Clamp(_currentRecoil + amount, 0f, _maxRecoil);
            return new RecoilState(newRecoil, _maxRecoil, _recoveryRate);
        }

        public RecoilState UpdateRecovery(float deltaTime)
        {
            float newRecoil = Mathf.Max(0f, _currentRecoil - _recoveryRate * deltaTime);
            return new RecoilState(newRecoil, _maxRecoil, _recoveryRate);
        }
        #endregion
    }
    #endregion

    #region Serialized Fields
    [TabGroup("Settings")]
    [Header("Accuracy Settings")]
    [SuffixLabel("degrees")]
    [SerializeField] private float _maxSpreadAngle = 15f;

    [TabGroup("Settings")]
    [Header("Recoil Recovery")]
    [SuffixLabel("units/sec")]
    [SerializeField] private float _recoilRecoveryRate = 5f;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float CurrentRecoilValue => CurrentRecoilState.CurrentRecoil;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float RecoilRatio => CurrentRecoilState.RecoilRatio;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float CurrentSpreadAngle { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float CurrentAccuracy { get; private set; }

    public RecoilState CurrentRecoilState { get; private set; }
    #endregion

    #region Private Fields
    private WeaponStatData _currentWeaponStats;
    #endregion

    #region Unity Lifecycle
    private void Update()
    {
        UpdateRecoilRecovery();
        UpdateCurrentAccuracy();
    }
    #endregion

    #region Public Methods - Accuracy Calculation
    public float CalculateFinalAccuracy(WeaponStatData weaponStats, RecoilState recoilState)
    {
        float baseAccuracy = weaponStats.CurrentAccuracy;

        // 반동으로 인한 정확도 감소
        float recoilPenalty = baseAccuracy * recoilState.RecoilRatio * 0.5f;
        float finalAccuracy = baseAccuracy - recoilPenalty;

        return Mathf.Clamp(finalAccuracy, 0f, 100f);
    }

    public Vector3 ApplySpreadToDirection(Vector3 baseDirection, float accuracy)
    {
        if (accuracy >= 100f)
            return baseDirection;

        float spreadAngle = CalculateSpreadAngle(accuracy);
        Vector2 spreadOffset = GetRandomSpreadOffset(spreadAngle);

        Vector3 right = Vector3.Cross(baseDirection, Vector3.up).normalized;
        if (right.sqrMagnitude < 0.1f)
            right = Vector3.Cross(baseDirection, Vector3.forward).normalized;

        Vector3 up = Vector3.Cross(right, baseDirection).normalized;
        Vector3 spreadDirection = baseDirection + (right * spreadOffset.x) + (up * spreadOffset.y);
        return spreadDirection.normalized;
    }

    public float GetCrosshairSpread(float accuracy)
    {
        float spreadAngle = CalculateSpreadAngle(accuracy);
        return spreadAngle / _maxSpreadAngle;
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
        float maxRecoil = weaponStats.CurrentRecoil;
        CurrentRecoilState = new RecoilState(maxRecoil, _recoilRecoveryRate);
    }
    #endregion

    #region Private Methods
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
        CurrentAccuracy = CalculateFinalAccuracy(_currentWeaponStats, CurrentRecoilState);
        CurrentSpreadAngle = CalculateSpreadAngle(CurrentAccuracy);
    }
    #endregion
}