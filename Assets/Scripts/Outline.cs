using UnityEngine;

public class Outline : MonoBehaviour
{
    [Header("������ ����")]
    public Color outlineColor = Color.black;
    [Range(0.01f, 0.1f)]
    public float outlineWidth = 0.03f;

    private GameObject outlineObject;

    void Start()
    {
        CreateOutline();
    }

    void CreateOutline()
    {
        // ���� ������Ʈ ����
        outlineObject = Instantiate(gameObject);
        outlineObject.name = gameObject.name + "_Outline";

        // �θ�-�ڽ� ���� ����
        outlineObject.transform.SetParent(transform);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one * (1 + outlineWidth);

        // ���ʿ��� ������Ʈ ����
        if (outlineObject.GetComponent<Outline>())
            DestroyImmediate(outlineObject.GetComponent<Outline>());

        // ������ ��Ƽ���� ����
        Renderer outlineRenderer = outlineObject.GetComponent<Renderer>();
        if (outlineRenderer != null)
        {
            Material outlineMat = new Material(Shader.Find("Unlit/Color"));
            outlineMat.color = outlineColor;
            outlineRenderer.material = outlineMat;

            // ���� ���� ���� (�������� �ڿ� �������ǵ���)
            outlineRenderer.sortingOrder = -1;
        }
    }

    // Inspector���� �ǽð����� �� ���� �� ����
    void OnValidate()
    {
        if (Application.isPlaying && outlineObject != null)
        {
            outlineObject.transform.localScale = Vector3.one * (1 + outlineWidth);

            Renderer outlineRenderer = outlineObject.GetComponent<Renderer>();
            if (outlineRenderer != null)
            {
                outlineRenderer.material.color = outlineColor;
            }
        }
    }
}