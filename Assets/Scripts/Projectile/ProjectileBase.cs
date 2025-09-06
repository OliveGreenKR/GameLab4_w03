using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
///  Projectile Base, 이펙트 부착이 가능하며
/// 생명 주기는 스스로 관리. 매니저를 통해 생명주기의 완료 프로세스를 위탁
/// </summary>
public class ProjectileBase : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("Movement")]
    [Header("Movement Settings")]
    [SuffixLabel("units/sec")]
    [SerializeField] private float _forwardSpeedUnitsPerSecond = 10f;

    [TabGroup("Lifetime")]
    [Header("Lifetime Settings")]
    [SuffixLabel("seconds")]
    [SerializeField] private float _lifetimeSeconds = 5f;

    [TabGroup("Components")]
    [Header("Attack Trigger")]
    [Required]
    [SerializeField] protected Collider _attackTrigger;

    [TabGroup("Components")]
    [Header("RigidBody")]
    [Required]
    [SerializeField] protected Rigidbody _rigid;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Collider AttackTrigger => _attackTrigger;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float ForwardSpeed => _forwardSpeedUnitsPerSecond;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float RemainingLifetime => _remainingLifetime;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsActive => gameObject.activeInHierarchy;
    #endregion

    #region Private Fields
    private float _remainingLifetime;

    [SerializeField]
    private List<IProjectileEffect> _effects = new List<IProjectileEffect>();
    #endregion

    #region Unity Lifecycle
    private void Update()
    {
        UpdateLifetime();
        GoForward();
        ProcessEffectsOnUpdate();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"{gameObject.name} hit {other.gameObject.name}");    

        ProcessEffectsOnHit(other);
        InteractBattleStat(); // 배틀 스탯 상호작용 처리

    }

    private void OnEnable()
    {
        _remainingLifetime = _lifetimeSeconds;
        OnProjectileActivated();
    }

    private void OnDisable()
    {
        OnProjectileDeactivated();
    }
    #endregion

    #region Public Methods
    public void Initialize(float lifetimeSeconds = -1f, float forwardSpeed = -1f)
    {
        if (lifetimeSeconds > 0f)
        {
            _lifetimeSeconds = lifetimeSeconds;
        }

        if (forwardSpeed > 0f)
        {
            _forwardSpeedUnitsPerSecond = forwardSpeed;
        }

        _remainingLifetime = _lifetimeSeconds;
    }

    public void AddEffect(IProjectileEffect effect)
    {
        if (effect == null) return;

        bool shouldAdd = effect.OnAttachedProjectile(this, _effects);

        if (shouldAdd)
        {
            _effects.Add(effect);
            SortEffectsByPriority();
        }
    }

    public void RemoveEffect(IProjectileEffect effect)
    {
        if (effect != null)
        {
            _effects.Remove(effect);
        }
    }

    public void DestroyProjectile()
    {
        ProcessEffectsOnDestroy();

        if (ProjectileManager.Instance != null)
        {
            ProjectileManager.Instance.ReturnProjectile(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    #endregion

    #region Protected Virtual Methods
    protected virtual void GoForward()
    {
        transform.Translate(Vector3.forward * _forwardSpeedUnitsPerSecond * Time.deltaTime);
    }

    protected virtual void OnProjectileActivated()
    {
        // 자식 클래스에서 오버라이드
    }

    protected virtual void OnProjectileDeactivated()
    {
        // 자식 클래스에서 오버라이드
    }
    #endregion

    #region Private Methods
    private void UpdateLifetime()
    {
        _remainingLifetime -= Time.deltaTime;

        if (_remainingLifetime <= 0f)
        {
            DestroyProjectile();
        }
    }

    private void ProcessEffectsOnHit(Collider target)
    {
        for (int i = 0; i < _effects.Count; i++)
        {
            _effects[i].OnHit(this, target);
        }
    }

    private void ProcessEffectsOnDestroy()
    {
        for (int i = 0; i < _effects.Count; i++)
        {
            _effects[i].OnDestroy(this);
        }

        _effects.Clear();
    }

    private void ProcessEffectsOnUpdate()
    {
        for (int i = 0; i < _effects.Count; i++)
        {
            _effects[i].OnUpdate(this);
        }
    }

    private void SortEffectsByPriority()
    {
        _effects = _effects.OrderBy(effect => effect.Priority).ToList();
    }
    #endregion

    #region Battle Stat Modifier
    private void InteractBattleStat()
    {
        Debug.Log($"{gameObject.name} Battle Stat Interaction!");

    }
    #endregion
}