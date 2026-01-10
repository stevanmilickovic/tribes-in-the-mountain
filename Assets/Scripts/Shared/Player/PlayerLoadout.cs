using UnityEngine;
using FishNet.Object;

public class PlayerLoadout : NetworkBehaviour
{
    [Header("Anchors on the Player prefab")]
    [SerializeField] private Transform modelRoot;
    [SerializeField] private Transform weaponRoot;

    [Header("Auto-cached (can be left empty)")]
    [SerializeField] private PlayerTeam playerTeam;
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private AimGun aimGun;
    [SerializeField] private PlayerAnimationDriver animDriver;
    [SerializeField] private CameraBinder cameraBinder;

    private string _appliedTeamId = TeamDatabase.NeutralId;

    private GameObject _modelInstance;
    private GameObject _weaponInstance;

    private void Awake()
    {
        if (playerTeam == null) playerTeam = GetComponent<PlayerTeam>();
        if (motor == null) motor = GetComponent<PlayerMotor>();
        if (aimGun == null) aimGun = GetComponent<AimGun>();
        if (animDriver == null) animDriver = GetComponent<PlayerAnimationDriver>();
        if (cameraBinder == null) cameraBinder = GetComponent<CameraBinder>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ForceRefresh();
    }

    private void OnDestroy()
    {
        ClearInstances();
    }

    private void Update()
    {
        if (!IsClientInitialized) return;
        if (playerTeam == null) return;

        string tid = playerTeam.TeamId;
        if (string.IsNullOrWhiteSpace(tid)) tid = TeamDatabase.NeutralId;

        if (tid != _appliedTeamId)
            Apply(tid);
    }

    public void ForceRefresh()
    {
        _appliedTeamId = TeamDatabase.NeutralId;
        Apply(playerTeam != null ? playerTeam.TeamId : TeamDatabase.NeutralId);
    }

    private void Apply(string teamId)
    {
        if (!IsClientInitialized) return;

        _appliedTeamId = string.IsNullOrWhiteSpace(teamId) ? TeamDatabase.NeutralId : teamId;

        ClearInstances();

        if (_appliedTeamId == TeamDatabase.NeutralId)
            return;

        var db = TeamDatabase.Instance;
        if (db == null) return;

        var def = db.Get(_appliedTeamId);
        if (def == null) return;

        if (modelRoot == null) modelRoot = transform;
        if (weaponRoot == null) weaponRoot = modelRoot;

        if (!string.IsNullOrWhiteSpace(def.ModelKey))
        {
            var modelPrefab = Resources.Load<GameObject>(def.ModelKey);
            if (modelPrefab != null)
            {
                _modelInstance = Instantiate(modelPrefab, modelRoot);
                _modelInstance.transform.localPosition = Vector3.zero;
                _modelInstance.transform.localRotation = Quaternion.identity;
                _modelInstance.transform.localScale = Vector3.one;
            }
        }

        Transform weaponParent = weaponRoot;

        if (_modelInstance != null && !string.IsNullOrWhiteSpace(def.WeaponSocketPath))
        {
            var socket = _modelInstance.transform.Find(def.WeaponSocketPath);
            if (socket != null)
                weaponParent = socket;
        }

        if (!string.IsNullOrWhiteSpace(def.WeaponKey))
        {
            var weaponPrefab = Resources.Load<GameObject>(def.WeaponKey);
            if (weaponPrefab != null)
            {
                _weaponInstance = Instantiate(weaponPrefab, weaponParent);
                _weaponInstance.transform.localPosition = Vector3.zero;
                _weaponInstance.transform.localRotation = Quaternion.identity;
                _weaponInstance.transform.localScale = Vector3.one;
            }
        }

        Animator animator = ResolveAnimator(def);
        Transform aimBone = ResolveAimBone(def);
        Transform muzzle = ResolveMuzzle(def);
        Transform aimTransform = ResolveAimTransform(def);
        PlayerAudio pa = GetComponent<PlayerAudio>();

        if (aimGun != null)
            aimGun.BindAimRig(motor, aimTransform, aimBone);

        if (motor != null)
            motor.BindLoadout(aimGun, muzzle);

        if (animDriver != null)
            animDriver.BindAnimator(animator);

        if (cameraBinder != null)
            cameraBinder.Bind();

        if (pa != null)
        {
            AudioSource ms = null;

            if (muzzle != null)
                ms = muzzle.GetComponent<AudioSource>();

            if (ms == null && _weaponInstance != null)
                ms = _weaponInstance.GetComponentInChildren<AudioSource>(true);

            if (ms != null)
                pa.muzzleSource = ms;
        }
    }

    private Animator ResolveAnimator(Team def)
    {
        if (_modelInstance == null) return null;

        if (string.IsNullOrWhiteSpace(def.AnimatorPath))
            return _modelInstance.GetComponentInChildren<Animator>(true);

        var t = _modelInstance.transform.Find(def.AnimatorPath);
        if (t == null) return _modelInstance.GetComponentInChildren<Animator>(true);

        return t.GetComponent<Animator>() ?? t.GetComponentInChildren<Animator>(true);
    }

    private Transform ResolveAimBone(Team def)
    {
        if (_modelInstance == null) return null;
        if (string.IsNullOrWhiteSpace(def.AimBonePath)) return null;
        return _modelInstance.transform.Find(def.AimBonePath);
    }

    private Transform ResolveMuzzle(Team def)
    {
        Transform root = _weaponInstance != null ? _weaponInstance.transform : (_modelInstance != null ? _modelInstance.transform : null);
        if (root == null) return null;

        if (string.IsNullOrWhiteSpace(def.MuzzlePath)) return null;

        var t = root.Find(def.MuzzlePath);
        if (t != null) return t;

        if (_modelInstance != null)
        {
            t = _modelInstance.transform.Find(def.MuzzlePath);
            if (t != null) return t;
        }

        return null;
    }

    private Transform ResolveAimTransform(Team def)
    {
        Transform root = _weaponInstance != null ? _weaponInstance.transform : (_modelInstance != null ? _modelInstance.transform : null);
        if (root == null) return null;

        if (string.IsNullOrWhiteSpace(def.AimTransformPath)) return null;

        var t = root.Find(def.AimTransformPath);
        if (t != null) return t;

        if (_modelInstance != null)
        {
            t = _modelInstance.transform.Find(def.AimTransformPath);
            if (t != null) return t;
        }

        return null;
    }

    private void ClearInstances()
    {
        if (_weaponInstance != null)
        {
            Destroy(_weaponInstance);
            _weaponInstance = null;
        }

        if (_modelInstance != null)
        {
            Destroy(_modelInstance);
            _modelInstance = null;
        }
    }
}
