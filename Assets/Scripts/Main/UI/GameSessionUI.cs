using UnityEngine;
using TMPro;

/// <summary>GameManager의 데이터를 참조하여 생존 정보를 표시하는 UI 컨트롤러</summary>
public class SurvivalUIController : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] private TMP_Text _monsterCountText;
    [SerializeField] private float _updateIntervalSeconds = 0.1f;
    #endregion

    #region Properties
    public bool IsInitialized { get; private set; }
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        IsInitialized = ValidateReferences();
        if (IsInitialized)
        {
            UpdateAllUI();
            InvokeRepeating(nameof(UpdateAllUI), 0f, _updateIntervalSeconds);
        }
        else
        {
            Debug.LogError("[SurvivalUIController] Initialization failed - check references", this);
        }

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (IsInvoking(nameof(UpdateAllUI)))
        {
            CancelInvoke(nameof(UpdateAllUI));
        }
    }
    #endregion

    #region Public Methods - Manual Control
    /// <summary>UI를 즉시 업데이트합니다</summary>
    public void RefreshUI()
    {
        if (IsInitialized)
        {
            UpdateAllUI();
        }
    }

    /// <summary>자동 업데이트를 중지합니다</summary>
    public void StopAutoUpdate()
    {
        if (IsInvoking(nameof(UpdateAllUI)))
        {
            CancelInvoke(nameof(UpdateAllUI));
        }
    }

    /// <summary>자동 업데이트를 다시 시작합니다</summary>
    public void StartAutoUpdate()
    {
        if (IsInitialized && !IsInvoking(nameof(UpdateAllUI)))
        {
            InvokeRepeating(nameof(UpdateAllUI), 0f, _updateIntervalSeconds);
        }
    }
    #endregion

    #region Private Methods - Initialization
    private bool ValidateReferences()
    {
        bool isValid = true;

        if (GameManager.Instance == null)
        {
            Debug.LogError("[SurvivalUIController] GameManager.Instance is null", this);
            isValid = false;
        }

        if (_monsterCountText == null)
        {
            Debug.LogError("[SurvivalUIController] MonsterCountText is not assigned", this);
            isValid = false;
        }

        return isValid;
    }
    #endregion

    #region Private Methods - UI Update
    private void UpdateAllUI()
    {
        if (GameManager.Instance == null) return;

        UpdateMonsterCountUI();
    }

    private void UpdateMonsterCountUI()
    {
        if (_monsterCountText == null || GameManager.Instance == null) return;

        int eliteCount = GameManager.Instance.ActiveEliteEnemyCount;
        int normalCount = GameManager.Instance.ActiveNormalEnemyCount;

        _monsterCountText.text = $"엘리트: {eliteCount} / 노멀: {normalCount}";
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
    #endregion
}