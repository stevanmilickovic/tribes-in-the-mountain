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
        if (teamAButton != null) teamAButton.onClick.AddListener(() => Submit(MatchController.TeamAId));
        if (teamBButton != null) teamBButton.onClick.AddListener(() => Submit(MatchController.TeamBId));
        SetInteractable(false);
    }

    private void Update()
    {
        if (gateway == null)
            gateway = FindObjectOfType<LobbySelectionGateway>();

        bool ready = InstanceFinder.IsClientStarted && gateway != null && gateway.IsClientInitialized;
        if (teamAButton != null && teamAButton.interactable != ready) SetInteractable(ready);
    }

    private void Submit(string teamId)
    {
        if (!InstanceFinder.IsClientStarted) return;
        if (gateway == null || !gateway.IsClientInitialized) return;
        if (string.IsNullOrWhiteSpace(teamId)) return;

        gateway.SubmitTeamChoice(teamId);

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
