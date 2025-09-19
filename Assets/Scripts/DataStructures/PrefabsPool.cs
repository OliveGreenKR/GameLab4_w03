using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 프리팹 타입 정의
/// </summary>
public enum PrefabType
{
    None,
    EnemyNormal, 
    EnemyElite,  
    PickUp
}

/// <summary>
/// GameObject 프리팹 기반 범용 풀링 시스템
/// </summary>
[System.Serializable]
public class PrefabsPool
{
    [System.Serializable]
    public struct PrefabMapEntry
    {
        public PrefabType type;
        public GameObject prefab;
    }

    #region Serialized Fields
    [Header("Pool Settings")]
    [SerializeField] private int _defaultPoolSize = 50;

    [Header("Prefab Mapping")]
    [Required]
    [InfoBox("각 PrefabType에 해당하는 프리팹을 직접 할당하세요")]
    [SerializeField] private PrefabMapEntry[] _prefabEntries;

    #endregion

    #region Properties
    [ShowInInspector, ReadOnly]
    public bool IsInitialized { get; private set; }

    [ShowInInspector, ReadOnly]
    public Dictionary<PrefabType, int> PoolCounts => GetPoolCounts();

    [ShowInInspector, ReadOnly]
    public Dictionary<PrefabType, int> ActiveObjectCounts => _activeObjects;

    [ShowInInspector, ReadOnly]
    public int TotalActiveObjectCounts => _activeObjectsWithTime?.Count ?? 0;

    [ShowInInspector, ReadOnly]
    public int RegisteredPrefabCount => _prefabMap?.Count ?? 0;
    #endregion

    #region Private Fields
    private Dictionary<PrefabType, GameObject> _prefabMap; // 런타임용
    private Dictionary<PrefabType, Queue<GameObject>> _prefabPools;
    private Transform _poolParentTransform;
    private Dictionary<PrefabType, int> _poolCreations = new Dictionary<PrefabType, int>();

    // 타입별 활성 객체 수 카운트 (외부 조회용)
    private Dictionary<PrefabType, int> _activeObjects = new Dictionary<PrefabType, int>();
    // 개별 GameObject 시간 추적 (강제 회수용)
    private Dictionary<GameObject, float> _activeObjectsWithTime = new Dictionary<GameObject, float>();
    #endregion

    #region Public Methods - Pool Management
    /// <summary>
    /// 풀 초기화
    /// </summary>
    /// <param name="poolParent">풀링된 오브젝트들의 부모 Transform</param>
    public void Initialize(Transform poolParent = null)
    {
        if (IsInitialized)
        {
            Debug.LogWarning("[PrefabsPool] Already initialized");
            return;
        }

        if (poolParent == null)
        {
            GameObject rootObject = new GameObject($"PooledPrefabs");
            _poolParentTransform = rootObject?.transform;
        }
        else
        {
            _poolParentTransform = poolParent;
        }

        InitializeDictionaries();
        InitializeFromPrefabMap();
        IsInitialized = true;

        // PreWarm
        foreach (var pair in _prefabMap)
        {
            _poolCreations.Add(pair.Key, 0);
            _activeObjects.Add(pair.Key, 0);
            PrewarmPool(pair.Key, _defaultPoolSize);
        }
        Debug.Log("[PrefabsPool] Initialization completed");
    }

    /// <summary>
    /// 지정된 타입의 오브젝트를 풀에서 가져옵니다
    /// </summary>
    /// <param name="prefabType">프리팹 타입</param>
    /// <returns>활성화된 GameObject 인스턴스</returns>
    public GameObject GetObject(PrefabType prefabType)
    {
        if (!IsInitialized)
        {
            Debug.LogError("[PrefabsPool] Pool not initialized");
            return null;
        }

        Queue<GameObject> pool = GetOrCreatePool(prefabType);

        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            return obj;
        }

        // 풀이 비어있으면 동적 확장 후 다시 시도
        Debug.LogWarning($"[PrefabsPool] Pool empty for type: {prefabType}. Expanding pool dynamically.");
        ExpandPool(prefabType);

        // 확장 후 다시 시도
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            return obj;
        }

        // 확장도 실패한 경우 (매우 드문 경우)
        Debug.LogError($"[PrefabsPool] Failed to expand pool for type: {prefabType}. Creating single object.");
        return CreateNewObject(prefabType);
    }

    /// <summary>
    /// 지정된 타입의 오브젝트를 풀에서 가져옵니다
    /// 풀이 비어있으면 가장 오래된 활성 오브젝트를 회수합니다
    /// </summary>
    /// <param name="prefabType">프리팹 타입</param>
    /// <returns>활성화된 GameObject 인스턴스</returns>
    public GameObject GetObjectForcely(PrefabType prefabType)
    {
        if (!IsInitialized)
        {
            Debug.LogError("[PrefabsPool] Pool not initialized");
            return null;
        }

        Queue<GameObject> pool = GetOrCreatePool(prefabType);

        // 풀에 사용 가능한 오브젝트가 있으면 반환
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            return obj;
        }

        // 풀이 비어있으면 가장 오래된 오브젝트 회수 시도
        GameObject recycledObject = RecycleOldestObject(prefabType);
        if (recycledObject != null)
        {
            Debug.LogWarning($"[PrefabsPool] Pool empty for type: {prefabType}. Reused oldest object.");
            return recycledObject;
        }

        // 회수할 오브젝트도 없으면 동적 확장
        Debug.LogWarning($"[PrefabsPool] Pool empty for type: {prefabType}. Expanding pool dynamically.");
        ExpandPool(prefabType);

        // 확장 후 다시 시도
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            return obj;
        }

        // 확장도 실패한 경우 (매우 드문 경우)
        Debug.LogError($"[PrefabsPool] Failed to expand pool for type: {prefabType}. Creating single object.");
        return CreateNewObject(prefabType);
    }

    /// <summary>
    /// 오브젝트를 풀로 반환합니다
    /// </summary>
    /// <param name="obj">반환할 GameObject</param>
    public void ReturnObject(GameObject obj)
    {
        if (obj == null) return;

        PrefabType prefabType = GetPrefabTypeFromGameObject(obj);

        // 활성 추적에서 제거
        _activeObjectsWithTime.Remove(obj);
        if (_activeObjects.ContainsKey(prefabType))
        {
            _activeObjects[prefabType] = Mathf.Max(0, _activeObjects[prefabType] - 1);
        }

        obj.SetActive(false);

        if (_poolParentTransform != null && obj.transform.parent != _poolParentTransform)
        {
            obj.transform.SetParent(_poolParentTransform);
        }

        Queue<GameObject> pool = GetOrCreatePool(prefabType);
        pool.Enqueue(obj);
    }

    /// <summary>
    /// 오브젝트를 지정된 위치와 방향으로 생성합니다
    /// </summary>
    /// <param name="prefabType">프리팹 타입</param>
    /// <param name="worldPosition">생성 위치</param>
    /// <param name="worldRotation">생성 방향</param>
    /// <param name="isForcely">강제 생성 여부</param>
    /// <returns>생성된 GameObject 인스턴스</returns>
    public GameObject SpawnObject(PrefabType prefabType, Vector3 worldPosition, Quaternion worldRotation, bool isForcely = false)
    {
        GameObject obj = isForcely ?
            GetObjectForcely(prefabType) : GetObject(prefabType);

        if (obj != null)
        {
            obj.transform.position = worldPosition;
            obj.transform.rotation = worldRotation;
            obj.SetActive(true);

            // 활성 오브젝트로 등록
            _activeObjectsWithTime[obj] = Time.time;
            if (!_activeObjects.ContainsKey(prefabType))
            {
                _activeObjects[prefabType] = 0;
            }
            _activeObjects[prefabType]++;
        }
        else
        {
            Debug.LogError($"[PrefabsPool] Failed to Spawn Object!");
        }

        return obj;
    }

    /// <summary>
    /// 지정된 타입의 풀을 미리 생성합니다
    /// </summary>
    /// <param name="prefabType">프리팹 타입</param>
    /// <param name="count">생성할 개수</param>
    public void PrewarmPool(PrefabType prefabType, int count)
    {
        if (!IsInitialized)
        {
            Debug.LogError("[PrefabsPool] Pool not initialized");
            return;
        }

        Queue<GameObject> pool = GetOrCreatePool(prefabType);

        for (int i = 0; i < count; i++)
        {
            GameObject newObject = CreateNewObject(prefabType);
            if (newObject != null)
            {
                newObject.SetActive(false);
                pool.Enqueue(newObject);
            }
        }

        Debug.Log($"[PrefabsPool] Prewarmed {count} instances of {prefabType}");
    }
    #endregion

    #region Public Methods - Query
    /// <summary>
    /// 현재 활성화된 오브젝트 개수 반환
    /// </summary>
    /// <returns>활성 오브젝트 개수</returns>
    public int GetActiveObjectCount()
    {
        return _activeObjectsWithTime.Count;
    }

    /// <summary>
    /// 현재 활성화된 모든 오브젝트 목록 반환
    /// </summary>
    /// <returns>활성 오브젝트 목록 (읽기 전용)</returns>
    public IEnumerable<GameObject> GetActiveObjects()
    {
        return _activeObjectsWithTime.Keys;
    }

    /// <summary>
    /// 특정 타입의 활성화된 오브젝트 개수 반환
    /// </summary>
    /// <param name="prefabType">조회할 프리팹 타입</param>
    /// <returns>해당 타입의 활성 오브젝트 개수</returns>
    public int GetActiveObjectCount(PrefabType prefabType)
    {
        return _activeObjects.ContainsKey(prefabType) ? _activeObjects[prefabType] : 0;
    }

    /// <summary>
    /// 모든 풀 정리
    /// </summary>
    public void ClearAllPools()
    {
        if (_prefabPools == null) return;

        foreach (var pool in _prefabPools.Values)
        {
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
        }

        _prefabPools.Clear();
        _activeObjects.Clear();
        _activeObjectsWithTime.Clear();

        Debug.Log("[PrefabsPool] All pools cleared");
    }
    #endregion

    #region Private Methods - Initialization
    private void InitializeDictionaries()
    {
        _prefabPools = new Dictionary<PrefabType, Queue<GameObject>>();
    }

    private void InitializeFromPrefabMap()
    {
        if (_prefabEntries == null)
        {
            Debug.LogWarning("[PrefabsPool] Prefab entries array is null");
            return;
        }

        if(_prefabMap !=null)
        {
            // 기존 Dictionary 초기화
            _prefabMap.Clear();
        }
        else
        {
            _prefabMap = new Dictionary<PrefabType, GameObject>();
        }

        // List에서 Dictionary로 변환
        foreach (var entry in _prefabEntries)
        {
            if (entry.prefab == null)
            {
                Debug.LogWarning($"[PrefabsPool] Null prefab found for type: {entry.type}");
                continue;
            }

            if (_prefabMap.ContainsKey(entry.type))
            {
                Debug.LogWarning($"[PrefabsPool] Duplicate prefab type: {entry.type}. Overwriting.");
            }

            _prefabMap[entry.type] = entry.prefab;
            Debug.Log($"[PrefabsPool] Registered {entry.type}: {entry.prefab.name}");
        }
    }
    #endregion

    #region Private Methods - Pool Management
    /// <summary>
    /// 동적 풀 확장 - 현재 풀 크기의 1.5배로 확장
    /// </summary>
    /// <param name="prefabType">확장할 프리팹 타입</param>
    private void ExpandPool(PrefabType prefabType)
    {
        Queue<GameObject> pool = GetOrCreatePool(prefabType);

        // 현재 풀의 총 생성된 객체 수를 기준으로 확장 크기 계산
        int currentPoolSize = _poolCreations.ContainsKey(prefabType) ? _poolCreations[prefabType] : _defaultPoolSize;
        int expansionSize = Mathf.Max(5, Mathf.RoundToInt(currentPoolSize * 0.5f)); // 최소 5개 보장

        Debug.Log($"[PrefabsPool] Expanding pool for {prefabType}: +{expansionSize} objects (Current: {currentPoolSize})", this);

        // 확장 실행
        for (int i = 0; i < expansionSize; i++)
        {
            GameObject newObject = CreateNewObject(prefabType);
            if (newObject != null)
            {
                newObject.SetActive(false);
                pool.Enqueue(newObject);
            }
        }

        Debug.Log($"[PrefabsPool] Pool expansion completed for {prefabType}. New pool size: {pool.Count}", this);
    }

    /// <summary>
    /// 지정된 타입의 풀을 가져오거나 새로 생성
    /// </summary>
    /// <param name="prefabType">프리팹 타입</param>
    /// <returns>해당 타입의 풀</returns>
    private Queue<GameObject> GetOrCreatePool(PrefabType prefabType)
    {
        if (!_prefabPools.ContainsKey(prefabType))
        {
            _prefabPools[prefabType] = new Queue<GameObject>();
        }
        return _prefabPools[prefabType];
    }

    /// <summary>
    /// 새 오브젝트 생성
    /// </summary>
    /// <param name="prefabType">생성할 프리팹 타입</param>
    /// <returns>생성된 GameObject</returns>
    private GameObject CreateNewObject(PrefabType prefabType)
    {
        if (!_prefabMap.ContainsKey(prefabType) || _prefabMap[prefabType] == null)
        {
            Debug.LogError($"[PrefabsPool] No prefab registered for type: {prefabType}");
            return null;
        }

        GameObject prefab = _prefabMap[prefabType];
        GameObject newObject = Object.Instantiate(prefab);

        newObject.name = prefab.name + ":" + (++_poolCreations[prefabType]).ToString();

        if (_poolParentTransform != null)
        {
            newObject.transform.SetParent(_poolParentTransform);
        }

        newObject.SetActive(false);

        return newObject;
    }

    /// <summary>
    /// 풀별 개체 수 반환
    /// </summary>
    /// <returns>타입별 풀 개체 수 딕셔너리</returns>
    private Dictionary<PrefabType, int> GetPoolCounts()
    {
        var counts = new Dictionary<PrefabType, int>();
        if (_prefabPools != null)
        {
            foreach (var kvp in _prefabPools)
            {
                counts[kvp.Key] = kvp.Value.Count;
            }
        }
        return counts;
    }

    /// <summary>
    /// GameObject로부터 PrefabType 추론
    /// </summary>
    /// <param name="obj">분석할 GameObject</param>
    /// <returns>추론된 PrefabType</returns>
    private PrefabType GetPrefabTypeFromGameObject(GameObject obj)
    {
        if (obj == null || _prefabMap == null)
            return PrefabType.None;

        // 딕셔너리 역검색: 오브젝트 이름에서 프리팹 이름 패턴 찾기
        string objName = obj.name;

        // "프리팹이름:인덱스" 형태에서 프리팹 이름 추출
        string prefabName = objName.Contains(":") ? objName.Split(':')[0] : objName;

        foreach (var kvp in _prefabMap)
        {
            if (kvp.Value != null && kvp.Value.name == prefabName)
            {
                return kvp.Key;
            }
        }

        Debug.LogWarning($"[PrefabsPool] Could not determine PrefabType for {obj.name}. Defaulting to None.");
        return PrefabType.None;
    }
    #endregion

    #region Private Methods - Pool Reuse
    /// <summary>
    /// 가장 오래된 활성 오브젝트를 강제 회수합니다
    /// </summary>
    /// <param name="prefabType">회수할 오브젝트 타입 (같은 타입 우선)</param>
    /// <returns>회수된 오브젝트, 없으면 null</returns>
    private GameObject RecycleOldestObject(PrefabType prefabType)
    {
        if (_activeObjectsWithTime.Count == 0)
            return null;

        // 같은 타입 중 가장 오래된 것 우선 검색
        GameObject oldestSameType = null;
        float oldestSameTypeTime = float.MaxValue;

        // 전체 중 가장 오래된 것 (백업)
        GameObject oldestOverall = null;
        float oldestOverallTime = float.MaxValue;

        foreach (var kvp in _activeObjectsWithTime)
        {
            GameObject obj = kvp.Key;
            float spawnTime = kvp.Value;

            PrefabType objType = GetPrefabTypeFromGameObject(obj);

            if (objType == prefabType && spawnTime < oldestSameTypeTime)
            {
                oldestSameType = obj;
                oldestSameTypeTime = spawnTime;
            }

            if (spawnTime < oldestOverallTime)
            {
                oldestOverall = obj;
                oldestOverallTime = spawnTime;
            }
        }

        // 같은 타입이 있으면 우선, 없으면 전체에서 가장 오래된 것
        GameObject targetObject = oldestSameType ?? oldestOverall;

        if (targetObject != null)
        {
            // 강제 반환 (풀로 자동 반환됨)
            ReturnObject(targetObject);

            // 풀에서 다시 가져오기
            PrefabType targetType = GetPrefabTypeFromGameObject(targetObject);
            Queue<GameObject> pool = GetOrCreatePool(targetType);
            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }
        }

        return null;
    }
    #endregion


}