using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniMapController : NetworkBehaviour
{
    public static MiniMapController Instance { get; private set; }

    [SerializeField] private RectTransform mapPanel;
    [SerializeField] private RectTransform mapImage;

    [Header("Icon Prefabs")]
    [SerializeField] private RectTransform playerIconPrefab;
    [SerializeField] private RectTransform teammateIconPrefab;
    [SerializeField] private RectTransform zoneIconPrefab;

    [Header("Zone Sprites")]
    [SerializeField] private Sprite flagTeamA;
    [SerializeField] private Sprite flagTeamB;
    [SerializeField] private Sprite flagNeutral;
    [SerializeField] private Sprite lockOverlaySprite;

    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    [Header("World Bounds")]
    [SerializeField] private Vector2 worldBottomLeft;
    [SerializeField] private Vector2 worldTopRight;

    [Header("Refresh")]
    [SerializeField] private float teammateRefreshSeconds = 1.0f;

    private PlayerTeam localTeam;
    private PlayerHealth localHealth;
    private Transform localPlayer;

    private readonly List<PlayerTeam> teammates = new();
    private readonly List<CaptureZone> zones = new();

    private readonly Dictionary<PlayerTeam, RectTransform> teammateIcons = new();
    private readonly Dictionary<CaptureZone, RectTransform> zoneIcons = new();

    private RectTransform localPlayerIcon;

    private int selectedZoneIndex = -1;
    private float teammateRefreshAccum;

    private bool forcedOpen;
    private bool wasAlive;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        if (mapPanel != null)
            mapPanel.gameObject.SetActive(false);

        while (!TryCacheLocalPlayer())
            yield return null;

        while (!TryCacheZones())
            yield return null;

        wasAlive = localHealth != null && localHealth.IsAlive;

        RefreshTeammates(fullRebuild: true);
        CreateIcons();

        ApplyCursorState();
    }

    private void Update()
    {
        if (localHealth != null)
        {
            bool alive = localHealth.IsAlive;

            if (wasAlive && !alive)
            {
                forcedOpen = true;
                if (mapPanel != null) mapPanel.gameObject.SetActive(true);
                ApplyCursorState();
            }
            else if (!wasAlive && alive)
            {
                forcedOpen = false;
                if (mapPanel != null) mapPanel.gameObject.SetActive(false);
                ApplyCursorState();
            }

            wasAlive = alive;
        }

        if (forcedOpen) ApplyCursorState();

        if (!forcedOpen)
        {
            if (Input.GetKeyDown(toggleKey))
            {
                if (mapPanel != null) mapPanel.gameObject.SetActive(true);
                ApplyCursorState();
            }
            if (Input.GetKeyUp(toggleKey))
            {
                if (mapPanel != null) mapPanel.gameObject.SetActive(false);
                ApplyCursorState();
            }
        }

        if (mapPanel == null || !mapPanel.gameObject.activeSelf)
            return;

        teammateRefreshAccum += Time.deltaTime;
        if (teammateRefreshAccum >= teammateRefreshSeconds)
        {
            teammateRefreshAccum = 0f;
            RefreshTeammates(fullRebuild: false);
        }

        UpdateLocalIcon();
        UpdateTeammateIcons();
        UpdateZoneIcons();
    }

    private void ApplyCursorState()
    {
        bool spawnSelectActive = forcedOpen;
        bool mapOpen = mapPanel != null && mapPanel.gameObject.activeSelf;

        bool show = spawnSelectActive || mapOpen;

        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private bool TryCacheLocalPlayer()
    {
        var players = FindObjectsByType<PlayerTeam>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (!p.IsOwner) continue;

            localTeam = p;
            localPlayer = p.transform;
            localHealth = p.GetComponent<PlayerHealth>();
            return true;
        }
        return false;
    }

    private bool TryCacheZones()
    {
        var mc = MatchController.Instance;
        if (mc == null || mc.zones == null || mc.zones.Length == 0) return false;

        zones.Clear();
        foreach (var z in mc.zones)
            if (z != null) zones.Add(z);

        return zones.Count > 0;
    }

    private void RefreshTeammates(bool fullRebuild)
    {
        if (localTeam == null) return;

        var all = FindObjectsByType<PlayerTeam>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (fullRebuild)
        {
            teammates.Clear();
            foreach (var kv in teammateIcons)
                if (kv.Value != null) Destroy(kv.Value.gameObject);
            teammateIcons.Clear();
        }

        var desired = new HashSet<PlayerTeam>();
        foreach (var t in all)
        {
            if (t == null) continue;
            if (t == localTeam) continue;
            if (t.team.Value != localTeam.team.Value) continue;
            desired.Add(t);
        }

        for (int i = teammates.Count - 1; i >= 0; i--)
        {
            var t = teammates[i];
            if (t == null || !desired.Contains(t))
            {
                if (teammateIcons.TryGetValue(t, out var icon) && icon != null)
                    Destroy(icon.gameObject);

                teammateIcons.Remove(t);
                teammates.RemoveAt(i);
            }
        }

        foreach (var t in desired)
        {
            if (teammates.Contains(t)) continue;

            teammates.Add(t);

            if (mapImage != null && teammateIconPrefab != null)
            {
                var icon = Instantiate(teammateIconPrefab, mapImage);
                teammateIcons[t] = icon;
            }
        }
    }

    private void CreateIcons()
    {
        if (mapImage == null) return;

        if (playerIconPrefab != null)
            localPlayerIcon = Instantiate(playerIconPrefab, mapImage);

        foreach (var t in teammates)
        {
            if (t == null) continue;
            if (teammateIcons.ContainsKey(t)) continue;

            var icon = Instantiate(teammateIconPrefab, mapImage);
            teammateIcons[t] = icon;
        }

        zoneIcons.Clear();

        foreach (var z in zones)
        {
            if (z == null) continue;

            var icon = Instantiate(zoneIconPrefab, mapImage);
            zoneIcons[z] = icon;

            var btn = icon.GetComponent<Button>();
            if (btn != null)
            {
                var captured = z;
                btn.onClick.AddListener(() => OnZoneClicked(captured));
            }
        }
    }

    private void OnZoneClicked(CaptureZone z)
    {
        Debug.Log("Zone clicked");

        if (z == null || localTeam == null) return;
        Debug.Log("Passed test 1");

        var mc = MatchController.Instance;
        if (mc == null) return;
        Debug.Log("Passed test 2");

        int zi = mc.GetZoneIndex(z);
        if (zi < 0) return;
        Debug.Log("Passed test 3");

        if (z.Owner != localTeam.team.Value) return;
        Debug.Log("Passed test 4");

        if (selectedZoneIndex == zi)
            selectedZoneIndex = -1;
        else
            selectedZoneIndex = zi;

        if (localHealth != null)
        {
            localHealth.SetPreferredSpawnZone(selectedZoneIndex);
            Debug.Log("Set preferred spawn zone to " + selectedZoneIndex);
        }

        if (localHealth != null && !localHealth.IsAlive && selectedZoneIndex >= 0)
        {
            localHealth.RequestRespawnNow();
            Debug.Log("Requested respawn now");
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
        if (localPlayer == null || localPlayerIcon == null) return;

        var pos = WorldToMapPosition(localPlayer.position);
        localPlayerIcon.anchoredPosition = pos;
        localPlayerIcon.localRotation = Quaternion.Euler(0, 0, -localPlayer.eulerAngles.y);
    }

    private void UpdateTeammateIcons()
    {
        foreach (var kvp in teammateIcons)
        {
            var t = kvp.Key;
            var icon = kvp.Value;

            if (t == null || icon == null) continue;

            var pos = WorldToMapPosition(t.transform.position);
            icon.anchoredPosition = pos;
            icon.localRotation = Quaternion.Euler(0, 0, -t.transform.eulerAngles.y);
        }
    }

    private void UpdateZoneIcons()
    {
        foreach (var kvp in zoneIcons)
        {
            var z = kvp.Key;
            var tr = kvp.Value;

            if (z == null || tr == null) continue;

            tr.anchoredPosition = WorldToMapPosition(z.transform.position);

            var img = tr.GetComponent<Image>();
            if (img != null)
                img.sprite = GetZoneSprite(z.Owner);

            int zi = -1;
            var mc = MatchController.Instance;
            if (mc != null) zi = mc.GetZoneIndex(z);

            bool selected = (zi >= 0 && zi == selectedZoneIndex);
            tr.localScale = selected ? Vector3.one * 1.15f : Vector3.one;

            var lockImg = tr.Find("LockOverlay")?.GetComponent<Image>();
            if (lockImg != null)
            {
                bool showLock = z.Uncapturable && lockOverlaySprite != null;
                lockImg.gameObject.SetActive(showLock);
                if (showLock) lockImg.sprite = lockOverlaySprite;
            }
        }
    }

    private Sprite GetZoneSprite(Team owner)
    {
        return owner switch
        {
            Team.TeamA => flagTeamA,
            Team.TeamB => flagTeamB,
            _ => flagNeutral
        };
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && forcedOpen)
            ApplyCursorState();
    }
}
