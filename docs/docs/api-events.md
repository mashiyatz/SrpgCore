# API: SrpgCore.Events

> Generated from `../SrpgCore` sources by `scripts/build-api.ts` — do not edit by hand.

Guide: [events](events.md).

## EventCommand

One event command: `verb;arg0;arg1;...`. Verbs are validated against CommandCatalog; the engine (Prowl or other) implements them in IEventExecutor.

### EventCommand.Verb

Gets the verb name validated against CommandCatalog.

```csharp
public string Verb { get; }
```

Type: `string`

### EventCommand.Args

Gets the trimmed arguments following the verb in source order.

```csharp
public string[] Args { get; }
```

Type: `string[]`

### EventCommand constructor

Initializes a command with a verb and its argument list.

```csharp
public EventCommand(string verb, string[] args)
```

| Parameter | Type | Description |
| --- | --- | --- |
| verb | `string` | The command verb such as speak or move_unit. |
| args | `string[]` | The argument values in source order. |

### EventCommand.Parse

Parses one semicolon separated line into a verb plus trimmed arguments.

```csharp
public static EventCommand Parse(string line)
```

| Parameter | Type | Description |
| --- | --- | --- |
| line | `string` | The source line shaped verb plus semicolon separated args. |

Returns: `EventCommand` — The parsed command with verb and argument values.

### EventCommand.ToString

Formats the command back to its semicolon separated source shape.

```csharp
public override string ToString()
```

Returns: `string` — The verb followed by its semicolon separated arguments.

## CommandCatalog

Known verbs with exact arity. The editor uses this for validation/autocomplete; the runner rejects unknown verbs before executing anything.

### CommandCatalog.TryValidate

Checks that the verb is known and the argument count fits its catalog arity.

```csharp
public static bool TryValidate(EventCommand command, out string? error)
```

| Parameter | Type | Description |
| --- | --- | --- |
| command | `EventCommand` | The command to validate without executing anything. |
| error | `out string?` | The reason the command was rejected or null when it is valid. |

Returns: `bool` — True when the verb is known and the arity matches.

### CommandCatalog.List

Enumerates every known verb with its one line authoring help.

```csharp
public static IEnumerable<(string verb, string help)> List()
```

Returns: `IEnumerable<(string verb, string help)>` — Each catalog verb paired with its help text.

## EventDispatcher

Multi-event dispatch: all events subscribed to a trigger run in priority order (then name — deterministic; duplicates all run).

### EventDispatcher.Dispatch

Runs every event subscribed to a trigger in priority then name order and stops at the first error.

```csharp
public static List<EventRunResult> Dispatch(EventTrigger trigger, IReadOnlyDictionary<string, object?> vars, IEnumerable<GameEvent> events, IEventExecutor executor)
```

| Parameter | Type | Description |
| --- | --- | --- |
| trigger | `EventTrigger` | The raised trigger selecting which events run. |
| vars | `IReadOnlyDictionary<string, object?>` | Trigger variables visible to the condition language. |
| events | `IEnumerable<GameEvent>` | The candidate events of which only trigger matches run. |
| executor | `IEventExecutor` | Host that executes each validated command. |

Returns: `List<EventRunResult>` — One result per attempted event in dispatch order.

## IEventExecutor

Implemented by the host engine (Prowl presentation layer, tests, future ports). The core validates and orders commands; only the executor touches the outside world.

## EventRunResult

Outcome of one EventRunner run including validation failures.

### EventRunResult.Ran

Gets or sets whether the condition passed and the commands ran.

```csharp
public bool Ran { get; set; }
```

Type: `bool`

### EventRunResult.CommandsExecuted

Gets or sets how many commands the executor ran in order.

```csharp
public int CommandsExecuted { get; set; }
```

Type: `int`

### EventRunResult.Error

Gets or sets the condition or validation failure or null on success.

```csharp
public string? Error { get; set; }
```

Type: `string?`

## EventRunner

Runs one event: checks the condition against trigger vars, validates every command up front (fail before side effects), then dispatches in order. Trigger dispatch across many events belongs to the future TurnManager, not here.

### EventRunner.Run

Runs one event when its condition passes; validates all commands before any side effect.

```csharp
public static EventRunResult Run(GameEvent gameEvent, IReadOnlyDictionary<string, object?> vars, IEventExecutor executor)
```

| Parameter | Type | Description |
| --- | --- | --- |
| gameEvent | `GameEvent` | The event definition (trigger already matched by the caller). |
| vars | `IReadOnlyDictionary<string, object?>` | Trigger variables visible to the condition language. |
| executor | `IEventExecutor` | Host that executes each validated command. |

Returns: `EventRunResult` — Whether the event ran, commands executed, and any validation error.

## EventTrigger

Engine-raised triggers. The TurnManager/battle code raises these; GameEvents subscribe to them.

| Name | Value | Description |
| --- | --- | --- |
| LevelStart | 0 | Fires when the chapter opens. |
| LevelEnd | 1 | Fires when the chapter closes. |
| PlayerPhaseStart | 2 | Fires at each player phase boundary. |
| EnemyPhaseStart | 3 | Fires at each enemy phase boundary. |
| UnitDeath | 4 | Fires when a unit dies with unit nid and position available. |
| UnitWait | 5 | Fires when a unit waits with unit nid and position available. |
| UnitSelect | 6 | Fires when a unit is selected with unit nid and position available. |
| UnitLevelUp | 7 | Fires when a unit levels up with unit nid available. |
| CombatStart | 8 | Fires when combat starts with attacker, defender, item, and position available. |
| CombatEnd | 9 | Fires when combat ends with attacker, defender, item, and position available. |
| Talk | 10 | Fires when two units talk with both units and position available. |
| Support | 11 | Fires when a support grows with both units and rank available. |
| RegionEntered | 12 | Fires when a unit enters a region with unit, region, and position available. |

## GameEvent

Scripted event: unique name + trigger + condition expression + command list. Conditions evaluate against trigger variables (see SimpleConditionEvaluator).

### GameEvent.Name

Gets the unique author facing event name.

```csharp
public string Name { get; }
```

Type: `string`

### GameEvent.Trigger

Gets the trigger this event subscribes to.

```csharp
public EventTrigger Trigger { get; }
```

Type: `EventTrigger`

### GameEvent.Condition

Gets the gating expression where empty means always run.

```csharp
public string Condition { get; }
```

Type: `string`

### GameEvent.Priority

Dispatch order among same-trigger events (LT default 20; higher runs first).

```csharp
public int Priority { get; set; }
```

Type: `int`

### GameEvent.Commands

Gets the ordered command list dispatched when the event runs.

```csharp
public IList<EventCommand> Commands => _commands;
```

Type: `IList<EventCommand>`

### GameEvent constructor

Initializes an event with a name, trigger, and optional gating condition.

```csharp
public GameEvent(string name, EventTrigger trigger, string condition =)
```

| Parameter | Type | Description |
| --- | --- | --- |
| name | `string` | The unique author facing event name. |
| trigger | `EventTrigger` | The trigger this event subscribes to. |
| condition | `string` | The gating expression where empty means always run. |

### GameEvent.AddCommand

Parses one command line and appends it to the ordered command list.

```csharp
public void AddCommand(string line)
```

| Parameter | Type | Description |
| --- | --- | --- |
| line | `string` | The source line shaped verb plus semicolon separated args. |

## SimpleConditionEvaluator

Minimal condition language for event gating: unit.nid == 'Eirika' && turn > 3 Clauses joined by &&; each is `path op literal` with ops ==, !=, >, <, >=, <=. Paths resolve against nested string/object dictionaries (e.g. vars["unit"]["nid"]). Literals: 'quoted', numbers, true/false. Unknown paths evaluate to null (== null-safe false). Deliberately tiny: replace with a real scripting bridge only if chapters outgrow it.

### SimpleConditionEvaluator.Evaluate

Evaluates a condition against trigger vars where empty always passes and clauses join with AND.

```csharp
public static bool Evaluate(string condition, IReadOnlyDictionary<string, object?> vars)
```

| Parameter | Type | Description |
| --- | --- | --- |
| condition | `string` | The clauses joined by AND with each shaped path op literal. |
| vars | `IReadOnlyDictionary<string, object?>` | The nested trigger variables that dotted paths resolve against. |

Returns: `bool` — True when every clause passes or the condition is empty.
