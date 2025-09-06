using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    #region Singleton
    public static ProjectileManager Instance { get; private set; }
    #endregion

    #region Serialized Fields
    [Header("Pool Settings")]
    [SerializeField] private int _defaultPoolSize = 200;
    #endregion

    #region Properties
    public bool IsInitialized { get; private set; }
    #endregion

    #region Private Fields
    private Dictionary<Type, Queue<ProjectileBase>> _projectilePools;

    [SerializeField]
    [DictionaryDrawerSettings(KeyLabel = "Projectile Type", ValueLabel = "Prefabs")]
    private Dictionary<Type, ProjectileBase> _projectilePrefabs;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDictionaries();
            IsInitialized = true;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion


    #region Public Methods
    /// <summary>
    /// 지정된 타입의 투사체를 Pool에서 가져옵니다
    /// </summary>
    /// <typeparam name="T">투사체 타입</typeparam>
    /// <returns>활성화된 투사체 인스턴스</returns>
    public T GetProjectile<T>() where T : ProjectileBase
    {
        Type projectileType = typeof(T);
        Queue<ProjectileBase> pool = GetOrCreatePool(projectileType);

        if (pool.Count > 0)
        {
            ProjectileBase projectile = pool.Dequeue();
            projectile.gameObject.SetActive(true);
            return projectile as T;
        }

        return CreateNewProjectile<T>();
    }

    /// <summary>
    /// 투사체를 Pool로 반환합니다
    /// </summary>
    /// <param name="projectile">반환할 투사체</param>
    public void ReturnProjectile(ProjectileBase projectile)
    {
        if (projectile == null) return;

        projectile.gameObject.SetActive(false);

        Type projectileType = projectile.GetType();
        Queue<ProjectileBase> pool = GetOrCreatePool(projectileType);
        pool.Enqueue(projectile);
    }

    /// <summary>
    /// 지정된 타입의 Pool을 초기화합니다
    /// </summary>
    /// <typeparam name="T">투사체 타입</typeparam>
    /// <param name="prefab">프리팹</param>
    /// <param name="count">초기 생성 개수</param>
    public void InitializePool<T>(T prefab, int count) where T : ProjectileBase
    {
        if (prefab == null)
        {
            Debug.LogError("[ProjectileManager] Prefab is null");
            return;
        }

        Type projectileType = typeof(T);
        _projectilePrefabs[projectileType] = prefab;

        Queue<ProjectileBase> pool = GetOrCreatePool(projectileType);

        for (int i = 0; i < count; i++)
        {
            T newProjectile = CreateNewProjectile<T>();
            if (newProjectile != null)
            {
                pool.Enqueue(newProjectile);
            }
        }
    }

    /// <summary>
    /// 투사체를 지정된 위치와 방향으로 발사합니다
    /// </summary>
    /// <typeparam name="T">투사체 타입</typeparam>
    /// <param name="worldPosition">발사 위치</param>
    /// <param name="worldRotation">발사 방향</param>
    /// <returns>발사된 투사체 인스턴스</returns>
    public T LaunchProjectile<T>(Vector3 worldPosition, Quaternion worldRotation) where T : ProjectileBase
    {
        T projectile = GetProjectile<T>();
        if (projectile != null)
        {
            projectile.transform.position = worldPosition;
            projectile.transform.rotation = worldRotation;
            projectile.gameObject.SetActive(true);
        }
        return projectile;
    }
    #endregion

    #region Private Methods
    private void InitializeDictionaries()
    {
        _projectilePools = new Dictionary<Type, Queue<ProjectileBase>>();
        _projectilePrefabs = new Dictionary<Type, ProjectileBase>();
    }

    private Queue<ProjectileBase> GetOrCreatePool(Type projectileType)
    {
        if (!_projectilePools.ContainsKey(projectileType))
        {
            _projectilePools[projectileType] = new Queue<ProjectileBase>();
        }
        return _projectilePools[projectileType];
    }

    private T CreateNewProjectile<T>() where T : ProjectileBase
    {
        Type projectileType = typeof(T);

        if (!_projectilePrefabs.ContainsKey(projectileType))
        {
            Debug.LogError($"[ProjectileManager] No prefab registered for type: {projectileType.Name}");
            return null;
        }

        ProjectileBase prefab = _projectilePrefabs[projectileType];
        GameObject newProjectile = Instantiate(prefab.gameObject);
        newProjectile.SetActive(false);

        return newProjectile.GetComponent<T>();
    }
    #endregion
}
