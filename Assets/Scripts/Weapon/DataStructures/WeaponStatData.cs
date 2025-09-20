using UnityEngine;

[System.Serializable]
public readonly struct WeaponStatData
{
    #region Fields
    private readonly float _currentFireRate;
    private readonly float _currentDamage;
    private readonly float _currentProjectileSpeed;
    private readonly float _currentProjectileLifetime;
    private readonly float _currentAccuracy;
    private readonly float _currentRecoil;
    #endregion

    #region Properties
    public float CurrentFireRate => _currentFireRate;
    public float CurrentDamage => _currentDamage;
    public float CurrentProjectileSpeed => _currentProjectileSpeed;
    public float CurrentProjectileLifetime => _currentProjectileLifetime;
    public float CurrentAccuracy => _currentAccuracy;
    public float CurrentRecoil => _currentRecoil;
    #endregion

    #region Constructor
    public WeaponStatData(WeaponStatSO baseStats)
    {
        _currentFireRate = baseStats.BaseFireRate;
        _currentDamage = baseStats.BaseDamage;
        _currentProjectileSpeed = baseStats.BaseProjectileSpeed;
        _currentProjectileLifetime = baseStats.BaseProjectileLifetime;
        _currentAccuracy = baseStats.BaseAccuracy;
        _currentRecoil = baseStats.BaseRecoil;
    }

    public WeaponStatData(float fireRate, float damage, float projectileSpeed, float projectileLifetime, float accuracy, float recoil)
    {
        _currentFireRate = Mathf.Max(0.1f, fireRate);
        _currentDamage = Mathf.Max(0f, damage);
        _currentProjectileSpeed = Mathf.Max(1f, projectileSpeed);
        _currentProjectileLifetime = Mathf.Max(0.1f, projectileLifetime);
        _currentAccuracy = Mathf.Clamp(accuracy, 0f, 100f);
        _currentRecoil = Mathf.Max(0f, recoil);
    }
    #endregion

    #region Public Methods - Stat Modification
    public WeaponStatData WithFireRate(float fireRate)
    {
        return new WeaponStatData(fireRate, _currentDamage, _currentProjectileSpeed, _currentProjectileLifetime, _currentAccuracy, _currentRecoil);
    }

    public WeaponStatData WithDamage(float damage)
    {
        return new WeaponStatData(_currentFireRate, damage, _currentProjectileSpeed, _currentProjectileLifetime, _currentAccuracy, _currentRecoil);
    }

    public WeaponStatData WithAccuracy(float accuracy)
    {
        return new WeaponStatData(_currentFireRate, _currentDamage, _currentProjectileSpeed, _currentProjectileLifetime, accuracy, _currentRecoil);
    }

    public WeaponStatData WithRecoil(float recoil)
    {
        return new WeaponStatData(_currentFireRate, _currentDamage, _currentProjectileSpeed, _currentProjectileLifetime, _currentAccuracy, recoil);
    }

    public WeaponStatData ApplyMultipliers(float fireRateMultiplier, float damageMultiplier, float accuracyMultiplier, float recoilMultiplier)
    {
        return new WeaponStatData(
            _currentFireRate * fireRateMultiplier,
            _currentDamage * damageMultiplier,
            _currentProjectileSpeed,
            _currentProjectileLifetime,
            _currentAccuracy * accuracyMultiplier,
            _currentRecoil * recoilMultiplier
        );
    }
    #endregion
}