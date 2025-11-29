using UnityEngine;
using UnityEngine.UI;
using FishNet.Object;
using System.Collections.Generic;

public class MiniMapController : NetworkBehaviour
{
    [SerializeField] private RectTransform mapPanel;
    [SerializeField] private RectTransform mapImage;
    [SerializeField] private RectTransform playerIconPrefab;
    [SerializeField] private RectTransform teammateIconPrefab;
    [SerializeField] private RectTransform zoneIconPrefab;
    [SerializeField] private Sprite flagTeamA;
    [SerializeField] private Sprite flagTeamB;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    [SerializeField] private Vector2 worldBottomLeft;
    [SerializeField] private Vector2 worldTopRight;

    private PlayerTeam localTeam;
    private List<PlayerTeam> teammates = new();
    private List<CaptureZone> zones = new();
    private Dictionary<PlayerTeam, RectTransform> teammateIcons = new();
    private Dictionary<CaptureZone, RectTransform> zoneIcons = new();
    private RectTransform localPlayerIcon;
    private Transform localPlayer;

    private void Start()
    {
        CacheLocal();
        CacheTeams();
        CacheZones();
        CreateIcons();
        mapPanel.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey)) mapPanel.gameObject.SetActive(true);
        if (Input.GetKeyUp(toggleKey)) mapPanel.gameObject.SetActive(false);
        if (!mapPanel.gameObject.activeSelf) return;

        UpdateLocalIcon();
        UpdateTeammateIcons();
        UpdateZoneIcons();
    }

    private void CacheLocal()
    {
        var players = FindObjectsByType<PlayerTeam>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p.IsOwner)
            {
                localTeam = p;
                localPlayer = p.transform;
                break;
            }
        }
    }

    private void CacheTeams()
    {
        var all = FindObjectsByType<PlayerTeam>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var t in all)
            if (t != localTeam && t.team.Value == localTeam.team.Value)
                teammates.Add(t);
    }

    private void CacheZones()
    {
        var mc = MatchController.Instance;
        foreach (var z in mc.zones)
            zones.Add(z);
    }

    private void CreateIcons()
    {
        localPlayerIcon = Instantiate(playerIconPrefab, mapImage);

        foreach (var t in teammates)
        {
            var icon = Instantiate(teammateIconPrefab, mapImage);
            teammateIcons[t] = icon;
        }

        foreach (var z in zones)
        {
            var icon = Instantiate(zoneIconPrefab, mapImage);
            zoneIcons[z] = icon;
        }
    }

    private Vector2 WorldToMapPosition(Vector3 world)
    {
        float normX = Mathf.InverseLerp(worldBottomLeft.x, worldTopRight.x, world.x);
        float normY = Mathf.InverseLerp(worldBottomLeft.y, worldTopRight.y, world.z);

        float px = (normX - mapImage.pivot.x) * mapImage.rect.width;
        float py = (normY - mapImage.pivot.y) * mapImage.rect.height;

        return new Vector2(px, py);
    }

    private void UpdateLocalIcon()
    {
        if (localPlayer == null)
        {
            CacheLocal();
        }
        var pos = WorldToMapPosition(localPlayer.position);
        localPlayerIcon.anchoredPosition = pos;
        localPlayerIcon.localRotation = Quaternion.Euler(0, 0, -localPlayer.eulerAngles.y);
    }

    private void UpdateTeammateIcons()
    {
        foreach (var kvp in teammateIcons)
        {
            if (kvp.Key == null) continue;

            var pos = WorldToMapPosition(kvp.Key.transform.position);
            kvp.Value.anchoredPosition = pos;
            kvp.Value.localRotation = Quaternion.Euler(0, 0, -kvp.Key.transform.eulerAngles.y);
        }
    }

    private void UpdateZoneIcons()
    {
        foreach (var kvp in zoneIcons)
        {
            var z = kvp.Key;
            var tr = kvp.Value;

            var pos = WorldToMapPosition(z.transform.position);
            tr.anchoredPosition = pos;
            tr.GetComponent<Image>().sprite = z.Owner == Team.TeamA ? flagTeamA : flagTeamB;
        }
    }
}
