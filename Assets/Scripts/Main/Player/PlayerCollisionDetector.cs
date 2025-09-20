using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CharacterController 기반 플레이어용 연속 충돌 감지 컴포넌트
/// OverlapBoxNonAlloc을 사용하여 주기적으로 적 탐지 및 피격 처리
/// </summary>
public class PlayerCollisionDetector : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("Detection Settings")]
    [Header("Box Detection Area")]
    [InfoBox("플레이어 중심 충돌 감지 박스 크기")]
    [SerializeField] private Vector3 _detectionBoxSize = new Vector3(1.5f, 2f, 1.5f);

    [TabGroup("Detection Settings")]
    [InfoBox("플레이어 Transform 기준 박스 오프셋")]
    [SerializeField] private Vector3 _detectionBoxOffset = Vector3.zero;

    [TabGroup("Detection Settings")]
    [Header("Detection Timing")]
    [InfoBox("적 감지 실행 간격")]
    [SuffixLabel("seconds")]
    [PropertyRange(0.05f, 0.5f)]
    [SerializeField] private float _detectionInterval = 0.1f;

    [TabGroup("Detection Settings")]
    [Header("Layer Filtering")]
    [InfoBox("감지할 적 레이어")]
    [SerializeField] private LayerMask _enemyLayerMask = -1;

    [TabGroup("Settings")]
    [Header("Buffer Settings")]
    [InfoBox("OverlapBox 결과 저장용 버퍼 크기")]
    [PropertyRange(10, 50)]
    [SerializeField] private int _colliderBufferSize = 20;

    [TabGroup("Gizmos")]
    [Header("Gizmo Display")]
    [InfoBox("Scene 뷰에서 감지 영역 표시")]
    [SerializeField] private bool _showGizmos = true;

    [TabGroup("Gizmos")]
    [InfoBox("기즈모 표시 색상")]
    [SerializeField] private Color _gizmoColor = Color.red;

    [TabGroup("Gizmos")]
    [InfoBox("기즈모 투명도")]
    [PropertyRange(0.1f, 1f)]
    [SerializeField] private float _gizmoAlpha = 0.3f;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    [InfoBox("현재 감지 활성 상태")]
    public bool IsDetecting { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    [InfoBox("현재 감지된 적 수")]
    public int DetectedEnemyCount => _detectedEnemies?.Count ?? 0;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    [InfoBox("감지 박스 중심 월드 좌표")]
    public Vector3 DetectionCenter => transform.position + transform.TransformDirection(_detectionBoxOffset);

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    [InfoBox("다음 감지 실행 시간")]
    public float NextDetectionTime { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    [InfoBox("플레이어 배틀 엔티티 참조")]
    public PlayerBattleEntity PlayerBattleEntity => _playerBattleEntity;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    [InfoBox("감지 박스 실제 크기 (Transform 스케일 적용)")]
    public Vector3 WorldBoxSize => Vector3.Scale(_detectionBoxSize, transform.lossyScale);
    #endregion

    #region Private Fields
    private PlayerBattleEntity _playerBattleEntity;
    private Collider[] _colliderBuffer;
    private List<IBattleEntity> _detectedEnemies = new List<IBattleEntity>();
    private Dictionary<IBattleEntity, float> _lastDamageTime = new Dictionary<IBattleEntity, float>();
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeReferences();
    }

    private void Start()
    {
        InitializeBuffer();
        StartDetection();
    }

    private void Update()
    {
        if (!IsDetecting || _playerBattleEntity == null)
            return;

        if (Time.time >= NextDetectionTime)
        {
            PerformDetection();
            NextDetectionTime = Time.time + _detectionInterval;
        }
    }

    private void OnDrawGizmos()
    {
        if (!_showGizmos)
            return;

        Color gizmoColorWithAlpha = _gizmoColor;
        gizmoColorWithAlpha.a = _gizmoAlpha;

        Gizmos.color = gizmoColorWithAlpha;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(_detectionBoxOffset, _detectionBoxSize);

        Gizmos.color = _gizmoColor;
        Gizmos.DrawWireCube(_detectionBoxOffset, _detectionBoxSize);
        Gizmos.matrix = Matrix4x4.identity;
    }

    #endregion

    #region Public Methods - Detection Control
    /// <summary>감지 시작</summary>
    public void StartDetection()
    {
        if (_playerBattleEntity == null)
        {
            Debug.LogWarning("[PlayerCollisionDetector] Cannot start detection: PlayerBattleEntity not found", this);
            return;
        }

        IsDetecting = true;
        NextDetectionTime = Time.time + _detectionInterval;
        Debug.Log("[PlayerCollisionDetector] Detection started", this);
    }

    /// <summary>감지 중지</summary>
    public void StopDetection()
    {
        IsDetecting = false;
        _detectedEnemies.Clear();
        _lastDamageTime.Clear();
        Debug.Log("[PlayerCollisionDetector] Detection stopped", this);
    }

    /// <summary>박스 크기 설정</summary>
    /// <param name="boxSize">박스 크기</param>
    public void SetDetectionBoxSize(Vector3 boxSize)
    {
        _detectionBoxSize.x = Mathf.Max(0.1f, boxSize.x);
        _detectionBoxSize.y = Mathf.Max(0.1f, boxSize.y);
        _detectionBoxSize.z = Mathf.Max(0.1f, boxSize.z);
    }

    /// <summary>박스 오프셋 설정</summary>
    /// <param name="offset">오프셋</param>
    public void SetDetectionBoxOffset(Vector3 offset)
    {
        _detectionBoxOffset = offset;
    }

    /// <summary>감지 간격 설정</summary>
    /// <param name="interval">감지 간격 (초)</param>
    public void SetDetectionInterval(float interval)
    {
        _detectionInterval = Mathf.Clamp(interval, 0.05f, 0.5f);
    }

    /// <summary>현재 감지된 적 목록 조회</summary>
    /// <returns>감지된 적 리스트 (읽기 전용)</returns>
    public IReadOnlyList<IBattleEntity> GetDetectedEnemies()
    {
        return _detectedEnemies.AsReadOnly();
    }
    #endregion

    #region Private Methods - Detection Logic
    private void InitializeReferences()
    {
        _playerBattleEntity = GetComponent<PlayerBattleEntity>();

        if (_playerBattleEntity == null)
        {
            Debug.LogError("[PlayerCollisionDetector] PlayerBattleEntity component required!", this);
        }
    }

    private void InitializeBuffer()
    {
        _colliderBuffer = new Collider[_colliderBufferSize];
    }

    private void PerformDetection()
    {
        if (_colliderBuffer == null)
        {
            InitializeBuffer();
        }

        Vector3 center = DetectionCenter;
        Vector3 halfExtents = WorldBoxSize * 0.5f;

        int hitCount = Physics.OverlapBoxNonAlloc(
            center,
            halfExtents,
            _colliderBuffer,
            transform.rotation,
            _enemyLayerMask
        );

        _detectedEnemies.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = _colliderBuffer[i];
            if (hitCollider == null) continue;

            IBattleEntity enemy = hitCollider.GetComponent<IBattleEntity>();
            if (enemy != null && enemy.IsAlive && !BattleInteractionSystem.IsSameTeam(_playerBattleEntity, enemy))
            {
                _detectedEnemies.Add(enemy);
            }
        }

        ProcessDetectedEnemies();
    }

    private void ProcessDetectedEnemies()
    {
        foreach (var enemy in _detectedEnemies)
        {
            if (CanTakeDamageFromEnemy(enemy))
            {
                ProcessDamageFromEnemy(enemy);
            }
        }
    }

    private bool CanTakeDamageFromEnemy(IBattleEntity enemy)
    {
        if (_playerBattleEntity.IsInvulnerable)
            return false;

        return true;
    }

    private void ProcessDamageFromEnemy(IBattleEntity enemy)
    {
        float damage = enemy.DealDamage(_playerBattleEntity, enemy.GetCurrentStat(BattleStatType.Attack));

        if (damage > 0f)
        {
            _lastDamageTime[enemy] = Time.time;
        }
    }
    #endregion
}