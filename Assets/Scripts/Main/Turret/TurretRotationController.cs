using Sirenix.OdinInspector;
using System;
using UnityEngine;

/// <summary>
/// 터렛 회전 실행 컴포넌트 (명령 기반)
/// TurretController로부터 회전 명령을 받아 실행
/// </summary>
public class TurretRotationController : MonoBehaviour
{
    private const float KINDA_SMALL = 0.001f;

    #region Serialized Fields
    [TabGroup("Rotation Settings")]
    [Header("Rotation Speeds")]
    [SuffixLabel("degrees/sec")]
    [PropertyRange(30f, 360f)]
    [SerializeField] private float _rotationSpeed = 180f;

    [TabGroup("References")]
    [Header("Rotation Target")]
    [Required]
    [SerializeField] private Transform _rotationTransform;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float CurrentYRotation { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float TargetYRotation { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsRotating { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsContinuousRotation { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float ContinuousMinAngle { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float ContinuousMaxAngle { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool ContinuousForward { get; private set; }
    #endregion

    #region Events
    /// <summary>목표 각도 도달 시 발생</summary>
    public event Action OnRotationComplete;

    /// <summary>회전 시작 시 발생</summary>
    public event Action<float> OnRotationStarted;

    /// <summary>회전 정지 시 발생</summary>
    public event Action OnRotationStopped;
    #endregion

    #region Private Fields
    private bool _continuousRotationForward = true;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeReferences();
    }

    private void Start()
    {
        InitializeCurrentRotation();
    }

    private void Update()
    {
        if (IsRotating)
        {
            UpdateRotation();
        }
    }

    private void OnValidate()
    {
        _rotationSpeed = Mathf.Clamp(_rotationSpeed, 30f, 360f);

        if (Application.isPlaying && _rotationTransform != null)
        {
            CurrentYRotation = _rotationTransform.eulerAngles.y;
        }
    }
    #endregion

    #region Public Methods - Rotation Commands
    /// <summary>특정 각도로 회전</summary>
    /// <param name="targetAngleDegrees">목표 각도</param>
    public void RotateTo(float targetAngleDegrees)
    {
        if (_rotationTransform == null) return;

        TargetYRotation = ClampAngle(targetAngleDegrees);
        IsContinuousRotation = false;
        IsRotating = true;

        OnRotationStarted?.Invoke(TargetYRotation);
        Debug.Log($"[TurretRotationController] Rotating to {TargetYRotation:F1}°", this);
    }

    /// <summary>연속 스캔 회전 시작</summary>
    /// <param name="minAngle">최소 각도</param>
    /// <param name="maxAngle">최대 각도</param>
    public void StartContinuousRotation(float minAngle, float maxAngle)
    {
        if (_rotationTransform == null) return;

        Debug.Log($"[TurretRotationController] Starting continuous rotation between {minAngle:F1}° and {maxAngle:F1}°", this);
        ContinuousMinAngle = minAngle;
        ContinuousMaxAngle = maxAngle;

        Debug.Log($"[TurretRotationController] Started continuous rotation BEFORE [{ContinuousMinAngle:F1}°, {ContinuousMaxAngle:F1}°]", this);

        if (Mathf.Abs(ContinuousMaxAngle - ContinuousMinAngle) < KINDA_SMALL)
        {
            Debug.LogWarning("[TurretRotationController] Invalid continuous range", this);
            return;
        }

        IsContinuousRotation = true;
        IsRotating = true;
        _continuousRotationForward = true;

        CalculateNextContinuousTarget();
        OnRotationStarted?.Invoke(TargetYRotation);
        Debug.Log($"[TurretRotationController] Started continuous rotation [{ContinuousMinAngle:F1}°, {ContinuousMaxAngle:F1}°]", this);
    }

    /// <summary>회전 즉시 정지</summary>
    public void StopRotation()
    {
        IsRotating = false;
        IsContinuousRotation = false;
        TargetYRotation = CurrentYRotation;

        OnRotationStopped?.Invoke();
        Debug.Log("[TurretRotationController] Rotation stopped", this);
    }

    /// <summary>회전 속도 설정</summary>
    /// <param name="speedDegreesPerSec">회전 속도 (도/초)</param>
    public void SetRotationSpeed(float speedDegreesPerSec)
    {
        _rotationSpeed = Mathf.Clamp(speedDegreesPerSec, 30f, 360f);
        Debug.Log($"[TurretRotationController] Rotation speed set to {_rotationSpeed:F1}°/s", this);
    }

    /// <summary>현재 바라보는 방향</summary>
    /// <returns>전진 방향 벡터</returns>
    public Vector3 GetForwardDirection()
    {
        return _rotationTransform != null ? _rotationTransform.forward : Vector3.forward;
    }

    /// <summary>목표 각도까지의 거리</summary>
    /// <returns>각도 차이 (절댓값)</returns>
    public float GetAngleToTarget()
    {
        return Mathf.Abs(Mathf.DeltaAngle(CurrentYRotation, TargetYRotation));
    }
    #endregion

    #region Public Methods - State Query
    /// <summary>회전 완료 확인</summary>
    /// <param name="toleranceDegrees">허용 오차</param>
    /// <returns>도달 여부</returns>
    public bool IsRotationComplete(float toleranceDegrees = 0.1f)
    {
        float angleDifference = Mathf.Abs(Mathf.DeltaAngle(CurrentYRotation, TargetYRotation));
        return angleDifference <= toleranceDegrees;
    }

    /// <summary>현재 회전 상태 정보</summary>
    /// <returns>상태 문자열</returns>
    public string GetRotationStatus()
    {
        if (!IsRotating)
            return "Stopped";

        if (IsContinuousRotation)
            return $"Continuous [{ContinuousMinAngle:F1}°, {ContinuousMaxAngle:F1}°] {(ContinuousForward ? "→" : "←")}";

        return $"Rotating to {TargetYRotation:F1}° (diff: {GetAngleToTarget():F1}°)";
    }
    #endregion

    #region Private Methods - Rotation Logic
    private void UpdateRotation()
    {
        if (IsContinuousRotation)
        {
            UpdateContinuousRotation();
        }
        else
        {
            UpdateSingleRotation();
        }

        ApplyRotationToTransform();
        CheckRotationComplete();
    }

    private void UpdateSingleRotation()
    {
        float angleDifference = Mathf.DeltaAngle(CurrentYRotation, TargetYRotation);
        float rotationStep = _rotationSpeed * Time.deltaTime;

        if (Mathf.Abs(angleDifference) <= rotationStep)
        {
            CurrentYRotation = TargetYRotation;
        }
        else
        {
            float rotationDirection = Mathf.Sign(angleDifference);
            CurrentYRotation += rotationDirection * rotationStep;
        }

        CurrentYRotation = ClampAngle(CurrentYRotation);
    }

    private void UpdateContinuousRotation()
    {
        float rotationStep = _rotationSpeed * Time.deltaTime;

        if (_continuousRotationForward)
        {
            // 항상 증가 방향
            if (CurrentYRotation >= TargetYRotation)
            {
                CurrentYRotation = TargetYRotation;
                CalculateNextContinuousTarget();
            }
            else
            {
                CurrentYRotation += rotationStep;
            }
        }
        else
        {
            // 항상 감소 방향  
            if (CurrentYRotation <= TargetYRotation)
            {
                CurrentYRotation = TargetYRotation;
                CalculateNextContinuousTarget();
            }
            else
            {
                CurrentYRotation -= rotationStep;
            }
        }

        CurrentYRotation = ClampAngle(CurrentYRotation);
    }

    private void CalculateNextContinuousTarget()
    {
        if (_continuousRotationForward)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(CurrentYRotation, ContinuousMaxAngle)) < KINDA_SMALL)
            {
                _continuousRotationForward = false;
                TargetYRotation = ContinuousMinAngle;
                ContinuousForward = false;
            }
            else
            {
                TargetYRotation = ContinuousMaxAngle;
                ContinuousForward = true;
            }
        }
        else
        {
            if (Mathf.Abs(Mathf.DeltaAngle(CurrentYRotation, ContinuousMinAngle)) < KINDA_SMALL)
            {
                _continuousRotationForward = true;
                TargetYRotation = ContinuousMaxAngle;
                ContinuousForward = true;
            }
            else
            {
                TargetYRotation = ContinuousMinAngle;
                ContinuousForward = false;
            }
        }
    }

    private void ApplyRotationToTransform()
    {
        if (_rotationTransform == null) return;

        Vector3 currentEulerAngles = _rotationTransform.eulerAngles;
        currentEulerAngles.y = CurrentYRotation;
        _rotationTransform.eulerAngles = currentEulerAngles;
    }

    private float ClampAngle(float angle)
    {
        return Mathf.Repeat(angle + 180f, 360f) - 180f;
    }

    private void CheckRotationComplete()
    {
        if (!IsRotating) return;

        bool isComplete = Mathf.Abs(Mathf.DeltaAngle(CurrentYRotation, TargetYRotation)) < KINDA_SMALL;

        if (isComplete && !IsContinuousRotation)
        {
            IsRotating = false;
            OnRotationComplete?.Invoke();
        }
    }
    #endregion

    #region Private Methods - Initialization
    private void InitializeReferences()
    {
        if (_rotationTransform == null)
        {
            _rotationTransform = transform;
            Debug.LogWarning("[TurretRotationController] Rotation Transform not assigned, using self transform.", this);
        }
    }

    private void InitializeCurrentRotation()
    {
        CurrentYRotation = _rotationTransform.eulerAngles.y;
        TargetYRotation = CurrentYRotation;
        IsRotating = false;
        IsContinuousRotation = false;

        Debug.Log($"[TurretRotationController] Initialized at {CurrentYRotation:F1}°", this);
    }
    #endregion
}