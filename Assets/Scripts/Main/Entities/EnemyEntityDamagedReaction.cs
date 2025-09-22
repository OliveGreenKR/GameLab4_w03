using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

public class EnemyEntityDamagedReaction : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField][Required] NavMeshAgent _targetNavMeshAgent = null;
    [SerializeField][Required] EnemyBattleEntity _enemyBattleEntity = null;

    [SerializeField][InfoBox("Speed Slow Multiplier")] private float _slowMultiplier = 0.5f;
    [SerializeField][SuffixLabel("secs")] private float _effectTime = 0.2f;

    [Header("Material Effect")]
    [SerializeField]
    [Required]
    [InfoBox("매터리얼 이펙트 적용 대상")]
    private Renderer _targetRenderer = null;
    [SerializeField]
    [InfoBox("데미지 이펙트 중 적용할 매터리얼")]
    private Material _damageEffectMaterial = null;
    #endregion

    #region Private Fields - Effect Control
    private bool _isDuringEffect = false;
    private float _effectTimeRemaining = 0f;
    private float _cachedSpeed = 0f;

    private Material _cachedMaterial = null;
    #endregion

    #region Properties
    public bool IsDuringEffect => _isDuringEffect;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        if (_targetNavMeshAgent == null)
        {
            Debug.LogError("[EnemyEntityDamagedReaction] NavMeshAgent is not assigned!", this);
            return;
        }
        if (_enemyBattleEntity == null)
        {
            Debug.LogError("[EnemyEntityDamagedReaction] EnemyBattleEntity is not assigned!", this);
            return;
        }
        if (_targetRenderer == null)
        {
            Debug.LogError("[EnemyEntityDamagedReaction] Target Renderer is not assigned!", this);
            return;
        }

        _enemyBattleEntity.BattleStat.OnDamageTaken -= OnDamaged;
        _enemyBattleEntity.BattleStat.OnDamageTaken += OnDamaged;

        _cachedSpeed = _targetNavMeshAgent.speed;
        _cachedMaterial = _targetRenderer.material;
    }

    private void Update()
    {
        UpdateEffectTime();
    }

    private void OnDestroy()
    {
        if (_enemyBattleEntity != null)
        {
            _enemyBattleEntity.BattleStat.OnDamageTaken -= OnDamaged;
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
        _effectTimeRemaining = _effectTime;
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
        _targetNavMeshAgent.speed = _cachedSpeed * _slowMultiplier;

        // 매터리얼 이펙트 적용
        if (_damageEffectMaterial != null && _cachedMaterial != null && _targetRenderer != null)
        {
            _targetRenderer.material = _damageEffectMaterial;
        }

        _isDuringEffect = true;
    }

    private void RestoreEffect()
    {
        _targetNavMeshAgent.speed = _cachedSpeed;

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