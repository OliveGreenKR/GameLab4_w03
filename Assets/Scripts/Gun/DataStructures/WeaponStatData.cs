using UnityEngine;

[System.Serializable]
public struct WeaponStatData
{
    #region Fields
    public float FireRate;
    public float Damage;
    public float ProjectileSpeed;
    public float ProjectileLifetime;
    public float Accuracy;
    public float Recoil;
    #endregion

    #region Constructor
    public WeaponStatData(WeaponStatSO baseStats)
    {
        FireRate = baseStats.BaseFireRate;
        Damage = baseStats.BaseDamage;
        ProjectileSpeed = baseStats.BaseProjectileSpeed;
        ProjectileLifetime = baseStats.BaseProjectileLifetime;
        Accuracy = baseStats.BaseAccuracy;
        Recoil = baseStats.BaseRecoil;
    }

    public WeaponStatData(float fireRate, float damage, float projectileSpeed, float projectileLifetime, float accuracy, float recoil)
    {
        FireRate = Mathf.Max(0.1f, fireRate);
        Damage = Mathf.Max(0f, damage);
        ProjectileSpeed = Mathf.Max(1f, projectileSpeed);
        ProjectileLifetime = Mathf.Max(0.1f, projectileLifetime);
        Accuracy = Mathf.Clamp(accuracy, 0f, 1000f);
        Recoil = Mathf.Max(0f, recoil);
    }
    #endregion

    #region Operators
    public static WeaponStatData operator +(WeaponStatData a, WeaponStatData b)
    {
        return new WeaponStatData(
            a.FireRate + b.FireRate,
            a.Damage + b.Damage,
            a.ProjectileSpeed + b.ProjectileSpeed,
            a.ProjectileLifetime + b.ProjectileLifetime,
            a.Accuracy + b.Accuracy,
            a.Recoil + b.Recoil
        );
    }
    #endregion

    #region Public Methods - Stat Modification
    public WeaponStatData ApplyMultipliers(float fireRateMultiplier, float damageMultiplier, float accuracyMultiplier, float recoilMultiplier)
    {
        return new WeaponStatData(
            FireRate * fireRateMultiplier,
            Damage * damageMultiplier,
            ProjectileSpeed,
            ProjectileLifetime,
            Accuracy * accuracyMultiplier,
            Recoil * recoilMultiplier
        );
    }
    #endregion
}