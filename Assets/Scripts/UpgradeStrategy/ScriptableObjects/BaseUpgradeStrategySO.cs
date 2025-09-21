using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>업그레이드 카테고리</summary>
public enum UpgradeCategory
{
    WeaponStat,
    PlayerStat
}

/// <summary>업그레이드 적용 전략 기본 클래스</summary>
public abstract class BaseUpgradeStrategySO : ScriptableObject
{
    #region Serialized Fields
    [BoxGroup("Display Info")]
    [Header("Basic Information")]
    [SerializeField] protected string _displayName = "New Upgrade";

    [BoxGroup("Display Info")]
    [SerializeField, TextArea(2, 4)] protected string _description = "업그레이드 설명을 입력하세요";

    [BoxGroup("Display Info")]
    [SerializeField] protected Sprite _icon;

    [BoxGroup("Application Settings")]
    [Header("Application Type")]
    [SerializeField] protected UpgradeApplicationType _applicationType = UpgradeApplicationType.Permanent;

    [BoxGroup("Application Settings")]
    [ShowIf("@_applicationType == UpgradeApplicationType.Temporary")]
    [SuffixLabel("seconds")]
    [SerializeField] protected float _temporaryDurationSeconds = 30f;
    #endregion

    #region Properties
    /// <summary>표시 이름</summary>
    public string DisplayName => _displayName;

    /// <summary>업그레이드 설명</summary>
    public string Description => _description;

    /// <summary>업그레이드 아이콘</summary>
    public Sprite Icon => _icon;

    /// <summary>적용 방식 (영구/임시)</summary>
    public UpgradeApplicationType ApplicationType => _applicationType;

    /// <summary>임시 업그레이드 지속 시간</summary>
    public float TemporaryDurationSeconds => _temporaryDurationSeconds;

    /// <summary>업그레이드 카테고리 (Factory 선택용)</summary>
    public abstract UpgradeCategory Category { get; }

    /// <summary>
    /// 업그레이드 대상 타입
    /// </summary>
    public abstract UpgradeType TargetUpgradeType { get; }
    #endregion

    #region Abstract Methods
    /// <summary>업그레이드를 대상에 적용 (모든 데이터는 SO 내부에서 처리)</summary>
    /// <param name="target">업그레이드 적용 대상</param>
    public abstract void ApplyUpgrade(IUpgradable target);

    /// <summary>업그레이드를 대상에서 제거 (모든 데이터는 SO 내부에서 처리)</summary>
    /// <param name="target">업그레이드 제거 대상</param>
    public abstract void RemoveUpgrade(IUpgradable target);
    #endregion

    #region Virtual Methods
    /// <summary>업그레이드 적용 가능 여부 검증</summary>
    /// <param name="target">검증할 대상</param>
    /// <returns>적용 가능하면 true</returns>
    public virtual bool CanApplyTo(IUpgradable target)
    {
        return target != null;
    }

    /// <summary>기본 유효성 검사</summary>
    /// <returns>Strategy 데이터가 유효하면 true</returns>
    public virtual bool IsValid()
    {
        return !string.IsNullOrEmpty(_displayName);
    }
    #endregion

    #region Unity Lifecycle
    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(_displayName))
        {
            _displayName = name;
        }

        if (_applicationType == UpgradeApplicationType.Temporary)
        {
            _temporaryDurationSeconds = Mathf.Max(0.1f, _temporaryDurationSeconds);
        }
    }
    #endregion
}