using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon Stats", menuName = "Weapon System/Weapon Stats")]
public class WeaponStatSO : ScriptableObject
{
    #region Serialized Fields
    [BoxGroup("Base Stats")]
    [Header("Fire Rate")]
    [SuffixLabel("shots/sec")]
    [SerializeField] private float _baseFireRate = 1f;

    [BoxGroup("Base Stats")]
    [Header("Damage")]
    [SuffixLabel("damage")]
    [SerializeField] private float _baseDamage = 10f;

    [BoxGroup("Base Stats")]
    [Header("Projectile Settings")]
    [SuffixLabel("units/sec")]
    [SerializeField] private float _baseProjectileSpeed = 20f;

    [BoxGroup("Base Stats")]
    [SuffixLabel("seconds")]
    [PropertyRange(0.1f, 10f)]
    [SerializeField] private float _baseProjectileLifetime = 5f;

    [BoxGroup("Accuracy")]
    [Header("Accuracy Stats")]
    [SuffixLabel("%")]
    [PropertyRange(0f, 100f)]
    [SerializeField] private float _baseAccuracy = 85f;

    [BoxGroup("Accuracy")]
    [SuffixLabel("degrees")]
    [PropertyRange(0f, 45f)]
    [SerializeField] private float _baseRecoil = 2f;
    #endregion

    #region Properties
    public float BaseFireRate => _baseFireRate;
    public float BaseDamage => _baseDamage;
    public float BaseProjectileSpeed => _baseProjectileSpeed;
    public float BaseProjectileLifetime => _baseProjectileLifetime;
    public float BaseAccuracy => _baseAccuracy;
    public float BaseRecoil => _baseRecoil;
    #endregion

    #region Public Methods - Stat Access
    public WeaponStatData CreateRuntimeStats()
    {
        return new WeaponStatData(this);
    }
    #endregion
}