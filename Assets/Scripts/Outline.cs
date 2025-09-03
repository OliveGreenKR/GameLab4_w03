using UnityEngine;

public class Outline : MonoBehaviour
{
    [Header("윤곽선 설정")]
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
        // 현재 오브젝트 복제
        outlineObject = Instantiate(gameObject);
        outlineObject.name = gameObject.name + "_Outline";

        // 부모-자식 관계 설정
        outlineObject.transform.SetParent(transform);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one * (1 + outlineWidth);

        // 불필요한 컴포넌트 제거
        if (outlineObject.GetComponent<Outline>())
            DestroyImmediate(outlineObject.GetComponent<Outline>());

        // 윤곽선 머티리얼 적용
        Renderer outlineRenderer = outlineObject.GetComponent<Renderer>();
        if (outlineRenderer != null)
        {
            Material outlineMat = new Material(Shader.Find("Unlit/Color"));
            outlineMat.color = outlineColor;
            outlineRenderer.material = outlineMat;

            // 렌더 순서 조정 (윤곽선이 뒤에 렌더링되도록)
            outlineRenderer.sortingOrder = -1;
        }
    }

    // Inspector에서 실시간으로 값 변경 시 적용
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