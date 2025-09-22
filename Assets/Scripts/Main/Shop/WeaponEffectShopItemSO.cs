using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static UnityEngine.CullingGroup;



/// <summary>
/// 무기 효과 상점 아이템
/// WeaponEffectSO를 추가하는 아이템에 사용
/// </summary>
[CreateAssetMenu(fileName = "WeaponEffectItem_", menuName = "Shop System/Weapon Effect Item", order = 2)]
public class WeaponEffectShopItemSO : ShopItemSO
{
    #region Serialized Fields
    [TabGroup("Effect")]
    [Header("Weapon Effect")]
    [Required]
    [SerializeField] private WeaponEffectSO _weaponEffect;
    #endregion

    #region Properties - Effect Data
    public WeaponEffectSO WeaponEffect => _weaponEffect;
    #endregion

    #region Protected Methods - Description Generation Override
    /// <summary>무기 효과 상점 아이템 특화 설명 생성</summary>
    /// <returns>무기 효과 아이템 맞춤 설명</returns>
    protected override string CreateAutoDescription()
    {
        var description = new StringBuilder();

        // 기본 아이템 정보
        description.AppendLine($"[{GetItemTypeDisplayName(ItemType)}]");

        // 무기 효과 정보
        if (_weaponEffect != null)
        {
            //description.AppendLine($"효과명: {_weaponEffect.EffectName}");

            // 스탯 변화 정보
            var statChanges = new List<string>();

            if (!Mathf.Approximately(_weaponEffect.FireRateMultiplier, 1.0f))
            {
                statChanges.Add($"발사속도 {FormatMultiplierChange(_weaponEffect.FireRateMultiplier)}");
            }

            if (!Mathf.Approximately(_weaponEffect.DamageMultiplier, 1.0f))
            {
                statChanges.Add($"데미지 {FormatMultiplierChange(_weaponEffect.DamageMultiplier)}");
            }

            if (!Mathf.Approximately(_weaponEffect.AccuracyMultiplier, 1.0f))
            {
                statChanges.Add($"정확도 {FormatMultiplierChange(_weaponEffect.AccuracyMultiplier)}");
            }

            if (!Mathf.Approximately(_weaponEffect.RecoilMultiplier, 1.0f))
            {
                statChanges.Add($"반동 {FormatMultiplierChange(_weaponEffect.RecoilMultiplier)}");
            }

            if (statChanges.Count > 0)
            {
                //description.AppendLine($"스탯: {string.Join(", ", statChanges)}");
                foreach (string statChange in statChanges)
                {
                    description.AppendLine(statChange);
                }
            }
        }
        else
        {
            description.AppendLine("효과: 무기 효과 미할당");
        }
        return description.ToString().Trim();
    }

    /// <summary>배율 변화를 사용자 친화적 텍스트로 변환</summary>
    /// <param name="multiplier">배율 값</param>
    /// <returns>변화 설명 텍스트</returns>
    private string FormatMultiplierChange(float multiplier)
    {
        if (multiplier > 1.0f)
        {
            return $"+{(multiplier - 1.0f) * 100:F0}%";
        }
        else if (multiplier < 1.0f)
        {
            return $"{(multiplier - 1.0f) * 100:F0}%";
        }
        else
        {
            return "변화없음";
        }
    }
    #endregion
}