using UnityEngine;


public class PlayerColorController : MonoBehaviour
{
    [SerializeField] private Renderer _playerRenderer = null;     // 플레이어 렌더러
    [SerializeField] private ObjectColor _playerColor = ObjectColor.Gray;
    

    public ObjectColor PlayerColor => _playerColor;

    [Header("Materials")]
    [SerializeField] private Material redSolidMaterial;      // 빨간색 원색 메테리얼
    [SerializeField] private Material blueSolidMaterial;     // 파란색 원색 메테리얼

    private Collider[] _cachedColliders;

    private void Start()
    {
        if(_playerRenderer == null)
        {
            _playerRenderer = gameObject.GetComponent<Renderer>();
            if(_playerRenderer == null)
            {
                Debug.LogError("PlayerColorController: Renderer is not assigned!");
            }
        }

        // 모든 Collider 캐싱 (자식 포함)
        _cachedColliders = gameObject.GetComponentsInChildren<Collider>();
        Debug.Log($"Cached {_cachedColliders.Length} colliders for player color changes");
        if(_cachedColliders == null)
        {
            Debug.LogWarning("PlayerColorController: No colliders found on player or its children!");
        }

        var gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            ChangeColor(gameManager.PlayerColor);
        }

    }

    public void ChangeColor(ObjectColor NewColor)
    {
        if (NewColor == PlayerColor)
        {
            return;
        }

        _playerColor = NewColor;
        Debug.Log($"Player color changed to: {_playerColor}");

        //매터리얼 변경
        ChangeMaterial(NewColor);

        // 타겟 레이어 결정
        int targetLayer;
        switch (NewColor)
        {
            case ObjectColor.Red:
                targetLayer = LayerMask.NameToLayer("Red");
                break;
            case ObjectColor.Blue:
                targetLayer = LayerMask.NameToLayer("Blue");
                break;
            default:
                targetLayer = LayerMask.NameToLayer("Default");
                break;
        }

        // GameObject 레이어 변경
        gameObject.layer = targetLayer;

        // 캐싱된 모든 Collider의 레이어 변경
        for (int i = 0; i < _cachedColliders.Length; i++)
        {
            if (_cachedColliders[i] != null && _cachedColliders[i].gameObject != null)
            {
                _cachedColliders[i].gameObject.layer = targetLayer;
            }
        }
    }

    private void ChangeMaterial(ObjectColor NewColor)
    {
        if (!_playerRenderer )
        {
            Debug.LogWarning("PlayerColorController: Renderer is not assigned!");
            return;
        }

        switch(NewColor)
        {
            case ObjectColor.Red:
                _playerRenderer.material = redSolidMaterial;
                Debug.Log("PlayerColorController: Changed to Red Material");
                break;
            case ObjectColor.Blue:
                _playerRenderer.material = blueSolidMaterial;
                Debug.Log("PlayerColorController: Changed to Blue Material");
                break;
            default:
                Debug.LogWarning("PlayerColorController: wrong Color!");
                break;
        }


    }

}

