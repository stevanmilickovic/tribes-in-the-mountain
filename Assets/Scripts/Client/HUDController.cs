using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI teamALiveText;
    [SerializeField] private TextMeshProUGUI teamBLiveText;
    [SerializeField] private GameObject rootPanel;

    private GameMode _mode;

    private void Start()
    {
        _mode = FindObjectOfType<GameMode>();
    }

    private void Update()
    {
        if (_mode == null) _mode = FindObjectOfType<GameMode>();
        if (_mode == null) return;
        if (rootPanel != null) rootPanel.SetActive(true);
        if (timerText != null)
        {
            int s = Mathf.Max(0, _mode.RemainingSeconds);
            int m = s / 60;
            int r = s % 60;
            timerText.text = m.ToString("00") + ":" + r.ToString("00");
        }
        if (teamALiveText != null) teamALiveText.text = _mode.TeamACount.ToString();
        if (teamBLiveText != null) teamBLiveText.text = _mode.TeamBCount.ToString();
    }
}
