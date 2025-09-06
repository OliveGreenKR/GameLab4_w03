using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Battle Stat Data", menuName = "Battle System/Battle Stat Data")]
public class BattleStatData : ScriptableObject
{
    #region Serialized Fields
    [BoxGroup("Base Stats")]
    [Header("Health")]
    [SuffixLabel("units")]
    [PropertyRange(1f, 100000f)]
    [SerializeField] private float _baseHealth = 100f;

    [BoxGroup("Base Stats")]
    [Header("Health")]
    [SuffixLabel("units")]
    [PropertyRange(1f, 100000f)]
    [SerializeField] private float _maxHealth = 100f;

    [BoxGroup("Base Stats")]
    [Header("Combat")]
    [SuffixLabel("damage")]
    [PropertyRange(1f, 10000f)]
    [SerializeField] private float _baseAttack = 10f;

    [BoxGroup("Base Stats")]
    [SuffixLabel("attacks/sec")]
    [PropertyRange(0.1f, 100.0f)]
    [SerializeField] private float _baseAttackSpeed = 1f;

    [BoxGroup("Base Stats")]
    [SuffixLabel("multiplier")]
    [PropertyRange(0.1f, 100.0f)]
    [SerializeField] private float _baseEffectRange = 1f;
    #endregion

    #region Properties
    public float BaseHealth => _baseHealth;
    public float MaxHealth => _maxHealth;
    public float BaseAttack => _baseAttack;
    public float BaseAttackSpeed => _baseAttackSpeed;
    public float BaseEffectRange => _baseEffectRange;
    #endregion

    #region Public Methods
    /// <summary>
    /// 특정 스탯의 기본값 조회
    /// </summary>
    /// <param name="statType">스탯 타입</param>
    /// <returns>기본 스탯 값</returns>
    public float GetBaseStat(BattleStatType statType)
    {
        switch (statType)
        {
            case BattleStatType.Health:
                return _baseHealth;
            case BattleStatType.MaxHealth:
                return _maxHealth;
            case BattleStatType.Attack:
                return _baseAttack;
            case BattleStatType.AttackSpeed:
                return _baseAttackSpeed;
            case BattleStatType.EffectRange:
                return _baseEffectRange;
            default:
                return 0f;
        }
    }

    /// <summary>
    /// 모든 스탯을 딕셔너리로 반환
    /// </summary>
    /// <returns>스탯 타입과 값의 딕셔너리</returns>
    public Dictionary<BattleStatType, float> GetAllBaseStats()
    {
        return new Dictionary<BattleStatType, float>
        {
            { BattleStatType.Health, _baseHealth },
            { BattleStatType.MaxHealth, _maxHealth },
            { BattleStatType.Attack, _baseAttack },
            { BattleStatType.AttackSpeed, _baseAttackSpeed },
            { BattleStatType.EffectRange, _baseEffectRange }
        };
    }
    #endregion

    #region Private Methods
    #endregion
}
