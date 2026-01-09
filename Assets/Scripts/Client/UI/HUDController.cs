using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("Core HUD")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI teamALiveText;
    [SerializeField] private TextMeshProUGUI teamBLiveText;
    [SerializeField] private TextMeshProUGUI teamAReservesText;
    [SerializeField] private TextMeshProUGUI teamBReservesText;

    [Header("Zones / Bleed (optional)")]
    [SerializeField] private TextMeshProUGUI teamAZonesText;
    [SerializeField] private TextMeshProUGUI teamBZonesText;
    [SerializeField] private TextMeshProUGUI bleedStatusText;
    [SerializeField] private bool includeUncapturableZonesInBleedUI = false;
    [SerializeField] private string teamAName = "Team A";
    [SerializeField] private string teamBName = "Team B";

    [Header("Capture UI")]
    [SerializeField] private TextMeshProUGUI zoneProgressText;
    [SerializeField] private GameObject zoneCapturingText;

    [Header("Panels")]
    [SerializeField] private GameObject tribesVictoryPanel;
    [SerializeField] private GameObject ottomansVictoryPanel;
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private GameObject reloadingText;

    [SerializeField] private GameObject spawnSelectPrompt;

    private MatchController MatchController => MatchController.Instance;

    private PlayerMotor localMotor;
    private PlayerHealth localHealth;

    private CaptureZone[] zones;

    private void Awake()
    {
        MatchController.Instance.hud = this;
    }

    private void Update()
    {
        if (zones == null || zones.Length == 0)
            zones = MatchController != null && MatchController.zones != null && MatchController.zones.Length > 0
                ? MatchController.zones
                : FindObjectsByType<CaptureZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (localMotor == null)
        {
            foreach (var m in FindObjectsOfType<PlayerMotor>())
            {
                if (m.IsOwner)
                {
                    localMotor = m;
                    break;
                }
            }
        }

        if (localHealth == null)
        {
            foreach (var h in FindObjectsOfType<PlayerHealth>())
                if (h.IsOwner)
                    localHealth = h;
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

        if (spawnSelectPrompt != null) spawnSelectPrompt.SetActive(localHealth != null && !localHealth.IsAlive);

        if (reloadingText != null)
            reloadingText.SetActive(localMotor != null && localMotor.IsReloadingNet.Value);

        UpdateZoneCountsAndBleedStatus();
        CheckCaptureZones();
    }

    private void UpdateZoneCountsAndBleedStatus()
    {
        if (tribesVictoryPanel != null && tribesVictoryPanel.activeSelf) { SetBleedText(""); return; }
        if (ottomansVictoryPanel != null && ottomansVictoryPanel.activeSelf) { SetBleedText(""); return; }

        int a = 0;
        int b = 0;

        if (zones != null)
        {
            foreach (var z in zones)
            {
                if (!z) continue;
                if (!includeUncapturableZonesInBleedUI && z.Uncapturable) continue;

                if (z.Owner == Team.TeamA) a++;
                else if (z.Owner == Team.TeamB) b++;
            }
        }

        if (teamAZonesText != null) teamAZonesText.text = a.ToString();
        if (teamBZonesText != null) teamBZonesText.text = b.ToString();

        if (bleedStatusText != null)
        {
            if (a == b) SetBleedText("");
            else if (a > b) SetBleedText($"{teamBName} bleeding reserves");
            else SetBleedText($"{teamAName} bleeding reserves");
        }
    }

    private void SetBleedText(string s)
    {
        if (bleedStatusText == null) return;

        bleedStatusText.text = s;
        bleedStatusText.gameObject.SetActive(!string.IsNullOrWhiteSpace(s));
    }

    public void ShowVictory(Team winner)
    {
        if (tribesVictoryPanel != null) tribesVictoryPanel.SetActive(false);
        if (ottomansVictoryPanel != null) ottomansVictoryPanel.SetActive(false);

        if (winner == Team.TeamA) tribesVictoryPanel?.SetActive(true);
        else if (winner == Team.TeamB) ottomansVictoryPanel?.SetActive(true);

        if (zoneCapturingText != null) zoneCapturingText.SetActive(false);
        SetBleedText("");
    }

    public void HideVictory()
    {
        if (tribesVictoryPanel != null) tribesVictoryPanel.SetActive(false);
        if (ottomansVictoryPanel != null) ottomansVictoryPanel.SetActive(false);
        SetBleedText("");
    }

    private void CheckCaptureZones()
    {
        if ((tribesVictoryPanel != null && tribesVictoryPanel.activeSelf) ||
            (ottomansVictoryPanel != null && ottomansVictoryPanel.activeSelf))
            return;

        bool anyContested = false;
        float highestProgress = 0f;

        if (zones != null)
        {
            foreach (var z in zones)
            {
                if (!z) continue;

                if (z.IsContested)
                    anyContested = true;

                if (z.Progress > highestProgress)
                    highestProgress = z.Progress;
            }
        }

        if (zoneCapturingText != null)
            zoneCapturingText.SetActive(anyContested);

        if (zoneProgressText != null)
        {
            int percent = Mathf.RoundToInt(highestProgress * 100f);
            zoneProgressText.text = percent + "%";
        }
    }
}
