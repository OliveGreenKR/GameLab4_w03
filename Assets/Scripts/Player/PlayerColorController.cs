using UnityEngine;


public class PlayerColorController : MonoBehaviour
{
    [SerializeField] private Renderer _playerRenderer = null;     // �÷��̾� ������
    [SerializeField] private ObjectColor _playerColor = ObjectColor.Gray;

    public ObjectColor PlayerColor => _playerColor;

    [Header("Materials")]
    [SerializeField] private Material redSolidMaterial;      // ������ ���� ���׸���
    [SerializeField] private Material blueSolidMaterial;     // �Ķ��� ���� ���׸���

    private Collider[] _cachedColliders;

    private void Start()
    {
        var gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            ChangeColor(gameManager.PlayerColor);
        }

        if(_playerRenderer == null)
        {
            _playerRenderer = gameObject.GetComponent<Renderer>();
            if(_playerRenderer == null)
            {
                Debug.LogError("PlayerColorController: Renderer is not assigned!");
            }
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

        //���͸��� ����
        ChangeMaterial(NewColor);

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

        //// ĳ�̵� ��� Collider�� ���̾� ����
        //for (int i = 0; i < _cachedColliders.Length; i++)
        //{
        //    if (_cachedColliders[i] != null)
        //    {
        //        _cachedColliders[i].gameObject.layer = targetLayer;
        //    }
        //}
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

