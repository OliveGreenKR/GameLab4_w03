using Sirenix.OdinInspector;
using System;
using UnityEngine;

/// <summary>
/// 터렛 부채꼴 탐지 및 스캔 범위 설정을 관리하는 경량 컴포넌트
/// 다른 터렛 하부 컴포넌트들이 공통으로 참조하는 설정 데이터 제공
/// </summary>
public class TurretSectorSettings : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("Sector Detection")]
    [Header("Detection Sector")]
    [InfoBox("부채꼴 탐지 영역 설정")]
    [SuffixLabel("degrees")]
    [PropertyRange(10f, 120f)]
    [SerializeField] private float _sectorAngleDegrees = 45f;

    [TabGroup("Sector Detection")]
    [SuffixLabel("units")]
    [PropertyRange(1f, 50f)]
    [SerializeField] private float _detectionRadius = 15f;

    [TabGroup("Scan Range")]
    [Header("Scanning Boundaries")]
    [InfoBox("터렛 스캔 가능 범위 설정 (중앙 기준)")]
    [SuffixLabel("degrees")]
    [PropertyRange(-180f, 0f)]
    [SerializeField] private float _scanRangeMin = -45f;

    [TabGroup("Scan Range")]
    [SuffixLabel("degrees")]
    [PropertyRange(0f, 180f)]
    [SerializeField] private float _scanRangeMax = 45f;
    #endregion

    #region Properties - Basic Settings
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float SectorAngleDegrees => _sectorAngleDegrees;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float DetectionRadius => _detectionRadius;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float ScanRangeMin => _scanRangeMin;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float ScanRangeMax => _scanRangeMax;
    #endregion

    #region Properties - Calculated Values
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    [InfoBox("실제 스캔 가능 범위 (부채꼴 영향 반영)")]
    public float EffectiveScanMin => _scanRangeMin + (_sectorAngleDegrees * 0.5f);

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float EffectiveScanMax => _scanRangeMax - (_sectorAngleDegrees * 0.5f);

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float ScanRangeTotal => _scanRangeMax - _scanRangeMin;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float EffectiveScanRangeTotal => EffectiveScanMax - EffectiveScanMin;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsValidConfiguration => ValidateCurrentSettings();
    #endregion

    #region Events
    /// <summary>설정 값이 변경되었을 때 발생하는 이벤트</summary>
    public event Action<TurretSectorSettings> OnSettingsChanged;

    /// <summary>부채꼴 각도가 변경되었을 때 발생하는 이벤트</summary>
    public event Action<float> OnSectorAngleChanged;

    /// <summary>탐지 반지름이 변경되었을 때 발생하는 이벤트</summary>
    public event Action<float> OnDetectionRadiusChanged;

    /// <summary>스캔 범위가 변경되었을 때 발생하는 이벤트</summary>
    public event Action<float, float> OnScanRangeChanged;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // 컴포넌트 초기화 시 기본 검증
        ClampAllValues();
    }

    private void Start()
    {
        // 초기 설정 유효성 검증
        if (!ValidateCurrentSettings())
        {
            Debug.LogWarning("[TurretSectorSettings] Invalid initial configuration detected", this);
            ClampAllValues();
        }

        // 초기 설정 완료 알림
        NotifySettingsChanged();

        Debug.Log($"[TurretSectorSettings] Initialized - {GetSettingsSummary()}", this);
    }

    private void OnValidate()
    {
        // Inspector에서 값 변경 시 실시간 검증
        ClampAllValues();

        // 런타임 중이면 변경 사항 알림
        if (Application.isPlaying)
        {
            NotifySettingsChanged();
        }
    }

    private void OnDrawGizmos()
    {
        if (!enabled) return;

        // 항상 표시할 최소 정보 - 탐지 반지름만 약하게
        Color fadeColor = Color.blue;
        fadeColor.a = 0.1f;
        Gizmos.color = fadeColor;
        DrawWireCircle(transform.position, _detectionRadius);
    }
    #endregion

    #region Public Methods - Settings Access
    /// <summary>현재 설정의 유효성 검증</summary>
    /// <returns>설정이 유효하면 true</returns>
    public bool ValidateCurrentSettings()
    {
        if (!IsAngleRangeValid())
        {
            Debug.LogWarning("[TurretSectorSettings] Invalid angle range configuration", this);
            return false;
        }

        if (!IsEffectiveRangeValid())
        {
            Debug.LogWarning("[TurretSectorSettings] Invalid effective range configuration", this);
            return false;
        }

        if (_detectionRadius <= 0f)
        {
            Debug.LogWarning("[TurretSectorSettings] Invalid detection radius", this);
            return false;
        }

        if (_sectorAngleDegrees <= 0f)
        {
            Debug.LogWarning("[TurretSectorSettings] Invalid sector angle", this);
            return false;
        }

        return true;
    }

    /// <summary>설정 요약 정보 반환</summary>
    /// <returns>설정 요약 문자열</returns>
    public string GetSettingsSummary()
    {
        return $"Sector: {_sectorAngleDegrees:F1}°, " +
               $"Radius: {_detectionRadius:F1}u, " +
               $"Scan: [{_scanRangeMin:F1}°, {_scanRangeMax:F1}°], " +
               $"Effective: [{EffectiveScanMin:F1}°, {EffectiveScanMax:F1}°]";
    }
    #endregion

    #region Public Methods - Upgrade Interface
    /// <summary>탐지 범위 업그레이드 적용</summary>
    /// <param name="radiusIncrease">반지름 증가량</param>
    public void UpgradeDetectionRange(float radiusIncrease)
    {
        if (radiusIncrease <= 0f)
        {
            Debug.LogWarning("[TurretSectorSettings] Invalid radius increase value", this);
            return;
        }

        float oldRadius = _detectionRadius;
        _detectionRadius += radiusIncrease;
        _detectionRadius = Mathf.Clamp(_detectionRadius, 1f, 50f);

        Debug.Log($"[TurretSectorSettings] Detection radius upgraded: {oldRadius:F1} → {_detectionRadius:F1}", this);
        OnDetectionRadiusChanged?.Invoke(_detectionRadius);
        OnSettingsChanged?.Invoke(this);
    }

    /// <summary>부채꼴 각도 업그레이드 적용</summary>
    /// <param name="angleIncrease">각도 증가량</param>
    public void UpgradeSectorAngle(float angleIncrease)
    {
        if (angleIncrease <= 0f)
        {
            Debug.LogWarning("[TurretSectorSettings] Invalid angle increase value", this);
            return;
        }

        float oldAngle = _sectorAngleDegrees;
        _sectorAngleDegrees += angleIncrease;
        _sectorAngleDegrees = Mathf.Clamp(_sectorAngleDegrees, 10f, 120f);

        Debug.Log($"[TurretSectorSettings] Sector angle upgraded: {oldAngle:F1}° → {_sectorAngleDegrees:F1}°", this);
        OnSectorAngleChanged?.Invoke(_sectorAngleDegrees);
        OnSettingsChanged?.Invoke(this);
    }

    /// <summary>스캔 범위 업그레이드 적용</summary>
    /// <param name="rangeIncrease">범위 증가량 (양쪽으로 확장)</param>
    public void UpgradeScanRange(float rangeIncrease)
    {
        if (rangeIncrease <= 0f)
        {
            Debug.LogWarning("[TurretSectorSettings] Invalid range increase value", this);
            return;
        }

        float oldMin = _scanRangeMin;
        float oldMax = _scanRangeMax;

        // 양쪽으로 절반씩 확장
        float halfIncrease = rangeIncrease * 0.5f;
        _scanRangeMin -= halfIncrease;
        _scanRangeMax += halfIncrease;

        // 범위 클램핑
        _scanRangeMin = Mathf.Clamp(_scanRangeMin, -180f, 0f);
        _scanRangeMax = Mathf.Clamp(_scanRangeMax, 0f, 180f);

        Debug.Log($"[TurretSectorSettings] Scan range upgraded: [{oldMin:F1}°, {oldMax:F1}°] → [{_scanRangeMin:F1}°, {_scanRangeMax:F1}°]", this);
        OnScanRangeChanged?.Invoke(_scanRangeMin, _scanRangeMax);
        OnSettingsChanged?.Invoke(this);
    }
    #endregion

    #region Private Methods - Validation
    private void ClampAllValues()
    {
        // 부채꼴 각도 범위 제한
        _sectorAngleDegrees = Mathf.Clamp(_sectorAngleDegrees, 10f, 120f);

        // 탐지 반지름 범위 제한
        _detectionRadius = Mathf.Clamp(_detectionRadius, 1f, 50f);

        // 스캔 범위 제한 및 순서 보정
        _scanRangeMin = Mathf.Clamp(_scanRangeMin, -180f, 0f);
        _scanRangeMax = Mathf.Clamp(_scanRangeMax, 0f, 180f);

        // 최소값이 최대값보다 크지 않도록 보정
        if (_scanRangeMin >= _scanRangeMax)
        {
            _scanRangeMin = _scanRangeMax - 10f;
            _scanRangeMin = Mathf.Max(_scanRangeMin, -180f);
        }
    }

    private void NotifySettingsChanged()
    {
        // 설정 변경 이벤트 발생
        OnSettingsChanged?.Invoke(this);
        OnSectorAngleChanged?.Invoke(_sectorAngleDegrees);
        OnDetectionRadiusChanged?.Invoke(_detectionRadius);
        OnScanRangeChanged?.Invoke(_scanRangeMin, _scanRangeMax);
    }

    private bool IsAngleRangeValid()
    {
        // 스캔 범위 기본 유효성 검사
        if (_scanRangeMax <= _scanRangeMin)
            return false;

        // 최소 스캔 범위 확보 (10도 이상)
        if ((_scanRangeMax - _scanRangeMin) < 10f)
            return false;

        return true;
    }

    private bool IsEffectiveRangeValid()
    {
        // 유효 스캔 범위 검사
        float effectiveRange = EffectiveScanRangeTotal;

        // 부채꼴 각도로 인해 유효 범위가 너무 작아지는지 확인
        if (effectiveRange <= 0f)
            return false;

        // 최소 유효 범위 확보 (5도 이상)
        if (effectiveRange < 5f)
            return false;

        return true;
    }
    #endregion

    #region Debug Visualization
    private void OnDrawGizmosSelected()
    {
        if (!enabled) return;

        Vector3 position = transform.position;
        Vector3 forward = transform.forward;

        // 탐지 반지름 - 파란색 원
        Gizmos.color = Color.blue;
        DrawWireCircle(position, _detectionRadius);

        // 현재 부채꼴 영역 - 노란색 반투명
        Color sectorColor = Color.yellow;
        sectorColor.a = 0.3f;
        Gizmos.color = sectorColor;
        DrawSectorGizmo(position, forward, 0f, _sectorAngleDegrees, _detectionRadius);

        // 전체 스캔 범위 경계 - 빨간색
        Gizmos.color = Color.red;
        DrawAngleLine(position, forward, _scanRangeMin, _detectionRadius);
        DrawAngleLine(position, forward, _scanRangeMax, _detectionRadius);
        DrawArcGizmo(position, forward, _scanRangeMin, _scanRangeMax, _detectionRadius * 0.8f);

        // 유효 스캔 범위 - 초록색
        Gizmos.color = Color.green;
        DrawAngleLine(position, forward, EffectiveScanMin, _detectionRadius);
        DrawAngleLine(position, forward, EffectiveScanMax, _detectionRadius);
        DrawArcGizmo(position, forward, EffectiveScanMin, EffectiveScanMax, _detectionRadius * 0.9f);

        // 중앙 전진 방향 - 흰색
        Gizmos.color = Color.white;
        Gizmos.DrawLine(position, position + forward * _detectionRadius);

        // 정보 텍스트 표시
        DrawDebugInfo(position);
    }

    private void DrawWireCircle(Vector3 center, float radius)
    {
        const int segments = 32;
        float angleStep = 360f / segments;
        Vector3 previousPoint = center + Vector3.forward * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 currentPoint = center + new Vector3(
                Mathf.Sin(angle) * radius,
                0f,
                Mathf.Cos(angle) * radius
            );
            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
    }

    private void DrawSectorGizmo(Vector3 center, Vector3 forward, float centerAngle, float sectorAngle, float radius)
    {
        const int segments = 16;
        float halfAngle = sectorAngle * 0.5f;
        float startAngle = centerAngle - halfAngle;
        float endAngle = centerAngle + halfAngle;
        float angleStep = sectorAngle / segments;

        // 부채꼴 면 그리기
        Vector3 centerToStart = GetDirectionFromAngle(forward, startAngle) * radius;
        Vector3 prevPoint = center + centerToStart;

        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            Vector3 currentDirection = GetDirectionFromAngle(forward, currentAngle);
            Vector3 currentPoint = center + currentDirection * radius;

            // 중심-이전점-현재점 삼각형
            Gizmos.DrawLine(center, prevPoint);
            Gizmos.DrawLine(prevPoint, currentPoint);
            Gizmos.DrawLine(currentPoint, center);

            prevPoint = currentPoint;
        }

        // 부채꼴 경계선 강조
        Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 1f);
        Gizmos.DrawLine(center, center + GetDirectionFromAngle(forward, startAngle) * radius);
        Gizmos.DrawLine(center, center + GetDirectionFromAngle(forward, endAngle) * radius);
    }

    private void DrawAngleLine(Vector3 center, Vector3 forward, float angleDegrees, float length)
    {
        Vector3 direction = GetDirectionFromAngle(forward, angleDegrees);
        Gizmos.DrawLine(center, center + direction * length);
    }

    private void DrawArcGizmo(Vector3 center, Vector3 forward, float startAngle, float endAngle, float radius)
    {
        const int segments = 10;
        float totalAngle = endAngle - startAngle;
        float angleStep = totalAngle / segments;

        Vector3 prevPoint = center + GetDirectionFromAngle(forward, startAngle) * radius;

        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            Vector3 currentPoint = center + GetDirectionFromAngle(forward, currentAngle) * radius;
            Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
    }

    private Vector3 GetDirectionFromAngle(Vector3 forward, float angleDegrees)
    {
        // Transform의 forward 기준으로 Y축 회전
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        Quaternion rotation = Quaternion.AngleAxis(angleDegrees, Vector3.up);
        return rotation * forward;
    }

    private void DrawDebugInfo(Vector3 position)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        Vector3 labelPos = position + Vector3.up * 2f;

        string info = $"Sector: {_sectorAngleDegrees:F1}°\n" +
                     $"Radius: {_detectionRadius:F1}u\n" +
                     $"Scan: [{_scanRangeMin:F1}°, {_scanRangeMax:F1}°]\n" +
                     $"Effective: [{EffectiveScanMin:F1}°, {EffectiveScanMax:F1}°]";

        UnityEditor.Handles.Label(labelPos, info);
#endif
    }
    #endregion
}