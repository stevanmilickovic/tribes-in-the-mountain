# Tribes in the Mountain — Engineering Guide

## Purpose and priorities

This is a Unity 6 multiplayer shooter using FishNet. When writing, reviewing, or refactoring code, use this order of priority:

1. Correct, secure, testable multiplayer behaviour.
2. Clear, maintainable, and well-separated code.
3. Simple designs with explicit data flow and ownership.
4. Measured performance improvements.

Do not make code harder to understand for a hypothetical performance gain. Optimise only after profiling identifies a bottleneck, retain a readable baseline, and record the reason for any non-obvious optimisation. Correctness and clarity are performance features in a networked game.

## Working in this repository

- Target the Unity version in `ProjectSettings/ProjectVersion.txt`; do not casually upgrade Unity, packages, render pipelines, or serialized project settings as part of feature work.
- Preserve Unity `.meta` files and GUIDs. Move or rename Unity assets through the Unity Editor when practical; never regenerate or hand-edit GUIDs to resolve a reference.
- Treat scenes, prefabs, animator controllers, materials, models, and project settings as serialized assets. Make the smallest intentional asset change and inspect the diff for accidental editor-wide rewrites.
- Keep runtime code under `Assets/Scripts` and editor-only tooling under `Assets/Editor` (or an editor-only assembly). Never reference `UnityEditor` from player code.
- Do not edit imported package/vendor code, including FishNet, merely to implement gameplay. Wrap or extend it in project code. A narrowly scoped compatibility fix required by a Unity upgrade is an exception and must explain the API/version reason in its commit or code comment.
- Before adding a dependency, prefer Unity/FishNet facilities already in the project. Pin its purpose, version, licence, platform support, and upgrade impact. Remove unused dependencies rather than accumulating them.
- Add assembly definitions only as a deliberate boundary (for example `Game.Shared`, `Game.Client`, `Game.Server`, `Game.Editor`, and tests). Keep dependency direction one-way; shared gameplay/domain code must not depend on client presentation or editor code.

## C# conventions

- Use ordinary, idiomatic C#: one public type per file; file name equals type name; braces on new lines; four spaces; `PascalCase` for types, methods, properties, events, enum values, and public members; `camelCase` for parameters and locals; `_camelCase` for private instance fields; `UPPER_SNAKE_CASE` only for true constants.
- Prefer small classes with one clear responsibility. A `MonoBehaviour` or `NetworkBehaviour` coordinates Unity/FishNet lifecycle and references; move substantial rules, calculations, and state transitions into plain C# collaborators where that makes them easier to understand and test.
- Make a method do one conceptual job. Name commands as verbs (`ApplyDamage`, `TryReload`) and queries as facts/questions (`CanReload`, `IsGrounded`). Avoid vague names such as `Handle`, `Process`, `Manager`, `Utils`, or `Data` unless the narrow context makes the meaning unambiguous.
- Prefer explicit types when they clarify domain meaning; use `var` when the right-hand side makes the type obvious. Avoid abbreviations except established domain terms (`Rpc`, `Id`, `UI`).
- Keep fields private by default. Use `[SerializeField] private` for Inspector-authored dependencies and values; expose read-only properties or narrow methods instead of public mutable fields.
- Validate required serialized dependencies in `Awake`/`OnValidate` and fail with a useful message when an invalid prefab or scene setup would otherwise fail later. Do not scatter defensive null checks that hide a required wiring error.
- Avoid deep nesting. Guard clauses, early returns, and small private methods are preferred when they preserve the happy path. Do not split tightly coupled logic into tiny methods purely to reduce line count.
- Comments explain *why*, constraints, authority, timing, or a surprising trade-off—not what obvious code already says. Keep TODOs actionable and include context/owner when known.
- Prefer immutable value objects and `readonly` fields where practical. Use `struct` only for small value-like data with clear copy semantics; do not introduce mutable structs casually.
- Use events for meaningful state changes with clear ownership and lifetime. Subscribe and unsubscribe symmetrically (`OnEnable`/`OnDisable`, or network start/stop callbacks); avoid static event leaks.
- Use `async Task`/`Task<T>` for awaitable operations and name them with an `Async` suffix. Avoid `async void` except Unity event handlers. Accept and propagate a `CancellationToken` for cancellable work, and ensure exceptions from background work are observed.
- Throw exceptions for broken invariants or truly exceptional failures, not routine control flow. Use `Try*` APIs for expected invalid input. Never catch and silently discard an exception.

## Unity design and lifecycle

- Use `Awake` to cache local required components and establish local invariants; use `OnEnable`/`OnDisable` for subscriptions; use `Start` only when another object's initialization order is genuinely required. Do not rely on incidental script execution order.
- Use `Update` for frame/input/presentation work, `FixedUpdate` for local physics only, and FishNet ticks for predicted/network simulation. Keep each callback short and make timing dependencies explicit.
- Cache stable component references. Do not call scene-wide searches, `GetComponent`, `Find`, LINQ, string formatting, or allocate collections every frame/tick unless profiling proves it harmless and the code is clearer that way.
- Use `Time.deltaTime` only for frame-driven presentation. Use the correct fixed/tick delta in physics or prediction. Never mix frame time with network-tick simulation without an explicit, documented conversion.
- Keep authored, shared configuration in `ScriptableObject` assets when it is stable data used by multiple prefabs (weapons, movement tuning, faction/loadout definitions). Do not mutate those asset instances as per-match runtime state in a build; create runtime state separately.
- Use `[SerializeReference]`, polymorphic serialized graphs, and custom editors only when their user-facing value outweighs their fragility. Avoid duplicate or cached serialized state; it easily becomes inconsistent after hot reloads and prefab changes.
- Prefer explicit prefab references, serialized registries, or a composition root over global scene searches and hidden singleton dependencies. A singleton is acceptable only for a true, unique application service with a documented lifetime and a narrow API.
- Pool frequently spawned short-lived effects/projectiles only after profiling or when lifecycle pressure is known. Every pooled object must reset all gameplay, visual, event, and network state on reuse.

## FishNet and multiplayer rules

- Design every feature by stating: who owns the input, who validates it, who mutates authoritative state, who receives the result, and whether it is reliable. Put this in a short comment when it is not obvious from the code.
- The server is authoritative for gameplay-affecting state: damage, health, ammo, score, inventory, spawns, cooldowns, hit validation, and ownership changes. A client request is intent, never proof.
- Check ownership and server/client/spawn state at network boundaries. A `[ServerRpc(RequireOwnership = false)]` is exceptional: validate the caller (use the trailing `NetworkConnection` parameter), target, permissions, distance/visibility where relevant, rate, and every client-supplied value.
- Use `ServerRpc` for client-to-server intent, `ObserversRpc` for server-to-relevant-clients effects, and `TargetRpc` for one-client responses. Give RPCs explicit names that convey direction and intent, such as `RequestReloadServerRpc` and `PlayReloadObserversRpc`.
- Prefer server-owned `SyncVar`/SyncTypes for persistent replicated state and RPCs for transient commands/effects. Do not write client-side SyncTypes unless the authority, permissions, owner echo, and bandwidth rationale are deliberate and documented.
- Treat SyncType send rate, channel, observer set, and payload size as design choices. Do not sync high-frequency transforms, input, or cosmetic data by default. Coalesce state and use unreliable delivery only when loss is safe and a newer update supersedes it.
- Keep prediction input and reconcile structures compact, explicit, allocation-free, and limited to deterministic simulation state. Reconcile server-authoritative state; do not run presentation-only effects, UI, audio, or irreversible side effects during replay.
- Run predicted movement and combat through FishNet ticks, not `Update`. Keep the simulation order stable and use the same inputs, settings, and time step on owner and server. Distinguish normal ticks from replay/reconcile state where effects would otherwise duplicate.
- Do not make network identity, observer scope, ownership, or spawn/despawn changes from arbitrary client code. Let the server use FishNet lifecycle APIs and ensure pooled/despawned objects reset their project-owned state.
- Test every multiplayer feature in host and dedicated-server/client arrangements. Host mode can hide ordering and previous-value differences, so it is not sufficient on its own.

## Refactoring and performance

- First preserve observable behaviour. Refactor in small, reviewable steps; do not combine a large style rewrite with gameplay changes, package upgrades, or asset reserialization.
- Remove dead code, obsolete comments, duplicated knowledge, and unused serialized fields when their absence is verified. Do not retain speculative abstractions or feature flags without an active use case.
- Prefer the clearest working implementation. Introduce caching, pooling, jobs/Burst, custom serialization, specialised collections, or hand-tuned network packing only with profiler/telemetry evidence and a stated target.
- For an optimisation, record the measured before/after metric, test realistic player counts and network conditions, and retain tests for the behaviour. Never trade server validation, replay correctness, or resource cleanup for speed.
- Avoid per-frame garbage in known hot paths, but do not obscure infrequent code to avoid a tiny allocation. If using a performance-oriented pattern that is less obvious, isolate it behind a well-named method and comment on the measured reason.

## Verification checklist

Before handing off a change:

1. Build/compile in the pinned Unity editor with no new warnings or errors.
2. Exercise the smallest relevant play-mode path; for networked code, test owner, remote observer, server, disconnect/despawn, and host behavior where applicable.
3. Confirm an untrusted client cannot mutate authoritative state or invoke the action outside its allowed conditions.
4. Inspect changed prefab/scene/project/package files for unintended serialized churn, GUID changes, or editor-version migration noise.
5. Add or update focused tests for pure logic and high-risk regressions where practical. Prefer deterministic unit tests for domain logic and play-mode/integration tests for Unity/FishNet lifecycle behaviour.
6. State what was verified and what could not be verified.

## Reference material

Recheck these primary sources when Unity or FishNet versions change; this guide intentionally favours them over generic style opinions.

- [Unity Manual: ScriptableObject](https://docs.unity3d.com/6000.1/Documentation/Manual/class-ScriptableObject.html) — shared authored data and asset lifetime.
- [Unity Manual: Script serialization](https://docs.unity3d.com/Manual/script-serialization.html) — serialization limits and compatibility concerns.
- [Unity Manual: Assembly definitions](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html) — compilation boundaries and dependency structure.
- [FishNet: Ownership](https://fish-networking.gitbook.io/docs/guides/features/ownership), [RPCs](https://fish-networking.gitbook.io/docs/guides/features/network-communication/remote-procedure-calls), and [SyncTypes](https://fish-networking.gitbook.io/docs/guides/features/network-communication/synchronizing) — authority and replication semantics.
- [FishNet: Controlling a predicted object](https://fish-networking.gitbook.io/docs/guides/features/prediction/creating-code/controlling-an-object) and [ReplicateState](https://fish-networking.gitbook.io/docs/guides/features/prediction/creating-code/understanding-replicatestate) — tick prediction, reconciliation, and replay.
- [Microsoft: C# async/TAP](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/task-asynchronous-programming-model) and [exception best practices](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions) — awaitable APIs, cancellation, and error handling.
