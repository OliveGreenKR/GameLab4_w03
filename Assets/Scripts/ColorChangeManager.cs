using System.Collections.Generic;
using UnityEngine;

public enum ObjectColor
{
    Red,
    Blue,
}

public class ObjectColorChangeManager : MonoBehaviour
{
    [Header("Stage 1")]
    [SerializeField] private List<GameObject> ObjectColorObjectsList1Red = new List<GameObject>();
    [SerializeField] private List<GameObject> ObjectColorObjectsList1Blue = new List<GameObject>();

    [Header("Stage 2")]
    [SerializeField] private List<GameObject> ObjectColorObjectsList2Red = new List<GameObject>();
    [SerializeField] private List<GameObject> ObjectColorObjectsList2Blue = new List<GameObject>();

    [Header("Stage 3")]
    [SerializeField] private List<GameObject> ObjectColorObjectsList3Red = new List<GameObject>();
    [SerializeField] private List<GameObject> ObjectColorObjectsList3Blue = new List<GameObject>();

    [Header("Materials")]
    [SerializeField] private Material redSolidMaterial;      // ������ ���� ���׸���
    [SerializeField] private Material redTransparentMaterial; // ������ ������ ���׸���
    [SerializeField] private Material blueSolidMaterial;     // �Ķ��� ���� ���׸���
    [SerializeField] private Material blueTransparentMaterial; // �Ķ��� ������ ���׸���

    [Header("Current Options")]
    [SerializeField] private int currentStage = 1;     // ���� �������� - ���߿� GameManager���� ���� ����
    public ObjectColor playerObjectColor; // ���� �÷��̾� ����
    GameObject testPlayer; // �׽�Ʈ�� �÷��̾� ������Ʈ

    private void Start()
    {
        // �ʱ� ���� ����
        InitializeAllObjects();
        SetActiveObjectsByStage(currentStage); // ���� ���������� �´� ������Ʈ�� Ȱ��ȭ
        Debug.Log($"ColorChangeManager initialized. Current Stage: {currentStage}, Player ObjectColor: {playerObjectColor}");
    }

    // �׽�Ʈ��
    private void Update()
    {
        // ��Ŭ���� �ؼ� �÷��̾� ���� ���� (�׽�Ʈ��)
        if (Input.GetMouseButtonDown(0))
        {
            playerObjectColor = playerObjectColor == ObjectColor.Red ? ObjectColor.Blue : ObjectColor.Red;
            Debug.Log($"Player ObjectColor changed to: {playerObjectColor}");
            ObjectColorChange(playerObjectColor);
        }

        // ����Ű�� �������� ���� (�׽�Ʈ��)
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetCurrentStage(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetCurrentStage(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetCurrentStage(3);
    }

    /// <summary>
    /// ��� ������Ʈ�� �ʱ� ����(����)�� ����
    /// </summary>
    private void InitializeAllObjects()
    {
        // �÷��̾� ���� ���� ���� ���������� ���� ������Ʈ�� �����ϰ� ����
        if (playerObjectColor == ObjectColor.Red)
        {  // �÷��̾� ������ �������̸� �Ķ��� ������Ʈ�� �����ϰ�
            SetObjectsSolid(GetObjectColorObjectsList(ObjectColor.Red, currentStage), ObjectColor.Red);
            SetObjectsTransparent(GetObjectColorObjectsList(ObjectColor.Blue, currentStage), ObjectColor.Blue);
        }
        else if (playerObjectColor == ObjectColor.Blue)
        {  // �÷��̾� ������ �Ķ����̸� ������ ������Ʈ�� �����ϰ�
            SetObjectsSolid(GetObjectColorObjectsList(ObjectColor.Blue, currentStage), ObjectColor.Blue);
            SetObjectsTransparent(GetObjectColorObjectsList(ObjectColor.Red, currentStage), ObjectColor.Red);
        }
    }

    /// <summary>
    /// ���������� ���� ������Ʈ���� Ȱ��ȭ/��Ȱ��ȭ ����
    /// </summary>
    /// <param name="activeStage">Ȱ��ȭ�� �������� ��ȣ</param>
    private void SetActiveObjectsByStage(int activeStage)
    {
        // ��� �������� ��ȸ�ϸ� Ȱ��ȭ/��Ȱ��ȭ ����
        for (int stage = 1; stage <= 3; stage++)
        {
            bool shouldActivate = (stage == activeStage);

            // �� ���������� �������� �Ķ��� ������Ʈ ����Ʈ ��������
            List<GameObject> redObjects = GetObjectColorObjectsList(ObjectColor.Red, stage);
            List<GameObject> blueObjects = GetObjectColorObjectsList(ObjectColor.Blue, stage);

            // ������Ʈ�� Ȱ��ȭ/��Ȱ��ȭ
            SetObjectsActive(redObjects, shouldActivate);
            SetObjectsActive(blueObjects, shouldActivate);
        }

        Debug.Log($"Stage {activeStage} objects activated, others deactivated");
    }

    /// <summary>
    /// ������Ʈ ����Ʈ�� Ȱ��ȭ ���� ����
    /// </summary>
    /// <param name="objects">��� ������Ʈ ����Ʈ</param>
    /// <param name="active">Ȱ��ȭ ����</param>
    private void SetObjectsActive(List<GameObject> objects, bool active)
    {
        foreach (GameObject obj in objects)
        {
            if (obj != null)
            {
                obj.SetActive(active);
            }
        }
    }

    /// <summary>
    /// �÷��̾� ���� ���� ������Ʈ���� ���׸���� ���̾� ����
    /// </summary>
    /// <param name="playerObjectColor">�÷��̾��� ���� ����</param>
    public void ObjectColorChange(ObjectColor playerObjectColor)
    {
        // ���� ���������� ��� ���� ����Ʈ ��������
        List<GameObject> redObjects = GetObjectColorObjectsList(ObjectColor.Red, currentStage);
        List<GameObject> blueObjects = GetObjectColorObjectsList(ObjectColor.Blue, currentStage);

        // �÷��̾� ���� ���� ó��
        switch (playerObjectColor)
        {
            case ObjectColor.Red:
                SetObjectsSolid(redObjects, ObjectColor.Red);           // ������ ������Ʈ�� ��������
                SetObjectsTransparent(blueObjects, ObjectColor.Blue);   // �������� �����ϰ�            
                break;

            case ObjectColor.Blue:
                SetObjectsSolid(blueObjects, ObjectColor.Blue);         // �Ķ��� ������Ʈ�� ��������
                SetObjectsTransparent(redObjects, ObjectColor.Red);     // �������� �����ϰ�
                break;
        }

        Debug.Log($"ObjectColor changed to: {playerObjectColor}");
    }

    /// <summary>
    /// ������ ����� ���������� �ش��ϴ� ������Ʈ ����Ʈ ��ȯ
    /// </summary>
    /// <param name="ObjectColor">����</param>
    /// <param name="stage">��������</param>
    /// <returns>�ش��ϴ� ������Ʈ ����Ʈ</returns>
    private List<GameObject> GetObjectColorObjectsList(ObjectColor ObjectColor, int stage)
    {
        switch (stage)
        {
            case 1:
                return GetStage1ObjectColorList(ObjectColor);
            case 2:
                return GetStage2ObjectColorList(ObjectColor);
            case 3:
                return GetStage3ObjectColorList(ObjectColor);
            default:
                Debug.LogWarning($"Invalid stage: {stage}");
                return new List<GameObject>();
        }
    }

    private List<GameObject> GetStage1ObjectColorList(ObjectColor ObjectColor)
    {
        switch (ObjectColor)
        {
            case ObjectColor.Red: return ObjectColorObjectsList1Red;
            case ObjectColor.Blue: return ObjectColorObjectsList1Blue;
            default: return new List<GameObject>();
        }
    }

    private List<GameObject> GetStage2ObjectColorList(ObjectColor ObjectColor)
    {
        switch (ObjectColor)
        {
            case ObjectColor.Red: return ObjectColorObjectsList2Red;
            case ObjectColor.Blue: return ObjectColorObjectsList2Blue;
            default: return new List<GameObject>();
        }
    }

    private List<GameObject> GetStage3ObjectColorList(ObjectColor ObjectColor)
    {
        switch (ObjectColor)
        {
            case ObjectColor.Red: return ObjectColorObjectsList3Red;
            case ObjectColor.Blue: return ObjectColorObjectsList3Blue;
            default: return new List<GameObject>();
        }
    }

    /// <summary>
    /// ������Ʈ���� ����(������) ���·� ����
    /// </summary>
    /// <param name="objects">������ ������Ʈ ����Ʈ</param>
    /// <param name="ObjectColor">����</param>
    private void SetObjectsSolid(List<GameObject> objects, ObjectColor ObjectColor)
    {
        Material solidMaterial = GetSolidMaterial(ObjectColor);

        foreach (GameObject obj in objects)
        {
            if (obj != null && obj.activeInHierarchy) // Ȱ��ȭ�� ������Ʈ�� ó��
            {
                // ���׸��� ����
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = solidMaterial;
                }

                /*
                // �ݶ��̴� Ȱ��ȭ (�ʿ��� ���)
                Collider collider = obj.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = true;
                }
                */
            }
        }
    }

    /// <summary>
    /// ������Ʈ���� ���� ���·� ����
    /// </summary>
    /// <param name="objects">������ ������Ʈ ����Ʈ</param>
    /// <param name="ObjectColor">����</param>
    private void SetObjectsTransparent(List<GameObject> objects, ObjectColor ObjectColor)
    {
        Material transparentMaterial = GetTransparentMaterial(ObjectColor);

        foreach (GameObject obj in objects)
        {
            if (obj != null && obj.activeInHierarchy) // Ȱ��ȭ�� ������Ʈ�� ó��
            {
                // ���׸��� ����
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = transparentMaterial;
                }

                /*
                // �ݶ��̴� ��Ȱ��ȭ (�ʿ��� ���)
                Collider collider = obj.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = false;
                }
                */
            }
        }
    }

    /// <summary>
    /// ���� �ش��ϴ� ���� ���׸��� ��ȯ
    /// </summary>
    private Material GetSolidMaterial(ObjectColor ObjectColor)
    {
        switch (ObjectColor)
        {
            case ObjectColor.Red: return redSolidMaterial;
            case ObjectColor.Blue: return blueSolidMaterial;
            default: return null;
        }
    }

    /// <summary>
    /// ���� �ش��ϴ� ���� ���׸��� ��ȯ
    /// </summary>
    private Material GetTransparentMaterial(ObjectColor ObjectColor)
    {
        switch (ObjectColor)
        {
            case ObjectColor.Red: return redTransparentMaterial;
            case ObjectColor.Blue: return blueTransparentMaterial;
            default: return null;
        }
    }

    /// <summary>
    /// ���� �������� ����
    /// </summary>
    /// <param name="stage">���ο� �������� ��ȣ</param>
    public void SetCurrentStage(int stage)
    {
        currentStage = stage;
        SetActiveObjectsByStage(currentStage); // ���������� ������Ʈ Ȱ��ȭ/��Ȱ��ȭ
        InitializeAllObjects(); // �������� ���� �� �ʱ�ȭ
        Debug.Log($"Stage changed to: {stage}");
    }

    /// <summary>
    /// �ܺο��� ȣ���� �� �ִ� ���� ���� �Լ� (�ٸ� ��ũ��Ʈ���� ���)
    /// </summary>
    /// <param name="playerObjectColor">�÷��̾� ����</param>
    public void OnPlayerObjectColorChanged(ObjectColor playerObjectColor)
    {
        ObjectColorChange(playerObjectColor);
    }

    /// <summary>
    /// ���� �������� ��ȯ (�ܺο��� ������)
    /// </summary>
    public int GetCurrentStage()
    {
        return currentStage;
    }

    /// <summary>
    /// Ư�� ���������� ��� ������Ʈ�� ������ Ȱ��ȭ/��Ȱ��ȭ (����׿�)
    /// </summary>
    /// <param name="stage">��� ��������</param>
    /// <param name="active">Ȱ��ȭ ����</param>
    public void ForceSetStageObjectsActive(int stage, bool active)
    {
        List<GameObject> redObjects = GetObjectColorObjectsList(ObjectColor.Red, stage);
        List<GameObject> blueObjects = GetObjectColorObjectsList(ObjectColor.Blue, stage);

        SetObjectsActive(redObjects, active);
        SetObjectsActive(blueObjects, active);

        Debug.Log($"Stage {stage} objects forced to {(active ? "active" : "inactive")}");
    }
}