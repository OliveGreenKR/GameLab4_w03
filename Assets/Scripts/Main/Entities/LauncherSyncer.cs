using Sirenix.OdinInspector;
using System.Xml.Serialization;
using UnityEngine;

public class LauncherSyncer : MonoBehaviour
{
    [Required][SerializeField] ProjectileLauncher _launcher = null;
    [Required][SerializeField] GameObject _launcherOwnerObject = null;

    IBattleEntity _launcherOwner = null;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(_launcherOwnerObject == null)
        {
           _launcherOwnerObject = gameObject;
        }

        if (_launcher == null)
        {
            _launcher = GetComponent<ProjectileLauncher>();

            if(_launcher == null)
            {
                Debug.LogError("[LauncherSyncer] ProjectileLauncher component not found on the GameObject!", this);
                enabled = false;
                return;
            }
            
        }
        if(_launcherOwner == null)
        {
            _launcherOwner = _launcherOwnerObject.GetComponentInParent<IBattleEntity>();

            if (_launcherOwner == null)
            {
                Debug.LogError("[LauncherSyncer] Launcher Owner (IBattleEntity) is not assigned!", this);
                enabled = false;
                return;
            }
        }

    }

    private void OnEnable()
    {
        _launcher.OnProjectileCreated -= OnProjectileCreated;
        _launcher.OnProjectileCreated += OnProjectileCreated;
    }

    private void OnDisable()
    {
        _launcher.OnProjectileCreated -= OnProjectileCreated;
    }

    private void OnProjectileCreated(IProjectile projectile)
    {
        if (projectile == null)
            return;
        if (_launcherOwner != null)
        {
            Debug.Log($"[LauncherSyncer] Setting projectile's owner and team. Owner: {_launcherOwner.GameObject.name}, TeamId: {_launcherOwner.TeamId}", this);
            (projectile as IBattleEntity)?.SetCurrentStat(BattleStatType.TeamId, _launcherOwner.TeamId);
        }
    }
}

