using System;
using UnityEngine;

[Serializable]
public class Team
{
    public string Id;
    public string DisplayName;

    public string ModelKey;
    public string WeaponKey;

    public string AnimatorPath;
    public string AimBonePath;
    public string WeaponSocketPath;
    public string MuzzlePath;
    public string AimTransformPath;

    public Sprite FlagSprite;
}
