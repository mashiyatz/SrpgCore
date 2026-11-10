# Hosting

SrpgCore has no engine references (`netstandard2.0`, no packages), so every host
follows the same shape: reference the library, keep presentation in thin adapters,
and call the same pure functions the Verify suite covers. The four examples below —
Unity, Godot, Blazor, and a framework-agnostic web client — show that shape across
very different runtimes; none of them is the design center, and a new host follows
the same rules.

## Shared mapping (all hosts)

| Concern | Core API | Host job |
| --- | --- | --- |
| Board + movement | [`Board`](api-grid.md#board), [`RangeFinder`](api-grid.md#rangefinder), [`Battlefield.Reachable`](api-tactics.md#battlefield-reachable) | tilemap render, range highlights, path preview |
| Forecast UI | [`CombatResolver.Forecast`](api-combat.md#combatresolver-forecast), [`TerrainAt`](api-combat.md#combatresolver-terrainat) | forecast window, effective/triangle badges |
| Combat | [`Resolve`](api-combat.md#combatresolver-resolve) entry overload (durability), [`ResolveHeal`](api-combat.md#combatresolver-resolveheal) | attack animation, HP bars, break icons |
| AI | [`EnemyAi.Decide`](api-tactics.md#enemyai-decide) with [`TerrainOf`](api-tactics.md#enemyai-terrainof)/[`DefenderWeaponOf`](api-tactics.md#enemyai-defenderweaponof) | enemy turn driver, threat overlay via `ThreatCalculator` |
| Events | [`EventRunner`](api-events.md#eventrunner)/[`EventDispatcher`](api-events.md#eventdispatcher) + [`IEventExecutor`](api-events.md#ieventexecutor) | dialogue boxes, portraits, movement tweens |
| Objectives | [`ChapterState.EvaluateObjectives`](api-tactics.md#chapterstate-evaluateobjectives) | banners, game-over screens |
| Fog | [`FogCalculator`](api-tactics.md#fogcalculator) + [`Unit.Vision`](api-data.md#unit-vision) | hide tokens, gate targeting |
| Rescue | [`Rescue`](api-tactics.md#rescue) + `rescue;`/`drop;` | carry/drop animations, laden move display |
| Skills | [`SkillCatalog.ApplyToForecast`](api-combat.md#skillcatalog-applytoforecast) | skill icons, proc flashes |
| Support | [`SupportLedger.BonusFor`](api-tactics.md#supportledger-bonusfor) | bond UI, adjusted forecast |
| Statuses | [`Data.Statuses`](api-data.md#statuses) + status staves | status icons, phase-boundary ticks |
| Saves | [`SaveSystem`](api-tactics.md#savesystem) DTOs | file/`localStorage`/cloud persistence |

## 1) Unity

- **Reference:** drop the built `SrpgCore.dll` under `Assets/Plugins/` (auto-referenced).
  No source port, no package install.
- **Data:** author chapters as the same Markdown files and load at runtime with
  [`MarkdownChapterLoader.Load`](api-data.md#markdownchapterloader-load) (via `Resources`/`StreamingAssets`), or convert armory,
  classes, and growths into `ScriptableObject` DBs that feed [`Weapon`](api-data.md#weapon)/[`UnitClass`](api-data.md#unitclass)/
  [`GrowthRates`](api-tactics.md#growthrates) constructors. [`ClassDatabase`](api-data.md#classdatabase) maps directly to a class-table asset.
- **Turn driver:** a `ChapterController : MonoBehaviour` owns the [`ChapterState`](api-tactics.md#chapterstate),
  cycles [`TurnManager`](api-tactics.md#turnmanager), and runs coroutines per phase: highlight
  `Battlefield.Reachable`, animate [`RangeFinder.FindPath`](api-grid.md#rangefinder-findpath), call `Resolve`, then
  [`CombatGains.ApplyAfterCombat`](api-tactics.md#combatgains-applyaftercombat), [`PhaseTick.Apply`](api-tactics.md#phasetick-apply) at phase boundaries, and
  `EvaluateObjectives` after every checkpoint.

```csharp
// Thin Unity adapter: the core decides, coroutines animate.
public sealed class ChapterController : MonoBehaviour {
    ChapterState _state = null!;
    ChapterRunner _runner = null!;

    void Start() {
        var loaded = MarkdownChapterLoader.Load(chapterText.text, "ch1.md");
        _state = ChapterState.StartNew(loaded.Chapter!, (uint)Random.Range(1, int.MaxValue));
        _runner = new ChapterRunner(_state);
        EventDispatcher.Dispatch(EventTrigger.LevelStart,
            new Dictionary<string, object?>(), _state.Chapter.Events, _runner);
    }

    IEnumerator Strike(PlacedUnit atk, InventoryEntry blade, PlacedUnit def) {
        // ... forecast panel via CombatResolver.Forecast, attack animation ...
        CombatResult r = CombatResolver.Resolve(atk.Unit, blade, def.Unit, counter,
            atk.Position.ManhattanDistance(def.Position),
            0, 0, 0, 0, _state.CombatRng);
        yield return AnimateStrikes(r); // HP bars follow result.AttackerHPLeft/DefenderHPLeft
    }
}
```
- **Events:** implement [`IEventExecutor`](api-events.md#ieventexecutor) with TextMeshPro dialogue, portrait
  `Sprite` swaps, and DOTween/Animator movement for `move_unit`. `recruit`/`promote`
  flip side/class and swap prefabs; `heal`/`damage` drive floating numbers.
- **Saves:** serialize [`ChapterSnapshot`](api-tactics.md#chaptersnapshot) with `JsonUtility` (or JSON.NET) to
  `Application.persistentDataPath`; armory keys are the contract, so renames need
  migration. RNG states round-trip, so suspend/resume continues the same sequence.
- **Why it fits:** Unity's prefab/UI/anim systems cover exactly what the core omits
  (rendering, input, audio), and C# means zero interop cost.

## 2) Godot

- **Reference:** Godot's C# build is an ordinary MSBuild SDK project, so
  `SrpgCore.csproj` goes straight into the generated `.csproj` as a normal
  `<ProjectReference>` (or the built DLL as a `<Reference>`) — no plugin-folder
  convention to work around, unlike engines that compile scripts out of band.
- **Data:** load the same Markdown chapters with [`MarkdownChapterLoader.Load`](api-data.md#markdownchapterloader-load),
  reading the text via `FileAccess.GetFileAsString("res://chapters/ch1.md")`;
  armory/classes can stay as the same Markdown-defined data or move into custom
  Godot `Resource`/`.tres` assets that feed the [`Weapon`](api-data.md#weapon)/[`UnitClass`](api-data.md#unitclass) constructors.
- **Turn driver:** a `ChapterController : Node` owns [`ChapterState`](api-tactics.md#chapterstate) and
  [`ChapterRunner`](api-tactics.md#chapterrunner). Godot C# has no Unity-style coroutines, so phase sequencing
  uses `async`/`await` with scene-tree timers instead:

```csharp
// Thin Godot adapter: async/await replaces Unity's coroutines for sequencing.
public partial class ChapterController : Node {
    ChapterState _state = null!;
    ChapterRunner _runner = null!;

    public override void _Ready() {
        var text = FileAccess.GetFileAsString("res://chapters/ch1.md");
        var loaded = MarkdownChapterLoader.Load(text, "ch1.md");
        _state = ChapterState.StartNew(loaded.Chapter!, (uint)GD.Randi());
        _runner = new ChapterRunner(_state);
        EventDispatcher.Dispatch(EventTrigger.LevelStart,
            new Dictionary<string, object?>(), _state.Chapter.Events, _runner);
    }

    async Task Strike(PlacedUnit atk, InventoryEntry blade, PlacedUnit def, InventoryEntry? counter) {
        CombatResult r = CombatResolver.Resolve(atk.Unit, blade, def.Unit, counter,
            atk.Position.ManhattanDistance(def.Position),
            0, 0, 0, 0, _state.CombatRng);
        await ToSignal(GetTree().CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
        RefreshHud(r); // HP bars follow result.AttackerHPLeft/DefenderHPLeft
    }
}
```
- **Events:** implement [`IEventExecutor`](api-events.md#ieventexecutor) over `Label`/`RichTextLabel` dialogue and
  `AnimationPlayer`/`Tween` movement for `move_unit`; emit a Godot `[Signal]` per
  command so UI nodes can react without the controller knowing about them directly.
- **Saves:** serialize [`ChapterSnapshot`](api-tactics.md#chaptersnapshot) with `System.Text.Json` and write it through
  `FileAccess.Open("user://save.json", FileAccess.ModeFlags.Write)` — `user://`
  resolves to the platform save directory the same way across desktop and console
  exports.
- **Why it fits:** Godot's node/signal composition maps cleanly onto
  `IEventExecutor`, and its .NET build treats `SrpgCore` like any other project
  reference rather than needing a special asset-folder route.

## 3) Blazor

Blazor is the one host shape where the "logic" and the "UI" are both C# running in
the same process — in WebAssembly, that process is the browser tab itself, so there
is no network hop and no serialization boundary between a player action and a core
call.

- **Reference:** a normal `<ProjectReference>` in the Blazor project's `.csproj`,
  same as any other .NET library — `netstandard2.0` is binary-compatible with the
  WASM runtime, so this needs no adapter project at all.
- **Data:** ship chapter Markdown as static web assets (`wwwroot/chapters/ch1.md`),
  fetch with `HttpClient`, and parse with [`MarkdownChapterLoader.Load`](api-data.md#markdownchapterloader-load).
- **State:** a scoped DI service holds [`ChapterState`](api-tactics.md#chapterstate)/[`ChapterRunner`](api-tactics.md#chapterrunner) so razor
  components share one turn loop instead of each re-deriving it:

```csharp
// Scoped service: the one ChapterState/ChapterRunner every component reads.
public sealed class ChapterStateService {
    public ChapterState State { get; private set; } = null!;
    public ChapterRunner Runner { get; private set; } = null!;
    public event Action? Changed;

    public async Task LoadAsync(HttpClient http, uint seed) {
        var text = await http.GetStringAsync("chapters/ch1.md");
        var loaded = MarkdownChapterLoader.Load(text, "ch1.md");
        State = ChapterState.StartNew(loaded.Chapter!, seed);
        Runner = new ChapterRunner(State);
        Changed?.Invoke();
    }

    public void Strike(PlacedUnit atk, InventoryEntry blade, PlacedUnit def, InventoryEntry? counter) {
        CombatResolver.Resolve(atk.Unit, blade, def.Unit, counter,
            atk.Position.ManhattanDistance(def.Position), 0, 0, 0, 0, State.CombatRng);
        Changed?.Invoke(); // a .razor component calls StateHasChanged() on this
    }
}
```
- **Rendering:** a `.razor` component lays the `Board` out as a CSS grid of
  elements, calling [`RangeFinder.ComputeReachable`](api-grid.md#rangefinder-computereachable) and [`CombatResolver.Forecast`](api-combat.md#combatresolver-forecast)
  directly from click handlers — the forecast panel reads straight off the same
  objects the combat call will use, with nothing to keep in sync.
- **Events:** [`IEventExecutor`](api-events.md#ieventexecutor) mutates component-bindable state (dialogue text,
  portrait id) and raises `ChapterStateService.Changed`, which components forward
  to `StateHasChanged()`.
- **Saves:** WebAssembly has no filesystem, so persist through `IJSRuntime`:
  serialize [`SaveSystem.Save(state)`](api-tactics.md#savesystem-save) with `System.Text.Json` and call
  `JSRuntime.InvokeVoidAsync("localStorage.setItem", key, json)`; [`Restore`](api-tactics.md#savesystem-restore) on load.
- **Blazor Server:** the same service/component code also runs unchanged under
  Blazor Server — the core still executes in one C# process, it is just the server
  process instead of the browser, with the UI mirrored down over SignalR. Pick this
  shape over WebAssembly when you want the RNG and save data to never leave the
  server.
- **Why it fits:** no other host here skips the serialization boundary entirely —
  the forecast a player sees and the `Resolve` call that follows it touch the exact
  same `Unit`/`Weapon` instances.

## 4) Framework-agnostic web client

Logic server + JS frontend, for when
the UI is owned by a non-.NET framework instead of Razor. Host the core in a minimal
ASP.NET API (or a headless `dotnet` worker): endpoints for reachable tiles,
forecasts, AI moves, event runs, and save blobs. The client
stays rendering-only — grid canvas, threat overlay, forecast panel, dialogue queue —
and mirrors its movement/combat/ai/events modules against the server as oracle. [`SaveSystem`](api-tactics.md#savesystem) DTOs cross as JSON; [`SeededRng`](api-random.md#seededrng) states
make replays and spectator links trivial (`?seed=…&turn=…`). Keep the TS side
behavior-locked with vectors copied from Verify (forecast hand-computed values, 2RN
bands, `<=` reinforcement spawns) so the web build cannot drift from the core.

```csharp
// Logic server (minimal API): numbers stay in C#, the browser only renders.
app.MapPost("/forecast", (ForecastRequest q) => {
    CombatResolver.TerrainAt(board, q.From, out int aAvo, out int aDef);
    CombatResolver.TerrainAt(board, q.To, out int dAvo, out int dDef);
    return CombatResolver.Forecast(q.Attacker, q.Weapon, q.Defender, q.Counter,
        aAvo, aDef, dAvo, dDef);
});
app.MapGet("/range", (int x, int y, int move, string group) =>
    RangeFinder.ComputeReachable(board, new GridPoint(x, y), move, group));
app.MapPost("/saves", (ChapterSnapshot snap) => store.Save(snap)); // SaveSystem DTO as JSON
```

## Non-goals for hosts

No host duplicates core math (triangle, hit modes, XP, growths, threat). New verbs
need a [`CommandCatalog`](api-events.md#commandcatalog) entry plus executor handling on every host, plus a Verify
check — the same rule as the core.
