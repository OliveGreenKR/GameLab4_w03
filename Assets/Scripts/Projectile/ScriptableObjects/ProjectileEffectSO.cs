using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 투사체 이펙트의 기본 ScriptableObject 추상 클래스
/// IProjectileEffect 인터페이스를 구현하여 기존 시스템과 호환
/// </summary>
public abstract class ProjectileEffectSO : ScriptableObject, IProjectileEffect
{
    #region Serialized Fields
    [Header("Effect Settings")]
    [InfoBox("낮은 우선순위가 먼저 적용됩니다")]
    [PropertyRange(0, 100)]
    [SerializeField] protected int _priority = 50;

    [Header("Effect Info")]
    [SerializeField, TextArea(2, 4)]
    protected string _description = "이펙트 설명을 입력하세요";
    #endregion

    #region IProjectileEffect Implementation
    /// <summary>
    /// 이펙트 우선순위 (낮을수록 먼저 적용)
    /// </summary>
    public int Priority => _priority;

    /// <summary>
    /// 투사체에 이펙트를 적용합니다
    /// </summary>
    /// <param name="projectile">효과를 적용할 투사체</param>
    public abstract void AttachToProjectile(IProjectile projectile);

    /// <summary>
    /// 투사체 소멸시 정리 작업 (이벤트 구독 해제 등)
    /// </summary>
    /// <param name="projectile">정리할 투사체</param>
    public abstract void DetachFromProjectile(IProjectile projectile);
    #endregion

    #region Properties
    [ShowInInspector, ReadOnly]
    public string EffectDescription => _description;
    #endregion

    #region Unity Lifecycle
    protected virtual void OnValidate()
    {
        // Inspector에서 값 변경 시 검증
        _priority = Mathf.Clamp(_priority, 0, 100);
    }
    #endregion

    #region Protected Methods - Utility
    /// <summary>
    /// 투사체 유효성 검사
    /// </summary>
    /// <param name="projectile">검사할 투사체</param>
    /// <returns>유효하면 true</returns>
    protected bool ValidateProjectile(IProjectile projectile)
    {
        if (projectile == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Projectile is null", this);
            return false;
        }

        if (projectile.GameObject == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Projectile GameObject is null", this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 디버그 로그 (에디터에서만)
    /// </summary>
    /// <param name="message">로그 메시지</param>
    /// <param name="projectile">관련 투사체</param>
    protected void LogEffect(string message, IProjectile projectile = null)
    {
#if UNITY_EDITOR
        string fullMessage = $"[{GetType().Name}] {message}";
        if (projectile != null)
        {
            Debug.Log(fullMessage, projectile.GameObject);
        }
        else
        {
            Debug.Log(fullMessage, this);
        }
#endif
    }
    #endregion
}