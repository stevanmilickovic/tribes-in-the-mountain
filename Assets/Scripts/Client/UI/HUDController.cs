using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI teamALiveText;
    [SerializeField] private TextMeshProUGUI teamBLiveText;
    [SerializeField] private GameObject rootPanel;

    private MatchController MatchController => MatchController.Instance;

    private void Update()
    {
        if (rootPanel != null) rootPanel.SetActive(true);
        if (timerText != null)
        {
            int s = Mathf.Max(0, MatchController.RemainingSeconds);
            int m = s / 60;
            int r = s % 60;
            timerText.text = m.ToString("00") + ":" + r.ToString("00");
        }
        if (teamALiveText != null) teamALiveText.text = MatchController.AliveA.ToString();
        if (teamBLiveText != null) teamBLiveText.text = MatchController.AliveB.ToString();
    }
}
