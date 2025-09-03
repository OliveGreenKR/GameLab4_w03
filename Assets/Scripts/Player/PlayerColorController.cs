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

        // ��� Collider ĳ�� (�ڽ� ����)
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

        // Ÿ�� ���̾� ����
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

        // GameObject ���̾� ����
        gameObject.layer = targetLayer;

        // ĳ�̵� ��� Collider�� ���̾� ����
        for (int i = 0; i < _cachedColliders.Length; i++)
        {
            if (_cachedColliders[i] != null)
            {
                _cachedColliders[i].gameObject.layer = targetLayer;
            }
        }
    }

}

