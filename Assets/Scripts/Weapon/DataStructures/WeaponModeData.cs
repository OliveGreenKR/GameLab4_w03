using UnityEngine;

public enum WeaponMode
{
    Normal,
    Focus,
    Rapid
}

[System.Serializable]
public struct WeaponModeModifiers
{
    #region Fields
    [Header("Fire Rate")]
    public float fireRateMultiplier;

    [Header("Accuracy")]
    public float accuracyMultiplier;
    public float recoilMultiplier;

    [Header("Damage")]
    public float damageMultiplier;
    #endregion

    #region Static Presets
    public static WeaponModeModifiers Normal => new WeaponModeModifiers
    {
        fireRateMultiplier = 1f,
        accuracyMultiplier = 1f,
        recoilMultiplier = 1f,
        damageMultiplier = 1f
    };

    public static WeaponModeModifiers Focus => new WeaponModeModifiers
    {
        fireRateMultiplier = 0.6f,
        accuracyMultiplier = 1.2f,
        recoilMultiplier = 0.5f,
        damageMultiplier = 1.1f
    };

    public static WeaponModeModifiers Rapid => new WeaponModeModifiers
    {
        fireRateMultiplier = 1.8f,
        accuracyMultiplier = 0.7f,
        recoilMultiplier = 1.5f,
        damageMultiplier = 0.9f
    };
    #endregion
}

public static class WeaponModeExtensions
{
    #region Mode Conversion
    public static WeaponModeModifiers GetModifiers(this WeaponMode mode)
    {
        switch (mode)
        {
            case WeaponMode.Normal:
                return WeaponModeModifiers.Normal;
            case WeaponMode.Focus:
                return WeaponModeModifiers.Focus;
            case WeaponMode.Rapid:
                return WeaponModeModifiers.Rapid;
            default:
                return WeaponModeModifiers.Normal;
        }
    }
    #endregion
}

[System.Serializable]
public struct RecoilState
{
    #region Fields
    private readonly float _currentRecoil;
    private readonly float _maxRecoil;
    private readonly float _recoveryRate;
    #endregion

    #region Properties
    public float CurrentRecoil => _currentRecoil;
    public float RecoilRatio => _maxRecoil > 0f ? _currentRecoil / _maxRecoil : 0f;
    #endregion

    #region Constructor
    public RecoilState(float maxRecoil, float recoveryRate)
    {
        _currentRecoil = 0f;
        _maxRecoil = Mathf.Max(0.1f, maxRecoil);
        _recoveryRate = Mathf.Max(0f, recoveryRate);
    }

    private RecoilState(float currentRecoil, float maxRecoil, float recoveryRate)
    {
        _currentRecoil = currentRecoil;
        _maxRecoil = maxRecoil;
        _recoveryRate = recoveryRate;
    }
    #endregion

    #region Public Methods
    public RecoilState AddRecoil(float amount)
    {
        float newRecoil = Mathf.Clamp(_currentRecoil + amount, 0f, _maxRecoil);
        return new RecoilState(newRecoil, _maxRecoil, _recoveryRate);
    }

    public RecoilState UpdateRecovery(float deltaTime)
    {
        float newRecoil = Mathf.Max(0f, _currentRecoil - _recoveryRate * deltaTime);
        return new RecoilState(newRecoil, _maxRecoil, _recoveryRate);
    }
    #endregion
}