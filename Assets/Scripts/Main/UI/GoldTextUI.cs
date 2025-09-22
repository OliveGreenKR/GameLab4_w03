using UnityEngine;
using TMPro;

public class GoldDisplayUI : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] private TMP_Text _goldText;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        GameManager.Instance.OnGoldChanged -= OnGoldChanged;
        GameManager.Instance.OnGoldChanged += OnGoldChanged;
        UpdateGoldDisplay(GameManager.Instance.CurrentGold);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGoldChanged -= OnGoldChanged;
        }
    }
    #endregion

    #region Private Methods - Event Handling
    private void OnGoldChanged(int prev, int newGoldAmount)
    {
        UpdateGoldDisplay(newGoldAmount);
    }
    #endregion

    #region Private Methods - UI Update
    private void UpdateGoldDisplay(int goldAmount)
    {
        if (_goldText != null)
        {
            _goldText.text = $"{goldAmount.ToString("N0")} Gold";
        }
    }
    #endregion
}