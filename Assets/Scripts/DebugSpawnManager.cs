using System.Collections.Generic;
using UnityEngine;

public class DebugSpawnManager : MonoBehaviour
{
    [Header("�÷��̾� ����")]
    [SerializeField] private Transform player;

    [Header("���� ��ġ ����")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    [Header("����� ����")]
    [SerializeField] private bool enableDebugMode = true;
    [SerializeField]
    private KeyCode[] spawnKeys = new KeyCode[]
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
        KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0
    };

    private void Start()
    {
        // �÷��̾ �������� �ʾҴٸ� �ڵ����� ã��
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log("�÷��̾ �ڵ����� ã�ҽ��ϴ�: " + playerObj.name);
            }
            else
            {
                Debug.LogWarning("�÷��̾ ã�� �� �����ϴ�. Player �±׸� Ȯ���ϰų� ���� �Ҵ����ּ���.");
            }
        }

        // ���� ����Ʈ ��ȿ�� �˻�
        ValidateSpawnPoints();
    }

    private void Update()
    {
        if (!enableDebugMode || player == null) return;

        // ���� Ű �Է� Ȯ��
        for (int i = 0; i < spawnKeys.Length && i < spawnPoints.Count; i++)
        {
            if (Input.GetKeyDown(spawnKeys[i]) && spawnPoints[i] != null)
            {
                SpawnPlayerAtPosition(i);
            }
        }
    }

    /// <summary>
    /// �÷��̾ ������ ���� ��ġ�� �̵���ŵ�ϴ�.
    /// </summary>
    /// <param name="spawnIndex">���� ����Ʈ �ε���</param>
    public void SpawnPlayerAtPosition(int spawnIndex)
    {
        if (spawnIndex < 0 || spawnIndex >= spawnPoints.Count)
        {
            Debug.LogWarning($"��ȿ���� ���� ���� �ε���: {spawnIndex}");
            return;
        }

        if (spawnPoints[spawnIndex] == null)
        {
            Debug.LogWarning($"���� ����Ʈ {spawnIndex}�� null�Դϴ�.");
            return;
        }

        if (player == null)
        {
            Debug.LogWarning("�÷��̾ �Ҵ���� �ʾҽ��ϴ�.");
            return;
        }

        // CharacterController�� �ִ� ��� ��Ȱ��ȭ �� �̵�
        CharacterController charController = player.GetComponent<CharacterController>();
        if (charController != null)
        {
            charController.enabled = false;
            player.position = spawnPoints[spawnIndex].position;
            player.rotation = spawnPoints[spawnIndex].rotation;
            charController.enabled = true;
        }
        // Rigidbody�� �ִ� ��� �ӵ� �ʱ�ȭ �� �̵�
        else if (player.GetComponent<Rigidbody>() != null)
        {
            Rigidbody rb = player.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            player.position = spawnPoints[spawnIndex].position;
            player.rotation = spawnPoints[spawnIndex].rotation;
        }
        // �Ϲ����� Transform �̵�
        else
        {
            player.position = spawnPoints[spawnIndex].position;
            player.rotation = spawnPoints[spawnIndex].rotation;
        }

        Debug.Log($"�÷��̾ ���� ����Ʈ {spawnIndex + 1}�� �̵��߽��ϴ�: {spawnPoints[spawnIndex].name}");
    }

    /// <summary>
    /// ���� ����Ʈ�� �߰��մϴ�.
    /// </summary>
    /// <param name="spawnPoint">�߰��� ���� ����Ʈ</param>
    public void AddSpawnPoint(Transform spawnPoint)
    {
        if (spawnPoint != null && !spawnPoints.Contains(spawnPoint))
        {
            spawnPoints.Add(spawnPoint);
            Debug.Log($"���� ����Ʈ �߰���: {spawnPoint.name}");
        }
    }

    /// <summary>
    /// ���� ����Ʈ�� �����մϴ�.
    /// </summary>
    /// <param name="spawnPoint">������ ���� ����Ʈ</param>
    public void RemoveSpawnPoint(Transform spawnPoint)
    {
        if (spawnPoints.Contains(spawnPoint))
        {
            spawnPoints.Remove(spawnPoint);
            Debug.Log($"���� ����Ʈ ���ŵ�: {spawnPoint.name}");
        }
    }

    /// <summary>
    /// ��� ���� ����Ʈ�� Ŭ�����մϴ�.
    /// </summary>
    public void ClearSpawnPoints()
    {
        spawnPoints.Clear();
        Debug.Log("��� ���� ����Ʈ�� ���ŵǾ����ϴ�.");
    }

    /// <summary>
    /// ���� ����Ʈ�� ��ȿ���� �˻��մϴ�.
    /// </summary>
    private void ValidateSpawnPoints()
    {
        for (int i = spawnPoints.Count - 1; i >= 0; i--)
        {
            if (spawnPoints[i] == null)
            {
                Debug.LogWarning($"���� ����Ʈ {i}�� null�Դϴ�. ����Ʈ���� �����մϴ�.");
                spawnPoints.RemoveAt(i);
            }
        }

        Debug.Log($"�� {spawnPoints.Count}���� ��ȿ�� ���� ����Ʈ�� ��ϵǾ����ϴ�.");
    }

    /// <summary>
    /// ���� ���� ����Ʈ ������ �ֿܼ� ����մϴ�.
    /// </summary>
    [ContextMenu("���� ����Ʈ ���� ���")]
    public void PrintSpawnPointInfo()
    {
        Debug.Log("=== ���� ����Ʈ ���� ===");
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (spawnPoints[i] != null)
            {
                Debug.Log($"[{i + 1}] {spawnPoints[i].name} - ��ġ: {spawnPoints[i].position}");
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

        // ���� ����Ʈ�� �� �信�� �ð�ȭ
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (spawnPoints[i] != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(spawnPoints[i].position, 0.5f);
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(spawnPoints[i].position, spawnPoints[i].forward * 2f);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(spawnPoints[i].position + Vector3.up, (i + 1).ToString());
#endif
            }
        }
    }
}