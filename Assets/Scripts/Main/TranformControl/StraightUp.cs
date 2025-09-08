using Sirenix.OdinInspector;
using UnityEngine;

public class StraightUp : MonoBehaviour
{

    [TabGroup("Settings")]
    [SerializeField] bool IsToTransform =false;
    [TabGroup("Settings")]
    [ShowIf("@IsToTransform == true")]
    [SerializeField] Transform TargetTransform = null;
    [TabGroup("Settings")]
    [ShowIf("@IsToTransform == false")]
    [SerializeField] Vector3 TargetUp = Vector3.up;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        gameObject.transform.up = IsToTransform ? TargetTransform.up : TargetUp;
    }
}
