using Sirenix.OdinInspector;
using System.Xml.Serialization;
using UnityEngine;

public class LauncherProjectileTeamSyncher : MonoBehaviour
{
    [SerializeField] int _targetTeamId = 0;
    [Required][SerializeField] ProjectileLauncher _launcher = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (_launcher == null)
        {
            _launcher = GetComponent<ProjectileLauncher>();

            if (_launcher == null)
            {
                Debug.LogError("[LauncherSyncer] ProjectileLauncher component not found on the GameObject!", this);
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
        //Debug.Log($"[LauncherSyncer] Setting projectile's TeamId: {_targetTeamId}", this);
        (projectile as IBattleEntity)?.SetCurrentStat(BattleStatType.TeamId, _targetTeamId);
    }
}

