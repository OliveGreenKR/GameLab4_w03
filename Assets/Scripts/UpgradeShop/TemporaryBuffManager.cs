using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>임시 버프의 시간 관리 및 자동 제거 처리</summary>
public class TemporaryBuffManager
{
    #region Constants
    private const int MAX_CONCURRENT_BUFFS = 50;
    #endregion

    #region Properties
    public int ActiveBuffCount => _activeBuffs.Count;
    public IReadOnlyDictionary<string, BuffInfo> ActiveBuffs => _activeBuffs;
    public bool EnableDebugLogs { get; set; } = true;
    #endregion

    #region Events
    public event Action<string, UpgradeDataSO> OnBuffStarted;
    public event Action<string, UpgradeDataSO> OnBuffExpired;
    public event Action<string, UpgradeDataSO> OnBuffRemoved;
    #endregion

    #region Private Fields
    private Dictionary<string, BuffInfo> _activeBuffs = new Dictionary<string, BuffInfo>();
    private MonoBehaviour _coroutineRunner;
    #endregion

    #region Nested Types
    [System.Serializable]
    public struct BuffInfo
    {
        public UpgradeDataSO upgradeData;
        public List<IUpgradable> targets;
        public float startTime;
        public float expireTime;
        public bool isActive;
    }
    #endregion

    #region Constructor
    /// <summary>TemporaryBuffManager 생성자</summary>
    /// <param name="coroutineRunner">코루틴 실행용 MonoBehaviour</param>
    public TemporaryBuffManager(MonoBehaviour coroutineRunner)
    {
        _coroutineRunner = coroutineRunner ?? throw new ArgumentNullException(nameof(coroutineRunner));
        _activeBuffs = new Dictionary<string, BuffInfo>();
    }
    #endregion

    #region Public Methods - Buff Control
    /// <summary>임시 버프 시작</summary>
    /// <param name="upgradeData">업그레이드 데이터</param>
    /// <param name="targets">적용 대상 목록</param>
    /// <returns>생성된 버프 ID (실패 시 null)</returns>
    public string StartBuff(UpgradeDataSO upgradeData, List<IUpgradable> targets)
    {
        if (upgradeData == null || targets == null || targets.Count == 0)
            return null;

        if (!upgradeData.IsTemporary)
            return null;

        if (_activeBuffs.Count >= MAX_CONCURRENT_BUFFS)
            return null;

        string buffId = GenerateBuffId(upgradeData);

        // 중복 ID 확인 및 재생성
        while (_activeBuffs.ContainsKey(buffId))
        {
            buffId = GenerateBuffId(upgradeData);
        }

        float currentTime = Time.time;
        BuffInfo buffInfo = new BuffInfo
        {
            upgradeData = upgradeData,
            targets = new List<IUpgradable>(targets),
            startTime = currentTime,
            expireTime = currentTime + upgradeData.DurationSeconds,
            isActive = true
        };

        _activeBuffs[buffId] = buffInfo;
        ApplyBuffToTargets(upgradeData, targets);
        _coroutineRunner.StartCoroutine(BuffTimerCoroutine(buffId, upgradeData.DurationSeconds));

        OnBuffStarted?.Invoke(buffId, upgradeData);
        return buffId;
    }

    /// <summary>임시 버프 즉시 제거</summary>
    /// <param name="buffId">제거할 버프 ID</param>
    /// <returns>제거 성공 여부</returns>
    public bool RemoveBuff(string buffId)
    {
        if (!_activeBuffs.TryGetValue(buffId, out BuffInfo buffInfo))
            return false;

        RemoveBuffFromTargets(buffInfo.upgradeData, buffInfo.targets);
        CleanupBuffData(buffId);

        OnBuffRemoved?.Invoke(buffId, buffInfo.upgradeData);
        return true;
    }

    /// <summary>특정 타입의 모든 버프 제거</summary>
    /// <param name="upgradeType">제거할 업그레이드 타입</param>
    /// <returns>제거된 버프 개수</returns>
    public int RemoveBuffsByType(UpgradeType upgradeType)
    {
        List<string> buffsToRemove = new List<string>();

        foreach (var kvp in _activeBuffs)
        {
            if (kvp.Value.upgradeData.UpgradeType == upgradeType)
                buffsToRemove.Add(kvp.Key);
        }

        foreach (string buffId in buffsToRemove)
            RemoveBuff(buffId);

        return buffsToRemove.Count;
    }

    /// <summary>모든 활성 버프 제거</summary>
    public void RemoveAllBuffs()
    {
        List<string> allBuffIds = new List<string>(_activeBuffs.Keys);

        foreach (string buffId in allBuffIds)
            RemoveBuff(buffId);
    }
    #endregion

    #region Public Methods - Buff Information
    /// <summary>버프 활성화 여부 확인</summary>
    /// <param name="buffId">확인할 버프 ID</param>
    /// <returns>활성화 여부</returns>
    public bool IsBuffActive(string buffId)
    {
        return _activeBuffs.ContainsKey(buffId) && _activeBuffs[buffId].isActive;
    }

    /// <summary>버프 남은 시간 조회</summary>
    /// <param name="buffId">조회할 버프 ID</param>
    /// <returns>남은 시간 (초), 버프가 없으면 -1</returns>
    public float GetBuffRemainingTime(string buffId)
    {
        if (!_activeBuffs.TryGetValue(buffId, out BuffInfo buffInfo))
            return -1f;

        float remainingTime = buffInfo.expireTime - Time.time;
        return Mathf.Max(0f, remainingTime);
    }

    /// <summary>특정 타입의 활성 버프 개수 조회</summary>
    /// <param name="upgradeType">조회할 업그레이드 타입</param>
    /// <returns>활성 버프 개수</returns>
    public int GetActiveBuffCountByType(UpgradeType upgradeType)
    {
        int count = 0;
        foreach (var buffInfo in _activeBuffs.Values)
        {
            if (buffInfo.upgradeData.UpgradeType == upgradeType && buffInfo.isActive)
                count++;
        }
        return count;
    }
    #endregion

    #region Private Methods - Buff Management
    private string GenerateBuffId(UpgradeDataSO upgradeData)
    {
        return $"{upgradeData.UpgradeType}_{upgradeData.GetInstanceID()}_{Time.time:F3}";
    }

    private System.Collections.IEnumerator BuffTimerCoroutine(string buffId, float duration)
    {
        yield return new WaitForSeconds(duration);
        ExpireBuff(buffId);
    }

    private void ExpireBuff(string buffId)
    {
        if (!_activeBuffs.TryGetValue(buffId, out BuffInfo buffInfo))
            return;

        RemoveBuffFromTargets(buffInfo.upgradeData, buffInfo.targets);
        CleanupBuffData(buffId);

        OnBuffExpired?.Invoke(buffId, buffInfo.upgradeData);

        if (EnableDebugLogs)
            Debug.Log($"Buff expired: {buffId} ({buffInfo.upgradeData.UpgradeType})");
    }

    private void ApplyBuffToTargets(UpgradeDataSO upgradeData, List<IUpgradable> targets)
    {
        foreach (IUpgradable target in targets)
        {
            if (target != null && target.CanReceiveUpgrade(upgradeData.UpgradeType))
            {
                target.ApplyUpgrade(upgradeData.UpgradeType, upgradeData.UpgradeValue);
            }
        }

        if (EnableDebugLogs)
            Debug.Log($"Applied buff {upgradeData.UpgradeType} to {targets.Count} targets");
    }

    private void RemoveBuffFromTargets(UpgradeDataSO upgradeData, List<IUpgradable> targets)
    {
        foreach (IUpgradable target in targets)
        {
            if (target != null)
            {
                target.RemoveUpgrade(upgradeData.UpgradeType, upgradeData.UpgradeValue);
            }
        }

        if (EnableDebugLogs)
            Debug.Log($"Removed buff {upgradeData.UpgradeType} from {targets.Count} targets");
    }
    #endregion

    #region Private Methods - Cleanup
    private void CleanupBuffData(string buffId)
    {
        if (_activeBuffs.ContainsKey(buffId))
        {
            _activeBuffs.Remove(buffId);
        }
    }
    #endregion
}