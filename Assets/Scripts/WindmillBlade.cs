using UnityEngine;

public class WindmillBlade : MonoBehaviour
{
    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 25f;  // 플레이어를 날려버릴 힘의 크기
    [SerializeField] private float upwardForce = 10f;     // 위쪽으로 추가로 가해지는 힘
    [SerializeField] private bool useBladeVelocity = true; // 날개 회전 속도 반영 여부
    [SerializeField] private float diableInputDuration = 0.5f;

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";  // 플레이어 태그

    [Header("Effects")]
    [SerializeField] private bool addRotation = true;      // 날아갈 때 회전 효과 추가
    [SerializeField] private float rotationForce = 10f;    // 회전 효과 강도

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;    // 디버그 정보 표시

    private Rigidbody parentRigidbody;  // 풍차 본체의 Rigidbody (회전 속도 계산용)

    void Start()
    {
        // 풍차 본체(부모)의 Rigidbody 찾기
        Transform parent = transform.parent;
        if (parent != null)
        {
            parentRigidbody = parent.GetComponent<Rigidbody>();
        }

        // Collider가 있는지 확인
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"[WindmillBlade] {gameObject.name}에 Collider가 없습니다. BoxCollider를 추가합니다.");
            gameObject.AddComponent<BoxCollider>();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // 플레이어 태그 확인
        if (!collision.gameObject.CompareTag(playerTag))
            return;

        // 플레이어의 Rigidbody 가져오기
        Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
        if (playerRb == null)
        {
            if (showDebugInfo)
                Debug.LogWarning($"[WindmillBlade] {collision.gameObject.name}에 Rigidbody가 없습니다.");
            return;
        }

        // 날려버릴 방향 계산
        Vector3 knockbackDirection = CalculateKnockbackDirection(collision);

        // 힘 적용
        ApplyKnockback(playerRb, knockbackDirection);

        // 선택적: 플레이어 컨트롤러 일시 정지
        DisablePlayerControl(collision.gameObject);
    }

    Vector3 CalculateKnockbackDirection(Collision collision)
    {
        Vector3 direction;

        if (useBladeVelocity && parentRigidbody != null)
        {
            // 날개의 회전 속도를 고려한 방향 계산
            Vector3 bladeVelocity = parentRigidbody.GetPointVelocity(collision.contacts[0].point);
            direction = bladeVelocity.normalized;

            // 속도가 너무 작으면 기본 방향 사용
            if (bladeVelocity.magnitude < 0.1f)
            {
                direction = (collision.transform.position - transform.position).normalized;
            }
        }
        else
        {
            // 날개 중심에서 플레이어 방향
            direction = (collision.transform.position - transform.position).normalized;
        }

        // 위쪽 방향 추가 (포물선 궤적을 위해)
        direction.y = 0;  // 수평 방향만 먼저 정규화
        direction = direction.normalized;
        //direction.y = 0.5f;  // 위쪽 성분 추가
        //direction = direction.normalized;

        if (showDebugInfo)
        {
            Debug.Log($"[WindmillBlade] Knockback 방향: {direction}");
            Debug.DrawRay(collision.contacts[0].point, direction * 3f, Color.red, 2f);
        }

        return direction;
    }

    void ApplyKnockback(Rigidbody playerRb, Vector3 direction)
    {
        // 기존 속도 초기화 (더 일관된 knockback을 위해)
        playerRb.linearVelocity = Vector3.zero;

        // 메인 knockback 힘 적용
        Vector3 force = direction * knockbackForce;

        // 추가 위쪽 힘
        force.y += upwardForce;

        // 힘 적용 (임펄스로 즉시 적용)
        playerRb.AddForce(force, ForceMode.VelocityChange);

        if (showDebugInfo)
        {
            Debug.Log($"[WindmillBlade] {playerRb.gameObject.name}에게 {force.magnitude}의 힘 적용!");
        }
    }

    void DisablePlayerControl(GameObject player)
    {
         PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            StartCoroutine(DisableControllerTemporary(playerController, diableInputDuration));
        }
    }

    System.Collections.IEnumerator DisableControllerTemporary(PlayerController controller, float duration)
    {
        controller.DisableInput();
        yield return new WaitForSeconds(duration);
        controller.EnableInput();
    }

    // Gizmos로 충돌 영역 시각화
    void OnDrawGizmos()
    {
        if (!showDebugInfo) return;

        Gizmos.color = Color.yellow;
        Collider col = GetComponent<Collider>();
        if (col != null && col is BoxCollider)
        {
            BoxCollider box = (BoxCollider)col;
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = oldMatrix;
        }
    }
}