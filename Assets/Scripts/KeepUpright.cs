using UnityEngine;

public class KeepUpright : MonoBehaviour
{
    void LateUpdate()
    {
        // 큐브의 월드 회전을 항상 (0, 0, 0)으로 재설정합니다.
        // 이로 인해 큐브가 부모 오브젝트의 회전 영향을 받지 않고
        // 항상 위쪽 방향(월드 축 기준)을 바라보게 됩니다.
        transform.rotation = Quaternion.identity;
        //transform.up = new Vector3(0, 1, 0);
    }
}