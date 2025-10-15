using UnityEngine;
using UnityEngine.UI;
using FishNet;

public class TeamSelectUI : MonoBehaviour
{
    [SerializeField] private Button teamAButton;
    [SerializeField] private Button teamBButton;
    [SerializeField] private GameObject rootPanel;

    private LobbySelectionGateway _gateway;

    private void Awake()
    {
        teamAButton.onClick.AddListener(() => Submit(Team.TeamA));
        teamBButton.onClick.AddListener(() => Submit(Team.TeamB));
        SetInteractable(false);
    }

    private void Update()
    {
        if (_gateway == null)
            _gateway = FindObjectOfType<LobbySelectionGateway>();

        bool ready = InstanceFinder.IsClientStarted && _gateway != null && _gateway.IsClientInitialized;
        if (teamAButton.interactable != ready) SetInteractable(ready);
    }

    private void Submit(Team t)
    {
        if (!InstanceFinder.IsClientStarted) return;
        if (_gateway == null || !_gateway.IsClientInitialized) return;

        _gateway.SubmitTeamChoice(t);
        if (rootPanel != null) rootPanel.SetActive(false);
    }

    private void SetInteractable(bool v)
    {
        if (teamAButton != null) teamAButton.interactable = v;
        if (teamBButton != null) teamBButton.interactable = v;
    }
}
