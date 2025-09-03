using UnityEngine;


public class PlayerColorController : MonoBehaviour
{
    [SerializeField] private ObjectColor _playerColor = ObjectColor.Gray;

    public ObjectColor PlayerColor => _playerColor;

    private Collider[] _cachedColliders;

    private void Start()
    {
        var gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            ChangeColor(gameManager.PlayerColor);
        }

        // 모든 Collider 캐싱 (자식 포함)
        _cachedColliders = gameObject.GetComponentsInChildren<Collider>();
        Debug.Log($"Cached {_cachedColliders.Length} colliders for player color changes");
    }

    public void ChangeColor(ObjectColor NewColor)
    {
        if (NewColor == PlayerColor)
        {
            return;
        }

        _playerColor = NewColor;
        Debug.Log($"Player color changed to: {_playerColor}");

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
            if (_cachedColliders[i] != null)
            {
                _cachedColliders[i].gameObject.layer = targetLayer;
            }
        }
    }

}

