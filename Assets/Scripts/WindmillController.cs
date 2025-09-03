using UnityEngine;
using System.Collections.Generic;

public class WindmillController : MonoBehaviour
{
    [Header("ȸ�� ����")]
    public float rotationSpeed = 50f;

    [Header("���� ����")]
    [Range(0.5f, 10f)]
    public float bladeDistance = 2f;

    [Header("�ڵ� ��ġ ����")]
    public bool autoArrangeBlade = true;
    [Range(0f, 360f)]
    public float startAngle = 0f;  // ù ��° ������ ���� ����

    [Header("���� ���� (autoArrangeBlade�� false�� ���� ���)")]
    public List<Transform> blades = new List<Transform>();

    private Vector3[] originalBladePositions;
    private float currentDistance;
    private bool currentAutoArrange;
    private float currentStartAngle;
    private int previousBladeCount;

    void Start()
    {
        // �ڵ����� �ڽ� ������Ʈ���� ������ ���
        if (blades.Count == 0)
        {
            FindBlades();
        }

        InitializeBlade();

        // ���� ���� ����
        currentDistance = bladeDistance;
        currentAutoArrange = autoArrangeBlade;
        currentStartAngle = startAngle;
        previousBladeCount = blades.Count;
    }

    void Update()
    {
        // ǳ�� ȸ��
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);

        // ������ ����Ǿ��� �� ���� ��ġ ������Ʈ
        bool needUpdate = false;

        // �Ÿ� ���� üũ
        if (Mathf.Abs(currentDistance - bladeDistance) > 0.01f)
        {
            needUpdate = true;
            currentDistance = bladeDistance;
        }

        // �ڵ� ��ġ ��� ���� üũ
        if (currentAutoArrange != autoArrangeBlade)
        {
            needUpdate = true;
            currentAutoArrange = autoArrangeBlade;
        }

        // ���� ���� ���� üũ
        if (autoArrangeBlade && Mathf.Abs(currentStartAngle - startAngle) > 0.1f)
        {
            needUpdate = true;
            currentStartAngle = startAngle;
        }

        // ���� ���� ���� üũ
        if (previousBladeCount != blades.Count)
        {
            needUpdate = true;
            previousBladeCount = blades.Count;
        }

        if (needUpdate)
        {
            UpdateBladePositions();
        }
    }

    void FindBlades()
    {
        blades.Clear();

        // �ڽ� ������Ʈ �߿��� "Blade" �Ǵ� "Wing"�� ���Ե� �̸��� ������Ʈ���� ã��
        foreach (Transform child in transform)
        {
            if (child.name.ToLower().Contains("blade") ||
                child.name.ToLower().Contains("wing") ||
                child.name.ToLower().Contains("����"))
            {
                blades.Add(child);
            }
        }

        // ���� Ư���� �̸��� ���ٸ� ��� �ڽ��� ������ ����
        if (blades.Count == 0)
        {
            foreach (Transform child in transform)
            {
                blades.Add(child);
            }
        }
    }

    void InitializeBlade()
    {
        if (!autoArrangeBlade)
        {
            // ���� ����� ���� ���� ��ġ ����
            SaveOriginalPositions();
        }
        else
        {
            // �ڵ� ��ġ ����� ���� �ٷ� ��ġ
            ArrangeBladesInCircle();
        }
    }

    void SaveOriginalPositions()
    {
        originalBladePositions = new Vector3[blades.Count];
        for (int i = 0; i < blades.Count; i++)
        {
            if (blades[i] != null)
            {
                originalBladePositions[i] = blades[i].localPosition;
            }
        }
    }

    void ArrangeBladesInCircle()
    {
        if (blades.Count == 0) return;

        float angleStep = 360f / blades.Count;  // ���� ������ ����

        for (int i = 0; i < blades.Count; i++)
        {
            if (blades[i] != null)
            {
                // �� ������ ���� ��� (���� ���� + �ܰ躰 ����)
                float angle = startAngle + (angleStep * i);
                float radian = angle * Mathf.Deg2Rad;

                // ���� ��ġ: X-Z ��鿡�� ��ġ (Y�� �߽� ȸ��)
                Vector3 position = new Vector3(
                    Mathf.Sin(radian) * bladeDistance,
                    0f,
                    Mathf.Cos(radian) * bladeDistance
                );

                blades[i].localPosition = position;

                // ������ �߽��� ���ϵ��� ȸ�� (���û���)
                // blades[i].localRotation = Quaternion.LookRotation(-position.normalized, Vector3.up);
            }
        }

        // �ڵ� ��ġ �� ���� ��ġ�� ���� ��ġ�� ����
        SaveOriginalPositions();
    }

    void UpdateBladePositions()
    {
        if (autoArrangeBlade)
        {
            // �ڵ� ��ġ ����� ���� �������� ���ġ
            ArrangeBladesInCircle();
        }
        else
        {
            // ���� ����� ���� ���� ��� ���
            UpdateManualBladePositions();
        }
    }

    void UpdateManualBladePositions()
    {
        if (originalBladePositions == null) return;

        for (int i = 0; i < blades.Count; i++)
        {
            if (blades[i] != null && i < originalBladePositions.Length)
            {
                // ���� ��ġ������ ������ ����
                Vector3 direction = originalBladePositions[i].normalized;
                // ���ο� �Ÿ��� ��ġ ����
                blades[i].localPosition = direction * bladeDistance;
            }
        }
    }

    // Inspector���� �� ���� �� �ǽð� ����
    void OnValidate()
    {
        if (Application.isPlaying && blades != null && blades.Count > 0)
        {
            UpdateBladePositions();
        }
    }

    // ��Ÿ�ӿ��� ������ �ٽ� ã�� �Լ� (���ο� �ڽ��� �߰��Ǿ��� �� ��)
    [ContextMenu("���� �ٽ� ã��")]
    public void RefreshBlades()
    {
        FindBlades();
        InitializeBlade();
        UpdateBladePositions();
        previousBladeCount = blades.Count;
    }

    // ���� ��ġ�� ���� ��ġ�� �����ϴ� �Լ�
    [ContextMenu("���� ��ġ ����")]
    public void SaveCurrentPositions()
    {
        SaveOriginalPositions();
    }
}