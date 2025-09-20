using Sirenix.OdinInspector;
using UnityEngine;

public class PlayerEntityDamagedReaction : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField][Required] CharacterController _targetCharacterController = null;
    [SerializeField][Required] NewPlayerController _playerController = null;
    [SerializeField][Required] PlayerBattleEntity _playerBattleEntity = null;

    [SerializeField][InfoBox("Speed Slow Multiplier")] private float _slowMultiplier = 0.5f;
    [SerializeField][SuffixLabel("secs")] private float _effeectTime = 0.2f;

    [Header("Material Effect")]
    [SerializeField]
    [Required]
    [InfoBox("매터리얼 이펙트 적용 대상")]
    private Renderer _targetRenderer = null;
    [SerializeField]
    [InfoBox("데미지 이펙트 중 적용할 매터리얼")]
    private Material _damageEffectMaterial = null;
    #endregion

    #region Private Fields - effectControl
    private bool _isDuringEffect = false;
    private float _effectTimeRemaining = 0f;
    private float _cachedSpeed = 0f;

    private Material _cachedMaterial = null;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        if (_targetCharacterController == null)
        {
            Debug.LogError("[PlayerEntityDamaged] target CharacterController is not assigned!", this);
            return;
        }
        if (_playerController == null)
        {
            Debug.LogError("[PlayerEntityDamaged] PlayerController is not assigned!", this);
            return;
        }
        if (_playerBattleEntity == null)
        {
            Debug.LogError("[PlayerEntityDamaged] BaseBattleEntity is not assigned!", this);
            return;
        }
        _playerBattleEntity.BattleStat.OnDamageTaken -= OnDamaged;
        _playerBattleEntity.BattleStat.OnDamageTaken += OnDamaged;

        _cachedSpeed = _playerController.MoveSpeed;
        _cachedMaterial = _targetRenderer.material;
    }

    private void Update()
    {
        UpdateEffectTime();
    }

    private void OnDestroy()
    {
        if (_playerBattleEntity != null)
        {
            _playerBattleEntity.BattleStat.OnDamageTaken -= OnDamaged;
        }
    }
    #endregion

    #region Private Methods - Damage Handling
    private void OnDamaged(float damage, IBattleEntity attacker)
    {
        // 이펙트 적용 
        if (!_isDuringEffect)
        {
            ApplyEffect();

        }

        // 새로고침 (기존 시간 리셋)
        _effectTimeRemaining = _effeectTime;
    }

    private void UpdateEffectTime()
    {
        if (!_isDuringEffect) return;

        _effectTimeRemaining -= Time.deltaTime;

        if (_effectTimeRemaining <= 0f)
        {
            RestoreEffect();
        }
    }

    private void ApplyEffect()
    {
        _playerController.SetMoveSpeed(_cachedSpeed * _slowMultiplier);

        // 매터리얼 이펙트 적용
        if (_damageEffectMaterial != null && _cachedMaterial != null && _targetRenderer != null)
        {
            _targetRenderer.material = _damageEffectMaterial;
        }

        _isDuringEffect = true;
    }

    private void RestoreEffect()
    {
        _playerController.SetMoveSpeed(_cachedSpeed);

        // 매터리얼 복구
        if (_cachedMaterial != null && _targetRenderer != null)
        {
            _targetRenderer.material = _cachedMaterial;
        }

        _isDuringEffect = false;
        _effectTimeRemaining = 0f;
    }
    #endregion
}
