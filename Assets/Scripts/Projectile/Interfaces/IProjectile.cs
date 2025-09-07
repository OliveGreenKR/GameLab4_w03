using System;
using UnityEngine;

/// <summary>
/// 투사체 타입 식별자
/// </summary>
public enum ProjectileType
{
    BasicProjectile,
    ExplosiveProjectile,
    PiercingProjectile,
    HomingProjectile
}

/// <summary>
/// 순수 투사체 인터페이스
/// </summary>
public interface IProjectile
{
    #region Identification
    /// <summary>
    /// 투사체 타입 식별자
    /// </summary>
    ProjectileType ProjectileType { get; }

    /// <summary>
    /// 투사체의 Transform
    /// </summary>
    Transform Transform { get; }

    /// <summary>
    /// 투사체의 GameObject
    /// </summary>
    GameObject GameObject { get; }
    #endregion

    #region State Properties
    /// <summary>
    /// 투사체 활성 상태
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// 남은 생명 시간
    /// </summary>
    float RemainingLifetime { get; }

    /// <summary>
    /// 전진 속도
    /// </summary>
    float ForwardSpeed { get; }
    #endregion

    #region Projectile Specific Properties
    /// <summary>
    /// 관통 횟수
    /// </summary>
    int PierceCount { get; }

    /// <summary>
    /// 데미지 배율
    /// </summary>
    float DamageMultiplier { get; }

    /// <summary>
    /// 분열 횟수
    /// </summary>
    int SplitCount { get; }

    /// <summary>
    /// 분열 가능 횟수
    /// </summary>
    int SplitAvailableCount { get; }      

    /// <summary>
    /// 분열 투사체 개수
    /// </summary>
    int SplitProjectileCount { get; }
    /// <summary>
    /// 분열 각도 범위
    /// </summary>
    float SplitAngleRange { get; }        

    /// <summary>
    /// 투사체 소유자 (Pool 접근용)
    /// </summary>
    ProjectileLauncher Owner { get; }
    #endregion

    #region Initialization
    /// <summary>
    /// 투사체 초기화
    /// </summary>
    /// <param name="lifetimeSeconds">생명 시간 (초)</param>
    /// <param name="forwardSpeed">전진 속도</param>
    void Initialize(float lifetimeSeconds = -1f, float forwardSpeed = -1f);
    #endregion

    #region Projectile Stat Modification
    /// <summary>
    /// 관통 횟수 설정
    /// </summary>
    /// <param name="pierceCount">관통 횟수</param>
    void SetPierceCount(int pierceCount);

    /// <summary>
    /// 관통 횟수 수정
    /// </summary>
    /// <param name="delta">변화량</param>
    void ModifyPierceCount(int delta);

    /// <summary>
    /// 데미지 배율 설정
    /// </summary>
    /// <param name="multiplier">데미지 배율</param>
    void SetDamageMultiplier(float multiplier);

    /// <summary>
    /// 데미지 배율 수정
    /// </summary>
    /// <param name="multiplier">곱할 배율</param>
    void ModifyDamageMultiplier(float multiplier);

    /// <summary>
    /// 분열 가능 횟수 설정
    /// </summary>
    /// <param name="availableCount">분열 가능 횟수</param>
    void SetSplitAvailableCount(int availableCount);

    /// <summary>
    /// 분열 가능 횟수 수정
    /// </summary>
    /// <param name="delta">변화량</param>
    void ModifySplitAvailableCount(int delta);

    /// <summary>
    /// 분열 투사체 개수 설정
    /// </summary>
    /// <param name="projectileCount">분열 투사체 개수</param>
    void SetSplitProjectileCount(int projectileCount);

    /// <summary>
    /// 분열 투사체 개수 수정
    /// </summary>
    /// <param name="delta">변화량</param>
    void ModifySplitProjectileCount(int delta);

    /// <summary>
    /// 분열 각도 범위 설정
    /// </summary>
    /// <param name="angleRange">분열 각도 범위</param>
    void SetSplitAngleRange(float angleRange);

    /// <summary>
    /// 분열 각도 범위 수정
    /// </summary>
    /// <param name="delta">변화량</param>
    void ModifySplitAngleRange(float delta);

    /// <summary>
    /// 투사체 소유자 설정
    /// </summary>
    /// <param name="owner">소유자 ProjectileLauncher</param>
    void SetOwner(ProjectileLauncher owner);

    /// <summary>
    /// 자신을 복제하여 새로운 투사체 생성
    /// </summary>
    /// <param name="worldPosition">복제본 위치</param>
    /// <param name="worldRotation">복제본 회전</param>
    /// <returns>복제된 투사체</returns>
    IProjectile CreateClone(Vector3 worldPosition, Quaternion worldRotation);
    #endregion

    #region Lifecycle
    /// <summary>
    /// 투사체 소멸
    /// </summary>
    void DestroyProjectile();
    #endregion

    #region Events
    /// <summary>
    /// 투사체가 활성화될 때 발생하는 이벤트
    /// </summary>
    event Action<IProjectile> OnProjectileActivated;

    /// <summary>
    /// 투사체가 충돌했을 때 발생하는 이벤트
    /// </summary>
    event Action<IProjectile, Collider> OnProjectileHit;

    /// <summary>
    /// 투사체가 충돌후 발생하는 이벤트
    /// </summary>
    event Action<IProjectile, Collider> AfterProjectileHit;

    /// <summary>
    /// 투사체가 소멸되기 직전에 발생하는 이벤트 (분열 처리용)
    /// </summary>
    event Action<IProjectile> BeforeProjectileDestroyed;

    /// <summary>
    /// 투사체가 소멸될 때 발생하는 이벤트
    /// </summary>
    event Action<IProjectile> OnProjectileDestroyed;

    /// <summary>
    /// 투사체 업데이트 시 발생하는 이벤트 (매 프레임)
    /// </summary>
    event Action<IProjectile> OnProjectileUpdate;
    #endregion
}