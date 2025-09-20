using Sirenix.OdinInspector;
using UnityEngine;

public class PlayerEntityDamagedReaction : MonoBehaviour
{
    [SerializeField][Required] CharacterController _targetCharacterController = null;
    [SerializeField][Required] NewPlayerController _playerController = null;
    [SerializeField][Required] PlayerBattleEntity _playerBattleEntity = null;

    [SerializeField][InfoBox("Speed Slow Multiplier")] private float _slowMultiplier = 0.5f;
    [SerializeField][SuffixLabel("secs")] private float _effeectTime = 0.2f;

    #region Private Fields - effectControl
    private bool _isDuringEffect = false;
    private float _effectTimeRemaining = 0f;
    private float _cachedSpeed = 0f;
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
        //Vector3 direction = (_playerController.transform.position - attacker.Transform.position).normalized;

        // 지면 상태에 따른 넉백 방향 계산
        //if (_playerController.IsGrounded)
        //{
        //    // 지면 위에서: Unity의 ProjectOnPlane 사용
        //    Vector3 groundNormal = _playerController.LastGroudnNormal;
        //    knockbackDirection = Vector3.ProjectOnPlane(direction, groundNormal).normalized;
        //}
        //else
        //{
        //    // 공중에서: 기존 방식 (수평 넉백)
        //    knockbackDirection = direction;
        //}

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
        _isDuringEffect = true;
    }

    private void RestoreEffect()
    {
        _playerController.SetMoveSpeed(_cachedSpeed);
        _isDuringEffect = false;
        _effectTimeRemaining = 0f;
    }
    #endregion
}
