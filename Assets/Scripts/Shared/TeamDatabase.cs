using System.Collections.Generic;
using UnityEngine;

public class TeamDatabase : MonoBehaviour
{
    public static TeamDatabase Instance { get; private set; }

    public const string NeutralId = "none";

    private readonly List<Team> _teams = new();
    private readonly Dictionary<string, Team> _byId = new();

    private const string DefaultAimBonePath = "metarig/spine/spine.001";
    private const string DefaultWeaponSocketPath = "metarig/spine/spine.001/spine.002/spine.003/shoulder.R/upper_arm.R/forearm.R/hand.R";
    private const string DefaultMuzzlePath = "Muzzle";
    private const string DefaultAimTransformPath = "RaycastObject";

    public IReadOnlyList<Team> Teams => _teams;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildDefaults();
    }

    private void BuildDefaults()
    {
        _teams.Clear();
        _byId.Clear();

        Add(new Team
        {
            Id = NeutralId,
            DisplayName = "Neutral",
            FlagSprite = null,

            ModelKey = null,
            WeaponKey = null,

            AnimatorPath = null,
            AimBonePath = null,
            WeaponSocketPath = null,
            MuzzlePath = null,
            AimTransformPath = null
        });

        Add(new Team
        {
            Id = "team_a",
            DisplayName = "Team A",

            ModelKey = "Tribes/Model",
            WeaponKey = "Jeferdar",

            AnimatorPath = "",
            AimBonePath = DefaultAimBonePath,
            WeaponSocketPath = DefaultWeaponSocketPath,
            MuzzlePath = DefaultMuzzlePath,
            AimTransformPath = DefaultAimTransformPath,

            FlagSprite = null
        });

        Add(new Team
        {
            Id = "team_b",
            DisplayName = "Team B",

            ModelKey = "Ottomans/Model",
            WeaponKey = "Jeferdar",

            AnimatorPath = "",
            AimBonePath = DefaultAimBonePath,
            WeaponSocketPath = DefaultWeaponSocketPath,
            MuzzlePath = DefaultMuzzlePath,
            AimTransformPath = DefaultAimTransformPath,

            FlagSprite = null
        });
    }

    public bool IsValidTeamId(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        return _byId.ContainsKey(id);
    }

    public bool TryGet(string id, out Team team)
    {
        team = null;
        if (string.IsNullOrWhiteSpace(id)) return false;
        return _byId.TryGetValue(id, out team);
    }

    public Team Get(string id)
    {
        if (TryGet(id, out var t))
            return t;

        return _byId.TryGetValue(NeutralId, out var neutral) ? neutral : null;
    }

    public void Add(Team team)
    {
        if (team == null) return;
        if (string.IsNullOrWhiteSpace(team.Id)) return;
        if (_byId.ContainsKey(team.Id)) return;

        _teams.Add(team);
        _byId[team.Id] = team;
    }
}
