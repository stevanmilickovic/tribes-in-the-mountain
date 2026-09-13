using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SoldierAnimatorSimplifiedBuilder
{
    private const string SourceControllerPath = "Assets/AnimatorControllers/SoldierAnimator.controller";
    private const string TargetControllerPath = "Assets/AnimatorControllers/SoldierAnimatorSimplified.controller";
    private const string MenuPath = "Tools/Animation/Rebuild Soldier Animator Simplified";
    private const string SoldierAnimationPath = "Assets/GameDevDave/Realistic Soldier Animation Pack/Animations/Soldier/";

    private const string BaseLayerName = "Base Layer";
    private const string AimLayerName = "Aim";
    private const string ReloadLayerName = "Fire & Reload";

    private const string SpeedParam = "Speed";
    private const string CombatModeParam = "CombatMode";
    private const string StandParam = "Stand";
    private const string CrouchParam = "Crouch";
    private const string ProneParam = "Prone";
    private const string ReloadParam = "Reload";
    private const string DeathParam = "Death";
    private const string ResetParam = "Reset";

    private const float DiveToProneThreshold = 0.40f;
    private const float LocomotionBlendThreshold = 0.35f;
    private const float LocomotionTransitionDuration = 0.08f;
    private const float FastTransitionDuration = 0.05f;

    [MenuItem(MenuPath)]
    public static void Rebuild()
    {
        AnimatorController source = AssetDatabase.LoadAssetAtPath<AnimatorController>(SourceControllerPath);
        if (source == null)
        {
            Debug.LogError($"Could not load source controller at {SourceControllerPath}");
            return;
        }

        AnimatorController target = AssetDatabase.LoadAssetAtPath<AnimatorController>(TargetControllerPath);
        if (target == null)
            target = AnimatorController.CreateAnimatorControllerAtPath(TargetControllerPath);

        if (target == null)
        {
            Debug.LogError($"Could not create target controller at {TargetControllerPath}");
            return;
        }

        try
        {
            ClearGeneratedController(target);
            CopyParameters(source, target);
            BuildLayers(source, target);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(TargetControllerPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"Built simplified soldier controller at {TargetControllerPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to build simplified soldier controller.\n{ex}");
        }
    }

    private static void ClearGeneratedController(AnimatorController controller)
    {
        while (controller.layers.Length > 0)
            controller.RemoveLayer(0);

        while (controller.parameters.Length > 0)
            controller.RemoveParameter(0);
    }

    private static void CopyParameters(AnimatorController source, AnimatorController target)
    {
        foreach (AnimatorControllerParameter parameter in source.parameters)
        {
            AnimatorControllerParameter copy = new AnimatorControllerParameter
            {
                name = parameter.name,
                type = parameter.type,
                defaultBool = parameter.defaultBool,
                defaultFloat = parameter.defaultFloat,
                defaultInt = parameter.defaultInt
            };
            target.AddParameter(copy);
        }
    }

    private static void BuildLayers(AnimatorController source, AnimatorController target)
    {
        AnimatorControllerLayer sourceAimLayer = FindLayer(source, AimLayerName);
        AnimatorControllerLayer sourceReloadLayer = FindLayer(source, ReloadLayerName);

        AnimatorStateMachine baseStateMachine = CreateStateMachine(target, BaseLayerName);
        AnimatorStateMachine aimStateMachine = CreateStateMachine(target, AimLayerName);
        AnimatorStateMachine reloadStateMachine = CreateStateMachine(target, ReloadLayerName);

        AddLayer(target, BaseLayerName, baseStateMachine, null, 1f);
        AddLayer(target, AimLayerName, aimStateMachine, sourceAimLayer != null ? sourceAimLayer.avatarMask : null, 0f);
        AddLayer(target, ReloadLayerName, reloadStateMachine, sourceReloadLayer != null ? sourceReloadLayer.avatarMask : null, 1f);

        BuildBaseLayer(source, target, baseStateMachine);
        BuildAimLayer(aimStateMachine);
        BuildReloadLayer(source, target, reloadStateMachine);
    }

    private static void BuildBaseLayer(AnimatorController source, AnimatorController target, AnimatorStateMachine sm)
    {
        AnimatorState sourceIdleStanding = RequireStateAnyLayer(source, "IdleStanding_1");
        AnimatorState sourceSprint = RequireState(source, BaseLayerName, "Sprint");
        AnimatorState sourceAimMove = RequireState(source, BaseLayerName, "Run Blend Tree");
        AnimatorState sourceIdleCrouch = RequireStateAnyLayer(source, "IdleCrouch");
        AnimatorState sourceIdleProne = RequireStateAnyLayer(source, "IdleProne");
        AnimatorState sourceCrawl = RequireState(source, BaseLayerName, "Crawl");

        Motion sprintMoveMotion = ExtractPreferredMotion(sourceSprint.motion, 0, "Sprint");
        Motion aimedRunMotion = ExtractPreferredMotion(sourceAimMove.motion, 0, "Run Blend Tree");

        AnimatorState standingFree = AddState(
            sm,
            "StandingFreeLocomotion",
            CreateSimpleBlendTree(target, "StandingFreeLocomotionTree", sourceIdleStanding.motion, sprintMoveMotion, LocomotionBlendThreshold),
            sourceSprint);
        AnimatorState standingAim = AddState(
            sm,
            "StandingLocomotion",
            CreateSimpleBlendTree(target, "StandingAimLocomotionTree", sourceIdleStanding.motion, aimedRunMotion, LocomotionBlendThreshold),
            sourceAimMove);
        AnimatorState crouch = AddState(sm, "CrouchLocomotion", CloneMotion(sourceIdleCrouch.motion, target), sourceIdleCrouch);
        AnimatorState prone = AddState(
            sm,
            "ProneLocomotion",
            CreateSimpleBlendTree(target, "ProneLocomotionTree", sourceIdleProne.motion, sourceCrawl.motion, LocomotionBlendThreshold),
            sourceIdleProne);

        AnimatorState standingToCrouch = AddStateFromSource(sm, target, source, BaseLayerName, "StandingToCrouch", "StandingToCrouch");
        AnimatorState crouchToStanding = AddStateFromSource(sm, target, source, BaseLayerName, "CrouchToStanding", "CrouchToStanding");
        AnimatorState standingToProne = AddStateFromSource(sm, target, source, BaseLayerName, "StandingToProne", "StandingToProne");
        AnimatorState diveToProne = AddStateFromSource(sm, target, source, BaseLayerName, "DiveToProne", "DiveToProne");
        AnimatorState proneToStanding = AddStateFromSource(sm, target, source, BaseLayerName, "ProneToStanding", "ProneToStanding");
        AnimatorState crouchToProne = AddStateFromSource(sm, target, source, BaseLayerName, "CrouchToProne", "CrouchToProne");
        AnimatorState proneToCrouch = AddStateFromSource(sm, target, source, BaseLayerName, "ProneToCrouch", "ProneToCrouch");

        AnimatorState deathStanding = AddStateFromSource(sm, target, source, BaseLayerName, "DeathStanding", "DeathStanding");
        AnimatorState deathCrouched = AddStateFromSource(sm, target, source, BaseLayerName, "DeathCrouched", "DeathCrouched");
        AnimatorState deathProne = AddStateFromSource(sm, target, source, BaseLayerName, "DeathProne", "DeathProne");
        AnimatorState deathRun = AddStateFromSource(sm, target, source, BaseLayerName, "DeathRun", "DeathRun");

        sm.defaultState = standingFree;

        // Keep standing snappy, but let the crossfade breathe a little instead of snapping on one frame.
        AddTransition(standingFree, standingAim, false, LocomotionTransitionDuration, 0f, If(CombatModeParam), If(StandParam));
        AddTransition(standingAim, standingFree, false, LocomotionTransitionDuration, 0f, IfNot(CombatModeParam), If(StandParam));

        AddTransition(standingFree, standingToCrouch, false, FastTransitionDuration, 0f, If(CrouchParam));
        AddTransition(standingAim, standingToCrouch, false, FastTransitionDuration, 0f, If(CrouchParam));

        AddTransition(standingFree, standingToProne, false, FastTransitionDuration, 0f, If(ProneParam), Less(SpeedParam, DiveToProneThreshold));
        AddTransition(standingAim, standingToProne, false, FastTransitionDuration, 0f, If(ProneParam));
        AddTransition(standingFree, diveToProne, false, 0.03f, 0f, If(ProneParam), Greater(SpeedParam, DiveToProneThreshold));

        AddTransition(crouch, crouchToStanding, false, FastTransitionDuration, 0f, If(StandParam));
        AddTransition(crouch, crouchToProne, false, FastTransitionDuration, 0f, If(ProneParam));

        AddTransition(prone, proneToStanding, false, FastTransitionDuration, 0f, If(StandParam));
        AddTransition(prone, proneToCrouch, false, FastTransitionDuration, 0f, If(CrouchParam));

        AddTransition(standingToCrouch, crouch, true, FastTransitionDuration, 0.95f);
        AddTransition(crouchToStanding, standingFree, true, FastTransitionDuration, 0.95f, IfNot(CombatModeParam));
        AddTransition(crouchToStanding, standingAim, true, FastTransitionDuration, 0.95f, If(CombatModeParam));
        AddTransition(standingToProne, prone, true, FastTransitionDuration, 0.95f);
        AddTransition(diveToProne, prone, true, 0.02f, 0.95f);
        AddTransition(proneToStanding, standingFree, true, FastTransitionDuration, 0.95f, IfNot(CombatModeParam));
        AddTransition(proneToStanding, standingAim, true, FastTransitionDuration, 0.95f, If(CombatModeParam));
        AddTransition(crouchToProne, prone, true, FastTransitionDuration, 0.95f);
        AddTransition(proneToCrouch, crouch, true, FastTransitionDuration, 0.95f);

        AddAnyStateTransition(sm, deathProne, false, 0.02f, 0f, If(DeathParam), If(ProneParam));
        AddAnyStateTransition(sm, deathCrouched, false, 0.02f, 0f, If(DeathParam), If(CrouchParam));
        AddAnyStateTransition(sm, deathRun, false, 0.02f, 0f, If(DeathParam), If(StandParam), Greater(SpeedParam, 0.5f));
        AddAnyStateTransition(sm, deathStanding, false, 0.02f, 0f, If(DeathParam), If(StandParam));

        AddAnyStateTransition(sm, prone, false, 0.02f, 0f, If(ResetParam), If(ProneParam));
        AddAnyStateTransition(sm, crouch, false, 0.02f, 0f, If(ResetParam), If(CrouchParam));
        AddAnyStateTransition(sm, standingFree, false, 0.02f, 0f, If(ResetParam), If(StandParam), IfNot(CombatModeParam));
        AddAnyStateTransition(sm, standingAim, false, 0.02f, 0f, If(ResetParam), If(StandParam), If(CombatModeParam));
    }

    private static void BuildAimLayer(AnimatorStateMachine sm)
    {
        // The pack's Aim layer is a transition graph, not a reliable source for its held poses.
        // Use the authored clips directly so the mask overrides locomotion above the hips.
        AnimatorState aimingStanding = AddState(sm, "AimingStanding", RequireClip("AimingStanding"), null);
        AnimatorState aimingCrouch = AddState(sm, "AimingCrouch", RequireClip("AimingCrouch"), null);
        AnimatorState aimingProne = AddState(sm, "AimingProne", RequireClip("AimingProne"), null);

        AnimatorState standingToCrouch = AddState(sm, "AimStandingToCrouch", RequireClip("AimStandingToCrouch"), null);
        AnimatorState crouchToStanding = AddState(sm, "AimCrouchToStanding", RequireClip("AimCrouchToStanding"), null);
        AnimatorState standingToProne = AddState(sm, "AimStandingToProne", RequireClip("AimStandingToProne"), null);
        AnimatorState proneToStanding = AddState(sm, "AimProneToStanding", RequireClip("AimProneToStanding"), null);
        AnimatorState crouchToProne = AddState(sm, "AimCrouchToProne", RequireClip("AimCrouchToProne"), null);
        AnimatorState proneToCrouch = AddState(sm, "AimProneToCrouch", RequireClip("AimProneToCrouch"), null);

        sm.defaultState = aimingStanding;

        AddTransition(aimingStanding, standingToCrouch, false, 0.05f, 0f, If(CrouchParam));
        AddTransition(aimingStanding, standingToProne, false, 0.05f, 0f, If(ProneParam));
        AddTransition(aimingCrouch, crouchToStanding, false, 0.05f, 0f, If(StandParam));
        AddTransition(aimingCrouch, crouchToProne, false, 0.05f, 0f, If(ProneParam));
        AddTransition(aimingProne, proneToStanding, false, 0.05f, 0f, If(StandParam));
        AddTransition(aimingProne, proneToCrouch, false, 0.05f, 0f, If(CrouchParam));

        AddTransition(standingToCrouch, aimingCrouch, true, 0.05f, 0.95f);
        AddTransition(crouchToStanding, aimingStanding, true, 0.05f, 0.95f);
        AddTransition(standingToProne, aimingProne, true, 0.05f, 0.95f);
        AddTransition(proneToStanding, aimingStanding, true, 0.05f, 0.95f);
        AddTransition(crouchToProne, aimingProne, true, 0.05f, 0.95f);
        AddTransition(proneToCrouch, aimingCrouch, true, 0.05f, 0.95f);
    }

    private static void BuildReloadLayer(AnimatorController source, AnimatorController target, AnimatorStateMachine sm)
    {
        AnimatorState empty = AddState(sm, "ReloadEmpty", null, null);
        AnimatorState reloadStanding = AddStateFromSource(sm, target, source, ReloadLayerName, "ReloadStanding", "ReloadStanding");
        AnimatorState reloadCrouching = AddStateFromSource(sm, target, source, ReloadLayerName, "ReloadCrouching", "ReloadCrouching");
        AnimatorState reloadProne = AddStateFromSource(sm, target, source, ReloadLayerName, "ReloadProne", "ReloadProne");

        sm.defaultState = empty;

        AddAnyStateTransition(sm, reloadProne, false, 0.03f, 0f, If(ReloadParam), If(ProneParam));
        AddAnyStateTransition(sm, reloadCrouching, false, 0.03f, 0f, If(ReloadParam), If(CrouchParam));
        AddAnyStateTransition(sm, reloadStanding, false, 0.03f, 0f, If(ReloadParam), If(StandParam));

        AddTransition(reloadStanding, empty, true, 0.05f, 0.95f);
        AddTransition(reloadCrouching, empty, true, 0.05f, 0.95f);
        AddTransition(reloadProne, empty, true, 0.05f, 0.95f);
    }

    private static AnimatorControllerLayer FindLayer(AnimatorController controller, string layerName)
    {
        foreach (AnimatorControllerLayer layer in controller.layers)
        {
            if (layer.name == layerName)
                return layer;
        }

        return null;
    }

    private static AnimatorStateMachine CreateStateMachine(AnimatorController target, string stateMachineName)
    {
        AnimatorStateMachine sm = new AnimatorStateMachine
        {
            name = stateMachineName
        };
        AssetDatabase.AddObjectToAsset(sm, target);
        return sm;
    }

    private static void AddLayer(AnimatorController target, string layerName, AnimatorStateMachine sm, AvatarMask mask, float defaultWeight)
    {
        AnimatorControllerLayer layer = new AnimatorControllerLayer
        {
            name = layerName,
            stateMachine = sm,
            avatarMask = mask,
            blendingMode = AnimatorLayerBlendingMode.Override,
            defaultWeight = defaultWeight,
            iKPass = true,
            syncedLayerIndex = -1
        };

        target.AddLayer(layer);
    }

    private static AnimatorState AddStateFromSource(
        AnimatorStateMachine sm,
        AnimatorController target,
        AnimatorController source,
        string layerName,
        string sourceStateName,
        string targetStateName)
    {
        AnimatorState sourceState = RequireState(source, layerName, sourceStateName);
        return AddState(sm, targetStateName, CloneMotion(sourceState.motion, target), sourceState);
    }

    private static AnimationClip RequireClip(string clipName)
    {
        string path = $"{SoldierAnimationPath}{clipName}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
            throw new InvalidOperationException($"Could not load required animation clip at {path}.");

        return clip;
    }

    private static AnimatorState AddState(AnimatorStateMachine sm, string stateName, Motion motion, AnimatorState sourceState)
    {
        AnimatorState state = sm.AddState(stateName);
        state.motion = motion;

        if (sourceState != null)
        {
            state.speed = sourceState.speed;
            state.cycleOffset = sourceState.cycleOffset;
            state.iKOnFeet = sourceState.iKOnFeet;
            state.mirror = sourceState.mirror;
            state.tag = sourceState.tag;
            state.writeDefaultValues = sourceState.writeDefaultValues;
        }

        return state;
    }

    private static AnimatorStateTransition AddTransition(
        AnimatorState from,
        AnimatorState to,
        bool hasExitTime,
        float duration,
        float exitTime,
        params AnimatorConditionSpec[] conditions)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        ConfigureTransition(transition, hasExitTime, duration, exitTime, conditions);
        return transition;
    }

    private static AnimatorStateTransition AddAnyStateTransition(
        AnimatorStateMachine sm,
        AnimatorState to,
        bool hasExitTime,
        float duration,
        float exitTime,
        params AnimatorConditionSpec[] conditions)
    {
        AnimatorStateTransition transition = sm.AddAnyStateTransition(to);
        ConfigureTransition(transition, hasExitTime, duration, exitTime, conditions);
        return transition;
    }

    private static void ConfigureTransition(
        AnimatorStateTransition transition,
        bool hasExitTime,
        float duration,
        float exitTime,
        AnimatorConditionSpec[] conditions)
    {
        transition.hasExitTime = hasExitTime;
        transition.hasFixedDuration = true;
        transition.duration = duration;
        transition.exitTime = exitTime;
        transition.canTransitionToSelf = false;
        transition.interruptionSource = TransitionInterruptionSource.None;
        transition.orderedInterruption = true;

        foreach (AnimatorConditionSpec condition in conditions)
            transition.AddCondition(condition.Mode, condition.Threshold, condition.Parameter);
    }

    private static AnimatorState RequireState(AnimatorController controller, string layerName, string stateName)
    {
        AnimatorState state = FindState(controller, layerName, stateName);
        if (state == null)
            throw new InvalidOperationException($"Could not find state '{stateName}' on layer '{layerName}'.");

        return state;
    }

    private static AnimatorState RequireStateAnyLayer(AnimatorController controller, string stateName)
    {
        AnimatorState state = FindStateAnyLayer(controller, stateName);
        if (state == null)
            throw new InvalidOperationException($"Could not find state '{stateName}' in source controller.");

        return state;
    }

    private static AnimatorState FindState(AnimatorController controller, string layerName, string stateName)
    {
        AnimatorControllerLayer layer = FindLayer(controller, layerName);
        if (layer != null && layer.stateMachine != null)
        {
            AnimatorState layeredState = FindStateRecursive(layer.stateMachine, stateName);
            if (layeredState != null)
                return layeredState;
        }

        AnimatorState anyLayerState = FindStateAnyLayer(controller, stateName);
        if (anyLayerState != null)
            return anyLayerState;

        return FindStateInControllerAssets(controller, stateName);
    }

    private static AnimatorState FindStateAnyLayer(AnimatorController controller, string stateName)
    {
        foreach (AnimatorControllerLayer layer in controller.layers)
        {
            if (layer.stateMachine == null)
                continue;

            AnimatorState state = FindStateRecursive(layer.stateMachine, stateName);
            if (state != null)
                return state;
        }

        return null;
    }

    private static AnimatorState FindStateInControllerAssets(AnimatorController controller, string stateName)
    {
        string assetPath = AssetDatabase.GetAssetPath(controller);
        if (string.IsNullOrEmpty(assetPath))
            return null;

        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (UnityEngine.Object asset in assets)
        {
            AnimatorState state = asset as AnimatorState;
            if (state != null && state.name == stateName)
                return state;
        }

        return null;
    }

    private static AnimatorState FindStateRecursive(AnimatorStateMachine sm, string stateName)
    {
        foreach (ChildAnimatorState child in sm.states)
        {
            if (child.state != null && child.state.name == stateName)
                return child.state;
        }

        foreach (ChildAnimatorStateMachine childMachine in sm.stateMachines)
        {
            AnimatorState found = FindStateRecursive(childMachine.stateMachine, stateName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static BlendTree CreateSimpleBlendTree(
        AnimatorController target,
        string treeName,
        Motion idleMotionSource,
        Motion moveMotionSource,
        float moveThreshold)
    {
        Motion idleMotion = CloneMotion(idleMotionSource, target);
        Motion moveMotion = CloneMotion(moveMotionSource, target);

        BlendTree tree = new BlendTree
        {
            name = treeName,
            blendType = BlendTreeType.Simple1D,
            blendParameter = SpeedParam,
            useAutomaticThresholds = false,
            minThreshold = 0f,
            maxThreshold = moveThreshold
        };

        AssetDatabase.AddObjectToAsset(tree, target);
        tree.AddChild(idleMotion, 0f);
        tree.AddChild(moveMotion, moveThreshold);
        return tree;
    }

    private static Motion CloneMotion(Motion sourceMotion, AnimatorController target)
    {
        if (sourceMotion == null)
            return null;

        AnimationClip clip = sourceMotion as AnimationClip;
        if (clip != null)
            return clip;

        BlendTree blendTree = sourceMotion as BlendTree;
        if (blendTree != null)
            return CloneBlendTree(blendTree, target, new Dictionary<BlendTree, BlendTree>());

        return sourceMotion;
    }

    private static Motion ExtractPreferredMotion(Motion sourceMotion, int preferredChildIndex, string sourceName)
    {
        BlendTree blendTree = sourceMotion as BlendTree;
        if (blendTree == null)
            return sourceMotion;

        ChildMotion[] children = blendTree.children;
        if (children == null || children.Length <= preferredChildIndex || children[preferredChildIndex].motion == null)
            throw new InvalidOperationException($"Blend tree '{sourceName}' did not have child index {preferredChildIndex}.");

        return children[preferredChildIndex].motion;
    }

    private static BlendTree CloneBlendTree(BlendTree sourceTree, AnimatorController target, Dictionary<BlendTree, BlendTree> cache)
    {
        if (cache.TryGetValue(sourceTree, out BlendTree existing))
            return existing;

        BlendTree clone = new BlendTree
        {
            name = sourceTree.name,
            blendType = sourceTree.blendType,
            blendParameter = sourceTree.blendParameter,
            blendParameterY = sourceTree.blendParameterY,
            minThreshold = sourceTree.minThreshold,
            maxThreshold = sourceTree.maxThreshold,
            useAutomaticThresholds = sourceTree.useAutomaticThresholds
        };

        cache[sourceTree] = clone;
        AssetDatabase.AddObjectToAsset(clone, target);

        ChildMotion[] children = sourceTree.children;
        List<ChildMotion> clonedChildren = new List<ChildMotion>(children.Length);
        foreach (ChildMotion child in children)
        {
            Motion childMotion = child.motion;
            BlendTree childTree = childMotion as BlendTree;
            if (childTree != null)
                childMotion = CloneBlendTree(childTree, target, cache);
            else if (!(childMotion is AnimationClip))
                childMotion = CloneMotion(childMotion, target);

            ChildMotion clonedChild = child;
            clonedChild.motion = childMotion;
            clonedChildren.Add(clonedChild);
        }

        clone.children = clonedChildren.ToArray();
        return clone;
    }

    private struct AnimatorConditionSpec
    {
        public readonly AnimatorConditionMode Mode;
        public readonly float Threshold;
        public readonly string Parameter;

        public AnimatorConditionSpec(AnimatorConditionMode mode, float threshold, string parameter)
        {
            Mode = mode;
            Threshold = threshold;
            Parameter = parameter;
        }
    }

    private static AnimatorConditionSpec If(string parameter)
    {
        return new AnimatorConditionSpec(AnimatorConditionMode.If, 0f, parameter);
    }

    private static AnimatorConditionSpec IfNot(string parameter)
    {
        return new AnimatorConditionSpec(AnimatorConditionMode.IfNot, 0f, parameter);
    }

    private static AnimatorConditionSpec Greater(string parameter, float threshold)
    {
        return new AnimatorConditionSpec(AnimatorConditionMode.Greater, threshold, parameter);
    }

    private static AnimatorConditionSpec Less(string parameter, float threshold)
    {
        return new AnimatorConditionSpec(AnimatorConditionMode.Less, threshold, parameter);
    }
}
