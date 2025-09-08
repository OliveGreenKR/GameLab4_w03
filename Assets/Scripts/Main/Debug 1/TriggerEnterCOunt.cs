using UnityEngine;

public class TriggerEnterCOunt : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] private int _triggerEntercount = 0;
    [SerializeField] private int _triggerExitcount = 0;

    void OnEnable()
    {
        _triggerEntercount = 0;
        _triggerExitcount = 0;
    }
    private void OnTriggerEnter(Collider other)
    {
        _triggerEntercount++;
    }

    private void OnTriggerExit(Collider other)
    {
        _triggerExitcount++;
    }
}
