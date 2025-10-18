using UnityEngine;
using UnityEngine.UI;
using FishNet;

public class TeamSelectUI : MonoBehaviour
{
    [SerializeField] private Button teamAButton;
    [SerializeField] private Button teamBButton;
    [SerializeField] private GameObject rootPanel;

    private LobbySelectionGateway gateway;

    private void Awake()
    {
        teamAButton.onClick.AddListener(() => Submit(Team.TeamA));
        teamBButton.onClick.AddListener(() => Submit(Team.TeamB));
        SetInteractable(false);
    }

    private void Update()
    {
        if (gateway == null)
            gateway = FindObjectOfType<LobbySelectionGateway>();

        bool ready = InstanceFinder.IsClientStarted && gateway != null && gateway.IsClientInitialized;
        if (teamAButton.interactable != ready) SetInteractable(ready);
    }

    private void Submit(Team t)
    {
        if (!InstanceFinder.IsClientStarted) return;
        if (gateway == null || !gateway.IsClientInitialized) return;

        gateway.SubmitTeamChoice(t);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (rootPanel != null) rootPanel.SetActive(false);
    }

    private void SetInteractable(bool v)
    {
        if (teamAButton != null) teamAButton.interactable = v;
        if (teamBButton != null) teamBButton.interactable = v;
    }
}
