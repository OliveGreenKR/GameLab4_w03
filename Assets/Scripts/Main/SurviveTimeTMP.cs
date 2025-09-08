using TMPro;
using UnityEngine;

public class SurviveTimeTMP : MonoBehaviour
{

    #region Serialized Fields
    [SerializeField] private TextMeshProUGUI _survivalTimeText;
    #endregion

    #region Unity Lifecycle
    private void Update()
    {
        if (_survivalTimeText != null && GameManager.Instance != null)
        {
            //_survivalTimeText.text = GameManager.Instance.GetFormattedSurvivalTime();
        }
    }
    #endregion
}
