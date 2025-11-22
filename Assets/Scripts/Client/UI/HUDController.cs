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
    [SerializeField] private GameObject tribesVictoryPanel;
    [SerializeField] private GameObject ottomansVictoryPanel;
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private GameObject reloadingText;

    private MatchController MatchController => MatchController.Instance;

    private PlayerMotor localMotor;

    private void Update()
    {
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
    }

    public void ShowVictory(Team winner)
    {
        if (tribesVictoryPanel != null) tribesVictoryPanel.SetActive(false);
        if (ottomansVictoryPanel != null) ottomansVictoryPanel.SetActive(false);

        if (winner == Team.TeamA) tribesVictoryPanel?.SetActive(true);
        else if (winner == Team.TeamB) ottomansVictoryPanel?.SetActive(true);
    }

    public void HideVictory()
    {
        if (tribesVictoryPanel != null) tribesVictoryPanel.SetActive(false);
        if (ottomansVictoryPanel != null) ottomansVictoryPanel.SetActive(false);
    }
}
