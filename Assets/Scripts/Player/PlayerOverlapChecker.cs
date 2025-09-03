using UnityEngine;

public class PlayerOverlapChecker : MonoBehaviour
{
    [SerializeField] private Collider _targetCollider = null;
    [SerializeField] private GameObject _targetPlayer = null;
    [SerializeField] bool _isOverlap = false;


    [SerializeField] bool IsOverlap => _isOverlap;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(_targetCollider == null)
        {
            _targetCollider = gameObject.GetComponent<Collider>();
            Debug.LogWarning("PlayerOverlapChecker: Target Collider is not assigned. Attempting to get Collider from the same GameObject.");
        }
        if (_targetPlayer == null)
        {
            Debug.LogWarning("PlayerOverlapChecker : TargetPlayer is missing!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        int otherLayer = other.gameObject.layer;
        string layerName = LayerMask.LayerToName(otherLayer);
        Debug.Log($"trigger Enter with {other.gameObject.name}:{otherLayer}");

        if (LayerMask.LayerToName(_targetPlayer.layer) == "Red" && layerName == "Blue")
        {
            _isOverlap = true;
        }
        else if (LayerMask.LayerToName(_targetPlayer.layer) == "Blue" && layerName == "Red")
        {
            _isOverlap = true;
        }
        else
        {
            _isOverlap = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        int otherLayer = other.gameObject.layer;
        string layerName = LayerMask.LayerToName(otherLayer);
        Debug.Log($"trigger Exit with {other.gameObject.name}:{otherLayer}");

        _isOverlap = false;
    }

    //void OnTriggerStay(Collider other)
    //{
    //    // 겹치는 동안 매 프레임 호출 ⚠️
    //}
}
