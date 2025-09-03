using UnityEngine;

public class RotateObject : MonoBehaviour
{
    public float rotationSpeed = 50f; // 회전 속도 (초당 각도)

    void Update()
    {
        // 현재 오브젝트의 Transform 컴포넌트에 접근       
        // rotationSpeed에 Time.deltaTime을 곱하여 프레임 속도에 독립적인 회전을 만듭니다.
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }
}