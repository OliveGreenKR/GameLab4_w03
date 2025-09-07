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
    [SerializeField]
    private List<GameObject> _projectilePrefabs = new List<GameObject>();
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
    private Dictionary<ProjectileType, GameObject> _projectilePrefabMap;
    private Transform _poolParent;

    // 활성 발사체 추적 
    private Dictionary<IProjectile, float> _activeProjectiles = new Dictionary<IProjectile, float>();
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


        //PreWarm
        foreach (var pair in _projectilePrefabMap)
        {
            PrewarmPool(pair.Key, _defaultPoolSize);
        }
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

        Debug.LogWarning($"[ProjectilePool] Pool empty for type: {projectileType}. Instantiating new projectile.");
        return CreateNewProjectile(projectileType);
    }

    /// <summary>
    /// 지정된 타입의 투사체를 풀에서 가져옵니다
    /// 풀이 비어있으면 가장 오래된 활성 발사체를 회수합니다
    /// </summary>
    /// <param name="projectileType">투사체 타입</param>
    /// <returns>활성화된 투사체 인스턴스</returns>
    public IProjectile GetProjectileFocely(ProjectileType projectileType)
    {
        if (!IsInitialized)
        {
            Debug.LogError("[ProjectilePool] Pool not initialized");
            return null;
        }

        Queue<IProjectile> pool = GetOrCreatePool(projectileType);

        // 풀에 사용 가능한 발사체가 있으면 반환
        if (pool.Count > 0)
        {
            IProjectile projectile = pool.Dequeue();
            projectile.GameObject.SetActive(true);
            return projectile;
        }

        // 풀이 비어있으면 가장 오래된 발사체 회수 시도
        IProjectile recycledProjectile = RecycleOldestProjectile(projectileType);
        if (recycledProjectile != null)
        {
            recycledProjectile.GameObject.SetActive(true);
            return recycledProjectile;
        }

        // 회수할 발사체도 없으면 새로 생성
        return CreateNewProjectile(projectileType);
    }

    /// <summary>
    /// 투사체를 풀로 반환합니다
    /// </summary>
    /// <param name="projectile">반환할 투사체</param>
    public void ReturnProjectile(IProjectile projectile)
    {
        if (projectile == null) return;

        // 활성 발사체 추적에서 제거
        _activeProjectiles.Remove(projectile);

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
    /// 투사체를 지정된 위치와 방향으로 생성합니다.
    /// </summary>
    /// <param name="projectileType">투사체 타입</param>
    /// <param name="worldPosition">발사 위치</param>
    /// <param name="worldRotation">발사 방향</param>
    /// <returns>생성된 투사체 인스턴스</returns>
    public IProjectile SpawnProjectile(ProjectileType projectileType, Vector3 worldPosition, Quaternion worldRotation)
    {
        IProjectile projectile = GetProjectile(projectileType);
        if (projectile != null)
        {
            projectile.Transform.position = worldPosition;
            projectile.Transform.rotation = worldRotation;
            projectile.GameObject.SetActive(true);

            // 활성 발사체로 등록
            _activeProjectiles[projectile] = Time.time;
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

    #region Private Methods - Initizialization & Pooling
    private void InitializeDictionaries()
    {
        _projectilePools = new Dictionary<ProjectileType, Queue<IProjectile>>();
        _projectilePrefabMap = new Dictionary<ProjectileType, GameObject>();
    }

    private void InitializeFromPrefabList()
    {
        if (_projectilePrefabs == null)
        {
            Debug.LogWarning("[ProjectilePool] Projectile prefabs list is null");
            return;
        }

        foreach (var prefabGameObject in _projectilePrefabs)
        {
            if (prefabGameObject == null)
            {
                Debug.LogWarning("[ProjectilePool] Null prefab found in list");
                continue;
            }

            IProjectile projectile = prefabGameObject.GetComponent<IProjectile>();
            if (projectile == null)
            {
                Debug.LogWarning($"[ProjectilePool] GameObject {prefabGameObject.name} does not have IProjectile component");
                continue;
            }

            ProjectileType projectileType = projectile.ProjectileType;

            if (_projectilePrefabMap.ContainsKey(projectileType))
            {
                Debug.LogWarning($"[ProjectilePool] Duplicate projectile type: {projectileType}. Overwriting.");
            }

            _projectilePrefabMap[projectileType] = prefabGameObject;

            Debug.Log($"[ProjectilePool] Registered {projectileType}");
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

        GameObject prefab = _projectilePrefabMap[projectileType];
        GameObject newProjectile = Object.Instantiate(prefab);

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

    #region Private Methods - Pool Reuse
    /// <summary>
    /// 가장 오래된 활성 발사체를 강제 회수합니다
    /// </summary>
    /// <param name="projectileType">회수할 발사체 타입 (같은 타입 우선)</param>
    /// <returns>회수된 발사체, 없으면 null</returns>
    private IProjectile RecycleOldestProjectile(ProjectileType projectileType)
    {
        if (_activeProjectiles.Count == 0)
            return null;

        // 같은 타입 중 가장 오래된 것 우선 검색
        IProjectile oldestSameType = null;
        float oldestSameTypeTime = float.MaxValue;

        // 전체 중 가장 오래된 것 (백업)
        IProjectile oldestOverall = null;
        float oldestOverallTime = float.MaxValue;

        foreach (var kvp in _activeProjectiles)
        {
            IProjectile projectile = kvp.Key;
            float spawnTime = kvp.Value;

            if (projectile.ProjectileType == projectileType && spawnTime < oldestSameTypeTime)
            {
                oldestSameType = projectile;
                oldestSameTypeTime = spawnTime;
            }

            if (spawnTime < oldestOverallTime)
            {
                oldestOverall = projectile;
                oldestOverallTime = spawnTime;
            }
        }

        // 같은 타입이 있으면 우선, 없으면 전체에서 가장 오래된 것
        IProjectile targetProjectile = oldestSameType ?? oldestOverall;

        if (targetProjectile != null)
        {
            // 강제 소멸 (풀로 자동 반환됨)
            targetProjectile.DestroyProjectile();

            // 풀에서 다시 가져오기
            Queue<IProjectile> pool = GetOrCreatePool(targetProjectile.ProjectileType);
            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }
        }

        return null;
    }
    #endregion
}