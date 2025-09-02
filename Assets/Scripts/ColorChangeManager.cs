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
    [SerializeField] private Material redSolidMaterial;      // 빨간색 원색 메테리얼
    [SerializeField] private Material redTransparentMaterial; // 빨간색 불투명 메테리얼
    [SerializeField] private Material blueSolidMaterial;     // 파란색 원색 메테리얼
    [SerializeField] private Material blueTransparentMaterial; // 파란색 불투명 메테리얼

    [Header("Current Options")]
    [SerializeField] private int currentStage = 1;     // 현재 스테이지 - 나중에 GameManager에서 참조 가능
    public ObjectColor playerObjectColor; // 현재 플레이어 색상
    GameObject testPlayer; // 테스트용 플레이어 오브젝트

    private void Start()
    {
        // 초기 상태 설정
        InitializeAllObjects();
        SetActiveObjectsByStage(currentStage); // 현재 스테이지에 맞는 오브젝트만 활성화
        Debug.Log($"ColorChangeManager initialized. Current Stage: {currentStage}, Player ObjectColor: {playerObjectColor}");
    }

    // 테스트용
    private void Update()
    {
        // 좌클릭을 해서 플레이어 색상 변경 (테스트용)
        if (Input.GetMouseButtonDown(0))
        {
            playerObjectColor = playerObjectColor == ObjectColor.Red ? ObjectColor.Blue : ObjectColor.Red;
            Debug.Log($"Player ObjectColor changed to: {playerObjectColor}");
            ObjectColorChange(playerObjectColor);
        }

        // 숫자키로 스테이지 변경 (테스트용)
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetCurrentStage(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetCurrentStage(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetCurrentStage(3);
    }

    /// <summary>
    /// 모든 오브젝트를 초기 상태(투명)로 설정
    /// </summary>
    private void InitializeAllObjects()
    {
        // 플레이어 색상에 따라 현재 스테이지의 색상 오브젝트를 투명하게 설정
        if (playerObjectColor == ObjectColor.Red)
        {  // 플레이어 색상이 빨간색이면 파란색 오브젝트들 투명하게
            SetObjectsSolid(GetObjectColorObjectsList(ObjectColor.Red, currentStage), ObjectColor.Red);
            SetObjectsTransparent(GetObjectColorObjectsList(ObjectColor.Blue, currentStage), ObjectColor.Blue);
        }
        else if (playerObjectColor == ObjectColor.Blue)
        {  // 플레이어 색상이 파란색이면 빨간색 오브젝트들 투명하게
            SetObjectsSolid(GetObjectColorObjectsList(ObjectColor.Blue, currentStage), ObjectColor.Blue);
            SetObjectsTransparent(GetObjectColorObjectsList(ObjectColor.Red, currentStage), ObjectColor.Red);
        }
    }

    /// <summary>
    /// 스테이지에 따라 오브젝트들의 활성화/비활성화 설정
    /// </summary>
    /// <param name="activeStage">활성화할 스테이지 번호</param>
    private void SetActiveObjectsByStage(int activeStage)
    {
        // 모든 스테이지 순회하며 활성화/비활성화 설정
        for (int stage = 1; stage <= 3; stage++)
        {
            bool shouldActivate = (stage == activeStage);

            // 각 스테이지의 빨간색과 파란색 오브젝트 리스트 가져오기
            List<GameObject> redObjects = GetObjectColorObjectsList(ObjectColor.Red, stage);
            List<GameObject> blueObjects = GetObjectColorObjectsList(ObjectColor.Blue, stage);

            // 오브젝트들 활성화/비활성화
            SetObjectsActive(redObjects, shouldActivate);
            SetObjectsActive(blueObjects, shouldActivate);
        }

        Debug.Log($"Stage {activeStage} objects activated, others deactivated");
    }

    /// <summary>
    /// 오브젝트 리스트의 활성화 상태 설정
    /// </summary>
    /// <param name="objects">대상 오브젝트 리스트</param>
    /// <param name="active">활성화 여부</param>
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
    /// 플레이어 색상에 따라 오브젝트들의 메테리얼과 레이어 변경
    /// </summary>
    /// <param name="playerObjectColor">플레이어의 현재 색상</param>
    public void ObjectColorChange(ObjectColor playerObjectColor)
    {
        // 현재 스테이지의 모든 색상 리스트 가져오기
        List<GameObject> redObjects = GetObjectColorObjectsList(ObjectColor.Red, currentStage);
        List<GameObject> blueObjects = GetObjectColorObjectsList(ObjectColor.Blue, currentStage);

        // 플레이어 색상에 따른 처리
        switch (playerObjectColor)
        {
            case ObjectColor.Red:
                SetObjectsSolid(redObjects, ObjectColor.Red);           // 빨간색 오브젝트를 원색으로
                SetObjectsTransparent(blueObjects, ObjectColor.Blue);   // 나머지는 투명하게            
                break;

            case ObjectColor.Blue:
                SetObjectsSolid(blueObjects, ObjectColor.Blue);         // 파란색 오브젝트를 원색으로
                SetObjectsTransparent(redObjects, ObjectColor.Red);     // 나머지는 투명하게
                break;
        }

        Debug.Log($"ObjectColor changed to: {playerObjectColor}");
    }

    /// <summary>
    /// 지정된 색상과 스테이지에 해당하는 오브젝트 리스트 반환
    /// </summary>
    /// <param name="ObjectColor">색상</param>
    /// <param name="stage">스테이지</param>
    /// <returns>해당하는 오브젝트 리스트</returns>
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
    /// 오브젝트들을 원색(불투명) 상태로 변경
    /// </summary>
    /// <param name="objects">변경할 오브젝트 리스트</param>
    /// <param name="ObjectColor">색상</param>
    private void SetObjectsSolid(List<GameObject> objects, ObjectColor ObjectColor)
    {
        Material solidMaterial = GetSolidMaterial(ObjectColor);

        foreach (GameObject obj in objects)
        {
            if (obj != null && obj.activeInHierarchy) // 활성화된 오브젝트만 처리
            {
                // 메테리얼 변경
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = solidMaterial;
                }

                /*
                // 콜라이더 활성화 (필요한 경우)
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
    /// 오브젝트들을 투명 상태로 변경
    /// </summary>
    /// <param name="objects">변경할 오브젝트 리스트</param>
    /// <param name="ObjectColor">색상</param>
    private void SetObjectsTransparent(List<GameObject> objects, ObjectColor ObjectColor)
    {
        Material transparentMaterial = GetTransparentMaterial(ObjectColor);

        foreach (GameObject obj in objects)
        {
            if (obj != null && obj.activeInHierarchy) // 활성화된 오브젝트만 처리
            {
                // 메테리얼 변경
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = transparentMaterial;
                }

                /*
                // 콜라이더 비활성화 (필요한 경우)
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
    /// 색상에 해당하는 원색 메테리얼 반환
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
    /// 색상에 해당하는 투명 메테리얼 반환
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
    /// 현재 스테이지 변경
    /// </summary>
    /// <param name="stage">새로운 스테이지 번호</param>
    public void SetCurrentStage(int stage)
    {
        currentStage = stage;
        SetActiveObjectsByStage(currentStage); // 스테이지별 오브젝트 활성화/비활성화
        InitializeAllObjects(); // 스테이지 변경 시 초기화
        Debug.Log($"Stage changed to: {stage}");
    }

    /// <summary>
    /// 외부에서 호출할 수 있는 색상 변경 함수 (다른 스크립트에서 사용)
    /// </summary>
    /// <param name="playerObjectColor">플레이어 색상</param>
    public void OnPlayerObjectColorChanged(ObjectColor playerObjectColor)
    {
        ObjectColorChange(playerObjectColor);
    }

    /// <summary>
    /// 현재 스테이지 반환 (외부에서 참조용)
    /// </summary>
    public int GetCurrentStage()
    {
        return currentStage;
    }

    /// <summary>
    /// 특정 스테이지의 모든 오브젝트를 강제로 활성화/비활성화 (디버그용)
    /// </summary>
    /// <param name="stage">대상 스테이지</param>
    /// <param name="active">활성화 여부</param>
    public void ForceSetStageObjectsActive(int stage, bool active)
    {
        List<GameObject> redObjects = GetObjectColorObjectsList(ObjectColor.Red, stage);
        List<GameObject> blueObjects = GetObjectColorObjectsList(ObjectColor.Blue, stage);

        SetObjectsActive(redObjects, active);
        SetObjectsActive(blueObjects, active);

        Debug.Log($"Stage {stage} objects forced to {(active ? "active" : "inactive")}");
    }
}