using Sirenix.OdinInspector;
using UnityEngine;

public class PlayerEntityDamagedReaction : MonoBehaviour
{
    [SerializeField][Required] CharacterController _targetCharacterController = null;
    [SerializeField][Required] NewPlayerController _playerController = null;
    [SerializeField][Required] PlayerBattleEntity _playerBattleEntity = null;

    [SerializeField] private float _knockbackMagnitude = 2f;
    [SerializeField][SuffixLabel("secs")] private float _inputPreventionTime = 0.2f;

    #region Private Fields - Input Control
    private bool _isMovementDisabled = false;
    private float _inputDisableTimeRemaining = 0f;
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
        _playerBattleEntity.GetComponent<BattleStatComponent>().OnDamageTaken -= OnDamaged;
        _playerBattleEntity.GetComponent<BattleStatComponent>().OnDamageTaken += OnDamaged;
    }

    private void Update()
    {
        UpdateInputRecovery();
    }

    private void OnDestroy()
    {
        if (_playerBattleEntity != null)
        {
            _playerBattleEntity.GetComponent<BattleStatComponent>().OnDamageTaken -= OnDamaged;
        }
    }
    #endregion

    #region Private Methods - Damage Handling
    private void OnDamaged(float damage, IBattleEntity attacker)
    {
        Vector3 direction = (_playerController.transform.position - attacker.Transform.position).normalized;

        // 지면 상태에 따른 넉백 방향 계산
        Vector3 knockbackDirection;
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
        // 지면 위에서: Unity의 ProjectOnPlane 사용
        Vector3 groundNormal = _playerController.LastGroudnNormal;
        knockbackDirection = Vector3.ProjectOnPlane(direction, groundNormal).normalized;

        Debug.Log($"[PlayerKnockBack]{knockbackDirection}");
        // 넉백 적용
        _targetCharacterController.Move(knockbackDirection * _knockbackMagnitude);

        // 움직임 입력 비활성화 (시간 새로고침)
        if (!_isMovementDisabled)
        {
            _playerController.DisableMovementInput();
            _isMovementDisabled = true;
        }

        // 마비 시간 새로고침 (기존 시간 리셋)
        _inputDisableTimeRemaining = _inputPreventionTime;
    }

    private void UpdateInputRecovery()
    {
        if (!_isMovementDisabled) return;

        _inputDisableTimeRemaining -= Time.deltaTime;

        if (_inputDisableTimeRemaining <= 0f)
        {
            _playerController.EnableMovementInput();
            _isMovementDisabled = false;
            _inputDisableTimeRemaining = 0f;
        }
    }
    #endregion
}
