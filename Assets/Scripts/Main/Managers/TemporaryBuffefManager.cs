using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 임시 버프 시간 관리자
/// </summary>
public class TemporaryBuffManager : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("Settings")]
    [Header("Buff Management")]
    [SerializeField] private bool _enableDebugLogging = true;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int ActiveBuffCount => _activeBuffs?.Count ?? 0;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Dictionary<UpgradeDataSO, float> ActiveBuffs => _activeBuffs;
    #endregion

    #region Events
    /// <summary>버프 시작 이벤트</summary>
    public event Action<UpgradeDataSO> OnBuffStarted;

    /// <summary>버프 만료 이벤트</summary>
    public event Action<UpgradeDataSO> OnBuffExpired;
    #endregion

    #region Private Fields
    private Dictionary<UpgradeDataSO, float> _activeBuffs = new Dictionary<UpgradeDataSO, float>();
    private Dictionary<UpgradeDataSO, Coroutine> _buffCoroutines = new Dictionary<UpgradeDataSO, Coroutine>();
    private PlayerWeaponController _playerWeapon;
    private PlayerBattleEntity _playerEntity;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // 딕셔너리 초기화
        if (_activeBuffs == null)
            _activeBuffs = new Dictionary<UpgradeDataSO, float>();

        if (_buffCoroutines == null)
            _buffCoroutines = new Dictionary<UpgradeDataSO, Coroutine>();
    }

    private void Start()
    {
        // 플레이어 참조 자동 검색
        if (_playerWeapon == null)
            _playerWeapon = FindFirstObjectByType<PlayerWeaponController>();

        if (_playerEntity == null)
            _playerEntity = FindFirstObjectByType<PlayerBattleEntity>();

        if (_playerWeapon == null || _playerEntity == null)
        {
            Debug.LogError("[TemporaryBuffManager] Player references not found!", this);
        }
    }

    private void OnDestroy()
    {
        // 모든 활성 코루틴 정리
        foreach (var coroutine in _buffCoroutines.Values)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }

        _buffCoroutines.Clear();
        _activeBuffs.Clear();
    }
    #endregion

    #region Public Methods - Buff Control
    /// <summary>임시 버프 시작</summary>
    /// <param name="upgradeData">업그레이드 데이터</param>
    /// <param name="weapon">플레이어 무기 컨트롤러</param>
    /// <param name="player">플레이어 배틀 엔티티</param>
    public void StartBuff(UpgradeDataSO upgradeData, PlayerWeaponController weapon, PlayerBattleEntity player)
    {
        if (upgradeData?.Effect == null || !upgradeData.Effect.IsTemporary)
        {
            Debug.LogWarning("[TemporaryBuffManager] Invalid temporary buff data", this);
            return;
        }

        // 기존 버프가 있으면 제거
        if (IsBuffActive(upgradeData))
        {
            RemoveBuff(upgradeData);
        }

        // 효과 적용
        upgradeData.Effect.ApplyUpgrade(weapon, player);

        // 버프 등록 및 타이머 시작
        float duration = upgradeData.Effect.BuffDuration;
        _activeBuffs[upgradeData] = Time.time + duration;

        Coroutine timerCoroutine = StartCoroutine(BuffTimerCoroutine(upgradeData, duration));
        _buffCoroutines[upgradeData] = timerCoroutine;

        OnBuffStarted?.Invoke(upgradeData);

        if (_enableDebugLogging)
            Debug.Log($"[TemporaryBuffManager] Started buff: {upgradeData.DisplayName} ({duration}s)", this);
    }

    /// <summary>버프 수동 제거</summary>
    /// <param name="upgradeData">제거할 업그레이드 데이터</param>
    public void RemoveBuff(UpgradeDataSO upgradeData)
    {
        if (!IsBuffActive(upgradeData))
            return;

        // 코루틴 정지
        if (_buffCoroutines.ContainsKey(upgradeData))
        {
            StopCoroutine(_buffCoroutines[upgradeData]);
            _buffCoroutines.Remove(upgradeData);
        }

        // 효과 제거
        if (upgradeData?.Effect != null)
        {
            upgradeData.Effect.RemoveUpgrade(_playerWeapon, _playerEntity);
        }

        // 등록 해제
        _activeBuffs.Remove(upgradeData);

        if (_enableDebugLogging)
            Debug.Log($"[TemporaryBuffManager] Removed buff: {upgradeData?.DisplayName}", this);
    }

    /// <summary>버프 활성 상태 확인</summary>
    /// <param name="upgradeData">확인할 업그레이드 데이터</param>
    /// <returns>활성 중이면 true</returns>
    public bool IsBuffActive(UpgradeDataSO upgradeData)
    {
        return upgradeData != null && _activeBuffs.ContainsKey(upgradeData);
    }

    /// <summary>모든 버프 제거</summary>
    public void ClearAllBuffs()
    {
        var activeBuffs = new List<UpgradeDataSO>(_activeBuffs.Keys);

        foreach (var upgradeData in activeBuffs)
        {
            RemoveBuff(upgradeData);
        }

        if (_enableDebugLogging)
            Debug.Log("[TemporaryBuffManager] Cleared all buffs", this);
    }
    #endregion

    #region Public Methods - Initialization
    /// <summary>플레이어 참조 설정</summary>
    /// <param name="weapon">플레이어 무기 컨트롤러</param>
    /// <param name="player">플레이어 배틀 엔티티</param>
    public void SetReferences(PlayerWeaponController weapon, PlayerBattleEntity player)
    {
        _playerWeapon = weapon;
        _playerEntity = player;

        if (_enableDebugLogging)
        {
            bool weaponValid = _playerWeapon != null;
            bool playerValid = _playerEntity != null;
            Debug.Log($"[TemporaryBuffManager] References set - Weapon: {weaponValid}, Player: {playerValid}", this);
        }
    }
    #endregion

    #region Private Methods - Buff Management
    /// <summary>버프 타이머 코루틴</summary>
    /// <param name="upgradeData">업그레이드 데이터</param>
    /// <param name="duration">버프 지속 시간</param>
    /// <returns>코루틴</returns>
    private IEnumerator BuffTimerCoroutine(UpgradeDataSO upgradeData, float duration)
    {
        yield return new WaitForSeconds(duration);

        // 시간 만료시 버프 제거
        ExpireBuff(upgradeData);
    }

    /// <summary>버프 만료 처리</summary>
    /// <param name="upgradeData">만료된 업그레이드 데이터</param>
    private void ExpireBuff(UpgradeDataSO upgradeData)
    {
        if (!IsBuffActive(upgradeData))
            return;

        // 효과 제거
        if (upgradeData?.Effect != null)
        {
            upgradeData.Effect.RemoveUpgrade(_playerWeapon, _playerEntity);
        }

        // 등록 해제
        _activeBuffs.Remove(upgradeData);
        _buffCoroutines.Remove(upgradeData);

        // 이벤트 발생
        OnBuffExpired?.Invoke(upgradeData);

        if (_enableDebugLogging)
            Debug.Log($"[TemporaryBuffManager] Buff expired: {upgradeData.DisplayName}", this);
    }
    #endregion
}