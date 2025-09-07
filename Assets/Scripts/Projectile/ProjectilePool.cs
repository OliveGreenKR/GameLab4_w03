using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IProjectile 기반 투사체 풀링 시스템
/// 각 Launcher가 개별적으로 소유하는 독립적인 풀 관리자
/// </summary>
[System.Serializable]
public class ProjectilePool
{
    #region Serialized Fields
    [Header("Pool Settings")]
    [SerializeField] private int _defaultPoolSize = 50;

    [Header("Projectile Prefabs")]
    [SerializeField, SerializeReference]
    private List<IProjectile> _projectilePrefabs = new List<IProjectile>();
    #endregion

    #region Properties
    [ShowInInspector, ReadOnly]
    public bool IsInitialized { get; private set; }

    [ShowInInspector, ReadOnly]
    public Dictionary<ProjectileType, int> PoolCounts => GetPoolCounts();

    [ShowInInspector, ReadOnly]
    public int RegisteredPrefabCount => _projectilePrefabs?.Count ?? 0;
    #endregion

    #region Private Fields
    private Dictionary<ProjectileType, Queue<IProjectile>> _projectilePools;
    private Dictionary<ProjectileType, IProjectile> _projectilePrefabMap;
    private Transform _poolParent;
    #endregion

    #region Public Methods
    /// <summary>
    /// 풀 초기화
    /// </summary>
    /// <param name="poolParent">풀링된 오브젝트들의 부모 Transform</param>
    public void Initialize(Transform poolParent = null)
    {
        if (IsInitialized)
        {
            Debug.LogWarning("[ProjectilePool] Already initialized");
            return;
        }

        _poolParent = poolParent;
        InitializeDictionaries();
        InitializeFromPrefabList();
        IsInitialized = true;

        Debug.Log("[ProjectilePool] Initialization completed");
    }

    /// <summary>
    /// 지정된 타입의 투사체를 풀에서 가져옵니다
    /// </summary>
    /// <param name="projectileType">투사체 타입</param>
    /// <returns>활성화된 투사체 인스턴스</returns>
    public IProjectile GetProjectile(ProjectileType projectileType)
    {
        if (!IsInitialized)
        {
            Debug.LogError("[ProjectilePool] Pool not initialized");
            return null;
        }

        Queue<IProjectile> pool = GetOrCreatePool(projectileType);

        if (pool.Count > 0)
        {
            IProjectile projectile = pool.Dequeue();
            projectile.GameObject.SetActive(true);
            return projectile;
        }

        return CreateNewProjectile(projectileType);
    }

    /// <summary>
    /// 투사체를 풀로 반환합니다
    /// </summary>
    /// <param name="projectile">반환할 투사체</param>
    public void ReturnProjectile(IProjectile projectile)
    {
        if (projectile == null) return;

        projectile.GameObject.SetActive(false);

        if (_poolParent != null && projectile.Transform.parent != _poolParent)
        {
            projectile.Transform.SetParent(_poolParent);
        }

        ProjectileType projectileType = projectile.ProjectileType;
        Queue<IProjectile> pool = GetOrCreatePool(projectileType);
        pool.Enqueue(projectile);
    }

    /// <summary>
    /// 투사체를 지정된 위치와 방향으로 발사합니다
    /// </summary>
    /// <param name="projectileType">투사체 타입</param>
    /// <param name="worldPosition">발사 위치</param>
    /// <param name="worldRotation">발사 방향</param>
    /// <returns>발사된 투사체 인스턴스</returns>
    public IProjectile LaunchProjectile(ProjectileType projectileType, Vector3 worldPosition, Quaternion worldRotation)
    {
        IProjectile projectile = GetProjectile(projectileType);
        if (projectile != null)
        {
            projectile.Transform.position = worldPosition;
            projectile.Transform.rotation = worldRotation;
            projectile.GameObject.SetActive(true);
        }
        return projectile;
    }

    /// <summary>
    /// 지정된 타입의 풀을 미리 생성합니다
    /// </summary>
    /// <param name="projectileType">투사체 타입</param>
    /// <param name="count">생성할 개수</param>
    public void PrewarmPool(ProjectileType projectileType, int count)
    {
        if (!IsInitialized)
        {
            Debug.LogError("[ProjectilePool] Pool not initialized");
            return;
        }

        Queue<IProjectile> pool = GetOrCreatePool(projectileType);

        for (int i = 0; i < count; i++)
        {
            IProjectile newProjectile = CreateNewProjectile(projectileType);
            if (newProjectile != null)
            {
                newProjectile.GameObject.SetActive(false);
                pool.Enqueue(newProjectile);
            }
        }

        Debug.Log($"[ProjectilePool] Prewarmed {count} instances of {projectileType}");
    }

    /// <summary>
    /// 모든 풀 정리
    /// </summary>
    public void ClearAllPools()
    {
        if (_projectilePools == null) return;

        foreach (var pool in _projectilePools.Values)
        {
            while (pool.Count > 0)
            {
                IProjectile projectile = pool.Dequeue();
                if (projectile?.GameObject != null)
                {
                    Object.DestroyImmediate(projectile.GameObject);
                }
            }
        }

        _projectilePools.Clear();
        Debug.Log("[ProjectilePool] All pools cleared");
    }
    #endregion

    #region Private Methods
    private void InitializeDictionaries()
    {
        _projectilePools = new Dictionary<ProjectileType, Queue<IProjectile>>();
        _projectilePrefabMap = new Dictionary<ProjectileType, IProjectile>();
    }

    private void InitializeFromPrefabList()
    {
        if (_projectilePrefabs == null)
        {
            Debug.LogWarning("[ProjectilePool] Projectile prefabs list is null");
            return;
        }

        foreach (var prefab in _projectilePrefabs)
        {
            if (prefab == null)
            {
                Debug.LogWarning("[ProjectilePool] Null prefab found in list");
                continue;
            }

            ProjectileType projectileType = prefab.ProjectileType;

            if (_projectilePrefabMap.ContainsKey(projectileType))
            {
                Debug.LogWarning($"[ProjectilePool] Duplicate projectile type: {projectileType}. Overwriting.");
            }

            _projectilePrefabMap[projectileType] = prefab;

            // 기본 풀 생성
            PrewarmPool(projectileType, _defaultPoolSize);

            Debug.Log($"[ProjectilePool] Registered {projectileType} with {_defaultPoolSize} instances");
        }
    }

    private Queue<IProjectile> GetOrCreatePool(ProjectileType projectileType)
    {
        if (!_projectilePools.ContainsKey(projectileType))
        {
            _projectilePools[projectileType] = new Queue<IProjectile>();
        }
        return _projectilePools[projectileType];
    }

    private IProjectile CreateNewProjectile(ProjectileType projectileType)
    {
        if (!_projectilePrefabMap.ContainsKey(projectileType))
        {
            Debug.LogError($"[ProjectilePool] No prefab registered for type: {projectileType}");
            return null;
        }

        IProjectile prefab = _projectilePrefabMap[projectileType];
        GameObject newProjectile = Object.Instantiate(prefab.GameObject);

        if (_poolParent != null)
        {
            newProjectile.transform.SetParent(_poolParent);
        }

        newProjectile.SetActive(false);

        return newProjectile.GetComponent<IProjectile>();
    }

    private Dictionary<ProjectileType, int> GetPoolCounts()
    {
        var counts = new Dictionary<ProjectileType, int>();
        if (_projectilePools != null)
        {
            foreach (var kvp in _projectilePools)
            {
                counts[kvp.Key] = kvp.Value.Count;
            }
        }
        return counts;
    }
    #endregion
}