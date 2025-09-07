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
    /// 투사체가 소멸될 때 발생하는 이벤트
    /// </summary>
    event Action<IProjectile> OnProjectileDestroyed;

    /// <summary>
    /// 투사체 업데이트 시 발생하는 이벤트 (매 프레임)
    /// </summary>
    event Action<IProjectile> OnProjectileUpdate;
    #endregion
}