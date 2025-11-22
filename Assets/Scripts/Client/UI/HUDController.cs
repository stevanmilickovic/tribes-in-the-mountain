using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI teamALiveText;
    [SerializeField] private TextMeshProUGUI teamBLiveText;
    [SerializeField] private TextMeshProUGUI teamAReservesText;
    [SerializeField] private TextMeshProUGUI teamBReservesText;
    [SerializeField] private TextMeshProUGUI zoneProgressText;
    [SerializeField] private GameObject tribesVictoryPanel;
    [SerializeField] private GameObject ottomansVictoryPanel;
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private GameObject reloadingText;
    [SerializeField] private GameObject zoneCapturingText;

    private MatchController MatchController => MatchController.Instance;

    private PlayerMotor localMotor;

    private CaptureZone[] zones;

    private void Awake()
    {
        MatchController.Instance.hud = this;
    }

    private void Update()
    {
        if (zones == null || zones.Length == 0)
            zones = FindObjectsByType<CaptureZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (localMotor == null)
        {
            foreach (var m in FindObjectsOfType<PlayerMotor>())
                if (m.IsOwner)
                    localMotor = m;
        }

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
        if (teamAReservesText != null) teamAReservesText.text = MatchController.ReservesA.ToString();
        if (teamBReservesText != null) teamBReservesText.text = MatchController.ReservesB.ToString();

        if (reloadingText != null)
        {
            if (localMotor != null)
                reloadingText.SetActive(localMotor.IsReloadingNet.Value);
            else
                reloadingText.SetActive(false);
        }

        CheckCaptureZones();
    }

    public void ShowVictory(Team winner)
    {
        if (tribesVictoryPanel != null) tribesVictoryPanel.SetActive(false);
        if (ottomansVictoryPanel != null) ottomansVictoryPanel.SetActive(false);

        if (winner == Team.TeamA) tribesVictoryPanel?.SetActive(true);
        else if (winner == Team.TeamB) ottomansVictoryPanel?.SetActive(true);

        zoneCapturingText.SetActive(false);
    }

    public void HideVictory()
    {
        if (tribesVictoryPanel != null) tribesVictoryPanel.SetActive(false);
        if (ottomansVictoryPanel != null) ottomansVictoryPanel.SetActive(false);
    }

    private void CheckCaptureZones()
    {
        if (tribesVictoryPanel.activeSelf || ottomansVictoryPanel.activeSelf) return;

        if (zoneCapturingText != null)
        {
            bool anyZoneCapturing = false;

            if (zones != null)
            {
                foreach (var z in zones)
                {
                    if (z.Attackers > 0)   // <--- NEW LOGIC
                    {
                        anyZoneCapturing = true;
                        break;
                    }
                }
            }

            zoneCapturingText.SetActive(anyZoneCapturing);
        }

        if (zoneProgressText != null)
        {
            float highestProgress = 0f;

            if (zones != null)
            {
                foreach (var z in zones)
                {
                    if (z.Progress > highestProgress)
                        highestProgress = z.Progress;
                }
            }

            int percent = Mathf.RoundToInt(highestProgress * 100f);
            zoneProgressText.text = percent + "%";
        }
    }
}
