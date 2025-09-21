using Sirenix.OdinInspector;
using UnityEngine;

public class RecoilSystem : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("Settings")]
    [Header("Recoil Intensity")]
    [SuffixLabel("degrees")]
    [SerializeField] private float _maxRecoilIntensity = 1f;

    [TabGroup("Settings")]
    [Header("Recoil Recovery")]
    [SuffixLabel("ratio per sec")]
    [SerializeField] private float _recoilRecoveryRate = 0.8f;

    [TabGroup("Settings")]
    [Header("Recoil Pattern")]
    [InfoBox("반동 방향 패턴")]
    [SerializeField] private Vector2 _recoilDirection = new Vector2(0f, 1f);

    [TabGroup("Settings")]
    [SuffixLabel("multiplier")]
    [SerializeField] private float _recoilRandomness = 0.2f;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float CurrentRecoilIntensity { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector3 CurrentRecoilVector { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float RecoilRatio => _maxRecoilIntensity > 0f ? CurrentRecoilIntensity / _maxRecoilIntensity : 0f;
    #endregion

    #region Unity Lifecycle
    private void Update()
    {
        UpdateRecoilVector();
        UpdateRecoilRecovery();

    }
    #endregion

    #region Public Methods - Recoil Control
    /// <summary>
    /// 반동 추가
    /// </summary>
    /// <param name="amount">반동 강도</param>
    public void AddRecoil(float amount)
    {
        if (amount <= 0f) return;

        float finalAmount = amount;

        CurrentRecoilIntensity = Mathf.Min(CurrentRecoilIntensity + finalAmount, _maxRecoilIntensity);
        UpdateRecoilVector();
    }

    /// <summary>
    /// 현재 반동 벡터 반환 (카메라용)
    /// </summary>
    /// <returns>반동 벡터</returns>
    public Vector3 GetCurrentRecoilVector()
    {
        return CurrentRecoilVector;
    }

    /// <summary>
    /// 현재 반동 강도 반환 (0~1)
    /// </summary>
    /// <returns>정규화된 반동 강도</returns>
    public float GetRecoilIntensityRatio()
    {
        return RecoilRatio;
    }

    /// <summary>
    /// 반동 초기화
    /// </summary>
    public void ResetRecoil()
    {
        CurrentRecoilIntensity = 0f;
        CurrentRecoilVector = Vector3.zero;
    }
    #endregion

    #region Private Methods - Recoil Calculation
    private void UpdateRecoilRecovery()
    {
        if (CurrentRecoilIntensity > 0f)
        {
            CurrentRecoilIntensity = Mathf.Max(0f, CurrentRecoilIntensity * _recoilRecoveryRate * Time.deltaTime);
        }
    }

    private void UpdateRecoilVector()
    {
        if (CurrentRecoilIntensity <= 0f)
        {
            CurrentRecoilVector = Vector3.zero;
            return;
        }

        Vector2 recoilDirection = CalculateRecoilDirection(CurrentRecoilIntensity);
        CurrentRecoilVector = new Vector3(recoilDirection.x, recoilDirection.y, 0f) * CurrentRecoilIntensity;
    }

    private Vector2 CalculateRecoilDirection(float baseRecoil)
    {
        Vector2 baseDirection = _recoilDirection.normalized;

        if (_recoilRandomness > 0f)
        {
            Vector2 randomOffset = Random.insideUnitCircle * _recoilRandomness;
            baseDirection += randomOffset;
        }

        return baseDirection.normalized;
    }
    #endregion
}