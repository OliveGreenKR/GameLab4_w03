using Sirenix.OdinInspector;
using System.Text;
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

    #region Public Methods - Description Generation
    /// <summary>이펙트 속성을 기반으로 설명 자동 생성</summary>
    [Button("Generate Description", ButtonSizes.Medium)]
    [GUIColor(0.7f, 1f, 0.7f)]
    public void GenerateDescription()
    {
        _description = CreateAutoDescription();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    /// <summary>이펙트 타입별 맞춤 설명 생성</summary>
    /// <returns>생성된 설명 텍스트</returns>
    protected virtual string CreateAutoDescription()
    {
        var description = new StringBuilder();

        // 이펙트 타입 정보
        string effectTypeName = GetEffectTypeName();
        description.AppendLine($"[{effectTypeName}]");

        // 우선순위 정보
        string priorityLevel = GetPriorityLevelDescription(_priority);
        description.Append($"적용 우선순위: {_priority} ({priorityLevel})");

        return description.ToString().Trim();
    }

    /// <summary>이펙트 타입 이름 추출</summary>
    /// <returns>이펙트 타입 표시명</returns>
    protected virtual string GetEffectTypeName()
    {
        string typeName = GetType().Name;

        // "SO" 접미사 제거
        if (typeName.EndsWith("SO"))
            typeName = typeName.Substring(0, typeName.Length - 2);

        // "Effect" 접미사 제거
        if (typeName.EndsWith("Effect"))
            typeName = typeName.Substring(0, typeName.Length - 6);

        return $"{typeName} 이펙트";
    }

    /// <summary>우선순위 레벨 설명 반환</summary>
    /// <param name="priority">우선순위 값</param>
    /// <returns>우선순위 설명</returns>
    protected string GetPriorityLevelDescription(int priority)
    {
        if (priority <= 20) return "최우선";
        if (priority <= 40) return "높음";
        if (priority <= 60) return "보통";
        if (priority <= 80) return "낮음";
        return "최후순";
    }
    #endregion
}