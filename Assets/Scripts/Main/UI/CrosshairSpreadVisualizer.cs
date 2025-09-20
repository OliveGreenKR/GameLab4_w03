using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AccuracySystem의 CrosshairSpread에 따라 Image 스케일 조절
/// </summary>
public class CrosshairSpreadVisualizer : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] private AccuracySystem _accuracySystem;
    [SerializeField] private float _maxAdditionalScale = 2f;
    [SerializeField] private Image _targetImage;
    #endregion

    #region Private Fields
    private Vector3 _originalScale = Vector3.one;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        if (_targetImage == null)
        {
            _targetImage = GetComponent<Image>();
        }

        if (_targetImage != null)
        {
            _originalScale = _targetImage.transform.localScale;
        }
    }

    private void Update()
    {
        if (_targetImage != null)
        {
            float scale = CalculateScale();
            _targetImage.transform.localScale = _originalScale * scale;
        }
    }
    #endregion

    #region Private Methods
    private float CalculateScale()
    {
        if (_accuracySystem == null)
            return 1f;

        float crosshairSpread = _accuracySystem.GetCrosshairSpread();
        return 1f + _maxAdditionalScale * crosshairSpread;
    }
    #endregion
}