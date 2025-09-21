/// <summary>업그레이드 적용 방식</summary>
public enum UpgradeApplicationType
{
    Permanent,  // 영구 적용
    Temporary   // 임시 적용 (TemporaryBuffManager 위임)
}

/// <summary>업그레이드 타입 열거형 - 상점 시스템 전용</summary>
public enum UpgradeType
{
    // 무기 기본 스탯
    WeaponDamage,
    WeaponFireRate,
    WeaponAccuracy,
    WeaponRecoil,
    WeaponProjectileSpeed,
    WeaponProjectileLifetime,

    // 무기 특수 효과
    WeaponPiercing,
    WeaponSplit,
    WeaponConcentration,
    WeaponBurst,

    // 플레이어 스탯
    PlayerHealth,
    PlayerMaxHealth,
    PlayerMoveSpeed,
}

/// <summary>업그레이드를 받을 수 있는 객체의 계약</summary>
public interface IUpgradable
{
    #region Upgrade Validation
    /// <summary>특정 타입의 업그레이드를 받을 수 있는지 확인</summary>
    /// <param name="upgradeType">업그레이드 타입</param>
    /// <returns>업그레이드 가능 여부</returns>
    bool CanReceiveUpgrade(UpgradeType upgradeType);
    #endregion

    #region Upgrade Application
    /// <summary>업그레이드를 적용 (영구/임시 구분하여 처리)</summary>
    /// <param name="upgradeType">업그레이드 타입</param>
    /// <param name="value">업그레이드 수치</param>
    /// <param name="applicationType">적용 방식 (영구/임시)</param>
    /// <param name="durationSeconds">임시 적용 시 지속 시간 (초)</param>
    /// <returns>임시 적용 시 업그레이드 ID, 영구 적용 시 null</returns>
    string ApplyUpgrade(UpgradeType upgradeType, float value, UpgradeApplicationType applicationType, float durationSeconds = 0f);
    #endregion

    #region Upgrade Removal
    /// <summary>업그레이드를 제거</summary>
    /// <param name="upgradeType">업그레이드 타입</param>
    /// <param name="value">제거할 업그레이드 수치</param>
    /// <param name="upgradeId">임시 업그레이드 ID (임시 제거 시 필요)</param>
    /// <returns>제거 성공 여부</returns>
    bool RemoveUpgrade(UpgradeType upgradeType, float value, string upgradeId = null);
    #endregion

    #region Upgrade Information
    /// <summary>현재 적용된 영구 업그레이드 수치 조회</summary>
    /// <param name="upgradeType">업그레이드 타입</param>
    /// <returns>현재 적용된 수치</returns>
    float GetCurrentUpgradeValue(UpgradeType upgradeType);

    /// <summary>현재 활성화된 임시 업그레이드 개수 조회</summary>
    /// <param name="upgradeType">업그레이드 타입</param>
    /// <returns>활성 임시 업그레이드 개수</returns>
    int GetActiveTemporaryUpgradeCount(UpgradeType upgradeType);
    #endregion
}