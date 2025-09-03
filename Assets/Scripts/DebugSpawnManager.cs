using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class DebugSpawnManager : MonoBehaviour
{
    [Header("플레이어 설정")]
    [SerializeField] private Transform player;

    [Header("스폰 위치 설정")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    [Header("디버그 설정")]
    [SerializeField] private bool enableDebugMode = true;
    [SerializeField]
    private KeyCode[] spawnKeys = new KeyCode[]
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
        KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0
    };

    private void Start()
    {
        // 플레이어가 할당되지 않았다면 자동으로 찾기
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log("플레이어를 자동으로 찾았습니다: " + playerObj.name);
            }
            else
            {
                Debug.LogWarning("플레이어를 찾을 수 없습니다. Player 태그를 확인하거나 수동 할당해주세요.");
            }
        }

        // 1. 자식 오브젝트들을 자동으로 스폰 포인트로 추가
        FindChildSpawnPoints();

        // 2. 스폰 포인트 유효성 검사
        ValidateSpawnPoints();
    }

    private void Update()
    {
        if (!enableDebugMode || player == null) return;

        // 스폰 키 입력 확인
        for (int i = 0; i < spawnKeys.Length && i < spawnPoints.Count; i++)
        {
            if (Input.GetKeyDown(spawnKeys[i]) && spawnPoints[i] != null)
            {
                SpawnPlayerAtPosition(i);
            }
        }
    }

    /// <summary>
    /// 플레이어를 지정된 스폰 위치로 이동시킵니다.
    /// </summary>
    /// <param name="spawnIndex">스폰 포인트 인덱스</param>
    public void SpawnPlayerAtPosition(int spawnIndex)
    {
        if (spawnIndex < 0 || spawnIndex >= spawnPoints.Count)
        {
            Debug.LogWarning($"유효하지 않은 스폰 인덱스: {spawnIndex}");
            return;
        }

        if (spawnPoints[spawnIndex] == null)
        {
            Debug.LogWarning($"스폰 포인트 {spawnIndex}가 null입니다.");
            return;
        }

        if (player == null)
        {
            Debug.LogWarning("플레이어가 할당되지 않았습니다.");
            return;
        }

        // CharacterController가 있다면 비활성화 후 이동
        CharacterController charController = player.GetComponent<CharacterController>();
        if (charController != null)
        {
            charController.enabled = false;
            player.position = spawnPoints[spawnIndex].position;
            player.rotation = spawnPoints[spawnIndex].rotation;
            charController.enabled = true;
        }
        // Rigidbody가 있다면 속도 초기화 후 이동
        else if (player.GetComponent<Rigidbody>() != null)
        {
            Rigidbody rb = player.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            player.position = spawnPoints[spawnIndex].position;
            player.rotation = spawnPoints[spawnIndex].rotation;
        }
        // 일반적으로 Transform 이동
        else
        {
            player.position = spawnPoints[spawnIndex].position;
            player.rotation = spawnPoints[spawnIndex].rotation;
        }

        Debug.Log($"플레이어를 스폰 포인트 {spawnIndex + 1}로 이동시켰습니다: {spawnPoints[spawnIndex].name}");
    }

    /// <summary>
    /// 스폰 포인트를 추가합니다.
    /// </summary>
    /// <param name="spawnPoint">추가할 스폰 포인트</param>
    public void AddSpawnPoint(Transform spawnPoint)
    {
        if (spawnPoint != null && !spawnPoints.Contains(spawnPoint))
        {
            spawnPoints.Add(spawnPoint);
            Debug.Log($"스폰 포인트 추가됨: {spawnPoint.name}");
        }
    }

    /// <summary>
    /// 스폰 포인트를 제거합니다.
    /// </summary>
    /// <param name="spawnPoint">제거할 스폰 포인트</param>
    public void RemoveSpawnPoint(Transform spawnPoint)
    {
        if (spawnPoints.Contains(spawnPoint))
        {
            spawnPoints.Remove(spawnPoint);
            Debug.Log($"스폰 포인트 삭제됨: {spawnPoint.name}");
        }
    }

    /// <summary>
    /// 모든 스폰 포인트를 클리어합니다.
    /// </summary>
    public void ClearSpawnPoints()
    {
        spawnPoints.Clear();
        Debug.Log("모든 스폰 포인트가 삭제되었습니다.");
    }

    /// <summary>
    /// 스폰 포인트를 유효성 검사합니다.
    /// </summary>
    private void ValidateSpawnPoints()
    {
        for (int i = spawnPoints.Count - 1; i >= 0; i--)
        {
            if (spawnPoints[i] == null)
            {
                Debug.LogWarning($"스폰 포인트 {i}가 null입니다. 리스트에서 제거합니다.");
                spawnPoints.RemoveAt(i);
            }
        }

        Debug.Log($"총 {spawnPoints.Count}개의 유효한 스폰 포인트가 등록되었습니다.");
    }

    // ---

    /// <summary>
    /// 이 스크립트를 포함하는 오브젝트의 모든 자식 오브젝트를 스폰 포인트로 찾고 추가합니다.
    /// </summary>
    [ContextMenu("자식 오브젝트들을 스폰 포인트로 자동 추가")]
    private void FindChildSpawnPoints()
    {
        // 기존 리스트를 비우고 다시 채웁니다.
        spawnPoints.Clear();

        // 이 스크립트가 붙어있는 오브젝트의 모든 자식들을 순회합니다.
        foreach (Transform child in transform)
        {
            spawnPoints.Add(child);
        }

        Debug.Log($"자식 오브젝트 {spawnPoints.Count}개가 스폰 포인트로 자동 추가되었습니다.");
    }

    // ---

    /// <summary>
    /// 현재 스폰 포인트 정보를 콘솔에 출력합니다.
    /// </summary>
    [ContextMenu("스폰 포인트 정보 출력")]
    public void PrintSpawnPointInfo()
    {
        Debug.Log("=== 스폰 포인트 정보 ===");
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (spawnPoints[i] != null)
            {
                Debug.Log($"[{i + 1}] {spawnPoints[i].name} - 위치: {spawnPoints[i].position}");
            }
            else
            {
                Debug.Log($"[{i + 1}] NULL");
            }
        }
        Debug.Log("=====================");
    }

    private void OnDrawGizmos()
    {
        if (spawnPoints == null || !enableDebugMode) return;

        // 스폰 포인트들을 씬 뷰에 시각화
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (spawnPoints[i] != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(spawnPoints[i].position, 0.5f);
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(spawnPoints[i].position, spawnPoints[i].forward * 2f);

#if UNITY_EDITOR
                Handles.Label(spawnPoints[i].position + Vector3.up, (i + 1).ToString());
#endif
            }
        }
    }
}