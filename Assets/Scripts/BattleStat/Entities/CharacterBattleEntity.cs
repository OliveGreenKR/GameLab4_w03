using Sirenix.OdinInspector;
using System;
using UnityEngine;

public class CharacterBattleEntity : BaseBattleEntity
{
    #region Serialized Fields
    [TabGroup("Combat")]
    [SerializeField] private bool _hasContactDamage = true;
    #endregion

    #region Events
    /// <summary>
    /// 캐릭터가 죽었을 때 발생하는 이벤트
    /// </summary>
    public event Action<IBattleEntity> OnCharacterDeath;

    /// <summary>
    /// 캐릭터가 데미지를 받았을 때 발생하는 이벤트
    /// </summary>
    public event Action<float, IBattleEntity> OnCharacterDamaged;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool HasContactDamage => _hasContactDamage;
    #endregion

    #region BaseBattleEntity Overrides
    public override void OnDeath(IBattleEntity killer = null)
    {
        base.OnDeath(killer);
        gameObject.SetActive(false);
        OnCharacterDeath?.Invoke(killer);
    }

    protected override float CalculateFinalDamage(IBattleEntity target)
    {
        if (!_hasContactDamage) return 0f;

        return base.CalculateFinalDamage(target);
    }

    protected override void OnDamageTakenFromBattleStat(float damage, IBattleEntity attacker)
    {
        OnCharacterDamaged?.Invoke(damage, attacker);
    }
    #endregion
}