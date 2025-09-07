using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileLauncher : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("References")]
    [Header("Owner Entity")]
    [Required]
    [SerializeField] private GameObject _ownerGameObject;

    [TabGroup("Projectile")]
    [Header("Projectile Settings")]
    [SerializeField] private ProjectileType _defaultProjectileType = ProjectileType.BasicProjectile;

    [TabGroup("Projectile")]
    [Header("Projectile Pool")]
    [SerializeField] private ProjectilePool _projectilePool = new ProjectilePool();

    [TabGroup("References")]
    [Header("Launch Transform")]
    [Required]
    [SerializeField] private Transform _shootTransform;

    [TabGroup("Settings")]
    [Header("Launch Settings")]
    [SuffixLabel("projectiles/sec")]
    [PropertyRange(0.1f, 50f)]
    [SerializeField] private float _fireRatePerSecond = 1f;

    [TabGroup("Settings")]
    [SuffixLabel("seconds")]
    [PropertyRange(0f, 10f)]
    [SerializeField] private float _projectileLifetime = 5f;

    [TabGroup("Settings")]
    [SuffixLabel("units/sec")]
    [PropertyRange(1f, 100f)]
    [SerializeField] private float _projectileSpeed = 20f;

    [TabGroup("Effects")]
    [Header("Projectile Effects")]
    [SerializeField] private List<IProjectileEffect> _effects = new List<IProjectileEffect>();
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool CanFire => _canFire;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float CooldownRemaining => _cooldownRemaining;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public GameObject Owner => _ownerObject;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int ActiveProjectileCount => _activeProjectiles.Count;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsPoolInitialized => _projectilePool?.IsInitialized ?? false;
    #endregion

    [GUIColor("green")]
    [Button(ButtonSizes.Large)]
    [ButtonGroup("Debug")]
    public void ShootForward()
    {
        Fire(transform.forward);
    }

    [GUIColor("cyan")]
    [Button(ButtonSizes.Medium)]
    [ButtonGroup("Debug")]
    public void InitializePool()
    {
        InitializeProjectilePool();
    }

    #region Private Fields
    private GameObject _ownerObject;
    private bool _canFire = true;
    private float _cooldownRemaining = 0f;
    private List<IProjectile> _activeProjectiles = new List<IProjectile>();
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializeReferences();
        InitializeProjectilePool();
    }

    private void Update()
    {
        UpdateCooldown();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 지정된 방향으로 투사체 발사
    /// </summary>
    /// <param name="worldDirection">월드 좌표 발사 방향</param>
    /// <returns>발사 성공 여부</returns>
    public bool Fire(Vector3 worldDirection)
    {
        return Fire(_defaultProjectileType, worldDirection);
    }

    /// <summary>
    /// 지정된 타입과 방향으로 투사체 발사
    /// </summary>
    /// <param name="projectileType">투사체 타입</param>
    /// <param name="worldDirection">월드 좌표 발사 방향</param>
    /// <returns>발사 성공 여부</returns>
    public bool Fire(ProjectileType projectileType, Vector3 worldDirection)
    {
        if (!_canFire || _shootTransform == null || !IsPoolInitialized)
            return false;

        Vector3 normalizedDirection = worldDirection.normalized;
        if (normalizedDirection == Vector3.zero)
        {
            Debug.LogWarning("[ProjectileLauncher] Invalid fire direction (zero vector)", this);
            return false;
        }

        // 투사체 생성 위치와 회전 계산
        Vector3 spawnPosition = _shootTransform.position;
        Quaternion spawnRotation = Quaternion.LookRotation(normalizedDirection);

        // 투사체 생성
        IProjectile projectile = CreateProjectile(projectileType, spawnPosition, spawnRotation);
        if (projectile == null)
            return false;

        // 이펙트 적용
        ApplyEffectsToProjectile(projectile);

        // 쿨다운 시작
        _canFire = false;
        _cooldownRemaining = 1.0f / _fireRatePerSecond;

        return true;
    }

    /// <summary>
    /// 지정된 위치로 투사체 발사
    /// </summary>
    /// <param name="worldTargetPosition">월드 좌표 목표 위치</param>
    /// <returns>발사 성공 여부</returns>
    public bool FireAt(Vector3 worldTargetPosition)
    {
        return FireAt(_defaultProjectileType, worldTargetPosition);
    }

    /// <summary>
    /// 지정된 타입과 위치로 투사체 발사
    /// </summary>
    /// <param name="projectileType">투사체 타입</param>
    /// <param name="worldTargetPosition">월드 좌표 목표 위치</param>
    /// <returns>발사 성공 여부</returns>
    public bool FireAt(ProjectileType projectileType, Vector3 worldTargetPosition)
    {
        if (_shootTransform == null)
            return false;

        Vector3 direction = (worldTargetPosition - _shootTransform.position).normalized;
        return Fire(projectileType, direction);
    }

    /// <summary>
    /// 기본 투사체 타입 설정
    /// </summary>
    /// <param name="projectileType">기본 투사체 타입</param>
    public void SetDefaultProjectileType(ProjectileType projectileType)
    {
        _defaultProjectileType = projectileType;
    }

    /// <summary>
    /// 이펙트 추가
    /// </summary>
    /// <param name="effect">추가할 이펙트</param>
    public void AddEffect(IProjectileEffect effect)
    {
        if (effect == null)
        {
            Debug.LogWarning("[ProjectileLauncher] Cannot add null effect", this);
            return;
        }

        if (_effects == null)
        {
            _effects = new List<IProjectileEffect>();
        }

        _effects.Add(effect);
    }

    /// <summary>
    /// 이펙트 제거
    /// </summary>
    /// <param name="effect">제거할 이펙트</param>
    /// <returns>제거 성공 여부</returns>
    public bool RemoveEffect(IProjectileEffect effect)
    {
        if (effect == null || _effects == null)
            return false;

        return _effects.Remove(effect);
    }

    /// <summary>
    /// 모든 이펙트 제거
    /// </summary>
    public void ClearAllEffects()
    {
        if (_effects != null)
        {
            _effects.Clear();
        }
    }

    /// <summary>
    /// 발사 속도 설정
    /// </summary>
    /// <param name="fireRatePerSecond">초당 발사 횟수</param>
    public void SetFireRate(float fireRatePerSecond)
    {
        _fireRatePerSecond = Mathf.Clamp(fireRatePerSecond, 0.1f, 50f);
    }
    #endregion

    #region Private Methods
    private void InitializeReferences()
    {
        if (_ownerGameObject != null)
        {
            //본인이 소유자
            _ownerObject = gameObject;

            if (_ownerObject == null)
            {
                Debug.LogError("[ProjectileLauncher] IBattleEntity component not found on owner GameObject!", this);
            }
        }
        else
        {
            Debug.LogError("[ProjectileLauncher] Owner Entity GameObject not assigned!", this);
        }

        if (_shootTransform == null)
        {
            _shootTransform = transform;
            Debug.LogWarning("[ProjectileLauncher] Shoot Transform not assigned, using self transform.", this);
        }
    }

    private void InitializeProjectilePool()
    {
        if (_projectilePool == null)
        {
            Debug.LogError("[ProjectileLauncher] ProjectilePool is null!", this);
            return;
        }

        if (!_projectilePool.IsInitialized)
        {
            // 풀의 부모를 이 Launcher로 설정
            _projectilePool.Initialize(transform);
        }
    }

    private void UpdateCooldown()
    {
        if (!_canFire)
        {
            _cooldownRemaining -= Time.deltaTime;

            if (_cooldownRemaining <= 0f)
            {
                _canFire = true;
                _cooldownRemaining = 0f;
            }
        }
    }

    private IProjectile CreateProjectile(ProjectileType projectileType, Vector3 worldPosition, Quaternion worldRotation)
    {
        if (_projectilePool == null || !_projectilePool.IsInitialized)
        {
            Debug.LogError("[ProjectileLauncher] ProjectilePool not initialized!", this);
            return null;
        }

        IProjectile projectile = _projectilePool.SpawnProjectile(projectileType, worldPosition, worldRotation);

        if (projectile != null)
        {
            // 투사체 초기화 (순수 투사체 속성만)
            projectile.Initialize(_projectileLifetime, _projectileSpeed);

            // 투사체 소멸 이벤트 구독
            projectile.OnProjectileDestroyed += OnProjectileDestroyed;

            // 활성 투사체 목록에 추가
            _activeProjectiles.Add(projectile);
        }

        return projectile;
    }

    private void ApplyEffectsToProjectile(IProjectile projectile)
    {
        if (projectile == null || _effects == null) return;

        // 우선순위 순으로 이펙트 적용
        var sortedEffects = new List<IProjectileEffect>(_effects);
        sortedEffects.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        foreach (var effect in sortedEffects)
        {
            if (effect != null)
            {
                effect.AttachToProjectile(projectile);
            }
        }
    }

    private void OnProjectileDestroyed(IProjectile projectile)
    {
        if (projectile == null) return;

        // 이펙트 정리
        if (_effects != null)
        {
            foreach (var effect in _effects)
            {
                if (effect != null)
                {
                    effect.DetachFromProjectile(projectile);
                }
            }
        }

        // 이벤트 구독 해제
        projectile.OnProjectileDestroyed -= OnProjectileDestroyed;

        // 활성 투사체 목록에서 제거
        _activeProjectiles.Remove(projectile);

        // 풀로 반환
        _projectilePool?.ReturnProjectile(projectile);
    }
    #endregion
}