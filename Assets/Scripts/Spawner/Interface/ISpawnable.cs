using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스폰 시스템에서 관리하는 스탯 타입들
/// </summary>
public enum SpawnStatType
{
    Health,
    MoveSpeed,
    Attack
}

/// <summary>
/// 스폰 가능한 객체가 구현해야 하는 인터페이스
/// 스폰 매니저와의 소통을 위한 순수 스폰 인터페이스
/// </summary>
public interface ISpawnable
{
    #region Properties
    /// <summary>
    /// 스폰 객체의 Transform
    /// </summary>
    Transform Transform { get; }

    /// <summary>
    /// 스폰 객체의 GameObject
    /// </summary>
    GameObject GameObject { get; }

    /// <summary>
    /// 현재 스폰된 상태인지 여부
    /// </summary>
    bool IsSpawned { get; }

    /// <summary>
    /// 스폰 가능한 상태인지 여부
    /// </summary>
    bool CanBeSpawned { get; }
    #endregion

    #region Position and Rotation
    /// <summary>
    /// 스폰 위치 설정
    /// </summary>
    /// <param name="worldPosition">월드 좌표 위치</param>
    void SetSpawnPosition(Vector3 worldPosition);

    /// <summary>
    /// 스폰 회전 설정
    /// </summary>
    /// <param name="worldRotation">월드 좌표 회전</param>
    void SetSpawnRotation(Quaternion worldRotation);

    /// <summary>
    /// 스폰 위치와 회전 동시 설정
    /// </summary>
    /// <param name="worldPosition">월드 좌표 위치</param>
    /// <param name="worldRotation">월드 좌표 회전</param>
    void SetSpawnTransform(Vector3 worldPosition, Quaternion worldRotation);
    #endregion

    #region Spawn Stats Management
    /// <summary>
    /// 특정 스폰 스탯 값 조회
    /// </summary>
    /// <param name="statType">스탯 타입</param>
    /// <returns>스탯 값</returns>
    float GetSpawnStat(SpawnStatType statType);

    /// <summary>
    /// 특정 스폰 스탯 값 설정
    /// </summary>
    /// <param name="statType">스탯 타입</param>
    /// <param name="value">설정할 값</param>
    void SetSpawnStat(SpawnStatType statType, float value);

    /// <summary>
    /// 여러 스폰 스탯을 한번에 적용
    /// </summary>
    /// <param name="stats">적용할 스탯 딕셔너리</param>
    void ApplySpawnStats(Dictionary<SpawnStatType, float> stats);
    #endregion

    #region Spawn Lifecycle
    /// <summary>
    /// 스폰 전 초기화 작업
    /// </summary>
    void PreSpawnInitialize();

    /// <summary>
    /// 스폰 완료 시 호출
    /// </summary>
    /// <param name="spawner">스폰을 실행한 스폰너 객체</param>
    void OnSpawned(object spawner = null);

    /// <summary>
    /// 스폰 후 추가 초기화 작업
    /// </summary>
    void PostSpawnInitialize();

    /// <summary>
    /// 디스폰 시 호출
    /// </summary>
    void OnDespawned();

    /// <summary>
    /// 스폰 기본값으로 리셋
    /// </summary>
    void ResetToSpawnDefaults();
    #endregion

    #region Spawner Management
    /// <summary>
    /// 스폰너 설정
    /// </summary>
    /// <param name="spawner">스폰너 객체</param>
    void SetSpawner(object spawner);

    /// <summary>
    /// 현재 스폰너 조회
    /// </summary>
    /// <returns>스폰너 객체</returns>
    object GetSpawner();
    #endregion

    #region Events
    /// <summary>
    /// 스폰 완료 시 발생하는 이벤트
    /// </summary>
    event Action<ISpawnable> OnSpawnCompleted;

    /// <summary>
    /// 디스폰 시작 시 발생하는 이벤트
    /// </summary>
    event Action<ISpawnable> OnDespawnStarted;

    /// <summary>
    /// 스폰 스탯 변경 시 발생하는 이벤트
    /// </summary>
    event Action<ISpawnable, SpawnStatType, float> OnSpawnStatChanged;
    #endregion
}