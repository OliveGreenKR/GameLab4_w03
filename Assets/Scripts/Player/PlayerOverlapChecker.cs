using UnityEngine;

public class PlayerOverlapChecker : MonoBehaviour
{
    [SerializeField] private Collider _targetCollider = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(_targetCollider == null)
        {
            _targetCollider = gameObject.GetComponent<Collider>();
            Debug.LogWarning("PlayerOverlapChecker: Target Collider is not assigned. Attempting to get Collider from the same GameObject.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        int otherLayer = other.gameObject.layer;
        string layerName = LayerMask.LayerToName(otherLayer);
        Debug.Log($"trigger Enter with {other.gameObject.name}:{otherLayer}");
    }

    private void OnTriggerExit(Collider other)
    {
        int otherLayer = other.gameObject.layer;
        string layerName = LayerMask.LayerToName(otherLayer);
        Debug.Log($"trigger Exit with {other.gameObject.name}:{otherLayer}");
    }

    //void OnTriggerStay(Collider other)
    //{
    //    // 겹치는 동안 매 프레임 호출 ⚠️
    //}
}
