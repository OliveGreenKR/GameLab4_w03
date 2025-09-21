using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>임시 업그레이드 데이터 구조체</summary>
[Serializable]
public struct TemporaryUpgrade
{
    public string id;
    public UpgradeType upgradeType;
    public float value;
    public float remainingTime;
    public Coroutine timerCoroutine;

    public TemporaryUpgrade(string id, UpgradeType upgradeType, float value, float duration)
    {
        this.id = id;
        this.upgradeType = upgradeType;
        this.value = value;
        this.remainingTime = duration;
        this.timerCoroutine = null;
    }
}

/// <summary>
/// 임시 업그레이드 생명주기 관리자
/// string ID 기반으로 임시 업그레이드를 추적하고 만료시 자동 제거
/// </summary>
public class TemporaryUpgradeManager : MonoBehaviour
{
    #region Serialized Fields
    [Header("Debug Settings")]
    [SerializeField] private bool _enableDebugLogs = true;
    #endregion

    #region Properties
    public int ActiveUpgradeCount => _activeUpgrades?.Count ?? 0;
    public bool HasActiveUpgrades => ActiveUpgradeCount > 0;
    #endregion

    #region Events
    /// <summary>임시 업그레이드가 만료되었을 때 발생하는 이벤트</summary>
    /// <param name="upgradeId">만료된 업그레이드 ID</param>
    /// <param name="upgradeType">업그레이드 타입</param>
    /// <param name="value">업그레이드 값</param>
    public event Action<string, UpgradeType, float> OnUpgradeExpired;

    /// <summary>임시 업그레이드가 추가되었을 때 발생하는 이벤트</summary>
    /// <param name="upgradeId">추가된 업그레이드 ID</param>
    /// <param name="upgradeType">업그레이드 타입</param>
    /// <param name="duration">지속 시간</param>
    public event Action<string, UpgradeType, float> OnUpgradeAdded;

    /// <summary>임시 업그레이드가 수동으로 제거되었을 때 발생하는 이벤트</summary>
    /// <param name="upgradeId">제거된 업그레이드 ID</param>
    /// <param name="upgradeType">업그레이드 타입</param>
    public event Action<string, UpgradeType> OnUpgradeRemoved;
    #endregion

    #region Private Fields
    private Dictionary<string, TemporaryUpgrade> _activeUpgrades = new Dictionary<string, TemporaryUpgrade>();
    private readonly string _idPrefix = "temp_upgrade_";
    private int _nextUpgradeIdCounter = 0;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        _activeUpgrades = new Dictionary<string, TemporaryUpgrade>();
        _nextUpgradeIdCounter = 0;

        if (_enableDebugLogs)
        {
            Debug.Log("[TemporaryUpgradeManager] Initialized", this);
        }
    }

    private void OnDestroy()
    {
        // 모든 활성 코루틴 정리
        foreach (var upgrade in _activeUpgrades.Values)
        {
            if (upgrade.timerCoroutine != null)
            {
                StopCoroutine(upgrade.timerCoroutine);
            }
        }

        // 이벤트 정리
        OnUpgradeExpired = null;
        OnUpgradeAdded = null;
        OnUpgradeRemoved = null;

        _activeUpgrades.Clear();

        if (_enableDebugLogs)
        {
            Debug.Log("[TemporaryUpgradeManager] Destroyed and cleaned up", this);
        }
    }
    #endregion

    #region Public Methods - Upgrade Management
    /// <summary>임시 업그레이드 추가</summary>
    /// <param name="upgradeType">업그레이드 타입</param>
    /// <param name="value">업그레이드 값</param>
    /// <param name="durationSeconds">지속 시간 (초)</param>
    /// <returns>생성된 업그레이드 ID</returns>
    public string AddTemporaryUpgrade(UpgradeType upgradeType, float value, float durationSeconds)
    {
        if (durationSeconds <= 0f)
        {
            Debug.LogWarning("[TemporaryUpgradeManager] Invalid duration for temporary upgrade", this);
            return null;
        }

        string upgradeId = GenerateUpgradeId();
        TemporaryUpgrade upgrade = new TemporaryUpgrade(upgradeId, upgradeType, value, durationSeconds);

        // 타이머 코루틴 시작
        upgrade.timerCoroutine = StartCoroutine(UpgradeTimerCoroutine(upgradeId));

        _activeUpgrades[upgradeId] = upgrade;

        OnUpgradeAdded?.Invoke(upgradeId, upgradeType, durationSeconds);
        LogUpgradeAction("Added", upgradeId, upgradeType, value);

        return upgradeId;
    }

    /// <summary>임시 업그레이드 수동 제거</summary>
    /// <param name="upgradeId">제거할 업그레이드 ID</param>
    /// <returns>제거 성공 여부</returns>
    public bool RemoveTemporaryUpgrade(string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId) || !_activeUpgrades.ContainsKey(upgradeId))
        {
            return false;
        }

        TemporaryUpgrade upgrade = _activeUpgrades[upgradeId];

        // 코루틴 정지
        if (upgrade.timerCoroutine != null)
        {
            StopCoroutine(upgrade.timerCoroutine);
        }

        _activeUpgrades.Remove(upgradeId);

        OnUpgradeRemoved?.Invoke(upgradeId, upgrade.upgradeType);
        LogUpgradeAction("Removed", upgradeId, upgrade.upgradeType, upgrade.value);

        return true;
    }

    /// <summary>특정 타입의 모든 임시 업그레이드 제거</summary>
    /// <param name="upgradeType">제거할 업그레이드 타입</param>
    /// <returns>제거된 업그레이드 개수</returns>
    public int RemoveAllTemporaryUpgrades(UpgradeType upgradeType)
    {
        List<string> upgradeIdsToRemove = new List<string>();

        foreach (var kvp in _activeUpgrades)
        {
            if (kvp.Value.upgradeType == upgradeType)
            {
                upgradeIdsToRemove.Add(kvp.Key);
            }
        }

        int removedCount = 0;
        foreach (string upgradeId in upgradeIdsToRemove)
        {
            if (RemoveTemporaryUpgrade(upgradeId))
            {
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            LogUpgradeAction($"Removed All ({removedCount})", "", upgradeType);
        }

        return removedCount;
    }

    /// <summary>모든 임시 업그레이드 제거</summary>
    public void ClearAllTemporaryUpgrades()
    {
        List<string> allUpgradeIds = new List<string>(_activeUpgrades.Keys);
        int totalRemoved = allUpgradeIds.Count;

        foreach (string upgradeId in allUpgradeIds)
        {
            RemoveTemporaryUpgrade(upgradeId);
        }

        if (totalRemoved > 0)
        {
            LogUpgradeAction($"Cleared All ({totalRemoved})", "", (UpgradeType)0);
        }
    }
    #endregion

    #region Public Methods - Query
    /// <summary>특정 업그레이드 존재 여부 확인</summary>
    /// <param name="upgradeId">확인할 업그레이드 ID</param>
    /// <returns>존재하면 true</returns>
    public bool HasUpgrade(string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId))
            return false;

        return _activeUpgrades.ContainsKey(upgradeId);
    }

    /// <summary>특정 타입의 활성 임시 업그레이드 개수 조회</summary>
    /// <param name="upgradeType">조회할 업그레이드 타입</param>
    /// <returns>활성 업그레이드 개수</returns>
    public int GetActiveUpgradeCount(UpgradeType upgradeType)
    {
        int count = 0;

        foreach (var upgrade in _activeUpgrades.Values)
        {
            if (upgrade.upgradeType == upgradeType)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>특정 업그레이드의 남은 시간 조회</summary>
    /// <param name="upgradeId">조회할 업그레이드 ID</param>
    /// <returns>남은 시간 (초), 존재하지 않으면 -1</returns>
    public float GetRemainingTime(string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId) || !_activeUpgrades.ContainsKey(upgradeId))
        {
            return -1f;
        }

        return _activeUpgrades[upgradeId].remainingTime;
    }

    /// <summary>특정 타입의 모든 활성 업그레이드 ID 목록 반환</summary>
    /// <param name="upgradeType">조회할 업그레이드 타입</param>
    /// <returns>업그레이드 ID 목록</returns>
    public List<string> GetActiveUpgradeIds(UpgradeType upgradeType)
    {
        List<string> upgradeIds = new List<string>();

        foreach (var kvp in _activeUpgrades)
        {
            if (kvp.Value.upgradeType == upgradeType)
            {
                upgradeIds.Add(kvp.Key);
            }
        }

        return upgradeIds;
    }
    #endregion

    #region Private Methods - Timer Management
    private IEnumerator UpgradeTimerCoroutine(string upgradeId)
    {
        if (!_activeUpgrades.ContainsKey(upgradeId))
            yield break;

        TemporaryUpgrade upgrade = _activeUpgrades[upgradeId];
        float remainingTime = upgrade.remainingTime;

        while (remainingTime > 0f)
        {
            yield return null;
            remainingTime -= Time.deltaTime;

            // 남은 시간 업데이트
            upgrade.remainingTime = remainingTime;
            _activeUpgrades[upgradeId] = upgrade;
        }

        // 시간 만료 - 업그레이드 제거
        if (_activeUpgrades.ContainsKey(upgradeId))
        {
            TemporaryUpgrade expiredUpgrade = _activeUpgrades[upgradeId];
            _activeUpgrades.Remove(upgradeId);

            OnUpgradeExpired?.Invoke(upgradeId, expiredUpgrade.upgradeType, expiredUpgrade.value);
            LogUpgradeAction("Expired", upgradeId, expiredUpgrade.upgradeType, expiredUpgrade.value);
        }
    }

    private string GenerateUpgradeId()
    {
        string id = _idPrefix + _nextUpgradeIdCounter.ToString();
        _nextUpgradeIdCounter++;
        return id;
    }

    private void LogUpgradeAction(string action, string upgradeId, UpgradeType upgradeType, float value = 0f)
    {
        if (!_enableDebugLogs)
            return;

        string message = $"[TemporaryUpgradeManager] {action}";

        if (!string.IsNullOrEmpty(upgradeId))
            message += $" ID:{upgradeId}";

        message += $" Type:{upgradeType}";

        if (value != 0f)
            message += $" Value:{value:F2}";

        Debug.Log(message, this);
    }
    #endregion
}