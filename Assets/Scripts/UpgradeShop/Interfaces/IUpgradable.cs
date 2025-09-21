/// <summary>업그레이드 타입 열거형 - 새 업그레이드 추가 시 여기에만 추가</summary>
public enum UpgradeType
{
    // 무기 기본 스탯
    Damage,
    FireRate,
    Accuracy,
    ProjectileSpeed,
    ProjectileLifetime,

    // 무기 특수 효과
    Piercing,
    Split,
    Concentration,
    Burst,

    // 플레이어 스탯
    Health,
    MaxHealth,
    MoveSpeed,

    // 임시 버프
    TemporaryDamage,
    TemporaryFireRate,
    TemporaryAccuracy,
    TemporaryMoveSpeed
}

/// <summary>업그레이드를 받을 수 있는 객체의 계약</summary>
public interface IUpgradable
{
    /// <summary>특정 타입의 업그레이드를 받을 수 있는지 확인</summary>
    /// <param name="upgradeType">업그레이드 타입</param>
    /// <returns>업그레이드 가능 여부</returns>
    bool CanReceiveUpgrade(UpgradeType upgradeType);

    /// <summary>업그레이드를 적용</summary>
    /// <param name="upgradeType">업그레이드 타입</param>
    /// <param name="value">업그레이드 수치</param>
    void ApplyUpgrade(UpgradeType upgradeType, float value);

    /// <summary>업그레이드를 제거</summary>
    /// <param name="upgradeType">업그레이드 타입</param>
    /// <param name="value">제거할 업그레이드 수치</param>
    void RemoveUpgrade(UpgradeType upgradeType, float value);
}