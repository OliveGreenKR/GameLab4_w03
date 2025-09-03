using UnityEngine;
using System.Collections.Generic;

public class PlayerOverlapChecker : MonoBehaviour
{
    #region Serialized Fields
    [Header("References")]
    [SerializeField] private Collider _targetCollider = null;
    [SerializeField] private PlayerController _targetPlayer = null;
    [SerializeField] private bool _isOverlap = false;

    [Header("Debug Settings")]
    [SerializeField] private bool _enableDebugLogging = false;
    #endregion

    #region Properties
    public bool IsOverlap => _isOverlap;
    public int OverlappingObjectCount => _overlappingColliders.Count;
    #endregion

    #region Private Fields
    private HashSet<Collider> _overlappingColliders = new HashSet<Collider>();
    private bool _hasDisabledColorControl = false;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializeReferences();
        _isOverlap = false;
        _hasDisabledColorControl = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_targetPlayer == null || other == null)
            return;

        string otherLayerName = LayerMask.LayerToName(other.gameObject.layer);
        string playerLayerName = LayerMask.LayerToName(_targetPlayer.gameObject.layer);

        //if (_enableDebugLogging)
        //{
        //    Debug.Log($"[PlayerOverlapChecker] TriggerEnter: {other.gameObject.name} ({otherLayerName}) vs Player ({playerLayerName})");
        //}

        // 반대 레이어인지 확인
        if (IsOppositeLayer(playerLayerName, otherLayerName))
        {
            // HashSet에 추가 (중복 자동 제거)
            if (_overlappingColliders.Add(other))
            {
                if (_enableDebugLogging)
                {
                    Debug.Log($"[PlayerOverlapChecker] Added overlapping object: {other.gameObject.name}. Total: {_overlappingColliders.Count}");
                }

                UpdateOverlapStatus();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null)
            return;

        string otherLayerName = LayerMask.LayerToName(other.gameObject.layer);

        //if (_enableDebugLogging)
        //{
        //    Debug.Log($"[PlayerOverlapChecker] TriggerExit: {other.gameObject.name} ({otherLayerName})");
        //}

        // HashSet에서 제거
        if (_overlappingColliders.Remove(other))
        {
            if (_enableDebugLogging)
            {
                Debug.Log($"[PlayerOverlapChecker] Removed overlapping object: {other.gameObject.name}. Total: {_overlappingColliders.Count}");
            }

            UpdateOverlapStatus();
        }
    }
    #endregion

    #region Public Methods
    public void ForceUpdateOverlapStatus()
    {
        UpdateOverlapStatus();
    }

    public void ClearAllOverlaps()
    {
        _overlappingColliders.Clear();
        UpdateOverlapStatus();
    }
    #endregion

    #region Private Methods
    private void InitializeReferences()
    {
        if (_targetCollider == null)
        {
            _targetCollider = gameObject.GetComponent<Collider>();
            if (_targetCollider == null)
            {
                Debug.LogError("[PlayerOverlapChecker] No Collider found on GameObject!");
            }
            else
            {
                Debug.LogWarning("[PlayerOverlapChecker] Target Collider auto-assigned from GameObject.");
            }
        }

        if (_targetPlayer == null)
        {
            Debug.LogError("[PlayerOverlapChecker] TargetPlayer is required!");
        }

        // Collider가 Trigger인지 확인
        if (_targetCollider != null && !_targetCollider.isTrigger)
        {
            Debug.LogWarning("[PlayerOverlapChecker] Target Collider should be set as Trigger!");
        }
    }

    private bool IsOppositeLayer(string playerLayerName, string otherLayerName)
    {
        return (playerLayerName == "Red" && otherLayerName == "Blue") ||
               (playerLayerName == "Blue" && otherLayerName == "Red");
    }

    private void UpdateOverlapStatus()
    {
        bool newOverlapStatus = _overlappingColliders.Count > 0;

        // 상태가 변경된 경우에만 처리
        if (newOverlapStatus != IsOverlap)
        {
            _isOverlap = newOverlapStatus;

            if (_targetPlayer != null)
            {
                if (IsOverlap && !_hasDisabledColorControl)
                {
                    GameManager.Instance.SetCanColorChange(false);
                    _hasDisabledColorControl = true;

                    if (_enableDebugLogging)
                    {
                        Debug.Log("[PlayerOverlapChecker] Player colorChange DISABLED due to overlap.");
                    }
                }
                else if (!IsOverlap && _hasDisabledColorControl)
                {
                    GameManager.Instance.SetCanColorChange(true);
                    _hasDisabledColorControl = false;

                    if (_enableDebugLogging)
                    {
                        Debug.Log("[PlayerOverlapChecker] Player colorChange ENABLED - no more overlaps.");
                    }
                }
            }

            if (_enableDebugLogging)
            {
                Debug.Log($"[PlayerOverlapChecker] Overlap status changed to: {IsOverlap} (Objects: {_overlappingColliders.Count})");
            }
        }
    }
    #endregion
}