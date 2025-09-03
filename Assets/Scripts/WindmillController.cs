using UnityEngine;
using System.Collections.Generic;

public class WindmillController : MonoBehaviour
{
    [Header("ȸ�� ����")]
    public float rotationSpeed = 50f;

    [Header("���� ����")]
    [Range(0.5f, 10f)]
    public float bladeDistance = 2f;
    public List<Transform> blades = new List<Transform>();

    private Vector3[] originalBladePositions;
    private float currentDistance;

    void Start()
    {
        // �ڵ����� �ڽ� ������Ʈ���� ������ ���
        if (blades.Count == 0)
        {
            FindBlades();
        }

        // ���� ��ġ ����
        SaveOriginalPositions();
        currentDistance = bladeDistance;
    }

    void Update()
    {
        // ǳ�� ȸ��
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);

        // �Ÿ��� ����Ǿ��� �� ���� ��ġ ������Ʈ
        if (Mathf.Abs(currentDistance - bladeDistance) > 0.01f)
        {
            UpdateBladePositions();
            currentDistance = bladeDistance;
        }
    }

    void FindBlades()
    {
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

    void UpdateBladePositions()
    {
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
        if (Application.isPlaying && originalBladePositions != null)
        {
            UpdateBladePositions();
        }
    }

    // ����׿� UI ���� ǥ��
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 20), $"���� �Ÿ�: {bladeDistance:F2}");
        GUI.Label(new Rect(10, 30, 300, 20), $"ȸ�� �ӵ�: {rotationSpeed:F1}");
    }
}