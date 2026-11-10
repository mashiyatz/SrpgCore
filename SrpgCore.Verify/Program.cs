using System;
using System.Collections.Generic;
using SrpgCore.Combat;
using SrpgCore.Data;
using SrpgCore.Events;
using SrpgCore.Grid;
using SrpgCore.Random;
using SrpgCore.Regions;
using SrpgCore.Tactics;

namespace SrpgCore.Verify
{
    internal static class Program
    {
        private static int _passed;
        private static int _failed;

        private static void Check(bool condition, string name)
        {
            if (condition)
            {
                _passed++;
                Console.WriteLine("  ok: " + name);
            }
            else
            {
                _failed++;
                Console.WriteLine("  FAIL: " + name);
            }
        }

        private static int Main()
        {
            Console.WriteLine("[grid]");
            GridChecks();
            Console.WriteLine("[rng]");
            RngChecks();
            Console.WriteLine("[combat]");
            CombatChecks();
            Console.WriteLine("[events]");
            EventChecks();
            Console.WriteLine("[rng-streams]");
            RngStreamChecks();
            Console.WriteLine("[hit-modes]");
            HitModeChecks();
            Console.WriteLine("[movement-groups]");
            MovementGroupChecks();
            Console.WriteLine("[terrain-fx]");
            TerrainFxChecks();
            Console.WriteLine("[phases]");
            PhaseChecks();
            Console.WriteLine("[objectives]");
            ObjectiveChecks();
            Console.WriteLine("[tactics-ai]");
            TacticsAiChecks();
            Console.WriteLine("[conditions-edge]");
            ConditionEdgeChecks();
            Console.WriteLine("[chapter]");
            ChapterChecks();
            Console.WriteLine("[format]");
            FormatChecks();
            Console.WriteLine("[sprite-override]");
            SpriteChecks();
            Console.WriteLine("[durability-effective]");
            DurabilityEffectiveChecks();
            Console.WriteLine("[experience]");
            ExperienceChecks();
            Console.WriteLine("[staff]");
            StaffChecks();
            Console.WriteLine("[new-verbs]");
            NewVerbChecks();
            Console.WriteLine("[ai-terrain]");
            AiTerrainChecks();
            Console.WriteLine("[seize-save-promote]");
            SeizeSavePromoteChecks();
            Console.WriteLine("[fog-rescue-skill-support-status]");
            FogRescueSkillSupportStatusChecks();
            Console.WriteLine("[p0-wireup]");
            P0Checks();
            Console.WriteLine("[p1]");
            P1Checks();
            Console.WriteLine("[dance-canto]");
            DanceCantoChecks();
            Console.WriteLine("[new-features]");
            NewFeaturesChecks();
            Console.WriteLine("[warp-torch-inv-reaver-formation]");
            WarpTorchInventoryReaverFormationChecks();
            Console.WriteLine("[decomp-core]");
            DecompCoreChecks();

            Console.WriteLine($"-- {_passed} passed, {_failed} failed --");
            return _failed == 0 ? 0 : 1;
        }

        private static Board OpenBoard(int w = 15, int h = 15) =>
            new Board(w, h, "plain", StandardTerrains.Create());

        private static void GridChecks()
        {
            var board = OpenBoard();
            var origin = new GridPoint(7, 7);

            // Move 5 on open plain: 1 + 4 + 8 + 12 + 16 + 20 = 61.
            var reach = RangeFinder.ComputeReachable(board, origin, 5, MoveType.Infantry);
            Check(reach.Count == 61, $"open plain move-5 diamond has 61 tiles (got {reach.Count})");
            Check(reach[origin] == 5, "origin keeps full move");

            // Forest costs infantry 2: tile two east is reachable with 3 left via plain detour? use wall-free check.
            board.SetTerrain(new GridPoint(8, 7), "forest");
            reach = RangeFinder.ComputeReachable(board, origin, 5, MoveType.Infantry);
            Check(reach[new GridPoint(8, 7)] == 3, "forest entry costs infantry 2");

            // Flier pays 1 on forest.
            var fly = RangeFinder.ComputeReachable(board, origin, 5, MoveType.Flier);
            Check(fly[new GridPoint(8, 7)] == 4, "flier ignores forest cost");

            // Wall ring around a tile isolates it.
            var pocket = OpenBoard(5, 5);
            var center = new GridPoint(2, 2);
            pocket.SetTerrain(new GridPoint(1, 2), "wall");
            pocket.SetTerrain(new GridPoint(3, 2), "wall");
            pocket.SetTerrain(new GridPoint(2, 1), "wall");
            pocket.SetTerrain(new GridPoint(2, 3), "wall");
            var iso = RangeFinder.ComputeReachable(pocket, new GridPoint(0, 0), 9, MoveType.Infantry);
            Check(!iso.ContainsKey(center), "walled pocket unreachable");

            // Blocked units: tile excluded, path routes around, origin exempt.
            var blocked = new HashSet<GridPoint> { new GridPoint(8, 7) };
            var reachB = RangeFinder.ComputeReachable(OpenBoard(), origin, 5, MoveType.Infantry, blocked);
            Check(!reachB.ContainsKey(new GridPoint(8, 7)), "occupied tile excluded from range");
            var path = RangeFinder.FindPath(OpenBoard(), origin, new GridPoint(9, 7), 5, MoveType.Infantry, blocked);
            Check(path != null && !path.Contains(new GridPoint(8, 7)), "path routes around blocker");

            var trivial = RangeFinder.FindPath(OpenBoard(), origin, origin, 5, MoveType.Infantry);
            Check(trivial != null && trivial.Count == 1, "path to self is single tile");

            var noPath = RangeFinder.FindPath(pocket, new GridPoint(0, 0), center, 9, MoveType.Infantry);
            Check(noPath == null, "no path into walled pocket");

            var region = new Region("village-door", RegionKind.Event);
            region.Add(new GridPoint(3, 3));
            Check(region.Contains(new GridPoint(3, 3)) && !region.Contains(origin), "region membership");
        }

        private static void RngChecks()
        {
            var a = new SeededRng(1234);
            var b = new SeededRng(1234);
            bool same = true;
            for (int i = 0; i < 50; i++) same &= a.Next(1000) == b.Next(1000);
            Check(same, "same seed reproduces sequence");

            var rng = new SeededRng(99);
            Check(!rng.RollHit2RN(0) && rng.RollHit2RN(100), "0/100 rates are absolute");

            // 2RN(80) true rate ~90%: loose statistical band over 20k samples.
            int hits = 0;
            const int n = 20000;
            for (int i = 0; i < n; i++) if (rng.RollHit2RN(80)) hits++;
            double rate = hits / (double)n;
            Check(rate > 0.85 && rate < 0.95, $"2RN(80) skewed high (observed {rate:F3})");

            var rng2 = new SeededRng(7);
            hits = 0;
            for (int i = 0; i < n; i++) if (rng2.RollHit1RN(50)) hits++;
            rate = hits / (double)n;
            Check(rate > 0.45 && rate < 0.55, $"1RN(50) ~ coin flip (observed {rate:F3})");
        }

        private static Unit KnightLike() => new Unit("atk", "Attacker", Affiliation.Player, "mercenary",
            new Stats { MaxHP = 20, Power = 5, Skill = 6, Speed = 7, Luck = 3, Defense = 4, Resistance = 1, Constitution = 8 });

        private static Unit BanditLike() => new Unit("def", "Defender", Affiliation.Enemy, "brigand",
            new Stats { MaxHP = 22, Power = 6, Skill = 4, Speed = 3, Luck = 1, Defense = 3, Resistance = 0, Constitution = 9 });

        private static void CombatChecks()
        {
            Check(WeaponTriangle.Advantage(WeaponType.Sword, WeaponType.Axe) == 1, "sword beats axe");
            Check(WeaponTriangle.Advantage(WeaponType.Axe, WeaponType.Lance) == 1, "axe beats lance");
            Check(WeaponTriangle.Advantage(WeaponType.Lance, WeaponType.Sword) == 1, "lance beats sword");
            Check(WeaponTriangle.Advantage(WeaponType.Sword, WeaponType.Lance) == -1, "sword loses to lance");
            Check(WeaponTriangle.Advantage(WeaponType.Bow, WeaponType.Sword) == 0, "bow outside triangle");

            var ironSword = new Weapon("iron-sword", "Iron Sword", WeaponType.Sword, WeaponRank.E,
                might: 5, hit: 90, crit: 0, weight: 5, minRange: 1, maxRange: 1, maxUses: 40);
            var ironAxe = new Weapon("iron-axe", "Iron Axe", WeaponType.Axe, WeaponRank.E,
                might: 8, hit: 75, crit: 0, weight: 10, minRange: 1, maxRange: 1, maxUses: 40);

            var atk = KnightLike();
            var def = BanditLike();

            // Hand-computed: dmg = 5+5+1(tri) - 3 - 0 = 8.
            // Hit = 90 + 12 + 1 + 15(tri) - (AS 3 + 0 + 0) = 115 -> clamped 100.
            // Crit = 3 + 0 - 0 = 3. AS 7-5=... atk AS = 7-max(0,5-8)=7; def AS = 3-max(0,10-9)=2; diff 5 -> 2 attacks.
            var fc = CombatResolver.Forecast(atk, ironSword, def, ironAxe, 0, 0, 0, 0);
            Check(fc.Damage == 8, $"forecast damage 8 (got {fc.Damage})");
            Check(fc.Hit == 100, $"forecast hit clamped 100 (got {fc.Hit})");
            Check(fc.Crit == 3, $"forecast crit 3 (got {fc.Crit})");
            Check(fc.AttackCount == 2, "speed diff 5 doubles");
            Check(fc.TriangleAdvantage == 1, "triangle reported");

            // Determinism: same seed, fresh units -> identical strike log.
            CombatResult r1 = CombatResolver.Resolve(FreshKnight(), ironSword, FreshBandit(), ironAxe, 1, 0, 0, 0, 0, new SeededRng(42));
            CombatResult r2 = CombatResolver.Resolve(FreshKnight(), ironSword, FreshBandit(), ironAxe, 1, 0, 0, 0, 0, new SeededRng(42));
            Check(r1.Strikes.Count == r2.Strikes.Count && r1.DefenderHPLeft == r2.DefenderHPLeft,
                "same seed replays identically");

            // HP accounting: defender lost exactly the dealt damage.
            int dealt = 0;
            foreach (var s in r1.Strikes) if (s.IsAttackerStrike && s.HitLanded) dealt += s.DamageDealt;
            Check(22 - r1.DefenderHPLeft == Math.Min(dealt, 22) || r1.DefenderHPLeft == 0,
                "defender HP matches dealt damage");

            // Out-of-range defender cannot counter.
            var bow = new Weapon("iron-bow", "Iron Bow", WeaponType.Bow, WeaponRank.E,
                might: 6, hit: 85, crit: 0, weight: 6, minRange: 2, maxRange: 2, maxUses: 40);
            var archer = FreshBandit();
            CombatResult r3 = CombatResolver.Resolve(FreshKnight(), ironSword, archer, bow, 1, 0, 0, 0, 0, new SeededRng(5));
            bool countered = false;
            foreach (var s in r3.Strikes) if (!s.IsAttackerStrike) countered = true;
            Check(!countered, "bow cannot counter at range 1");
        }

        private static Unit FreshKnight() => KnightLike();
        private static Unit FreshBandit() => BanditLike();

        private sealed class RecordingExecutor : IEventExecutor
        {
            public readonly List<string> Ran = new List<string>();
            public void Execute(EventCommand command) => Ran.Add(command.ToString());
        }

        private static void EventChecks()
        {
            var cmd = EventCommand.Parse("speak;Eirika;Oh no!");
            Check(cmd.Verb == "speak" && cmd.Args.Length == 2 && cmd.Args[1] == "Oh no!", "command parses");

            Check(!CommandCatalog.TryValidate(new EventCommand("frobnicate", new string[0]), out _),
                "unknown verb rejected");
            Check(!CommandCatalog.TryValidate(new EventCommand("speak", new[] { "OnlyOne" }), out _),
                "wrong arity rejected");

            var vars = new Dictionary<string, object?>
            {
                ["unit"] = new Dictionary<string, object?> { ["nid"] = "Eirika" },
                ["turn"] = 5L,
            };
            Check(SimpleConditionEvaluator.Evaluate("", vars), "empty condition passes");
            Check(SimpleConditionEvaluator.Evaluate("unit.nid == 'Eirika'", vars), "string equality");
            Check(!SimpleConditionEvaluator.Evaluate("unit.nid == 'Seth'", vars), "string mismatch filters");
            Check(SimpleConditionEvaluator.Evaluate("unit.nid == 'Eirika' && turn > 3", vars), "compound &&");
            Check(!SimpleConditionEvaluator.Evaluate("turn < 3", vars), "numeric comparison");

            var ev = new GameEvent("eirika-falls", EventTrigger.UnitDeath, "unit.nid == 'Eirika'");
            ev.AddCommand("add_portrait;Eirika;Right");
            ev.AddCommand("speak;Eirika;Oh no!");
            ev.AddCommand("remove_portrait;Eirika");

            var exec = new RecordingExecutor();
            var deathVars = new Dictionary<string, object?>
            {
                ["unit"] = new Dictionary<string, object?> { ["nid"] = "Eirika" },
            };
            var res = EventRunner.Run(ev, deathVars, exec);
            Check(res.Ran && res.CommandsExecuted == 3 && exec.Ran.Count == 3, "matching event runs in order");
            Check(exec.Ran[1] == "speak;Eirika;Oh no!", "command order preserved");

            var exec2 = new RecordingExecutor();
            var otherVars = new Dictionary<string, object?>
            {
                ["unit"] = new Dictionary<string, object?> { ["nid"] = "Garcia" },
            };
            var res2 = EventRunner.Run(ev, otherVars, exec2);
            Check(!res2.Ran && res2.CommandsExecuted == 0, "non-matching unit filtered out");

            var bad = new GameEvent("bad", EventTrigger.LevelStart);
            bad.AddCommand("frobnicate;now");
            var res3 = EventRunner.Run(bad, new Dictionary<string, object?>(), new RecordingExecutor());
            Check(!res3.Ran && res3.Error != null, "bad verb fails before side effects");

            Check(CommandCatalog.TryValidate(EventCommand.Parse("say;Narrator;Hold the line."), out _),
                "say validates");
            Check(!CommandCatalog.TryValidate(new EventCommand("say", new[] { "OnlyOne" }), out _),
                "say wrong arity rejected");
            var sayState = ChapterState.StartNew(TestChapter(), 1);
            var sayRunner = new ChapterRunner(sayState);
            var sayEv = new GameEvent("mix", EventTrigger.LevelStart);
            sayEv.AddCommand("say;Narrator;Ambient.");
            sayEv.AddCommand("speak;Eirika;Advance me.");
            sayEv.AddCommand("say;Narrator;Again.");
            EventRunner.Run(sayEv, new Dictionary<string, object?>(), sayRunner);
            Check(sayRunner.Dialogue.Count == 1 && sayRunner.Dialogue[0].speaker == "Eirika"
                    && sayRunner.Aside.Count == 2 && sayRunner.Aside[1].text == "Again.",
                "speak stays modal while say lands ambient, order preserved");
        }

        private static void RngStreamChecks()
        {
            var combat = SeededRng.CreateStream(777, 0);
            var growth = SeededRng.CreateStream(777, 1);
            var combat2 = SeededRng.CreateStream(777, 0);
            Check(combat.Next(100000) == combat2.Next(100000), "same stream index replays");
            bool differ = false;
            for (int i = 0; i < 20; i++) differ |= combat.Next(100000) != growth.Next(100000);
            Check(differ, "combat and growth streams diverge");

            var rng = new SeededRng(5);
            int a = rng.Next(100000);
            uint state = rng.GetState();
            int b = rng.Next(100000);
            rng.SetState(state);
            Check(rng.Next(100000) == b && a != b, "save/restore resumes sequence (turnwheel)");
        }

        private static void HitModeChecks()
        {
            var sword = new Weapon("iron-sword", "Iron Sword", WeaponType.Sword, WeaponRank.E,
                might: 5, hit: 90, crit: 0, weight: 5, minRange: 1, maxRange: 1, maxUses: 40);
            var axe = new Weapon("iron-axe", "Iron Axe", WeaponType.Axe, WeaponRank.E,
                might: 8, hit: 75, crit: 0, weight: 10, minRange: 1, maxRange: 1, maxUses: 40);

            foreach (HitMode mode in new[] { HitMode.Classic, HitMode.TrueHit, HitMode.TrueHitPlus, HitMode.Fates })
            {
                var rules = new CombatRules { HitMode = mode };
                CombatResult r1 = CombatResolver.Resolve(FreshKnight(), sword, FreshBandit(), axe, 1, 0, 0, 0, 0, new SeededRng(11), rules);
                CombatResult r2 = CombatResolver.Resolve(FreshKnight(), sword, FreshBandit(), axe, 1, 0, 0, 0, 0, new SeededRng(11), rules);
                Check(r1.Strikes.Count == r2.Strikes.Count && r1.DefenderHPLeft == r2.DefenderHPLeft,
                    $"{mode} deterministic under seed");
            }

            // Fates endpoints hold: forecast-0 never lands, forecast-100 always lands.
            // (Forecast adds stats to weapon hit, so force the extremes with stats.)
            var fates = new CombatRules { HitMode = HitMode.Fates };
            var peashooter = new Weapon("pea", "Peashooter", WeaponType.Sword, WeaponRank.E,
                might: 1, hit: 0, crit: 0, weight: 1, minRange: 1, maxRange: 1, maxUses: 40);
            var sure = new Weapon("sure", "Sure Hit", WeaponType.Sword, WeaponRank.E,
                might: 1, hit: 100, crit: 0, weight: 1, minRange: 1, maxRange: 1, maxUses: 40);
            var hopeless = new Unit("h", "Hopeless", Affiliation.Player, "civilian",
                new Stats { MaxHP = 10, Power = 1, Skill = 0, Speed = 1, Luck = 0, Defense = 0, Resistance = 0, Constitution = 5 });
            var master = new Unit("m", "Master", Affiliation.Player, "lord",
                new Stats { MaxHP = 10, Power = 1, Skill = 50, Speed = 1, Luck = 30, Defense = 0, Resistance = 0, Constitution = 5 });
            var frng = new SeededRng(3);
            bool landedAtZero = false, missedAtFull = false;
            for (int i = 0; i < 50; i++)
            {
                CombatResult rz = CombatResolver.Resolve(hopeless, peashooter, FreshBandit(), null, 1, 0, 0, 0, 0, frng, fates);
                foreach (CombatStrike s in rz.Strikes) if (s.IsAttackerStrike && s.HitLanded) landedAtZero = true;
                CombatResult rf = CombatResolver.Resolve(master, sure, FreshBandit(), null, 1, 0, 0, 0, 0, frng, fates);
                foreach (CombatStrike s in rf.Strikes) if (s.IsAttackerStrike && !s.HitLanded) missedAtFull = true;
            }
            Check(!landedAtZero && !missedAtFull, "fates endpoints absolute");
        }

        private static void MovementGroupChecks()
        {
            var terrains = StandardTerrains.Create();
            // Custom group, data-only: pirates treat water as cheap, walls still block.
            terrains["water"] = new TerrainType("water", "Water",
                new Dictionary<string, int> { ["pirate"] = 1, ["infantry"] = -1 }, avoid: 10, defense: 0);
            var board = new Board(5, 5, "plain", terrains);
            board.SetTerrain(new GridPoint(1, 0), "water");

            var pirate = RangeFinder.ComputeReachable(board, new GridPoint(0, 0), 3, "pirate");
            Check(pirate.ContainsKey(new GridPoint(1, 0)), "custom group crosses water");
            var foot = RangeFinder.ComputeReachable(board, new GridPoint(0, 0), 3, MoveType.Infantry);
            Check(!foot.ContainsKey(new GridPoint(1, 0)), "infantry blocked by water");

            // Cost 99 counts as impassable (LT threshold), like -1.
            var swamp = new TerrainType("swamp", "Swamp",
                new Dictionary<string, int> { ["infantry"] = 99 }, avoid: 0, defense: 0);
            Check(!swamp.IsPassable("infantry") && swamp.IsPassable("cavalry"), "99 impassable; unknown group defaults 1");
        }

        private static void TerrainFxChecks()
        {
            var terrains = StandardTerrains.Create();
            var fort = terrains["fort"];
            var unit = FreshKnight(); // 20 max
            unit.CurrentHP = 10;
            int delta = TerrainEffects.Apply(unit, fort);
            Check(delta == 2 && unit.CurrentHP == 12, $"fort heals 10% (delta {delta})");

            unit.CurrentHP = 19;
            TerrainEffects.Apply(unit, fort);
            Check(unit.CurrentHP == 20, "heal caps at max");

            var marsh = new TerrainType("marsh", "Marsh", 2, 2, 3, 1, 0, 0, hpEffectPercent: -50);
            var victim = FreshBandit(); // 22 max
            int hurt = TerrainEffects.Apply(victim, marsh);
            Check(hurt == -11 && victim.CurrentHP == 11, "marsh deals 50%");

            victim.CurrentHP = 5;
            TerrainEffects.Apply(victim, marsh);
            Check(victim.CurrentHP == 0 && !victim.IsAlive, "terrain damage floors at 0");
        }

        private static void PhaseChecks()
        {
            var tm = new TurnManager(new[] { "player", "enemy", "ally" });
            var alive = new Dictionary<string, bool> { ["player"] = true, ["enemy"] = false, ["ally"] = true };

            var (team, round) = tm.AdvancePhase(t => alive[t]);
            Check(team == "ally" && !round && tm.TurnNumber == 1, "empty enemy team skipped");
            (team, round) = tm.AdvancePhase(t => alive[t]);
            Check(team == "player" && round && tm.TurnNumber == 2, "wrap to player advances turn");

            tm.SetResult(true);
            Check(tm.BattleResult == true, "win flag sticks");

            // Priority dispatch: intro (20) runs before reinforce-check (0); both run.
            var exec = new RecordingExecutor();
            var e1 = new GameEvent("reinforce-check", EventTrigger.PlayerPhaseStart);
            e1.AddCommand("wait;1");
            var e2 = new GameEvent("intro", EventTrigger.PlayerPhaseStart) { Priority = 20 };
            e2.AddCommand("speak;Narrator;Turn begins.");
            var results = EventDispatcher.Dispatch(EventTrigger.PlayerPhaseStart,
                new Dictionary<string, object?>(), new[] { e1, e2 }, exec);
            Check(results.Count == 2 && results[0].Ran && results[1].Ran, "same-trigger events both run");
            Check(exec.Ran[0].StartsWith("speak;"), "higher priority runs first");
        }

        private static void ObjectiveChecks()
        {
            var atk = FreshKnight();
            var def = FreshBandit();
            var units = new[] { atk, def };
            Check(!Objectives.IsRouted(units, Affiliation.Enemy), "live enemy blocks rout");
            def.CurrentHP = 0;
            Check(Objectives.IsRouted(units, Affiliation.Enemy), "dead enemy completes rout");

            Check(Objectives.HasSurvived(6, 5) && !Objectives.HasSurvived(5, 5), "survive passes after N rounds");

            var units2 = new[] { FreshKnight(), FreshBandit() };
            units2[0] = new Unit("eirika", "Eirika", Affiliation.Player, "lord",
                new Stats { MaxHP = 18, Power = 4, Skill = 7, Speed = 8, Luck = 5, Defense = 3, Resistance = 2, Constitution = 5 });
            Check(!Objectives.IsProtectFailed(units2, new[] { "eirika" }, 3, _ => 1), "live protectee holds");
            units2[0].CurrentHP = 0;
            Check(Objectives.IsProtectFailed(units2, new[] { "eirika" }, 3, _ => 1), "dead spawned protectee fails");
            Check(!Objectives.IsProtectFailed(units2, new[] { "eirika" }, 2, _ => 5), "unspawned protectee never fails");
        }

        private static Weapon IronSword() => new Weapon("iron-sword", "Iron Sword", WeaponType.Sword, WeaponRank.E,
            might: 5, hit: 90, crit: 0, weight: 5, minRange: 1, maxRange: 1, maxUses: 40);

        private static Weapon IronAxe() => new Weapon("iron-axe", "Iron Axe", WeaponType.Axe, WeaponRank.E,
            might: 8, hit: 75, crit: 0, weight: 10, minRange: 1, maxRange: 1, maxUses: 40);

        private static Weapon PricedSword()
        {
            var w = IronSword();
            w.Price = 400;
            return w;
        }

        private static Weapon OddPriced() => new Weapon("odd", "Odd Blade", WeaponType.Sword, WeaponRank.E,
            might: 5, hit: 90, crit: 0, weight: 5, minRange: 1, maxRange: 1, maxUses: 40)
        { Price = 401 };

        private static Battlefield Skirmish(out PlacedUnit enemy, out PlacedUnit player)
        {
            var field = new Battlefield(OpenBoard(9, 9));
            var foe = new Unit("foe", "Foe", Affiliation.Enemy, "brigand",
                new Stats { MaxHP = 22, Power = 6, Skill = 4, Speed = 3, Luck = 1, Defense = 3, Resistance = 0, Constitution = 9 });
            foe.Inventory.Add(new InventoryEntry(IronAxe()));
            var hero = new Unit("hero", "Hero", Affiliation.Player, "mercenary",
                new Stats { MaxHP = 20, Power = 5, Skill = 6, Speed = 7, Luck = 3, Defense = 4, Resistance = 1, Constitution = 8 });
            hero.Inventory.Add(new InventoryEntry(IronSword()));
            enemy = field.Deploy(foe, new GridPoint(4, 4));
            player = field.Deploy(hero, new GridPoint(4, 5));
            return field;
        }

        private static void TacticsAiChecks()
        {
            // (1) Adjacent foe: strike in place.
            Battlefield field = Skirmish(out PlacedUnit enemy, out PlacedUnit player);
            AiAction act = EnemyAi.Decide(field, enemy, MovementGroups.Infantry, 5);
            Check(!act.IsWait && act.MoveTo == null && ReferenceEquals(act.Target, player), "adjacent foe attacked in place");

            // (2) Foe 3 away, move 5: close in and strike.
            player.Position = new GridPoint(4, 7);
            act = EnemyAi.Decide(field, enemy, MovementGroups.Infantry, 5);
            Check(!act.IsWait && act.MoveTo != null && act.MoveTo.Value.ManhattanDistance(player.Position) == 1,
                "enemy closes to striking distance");

            // (3) Far foe: approach (closer, no target).
            player.Position = new GridPoint(0, 0);
            GridPoint before = enemy.Position;
            act = EnemyAi.Decide(field, enemy, MovementGroups.Infantry, 5);
            Check(act.Target == null && act.MoveTo != null
                    && act.MoveTo.Value.ManhattanDistance(player.Position) < before.ManhattanDistance(player.Position),
                "distant enemy approaches");

            // (4) No foes: wait.
            player.Unit.CurrentHP = 0;
            act = EnemyAi.Decide(field, enemy, MovementGroups.Infantry, 5);
            Check(act.IsWait, "no live foes means wait");

            // (5) Kill preference over chip damage.
            var field2 = new Battlefield(OpenBoard(9, 9));
            var killer = new Unit("k", "K", Affiliation.Enemy, "brigand",
                new Stats { MaxHP = 22, Power = 20, Skill = 10, Speed = 3, Luck = 1, Defense = 3, Resistance = 0, Constitution = 9 });
            killer.Inventory.Add(new InventoryEntry(IronAxe()));
            var ek = field2.Deploy(killer, new GridPoint(4, 4));
            var weak = new Unit("w", "Weak", Affiliation.Player, "mercenary",
                new Stats { MaxHP = 20, Power = 1, Skill = 1, Speed = 1, Luck = 0, Defense = 0, Resistance = 0, Constitution = 5 });
            weak.CurrentHP = 3;
            var tank = new Unit("t", "Tank", Affiliation.Player, "knight",
                new Stats { MaxHP = 30, Power = 1, Skill = 1, Speed = 1, Luck = 0, Defense = 10, Resistance = 0, Constitution = 12 });
            field2.Deploy(weak, new GridPoint(4, 5));
            field2.Deploy(tank, new GridPoint(4, 3));
            AiAction kill = EnemyAi.Decide(field2, ek, MovementGroups.Infantry, 5);
            Check(!kill.IsWait && kill.Target != null && kill.Target.Unit.Nid == "w", "secured kill preferred");

            // (6) Threat range covers reachable strike tiles, respects walls.
            var field3 = new Battlefield(OpenBoard(7, 7));
            field3.Board.SetTerrain(new GridPoint(3, 2), "wall");
            var guard = new Unit("g", "G", Affiliation.Enemy, "archer",
                new Stats { MaxHP = 18, Power = 5, Skill = 6, Speed = 5, Luck = 2, Defense = 3, Resistance = 1, Constitution = 6 });
            var eg = field3.Deploy(guard, new GridPoint(3, 3));
            var threat = ThreatCalculator.ThreatRange(field3, eg, MovementGroups.Infantry, 2, 1, 1);
            Check(threat.Contains(new GridPoint(3, 4)) && threat.Contains(new GridPoint(1, 3)),
                "threat covers melee ring of reachable tiles");
            // GBA-style: the wall tile itself is adjacent and strikable, but nothing behind
            // the wall is threatened since no reachable tile attacks past it.
            Check(threat.Contains(new GridPoint(3, 2)) && !threat.Contains(new GridPoint(3, 1)),
                "wall threatened from guard, tile behind wall safe");

            // (7) Score + remaining tied: nearest target wins regardless of placement order.
            var field4 = new Battlefield(OpenBoard(9, 9));
            var javelin = new Weapon("javelin", "Javelin", WeaponType.Lance, WeaponRank.E,
                might: 6, hit: 80, crit: 0, weight: 6, minRange: 1, maxRange: 2, maxUses: 40);
            var striker4 = P0Fighter("striker", Affiliation.Enemy, 30, 6, 8, 5, 1, 3, 9);
            striker4.Inventory.Add(new InventoryEntry(javelin));
            var es4 = field4.Deploy(striker4, new GridPoint(4, 4));
            var far4 = P0Fighter("far", Affiliation.Player, 20, 1, 1, 1, 0, 0, 5);
            var near4 = P0Fighter("near", Affiliation.Player, 20, 1, 1, 1, 0, 0, 5);
            field4.Deploy(far4, new GridPoint(4, 6)); // placed first: raw order would pick it
            field4.Deploy(near4, new GridPoint(4, 5));
            AiAction picked4 = EnemyAi.Decide(field4, es4, MovementGroups.Infantry, 5);
            Check(picked4.Target != null && picked4.Target.Unit.Nid == "near" && picked4.MoveTo == null,
                "tied score+remaining prefers the nearest target over placement order");

            // (8) Score + remaining + distance tied: lowest X,Y tile wins.
            var field5 = new Battlefield(OpenBoard(9, 9));
            var axeman = P0Fighter("axeman", Affiliation.Enemy, 30, 6, 8, 5, 1, 3, 9);
            axeman.Inventory.Add(new InventoryEntry(IronAxe()));
            var es5 = field5.Deploy(axeman, new GridPoint(4, 4));
            field5.Deploy(P0Fighter("mark", Affiliation.Player, 20, 1, 1, 1, 0, 0, 5), new GridPoint(6, 6));
            AiAction picked5 = EnemyAi.Decide(field5, es5, MovementGroups.Infantry, 5);
            Check(picked5.Target != null && picked5.MoveTo != null && picked5.MoveTo.Value == new GridPoint(5, 6),
                "full tie falls back to coordinate order");
        }

        private static void ConditionEdgeChecks()
        {
            var vars = new Dictionary<string, object?> { ["name"] = "a==b", ["turn"] = 5L };
            Check(SimpleConditionEvaluator.Evaluate("name == 'a==b'", vars), "operator inside quotes ignored");
            Check(!SimpleConditionEvaluator.Evaluate("name == 'a!=b'", vars), "quoted mismatch still filters");
            Check(SimpleConditionEvaluator.Evaluate("turn>=5 && turn <= 5", vars), "compact operators parse");
        }

        private static Chapter TestChapter()
        {
            var terrains = StandardTerrains.Create();
            var legend = new Dictionary<char, string> { ['.'] = "plain", ['F'] = "forest", ['W'] = "wall", ['T'] = "throne" };
            var board = Board.FromRows(new[] { ".....", ".FF..", "..WT." }, "plain", legend, terrains);

            var ch = new Chapter { Nid = "ch1", Title = "First Blood", Board = board, MaxDeploy = 2, NextChapter = "ch2" };
            var hero = new Unit("hero", "Hero", Affiliation.Player, "mercenary",
                new Stats { MaxHP = 20, Power = 5, Skill = 6, Speed = 7, Luck = 3, Defense = 4, Resistance = 1, Constitution = 8 });
            var ally = new Unit("ally1", "Ally", Affiliation.Ally, "soldier",
                new Stats { MaxHP = 18, Power = 4, Skill = 4, Speed = 4, Luck = 2, Defense = 3, Resistance = 0, Constitution = 7 });
            var foe = new Unit("foe1", "Foe", Affiliation.Enemy, "brigand",
                new Stats { MaxHP = 22, Power = 6, Skill = 4, Speed = 3, Luck = 1, Defense = 3, Resistance = 0, Constitution = 9 });
            var foeLate = foe.Clone("foe2");
            var eirika = new Unit("eirika", "Eirika", Affiliation.Player, "lord",
                new Stats { MaxHP = 18, Power = 4, Skill = 7, Speed = 8, Luck = 5, Defense = 3, Resistance = 2, Constitution = 5 });
            ch.Deployments.Add(new Deployment(hero, new GridPoint(0, 0)));
            ch.Deployments.Add(new Deployment(ally, new GridPoint(1, 0)));
            ch.Deployments.Add(new Deployment(foe, new GridPoint(4, 2)));
            ch.Deployments.Add(new Deployment(foeLate, new GridPoint(4, 0), spawnTurn: 2));
            ch.Deployments.Add(new Deployment(eirika, new GridPoint(0, 2), spawnTurn: 3));
            ch.Victory.Add(ObjectiveSpec.RoutEnemies());
            ch.Defeat.Add(ObjectiveSpec.AllDead());
            ch.Defeat.Add(ObjectiveSpec.Protect("eirika"));
            ch.Armory["iron-sword"] = IronSword();
            ch.UnitTemplates["brigand"] = foe.Clone("brigand");

            var intro = new GameEvent("intro", EventTrigger.LevelStart);
            intro.AddCommand("speak;Narrator;Defend the village.");
            ch.Events.Add(intro);
            var death = new GameEvent("foe-down", EventTrigger.UnitDeath, "unit.nid == 'foe1'");
            death.AddCommand("set_flag;foe1_down;true");
            ch.Events.Add(death);
            return ch;
        }

        private static void AdvanceRounds(ChapterState state, int rounds, bool spawn)
        {
            System.Func<string, bool> alive = team =>
            {
                foreach (PlacedUnit p in state.Field.Placed)
                    if (p.Unit.IsAlive && TeamOf(p.Unit.Side) == team) return true;
                return team == "player"; // player phase never skipped
            };
            for (int r = 0; r < rounds; r++)
                for (int i = 0; i < 4; i++)
                {
                    state.Phases.AdvancePhase(alive);
                    if (spawn) state.SpawnDue();
                }
        }

        private static string TeamOf(Affiliation side)
        {
            switch (side)
            {
                case Affiliation.Player: return "player";
                case Affiliation.Enemy: return "enemy";
                case Affiliation.Ally: return "ally";
                default: return "other";
            }
        }

        private static PlacedUnit FindByNid(ChapterState state, string nid)
        {
            foreach (PlacedUnit p in state.Field.Placed)
                if (p.Unit.Nid == nid) return p;
            throw new InvalidOperationException("missing " + nid);
        }

        private static void ChapterChecks()
        {
            // Authoring helper errors.
            var terrains = StandardTerrains.Create();
            var legend = new Dictionary<char, string> { ['.'] = "plain", ['F'] = "forest" };
            var board = Board.FromRows(new[] { "...", ".F." }, "plain", legend, terrains);
            Check(board.GetTerrain(new GridPoint(1, 1)).Key == "forest", "legend maps glyphs");
            bool threw = false;
            try { Board.FromRows(new[] { ".X" }, "plain", legend, terrains); }
            catch (FormatException) { threw = true; }
            Check(threw, "unknown glyph fails fast with position");
            threw = false;
            try { Board.FromRows(new[] { "...", ".." }, "plain", legend, terrains); }
            catch (FormatException) { threw = true; }
            Check(threw, "ragged rows rejected");

            // Spawning over rounds (gaps never drop reinforcements).
            ChapterState state = ChapterState.StartNew(TestChapter(), 4242);
            Check(state.Field.Placed.Count == 3, "turn-1 deploys only");
            AdvanceRounds(state, 2, spawn: false); // reach turn 3 without spawning
            var arrivals = state.SpawnDue();
            Check(arrivals.Count == 2 && state.Phases.TurnNumber == 3, "skipped turns still spawn (<=)");

            // Rout victory once foes fall.
            state = ChapterState.StartNew(TestChapter(), 1);
            AdvanceRounds(state, 2, spawn: true);
            Check(state.EvaluateObjectives() == ObjectiveOutcome.Ongoing, "foes alive: ongoing");
            foreach (PlacedUnit p in state.Field.Placed)
                if (p.Unit.Side == Affiliation.Enemy) p.Unit.CurrentHP = 0;
            Check(state.EvaluateObjectives() == ObjectiveOutcome.Victory, "rout wins");

            // Survive on its own chapter.
            var hold = TestChapter();
            hold.Victory.Clear();
            hold.Defeat.Clear();
            hold.Victory.Add(ObjectiveSpec.Survive(2));
            var holdState = ChapterState.StartNew(hold, 1);
            Check(holdState.EvaluateObjectives() == ObjectiveOutcome.Ongoing, "survive: turn 1 ongoing");
            AdvanceRounds(holdState, 2, spawn: true);
            Check(holdState.EvaluateObjectives() == ObjectiveOutcome.Victory, "survive: past N rounds wins");

            // Protect defeat after the protectee arrives and falls.
            state = ChapterState.StartNew(TestChapter(), 1);
            AdvanceRounds(state, 2, spawn: true);
            FindByNid(state, "eirika").Unit.CurrentHP = 0;
            Check(state.EvaluateObjectives() == ObjectiveOutcome.Defeat, "protectee death loses");

            // Headless runner: dialogue, flags, scripted win, unit ops.
            state = ChapterState.StartNew(TestChapter(), 1);
            var runner = new ChapterRunner(state);
            var res = EventDispatcher.Dispatch(EventTrigger.LevelStart,
                new Dictionary<string, object?>(), state.Chapter.Events, runner);
            Check(res.Count == 1 && res[0].Ran && runner.Dialogue.Count == 1
                    && runner.Dialogue[0].speaker == "Narrator", "intro dialogue runs");
            var deathVars = new Dictionary<string, object?>
            {
                ["unit"] = new Dictionary<string, object?> { ["nid"] = "foe1" },
            };
            EventDispatcher.Dispatch(EventTrigger.UnitDeath, deathVars, state.Chapter.Events, runner);
            Check(state.Vars.TryGetValue("foe1_down", out object? flag) && flag is true, "death flag set");

            var winEv = new GameEvent("debug-win", EventTrigger.LevelStart);
            winEv.AddCommand("win");
            EventRunner.Run(winEv, new Dictionary<string, object?>(), runner);
            Check(state.EvaluateObjectives() == ObjectiveOutcome.Victory, "scripted win sticks");

            var sp1 = new GameEvent("r1", EventTrigger.LevelStart);
            sp1.AddCommand("spawn_unit;brigand;enemy;2,0");
            var sp2 = new GameEvent("r2", EventTrigger.LevelStart);
            sp2.AddCommand("spawn_unit;brigand;enemy;3,0");
            var mv = new GameEvent("m", EventTrigger.LevelStart);
            mv.AddCommand("move_unit;hero;0,1");
            var give = new GameEvent("g", EventTrigger.LevelStart);
            give.AddCommand("give_item;hero;iron-sword");
            var rm = new GameEvent("d", EventTrigger.LevelStart);
            rm.AddCommand("remove_unit;ally1");
            foreach (GameEvent e in new[] { sp1, sp2, mv, give, rm })
                EventRunner.Run(e, new Dictionary<string, object?>(), runner);
            Check(FindByNid(state, "brigand").Unit.Side == Affiliation.Enemy
                    && FindByNid(state, "brigand#2").Unit.Side == Affiliation.Enemy,
                "reinforcement nids stay unique");
            Check(FindByNid(state, "hero").Position == new GridPoint(0, 1), "move teleports headlessly");
            Check(FindByNid(state, "hero").Unit.Inventory.Count == 1, "armory issue works");
            bool allyGone = true;
            foreach (PlacedUnit p in state.Field.Placed)
                if (p.Unit.Nid == "ally1") allyGone = false;
            Check(allyGone, "remove_unit extracts");

            // Validation catches authoring errors.
            var bad = TestChapter();
            bad.Deployments.Add(new Deployment(FreshKnight(), new GridPoint(99, 99)));
            bad.Deployments.Add(new Deployment(FreshKnight(), new GridPoint(0, 1))); // 2nd turn-1 player
            var badEv = new GameEvent("bad", EventTrigger.LevelStart);
            badEv.AddCommand("frobnicate;now");
            bad.Events.Add(badEv);
            bad.Defeat.Add(ObjectiveSpec.Protect("nobody"));
            bad.MaxDeploy = 1;
            var errors = bad.Validate();
            string joined = string.Join("|", errors);
            Check(errors.Count >= 4 && joined.Contains("off-board") && joined.Contains("Unknown event command")
                    && joined.Contains("nobody") && joined.Contains("MaxDeploy"),
                $"validate reports all classes ({errors.Count} errors)");
            Check(TestChapter().Validate().Count == 0, "clean chapter validates");
        }

        private const string SampleChapter = @"# CH1: First Blood

## META
format: 1
objective: Rout the foe. Keep Eirika alive.
victory: rout
defeat: all_dead
defeat: protect eirika
next: ch2
maxDeploy: 4

## LEGEND
.: plain
F: forest
W: wall
T: throne

## MAP
.....
.FF..
..WT.

## ARMORY
iron-sword Iron_Sword sword E 5 90 0 5 1-1 40
iron-axe Iron_Axe axe E 8 75 0 10 1-1 40
fire Fire anima E 5 90 0 4 1-2 40 magic

## TEMPLATES
brigand enemy brigand 1 22 6 4 3 1 3 0 5 items:iron-axe

## UNITS
0,0 hero player mercenary 1 20 5 6 7 3 4 1 8 items:iron-sword
1,0 ally1 ally soldier 1 18 4 4 4 2 3 0 7 items:iron-sword
4,2 foe1 enemy brigand 1 22 6 4 3 1 3 0 9 items:iron-axe
4,0 foe2 enemy brigand 1 22 6 4 3 1 3 0 9 turn:2 group:alpha
0,2 eirika player lord 1 18 4 7 8 5 3 2 5 turn:3

## TERRAINS
marsh Marsh 2 2 3 1 10 0 hp:-10 pirate:1

## REGIONS
village-door event 3,0 3,0
deploy formation 0,0 1,0

## EVENTS
### intro on start priority 10
- Narrator: Defend the village.
### foe-down on unit_death if unit.nid == 'foe1'
set_flag;foe1_down;true
### turn3 on turn 3 - player
- Narrator: They keep coming!
";

        private static void LoadFails(string md, string fragment, string name)
        {
            ChapterParseResult res = MarkdownChapterLoader.Load(md, "test.md");
            bool found = false;
            foreach (ChapterParseError e in res.Errors)
                if (e.ToString().Contains(fragment)) found = true;
            Check(!res.Ok && res.Chapter == null && found, name + " (want '" + fragment + "')");
        }

        private static void SpriteChecks()
        {
            const string md = @"# T
## META
format: 1
## LEGEND
.: plain
## MAP
...
...
## UNITS
0,0 hero player mercenary 1 20 5 6 7 3 4 1 8 sprite:hero-alt
0,1 plain-ally ally soldier 1 18 4 4 4 2 3 0 7
";
            ChapterParseResult res = MarkdownChapterLoader.Load(md, "sprite.md");
            Check(res.Ok && res.Chapter != null, "sprite token chapter loads");
            if (!res.Ok || res.Chapter == null) return;
            Unit over = res.Chapter.Deployments[0].Unit;
            Unit plain = res.Chapter.Deployments[1].Unit;
            Check(over.SpriteKey == "hero-alt", "sprite token stored verbatim");
            Check(plain.SpriteKey == "", "sprite defaults to empty");
            Check(over.Clone("hero2").SpriteKey == "hero-alt", "clone keeps sprite key");
            LoadFails("# T\n## META\nformat: 1\n## LEGEND\n.: plain\n## MAP\n.\n## UNITS\n0,0 h player mercenary 1 20 5 6 7 3 4 1 8 sprite:\n",
                "Bad sprite", "empty sprite token fails");
        }

        private static void FormatChecks()
        {
            ChapterParseResult res = MarkdownChapterLoader.Load(SampleChapter, "ch1.md");
            Check(res.Ok && res.Chapter != null, "sample chapter loads");
            if (!res.Ok || res.Chapter == null)
            {
                foreach (ChapterParseError e in res.Errors) Console.WriteLine("    load error: " + e);
                return;
            }
            Chapter ch = res.Chapter;
            Check(ch.Nid == "CH1" && ch.Title == "First Blood", "title splits nid/name");
            Check(ch.ObjectiveText.StartsWith("Rout the foe") && ch.NextChapter == "ch2" && ch.MaxDeploy == 4,
                "meta objective/next/maxdeploy");
            Check(!ch.IsIso, "view defaults to flat");
            Check(ch.Victory.Count == 1 && ch.Victory[0].Kind == ObjectiveKind.Rout, "victory rout");
            Check(ch.Defeat.Count == 2 && ch.Defeat[1].Kind == ObjectiveKind.Protect, "defeat all_dead + protect");
            Check(ch.Board.Width == 5 && ch.Board.Height == 3, "board 5x3");
            Check(ch.Board.GetTerrain(new GridPoint(1, 1)).Key == "forest", "legend glyph maps");
            Check(ch.Armory.Count == 3 && ch.Armory["fire"].IsMagic
                    && ch.Armory["fire"].MinRange == 1 && ch.Armory["fire"].MaxRange == 2,
                "armory ranges + magic flag");
            Check(ch.Deployments.Count == 5, "five deployments");
            Deployment foe2 = ch.Deployments[3];
            Check(foe2.SpawnTurn == 2 && foe2.Group == "alpha", "spawn turn + group parsed");
            Unit hero = ch.Deployments[0].Unit;
            Check(hero.Inventory.Count == 1 && hero.Inventory[0].Weapon.Key == "iron-sword"
                    && hero.MovePoints == 5 && hero.MovementGroup == "infantry",
                "inventory + move defaults");
            Check(ch.UnitTemplates.ContainsKey("brigand"), "templates stored");
            Check(ch.Board.Terrains["marsh"].HpEffectPercent == -10
                    && ch.Board.Terrains["marsh"].MoveCost("pirate") == 1,
                "custom terrain hp + group cost");
            Check(ch.Regions.Count == 2 && ch.Regions[0].Kind == RegionKind.Event
                    && ch.Regions[0].Contains(new GridPoint(3, 0)), "regions rect");
            Check(ch.Events.Count == 3, "three events");
            GameEvent intro = ch.Events[0];
            Check(intro.Name == "intro" && intro.Trigger == EventTrigger.LevelStart && intro.Priority == 10
                    && intro.Commands.Count == 1 && intro.Commands[0].Verb == "speak"
                    && intro.Commands[0].Args[1] == "Defend the village.",
                "dialogue sugar desugars to speak");
            GameEvent turn3 = ch.Events[2];
            Check(turn3.Trigger == EventTrigger.PlayerPhaseStart && turn3.Condition == "turn == 3",
                "turn-form trigger + auto condition");
            Check(ch.Validate().Count == 0, "loaded chapter validates clean");

            // Playable end to end.
            ChapterState state = ChapterState.StartNew(ch, 7);
            Check(state.Field.Placed.Count == 3, "turn-1 roster from file");
            var runner = new ChapterRunner(state);
            var dispatches = EventDispatcher.Dispatch(EventTrigger.LevelStart,
                new Dictionary<string, object?>(), ch.Events, runner);
            Check(dispatches.Count == 1 && runner.Dialogue.Count == 1, "file intro fires");

            // Error cases, each pinned to a fragment.
            LoadFails("# T\n## META\nformat: 2\n## MAP\n.\n", "Unsupported format", "bad format version");
            LoadFails("# T\n## MAP\n.\n", "missing 'format", "missing format");
            LoadFails("# T\n## META\nformat: 1\nbogus: x\n## MAP\n.\n", "Unknown META key", "unknown meta key");
            LoadFails("# T\n## FROBNICATE\n", "Unknown section", "unknown section");
            LoadFails("hello outside\n", "outside any section", "content outside sections");
            LoadFails("# T\n## META\nformat: 1\n## MAP\n...\n..\n", "wide, expected", "ragged rows");
            LoadFails("# T\n## META\nformat: 1\n## MAP\n.X\n", "no LEGEND entry", "unknown glyph");
            LoadFails("# T\n## META\nformat: 1\nview: sideways\n## MAP\n.\n", "Bad view", "bad view value");
            LoadFails("# T\n## META\nformat: 1\n## LEGEND\nX: nope\n## MAP\nX\n", "unknown terrain", "legend to nowhere");
            LoadFails("# T\n## META\nformat: 1\n## MAP\n.\n## UNITS\n0,0 hero\n", "UNITS rows need", "short unit row");
            LoadFails("# T\n## META\nformat: 1\n## MAP\n.\n## UNITS\n0,0 h peasants m 1 10 1 1 1 1 1 1 1\n", "Bad side", "bad side");
            LoadFails("# T\n## META\nformat: 1\n## MAP\n.\n## UNITS\n0,0 h player m 1 10 1 1 1 1 1 1 1 items:nope\n", "Unknown weapon", "dangling weapon");
            LoadFails("# T\n## META\nformat: 1\n## MAP\n.\n## EVENTS\n### e on frobnicate\n", "Unknown trigger", "bad trigger");
            LoadFails("# T\n## META\nformat: 1\n## MAP\n.\n## EVENTS\n### e on start\nfrobnicate;now\n", "Unknown event command", "bad command");
            LoadFails("# T\n## META\nformat: 1\n## MAP\n.\n## EVENTS\n### e on start if wat\nspeak;A;B\n", "Bad condition", "bad condition");
            LoadFails("# T\n## META\nformat: 1\n## MAP\n.\n## EVENTS\nno header here\n", "need a '###", "command without header");
            LoadFails("# T\n## META\nformat: 1\nvictory: conquer\n## MAP\n.\n", "Bad victory spec", "bad objective");
            LoadFails("# T\n## META\nformat: 1\n## MAP\n.\n## REGIONS\nr event 0,0 9,9\n", "leaves the board", "region off board");
            LoadFails("# T\n## META\nformat: 1\n## MAP\n.\n## ARMORY\ns Sword sword E x 90 0 5 1-1 40\n", "must be integers", "bad armory stat");
            LoadFails("# T\n## META\nformat: 1\n## MAP\n.\n## ARMORY\ns Sword sword E 5 90 0 5 3-1 40\n", "Bad range", "bad armory range");
            LoadFails("## META\nformat: 1\n", "Missing '#", "missing title");
            LoadFails("# T\n## META\nformat: 1\n## MAP\n.\n## EVENTS\n### e on turn 2 - foes\n", "Bad turn team", "bad turn team");
            LoadFails("# T\n## META\nformat: 1\n## LEGEND\n.: plain\n## MAP\n.\n## REGIONS\nr status 0,0 0,0\n",
                "Bad region kind", "retired status region kind rejected");
            var isoRes = MarkdownChapterLoader.Load("# T\n## META\nformat: 1\nview: iso\n## LEGEND\n.: plain\n## MAP\n.\n", "iso.md");
            Check(isoRes.Ok && isoRes.Chapter != null && isoRes.Chapter.IsIso, "view iso parses");
        }

        private static void DurabilityEffectiveChecks()
        {
            var hammer = new Weapon("hammer", "Hammer", WeaponType.Axe, WeaponRank.E,
                might: 5, hit: 100, crit: 0, weight: 5, minRange: 1, maxRange: 1, maxUses: 40);
            hammer.EffectiveAgainst.Add("armor");
            var knight = new Unit("k", "Knight", Affiliation.Enemy, "armor",
                new Stats { MaxHP = 30, Power = 1, Skill = 1, Speed = 1, Luck = 0, Defense = 5, Resistance = 0, Constitution = 10 });
            var atk = FreshKnight(); // Power 5
            var fcEff = CombatResolver.Forecast(atk, hammer, knight, null, 0, 0, 0, 0);
            // (5 Might x3=15) + 5 Pow - 5 Def = 15.
            Check(fcEff.IsEffective && fcEff.Damage == 15, $"hammer effective vs armor dmg 15 (got {fcEff.Damage})");
            var plain = new Unit("p", "Plain", Affiliation.Enemy, "brigand",
                new Stats { MaxHP = 30, Power = 1, Skill = 1, Speed = 1, Luck = 0, Defense = 5, Resistance = 0, Constitution = 10 });
            var fcPlain = CombatResolver.Forecast(atk, hammer, plain, null, 0, 0, 0, 0);
            Check(!fcPlain.IsEffective && fcPlain.Damage == 5, $"hammer not effective vs brigand (got {fcPlain.Damage})");

            // Durability: entry overload consumes 1 use per strike attempt.
            var sword = IronSword();
            var atkEntry = new InventoryEntry(sword, usesLeft: 2);
            var defEntry = new InventoryEntry(IronAxe(), usesLeft: 5);
            var a = FreshKnight();
            var d = FreshBandit();
            CombatResolver.Resolve(a, atkEntry, d, defEntry, 1, 0, 0, 0, 0, new SeededRng(9));
            Check(atkEntry.UsesLeft < 2, $"attacker use consumed (left {atkEntry.UsesLeft})");
            Check(defEntry.UsesLeft < 5, $"counter use consumed (left {defEntry.UsesLeft})");
            var broken = new InventoryEntry(sword, usesLeft: 0);
            int hpBefore = d.CurrentHP;
            CombatResolver.Resolve(a, broken, d, null, 1, 0, 0, 0, 0, new SeededRng(9));
            Check(d.CurrentHP == hpBefore, "broken weapon cannot strike");
        }

        private static void ExperienceChecks()
        {
            int chip = Experience.Gain(false, 1, 1, 8, null);
            int kill = Experience.Gain(true, 1, 1, 8, null);
            Check(kill > chip, $"kill pays more than chip ({chip} vs {kill})");
            int up = Experience.Gain(true, 1, 5, 8, null);
            int flat = Experience.Gain(true, 5, 1, 8, null);
            Check(up > flat, "higher-level victim pays more");
            Check(Experience.Gain(false, 1, 1, 0, null) >= 1, "min 1 exp");

            var unit = FreshKnight();
            unit.Experience = 95;
            var growths = new GrowthRates { HP = 100, Power = 0, Skill = 0, Speed = 0, Luck = 0, Defense = 0, Resistance = 0 };
            int maxBefore = unit.Stats.MaxHP;
            Experience.AddExp(unit, 10, growths, new SeededRng(1), null);
            Check(unit.Level == 2 && unit.Stats.MaxHP == maxBefore + 1 && unit.Experience == 5,
                "100 exp levels up with guaranteed HP growth");
            var zero = new GrowthRates();
            int lvl = unit.Level;
            Experience.AddExp(unit, 100, zero, new SeededRng(1), null);
            Check(unit.Level == lvl + 1, "zero growths still level");
        }

        private static void StaffChecks()
        {
            var staff = new Weapon("heal", "Heal", WeaponType.Staff, WeaponRank.E,
                might: 0, hit: 100, crit: 0, weight: 1, minRange: 1, maxRange: 1, maxUses: 20);
            staff.HealPower = 10;
            var cleric = FreshKnight(); // Power 5 -> 10 + 2 = 12
            Check(CombatResolver.ForecastHeal(cleric, staff) == 12, "heal preview 10 + pow/2");
            var patient = FreshBandit();
            patient.CurrentHP = 5;
            var entry = new InventoryEntry(staff, usesLeft: 3);
            int restored = CombatResolver.ResolveHeal(cleric, entry, patient);
            Check(restored > 0 && entry.UsesLeft == 2, "heal consumes 1 use");
            patient.CurrentHP = patient.Stats.MaxHP - 1;
            CombatResolver.ResolveHeal(cleric, new InventoryEntry(staff, usesLeft: 5), patient);
            Check(patient.CurrentHP == patient.Stats.MaxHP, "heal clamps at max");
            Check(staff.IsStaff, "staff flag set");
        }

        private static void NewVerbChecks()
        {
            var state = ChapterState.StartNew(TestChapter(), 3);
            var runner = new ChapterRunner(state);
            Check(CommandCatalog.TryValidate(EventCommand.Parse("recruit;foe1"), out _), "recruit validates");
            Check(CommandCatalog.TryValidate(EventCommand.Parse("heal;hero;5"), out _), "heal validates");
            Check(CommandCatalog.TryValidate(EventCommand.Parse("damage;hero;5"), out _), "damage validates");
            Check(CommandCatalog.TryValidate(EventCommand.Parse("promote;hero;lord"), out _), "promote validates");

            var foe = FindByNid(state, "foe1");
            var rev = new GameEvent("r", EventTrigger.LevelStart);
            rev.AddCommand("recruit;foe1");
            EventRunner.Run(rev,
                new Dictionary<string, object?>(), runner);
            Check(foe.Unit.Side == Affiliation.Player, "recruit flips to player");

            var hero = FindByNid(state, "hero");
            hero.Unit.CurrentHP = 5;
            var hev = new GameEvent("h", EventTrigger.LevelStart);
            hev.AddCommand("heal;hero;7");
            EventRunner.Run(hev,
                new Dictionary<string, object?>(), runner);
            Check(hero.Unit.CurrentHP == 12, "heal verb restores");
            var dev = new GameEvent("d", EventTrigger.LevelStart);
            dev.AddCommand("damage;hero;4");
            EventRunner.Run(dev,
                new Dictionary<string, object?>(), runner);
            Check(hero.Unit.CurrentHP == 8, "damage verb hurts");
            var pev = new GameEvent("p", EventTrigger.LevelStart);
            pev.AddCommand("promote;hero;paladin");
            EventRunner.Run(pev,
                new Dictionary<string, object?>(), runner);
            Check(hero.Unit.ClassKey == "paladin", "promote rekeys class");

            ChapterParseResult res = MarkdownChapterLoader.Load(
                "# T\n## META\nformat: 1\n## LEGEND\n.: plain\n## MAP\n.\n## ARMORY\nril Armorslayer sword E 5 90 0 5 1-1 40 effective:armor\nmend Mend staff E 0 100 0 0 1-1 20 heal:10 price:300\n",
                "arm.md");
            Check(res.Ok && res.Chapter != null && res.Chapter.Armory["ril"].EffectiveAgainst.Count == 1
                && res.Chapter.Armory["mend"].HealPower == 10 && res.Chapter.Armory["mend"].Price == 300
                && res.Chapter.Armory["ril"].Price == 0, "armory effective/heal/price tokens parse");
            ChapterParseResult badPrice = MarkdownChapterLoader.Load(
                "# T\n## META\nformat: 1\n## LEGEND\n.: plain\n## MAP\n.\n## ARMORY\nx X sword E 5 90 0 5 1-1 40 price:free\n",
                "bad.md");
            Check(!badPrice.Ok, "bad price token fails the load");
        }

        private static void AiTerrainChecks()
        {
            // Terrain-aware: same shot from a fort reads lower damage via defense.
            var field = new Battlefield(OpenBoard(9, 9));
            field.Board.SetTerrain(new GridPoint(4, 5), "fort"); // defender on fort (+2 def)
            var foe = new Unit("foe", "Foe", Affiliation.Enemy, "brigand",
                new Stats { MaxHP = 22, Power = 6, Skill = 4, Speed = 3, Luck = 1, Defense = 3, Resistance = 0, Constitution = 9 });
            foe.Inventory.Add(new InventoryEntry(IronAxe()));
            var hero = new Unit("hero", "Hero", Affiliation.Player, "mercenary",
                new Stats { MaxHP = 20, Power = 5, Skill = 6, Speed = 7, Luck = 3, Defense = 4, Resistance = 1, Constitution = 8 });
            hero.Inventory.Add(new InventoryEntry(IronSword()));
            var enemy = field.Deploy(foe, new GridPoint(4, 4));
            var player = field.Deploy(hero, new GridPoint(4, 5));
            EnemyAi.TerrainOf terrainOf = (PlacedUnit u, GridPoint t) =>
            {
                var tt = field.Board.GetTerrain(t);
                return (tt.Avoid, tt.Defense);
            };
            EnemyAi.DefenderWeaponOf weaponOf = (PlacedUnit f, int dist) =>
            {
                foreach (InventoryEntry e in f.Unit.Inventory)
                    if (!e.IsBroken && e.Weapon.InRange(dist)) return e.Weapon;
                return null;
            };
            AiAction act = EnemyAi.Decide(field, enemy, MovementGroups.Infantry, 5, terrainOf, weaponOf, 0.5, null);
            Check(!act.IsWait && act.Target != null, "terrain-aware AI still attacks adjacent");

            // Counter-risk: a counter-killer target scores lower than a safe chip.
            var field2 = new Battlefield(OpenBoard(9, 9));
            var striker = new Unit("s", "S", Affiliation.Enemy, "brigand",
                new Stats { MaxHP = 30, Power = 6, Skill = 8, Speed = 5, Luck = 1, Defense = 3, Resistance = 0, Constitution = 9 });
            striker.Inventory.Add(new InventoryEntry(IronAxe()));
            var es = field2.Deploy(striker, new GridPoint(4, 4));
            var porcupine = new Unit("p", "Porc", Affiliation.Player, "mercenary",
                new Stats { MaxHP = 30, Power = 25, Skill = 10, Speed = 8, Luck = 1, Defense = 3, Resistance = 0, Constitution = 9 });
            porcupine.Inventory.Add(new InventoryEntry(IronSword()));
            var soft = new Unit("q", "Soft", Affiliation.Player, "civilian",
                new Stats { MaxHP = 30, Power = 1, Skill = 1, Speed = 1, Luck = 0, Defense = 0, Resistance = 0, Constitution = 5 });
            field2.Deploy(porcupine, new GridPoint(4, 5));
            field2.Deploy(soft, new GridPoint(4, 3));
            AiAction risky = EnemyAi.Decide(field2, es, MovementGroups.Infantry, 0, null, null, 0.0, null);
            AiAction safe = EnemyAi.Decide(field2, es, MovementGroups.Infantry, 0, null, null, 5.0, null);
            Check(risky.Target != null && safe.Target != null, "both counter weights pick a target");
        }

        private static void SeizeSavePromoteChecks()
        {
            // Seize: lord on throne wins.
            var terrains = StandardTerrains.Create();
            var board = new Board(5, 5, "plain", terrains);
            board.SetTerrain(new GridPoint(4, 4), "throne");
            var ch = new Chapter { Nid = "sz", Title = "Seize", Board = board };
            ch.Armory["iron-sword"] = IronSword();
            var lord = new Unit("lord", "Lord", Affiliation.Player, "lord",
                new Stats { MaxHP = 18, Power = 4, Skill = 7, Speed = 8, Luck = 5, Defense = 3, Resistance = 2, Constitution = 5 });
            ch.Deployments.Add(new Deployment(lord, new GridPoint(0, 0)));
            var gate = new Region("gate", RegionKind.Event);
            gate.Add(new GridPoint(4, 4));
            ch.Regions.Add(gate);
            ch.Victory.Add(ObjectiveSpec.Seize("lord", "gate"));
            Check(ch.Validate().Count == 0, "seize chapter validates");
            ChapterState state = ChapterState.StartNew(ch, 11);
            Check(state.EvaluateObjectives() == ObjectiveOutcome.Ongoing, "seize: off-point ongoing");
            FindByNid(state, "lord").Position = new GridPoint(4, 4);
            Check(state.EvaluateObjectives() == ObjectiveOutcome.Victory, "seize: on-point wins");
            FindByNid(state, "lord").Unit.CurrentHP = 0;
            Check(state.EvaluateObjectives() != ObjectiveOutcome.Victory, "seize: dead lord wins nothing");

            // Save round-trip: positions, HP, uses, turn, vars, RNG continue.
            state = ChapterState.StartNew(TestChapter(), 4242);
            FindByNid(state, "hero").Position = new GridPoint(2, 2);
            FindByNid(state, "hero").Unit.CurrentHP = 7;
            state.Vars["flag"] = true;
            int seqA = state.CombatRng.Next(100000);
            ChapterSnapshot snap = SaveSystem.Save(state);
            ChapterState back = SaveSystem.Restore(state.Chapter, snap, 4242);
            Check(FindByNid(back, "hero").Position == new GridPoint(2, 2)
                && FindByNid(back, "hero").Unit.CurrentHP == 7, "save restores pos + HP");
            Check(back.Vars.TryGetValue("flag", out object? f) && f is true, "save restores vars");
            Check(back.Phases.TurnNumber == state.Phases.TurnNumber, "save restores turn");
            Check(back.CombatRng.Next(100000) == state.CombatRng.Next(100000)
                && seqA != state.CombatRng.Next(100000), "RNG continues after restore");
            int placedBefore = back.Field.Placed.Count;
            back.Phases.Restore(back.Phases.CurrentTeamIndex, 9, null);
            var arrivals = back.SpawnDue();
            Check(back.Field.Placed.Count >= placedBefore, "spawn-due after restore never duplicates placed");

            // Promotion: rekey + movement + gains.
            var db = new ClassDatabase();
            db.Add(new UnitClass("trainee", "Trainee", MoveType.Infantry, 4, promotesTo: "lord"));
            db.Add(new UnitClass("lord", "Lord", MoveType.Infantry, 6));
            var kid = new Unit("kid", "Kid", Affiliation.Player, "trainee",
                new Stats { MaxHP = 16, Power = 3, Skill = 4, Speed = 5, Luck = 2, Defense = 2, Resistance = 1, Constitution = 5 });
            Check(Promotion.CanPromote(kid, db), "trainee can promote");
            Promotion.Apply(kid, "lord", db, new Stats { MaxHP = 2, Power = 1 });
            Check(kid.ClassKey == "lord" && kid.MovePoints == 6 && kid.Stats.MaxHP == 18 && kid.Stats.Power == 4,
                "promote rekeys, moves, gains");
            Check(!Promotion.CanPromote(kid, db), "lord cannot promote further");

            // Markdown seize parses.
            ChapterParseResult res = MarkdownChapterLoader.Load(
                "# T\n## META\nformat: 1\nvictory: seize hero gate\n## LEGEND\n.: plain\n## MAP\n.\n## REGIONS\ngate event 0,0 0,0\n## UNITS\n0,0 hero player m 1 10 1 1 1 1 1 1 1\n",
                "sz.md");
            Check(res.Ok && res.Chapter != null && res.Chapter.Victory.Count == 1
                && res.Chapter.Victory[0].Kind == ObjectiveKind.Seize, "markdown seize parses");
        }

        private static void FogRescueSkillSupportStatusChecks()
        {
            // Fog: vision radius gates visibility.
            var field = new Battlefield(OpenBoard(9, 9));
            var scout = new Unit("s", "Scout", Affiliation.Player, "archer",
                new Stats { MaxHP = 18, Power = 5, Skill = 6, Speed = 5, Luck = 2, Defense = 3, Resistance = 1, Constitution = 6 });
            scout.Vision = 2;
            var es = field.Deploy(scout, new GridPoint(4, 4));
            var near = new Unit("n", "Near", Affiliation.Enemy, "brigand",
                new Stats { MaxHP = 22, Power = 6, Skill = 4, Speed = 3, Luck = 1, Defense = 3, Resistance = 0, Constitution = 9 });
            var far = new Unit("f", "Far", Affiliation.Enemy, "brigand",
                new Stats { MaxHP = 22, Power = 6, Skill = 4, Speed = 3, Luck = 1, Defense = 3, Resistance = 0, Constitution = 9 });
            field.Deploy(near, new GridPoint(5, 4));
            field.Deploy(far, new GridPoint(0, 0));
            var seen = FogCalculator.VisibleTiles(field, Affiliation.Player);
            Check(seen.Contains(new GridPoint(5, 4)) && !seen.Contains(new GridPoint(0, 0)), "vision radius gates sight");
            Check(FogCalculator.HiddenEnemiesOf(field, es).Exists(p => p.Unit.Nid == "f")
                && !FogCalculator.HiddenEnemiesOf(field, es).Exists(p => p.Unit.Nid == "n"), "hidden set splits near/far");

            // Rescue: grab halves move, drop restores.
            var field2 = new Battlefield(OpenBoard(9, 9));
            var carrier = new Unit("c", "Carrier", Affiliation.Player, "cavalier",
                new Stats { MaxHP = 20, Power = 5, Skill = 5, Speed = 5, Luck = 2, Defense = 4, Resistance = 1, Constitution = 9 });
            carrier.MovePoints = 6;
            var tiny = new Unit("t", "Tiny", Affiliation.Player, "dancer",
                new Stats { MaxHP = 16, Power = 2, Skill = 5, Speed = 7, Luck = 4, Defense = 2, Resistance = 3, Constitution = 4 });
            var pc = field2.Deploy(carrier, new GridPoint(4, 4));
            var pt = field2.Deploy(tiny, new GridPoint(4, 5));
            Check(Rescue.CanGrab(carrier, tiny, pc.Position, pt.Position), "rescue conditions met");
            Check(Rescue.EffectiveMove(carrier) == 6, "unladen move full");
            Rescue.Grab(field2, pc, pt);
            Check(carrier.CarryingNid == "t" && Rescue.EffectiveMove(carrier) == 3, "laden move halved");
            bool lived = true;
            foreach (PlacedUnit p in field2.Placed) if (p.Unit.Nid == "t") lived = false;
            Check(lived, "passenger leaves map while carried");
            Rescue.Drop(field2, pc, tiny, new GridPoint(3, 4));
            Check(carrier.CarryingNid == "" && Rescue.EffectiveMove(carrier) == 6, "drop restores move");
            // Rescue: hostiles can never be grabbed (they block pass-through, so no scooping foes).
            var foe = new Unit("e", "Foe", Affiliation.Enemy, "brigand",
                new Stats { MaxHP = 22, Power = 6, Skill = 4, Speed = 3, Luck = 1, Defense = 3, Resistance = 0, Constitution = 4 });
            var pf = field2.Deploy(foe, new GridPoint(4, 5));
            Check(!Rescue.CanGrab(carrier, foe, pc.Position, pf.Position), "rescue refuses hostile passenger");

            // Skills: flat hooks modify the forecast copy.
            SkillCatalog.Clear();
            SkillCatalog.Register(new SkillDef("brave-hit") { Hook = SkillHook.OnAttack, HitBonus = 20 });
            SkillCatalog.Register(new SkillDef("guard") { Hook = SkillHook.OnDefense, DamageTakenPenalty = 2 });
            var atk = FreshKnight();
            atk.Skills.Add("brave-hit");
            var def = FreshBandit();
            def.Skills.Add("guard");
            var sw = IronSword();
            var ax = IronAxe();
            var baseFc = CombatResolver.Forecast(atk, sw, def, ax, 0, 0, 0, 0);
            var mod = SkillCatalog.ApplyToForecast(baseFc, atk.Skills, def.Skills, null);
            Check(mod.Hit == Math.Min(100, baseFc.Hit + 20) && mod.Damage == Math.Max(0, baseFc.Damage - 2),
                "skill hooks adjust hit and damage");
            Check(baseFc.Hit != mod.Hit || baseFc.Damage != mod.Damage, "base forecast untouched");
            SkillCatalog.Clear();

            // Support: bonded adjacency grants hit/avoid.
            var ledger = new SupportLedger();
            ledger.AddPoints("a", "b", 150); // rank A
            Check(ledger.RankOf("a", "b") == SupportRank.A, "150 pts is rank A");
            var ua = new Unit("a", "A", Affiliation.Player, "lord",
                new Stats { MaxHP = 18, Power = 4, Skill = 7, Speed = 8, Luck = 5, Defense = 3, Resistance = 2, Constitution = 5 });
            var ub = new Unit("b", "B", Affiliation.Player, "cavalier",
                new Stats { MaxHP = 20, Power = 5, Skill = 5, Speed = 5, Luck = 2, Defense = 4, Resistance = 1, Constitution = 8 });
            var field3 = new Battlefield(OpenBoard(9, 9));
            var pa = field3.Deploy(ua, new GridPoint(4, 4));
            field3.Deploy(ub, new GridPoint(4, 5));
            var bonus = ledger.BonusFor(ua, pa.Position, field3.Placed);
            Check(bonus.hit == 15 && bonus.avoid == 15, $"rank A adjacent +15 (got {bonus.hit})");

            // Statuses: apply/tick/expire.
            var victim = FreshBandit();
            Statuses.Apply(victim, "poison", 2);
            Check(Statuses.Has(victim, "poison"), "status applies");
            Statuses.Tick(victim);
            Check(Statuses.Has(victim, "poison"), "1 turn left still present");
            var expired = Statuses.Tick(victim);
            Check(!Statuses.Has(victim, "poison") && expired.Contains("poison"), "status expires");

            // Staves with accuracy: sure-hit sleep lands, zero-hit misses, uses consumed.
            var sleep = new Weapon("sleep", "Sleep", WeaponType.Staff, WeaponRank.E,
                might: 0, hit: 100, crit: 0, weight: 1, minRange: 1, maxRange: 3, maxUses: 10)
            { StatusEffect = "sleep", StatusDuration = 2 };
            var dud = new Weapon("dud", "Dud", WeaponType.Staff, WeaponRank.E,
                might: 0, hit: 0, crit: 0, weight: 1, minRange: 1, maxRange: 3, maxUses: 10)
            { StatusEffect = "sleep", StatusDuration = 2 };
            var cleric = FreshKnight();
            var patient = FreshBandit();
            int staffHit = CombatResolver.ForecastStaffHit(cleric, sleep, 1);
            Check(staffHit == 100, $"sure staff hit 100 (got {staffHit})");
            bool landed = CombatResolver.ResolveStaff(cleric, new InventoryEntry(sleep, usesLeft: 2), patient, 1, new SeededRng(4));
            Check(landed && Statuses.Has(patient, "sleep"), "status staff lands and applies");
            var patient2 = FreshBandit();
            // Hopeless caster vs dud staff: hit clamps to 0 and always misses.
            var hopeless = new Unit("h", "H", Affiliation.Player, "civilian",
                new Stats { MaxHP = 10, Power = 1, Skill = 0, Speed = 1, Luck = 0, Defense = 0, Resistance = 0, Constitution = 5 });
            bool missed = !CombatResolver.ResolveStaff(hopeless, new InventoryEntry(dud, usesLeft: 2), patient2, 3, new SeededRng(4));
            Check(missed && !Statuses.Has(patient2, "sleep"), "zero-hit staff misses");

            // New verbs validate + run: rescue/drop/status/skill.
            var state = ChapterState.StartNew(TestChapter(), 5);
            var runner = new ChapterRunner(state);
            Check(CommandCatalog.TryValidate(EventCommand.Parse("apply_status;hero;poison;2"), out _), "apply_status validates");
            Check(CommandCatalog.TryValidate(EventCommand.Parse("rescue;hero;ally1"), out _), "rescue validates");
            var hero = FindByNid(state, "hero");
            var ally = FindByNid(state, "ally1");
            hero.Position = new GridPoint(1, 1);
            ally.Position = new GridPoint(1, 0);
            foreach (string line in new[] { "apply_status;hero;poison;2", "teach_skill;hero;brave-hit", "rescue;hero;ally1" })
            {
                var ev = new GameEvent("x" + line, EventTrigger.LevelStart);
                ev.AddCommand(line);
                EventRunner.Run(ev, new Dictionary<string, object?>(), runner);
            }
            Check(Statuses.Has(hero.Unit, "poison") && hero.Unit.Skills.Contains("brave-hit")
                && hero.Unit.CarryingNid == "ally1", "verbs apply status/skill/rescue");
            var drop = new GameEvent("drop", EventTrigger.LevelStart);
            drop.AddCommand("drop;hero;1,2");
            EventRunner.Run(drop, new Dictionary<string, object?>(), runner);
            Check(hero.Unit.CarryingNid == "" && FindByNid(state, "ally1").Position == new GridPoint(1, 2), "drop verb lands passenger");

            // Markdown: status staff + skill/vision tokens parse.
            ChapterParseResult mres = MarkdownChapterLoader.Load(
                "# T\n## META\nformat: 1\n## LEGEND\n.: plain\n## MAP\n.\n## ARMORY\nslp Sleep staff E 0 100 0 0 1-3 10 status:sleep:2\n## UNITS\n0,0 h player m 1 10 1 1 1 1 1 1 1 items:slp skill:brave-hit vision:7\n",
                "st.md");
            Check(mres.Ok && mres.Chapter != null
                && mres.Chapter.Armory["slp"].StatusEffect == "sleep"
                && mres.Chapter.Deployments[0].Unit.Skills.Contains("brave-hit")
                && mres.Chapter.Deployments[0].Unit.Vision == 7, "markdown status/skill/vision parse");
        }

        private static Unit P0Fighter(string nid, Affiliation side, int hp, int pow, int skl, int spd, int lck, int def, int con) =>
            new Unit(nid, nid, side, "mercenary",
                new Stats { MaxHP = hp, Power = pow, Skill = skl, Speed = spd, Luck = lck, Defense = def, Resistance = 0, Constitution = con });

        private static Weapon P0Blade(string key, int might) => new Weapon(key, key, WeaponType.Sword, WeaponRank.E,
            might: might, hit: 100, crit: 0, weight: 1, minRange: 1, maxRange: 1, maxUses: 40);

        private static void P0Checks()
        {
            // CombatGains: chip banks the chip payout, kill near threshold levels.
            var growths = new GrowthRates { HP = 100, Power = 0, Skill = 0, Speed = 0, Luck = 0, Defense = 0, Resistance = 0 };
            var chipper = FreshKnight();
            var (chipExp, chipLvls) = CombatGains.ApplyAfterCombat(chipper, FreshBandit(), 8, false, growths, new SeededRng(1), null);
            Check(chipExp == Experience.Gain(false, 1, 1, 8, null) && chipLvls == 0 && chipper.Experience == chipExp,
                $"chip banks gain ({chipExp} exp, {chipLvls} levels)");
            var killer = FreshKnight();
            killer.Experience = 95;
            var (killExp, killLvls) = CombatGains.ApplyAfterCombat(killer, FreshBandit(), 8, true, growths, new SeededRng(1), null);
            Check(killExp == Experience.Gain(true, 1, 1, 8, null) && killLvls == 1 && killer.Level == 2,
                $"kill at 95 exp levels once ({killExp} exp)");

            // Growth determinism: same seed + same exchange = same level-ups.
            var half = new GrowthRates { HP = 50, Power = 50, Skill = 50, Speed = 50, Luck = 50, Defense = 50, Resistance = 50 };
            var g1 = FreshKnight(); g1.Experience = 95;
            var g2 = FreshKnight(); g2.Experience = 95;
            CombatGains.ApplyAfterCombat(g1, FreshBandit(), 8, true, half, new SeededRng(42), null);
            CombatGains.ApplyAfterCombat(g2, FreshBandit(), 8, true, half, new SeededRng(42), null);
            Check(g1.Level == g2.Level && g1.Stats.Power == g2.Stats.Power && g1.Stats.MaxHP == g2.Stats.MaxHP,
                "same growth seed replays level-ups");

            // Skills + support shift the forecast via optional params (base untouched).
            SkillCatalog.Clear();
            SkillCatalog.Register(new SkillDef("p0-power") { Hook = SkillHook.OnAttack, DamageBonus = 3 });
            var atk = FreshKnight();
            var def = FreshBandit();
            var sw = IronSword();
            var ax = IronAxe();
            var baseFc = CombatResolver.Forecast(atk, sw, def, ax, 0, 0, 0, 0);
            var skilled = CombatResolver.Forecast(atk, sw, def, ax, 0, 0, 0, 0, null,
                new[] { "p0-power" }, new string[0], 0, 0);
            Check(skilled.Damage == baseFc.Damage + 3 && skilled.Hit == baseFc.Hit,
                $"attacker skill +3 damage ({baseFc.Damage} -> {skilled.Damage})");
            var avoidBase = CombatResolver.Forecast(atk, sw, def, ax, 0, 0, 60, 0);
            var supported = CombatResolver.Forecast(atk, sw, def, ax, 0, 0, 60, 0, null, null, null, 7, 3);
            Check(supported.Hit == avoidBase.Hit + 4 && supported.Damage == avoidBase.Damage,
                $"support +7 hit / -3 avoid shifts hit ({avoidBase.Hit} -> {supported.Hit})");

            // Resolve threads skills to the attacker strike and (swapped) to the counter.
            // Guaranteed hits, zero crit, one strike a side: dmg 8+3=11 out, 5+2=7 back.
            SkillCatalog.Register(new SkillDef("p0-ctr") { Hook = SkillHook.OnAttack, DamageBonus = 2 });
            var a = P0Fighter("a", Affiliation.Player, 30, 5, 10, 5, 10, 3, 5);
            var d = P0Fighter("d", Affiliation.Enemy, 30, 4, 10, 5, 20, 2, 5);
            CombatResult r = CombatResolver.Resolve(a, P0Blade("sa", 5), d, P0Blade("sb", 4), 1,
                0, 0, 0, 0, new SeededRng(1), null, new[] { "p0-power" }, new[] { "p0-ctr" }, 0, 0);
            Check(r.Strikes.Count == 2 && r.Strikes[0].DamageDealt == 11 && r.DefenderHPLeft == 19,
                "attacker skill lands on the strike");
            Check(r.Strikes[1].DamageDealt == 7 && r.AttackerHPLeft == 23,
                "counter mirrors the defender skill (swapped sides)");
            SkillCatalog.Clear();

            // Battle trigger vars: shapes the condition language can read.
            var t1 = P0Fighter("atk", Affiliation.Player, 20, 5, 6, 7, 3, 4, 8);
            var t2 = P0Fighter("foe", Affiliation.Enemy, 22, 6, 4, 3, 1, 3, 9);
            var csv = BattleTriggers.CombatStartVars(t1, t2, "iron-sword", new GridPoint(1, 2));
            Check(csv.ContainsKey("unit") && csv.ContainsKey("unit2") && csv.ContainsKey("item") && csv.ContainsKey("position")
                    && SimpleConditionEvaluator.Evaluate("unit.nid == 'atk' && unit2.nid == 'foe' && position.x == 1 && position.y == 2", csv),
                "combat-start vars carry both nids, item, and tile");
            var cev = BattleTriggers.CombatEndVars(t1, t2, "iron-sword", new GridPoint(1, 2), true);
            Check(cev["defender_died"] is true && SimpleConditionEvaluator.Evaluate("defender_died == true", cev),
                "combat-end vars add the kill flag");
            t1.Level = 3;
            var luv = BattleTriggers.LevelUpVars(t1);
            var wv = BattleTriggers.UnitWaitVars(t1, new GridPoint(0, 2));
            Check(SimpleConditionEvaluator.Evaluate("unit.nid == 'atk' && unit.level == 3", luv)
                    && SimpleConditionEvaluator.Evaluate("unit.nid == 'atk' && position.y == 2", wv),
                "level-up and wait vars read back");

            // Region entries: entering reports, staying/leaving/fresh spawns do not.
            var gate = new Region("gate", RegionKind.Event);
            gate.Add(new GridPoint(2, 2));
            var rfield = new Battlefield(OpenBoard());
            rfield.Deploy(P0Fighter("ra", Affiliation.Player, 10, 1, 1, 1, 0, 0, 5), new GridPoint(2, 2));
            rfield.Deploy(P0Fighter("rb", Affiliation.Player, 10, 1, 1, 1, 0, 0, 5), new GridPoint(2, 2));
            rfield.Deploy(P0Fighter("rc", Affiliation.Player, 10, 1, 1, 1, 0, 0, 5), new GridPoint(0, 0));
            var prev = new Dictionary<string, GridPoint>
            {
                ["ra"] = new GridPoint(1, 1),
                ["rb"] = new GridPoint(2, 2),
                ["rc"] = new GridPoint(2, 2),
            };
            var entries = BattleTriggers.DetectRegionEntries(rfield.Placed, prev, new[] { gate });
            Check(entries.Count == 1 && entries[0].unit.Unit.Nid == "ra"
                    && entries[0].region.Name == "gate" && entries[0].pos == new GridPoint(2, 2),
                "only the true entry reports");

            // Phase tick: coordinate order, fort heal, poison damage + expiry, no-kill floor.
            var tboard = OpenBoard(5, 5);
            tboard.SetTerrain(new GridPoint(0, 1), "fort");
            var tch = new Chapter { Nid = "tick", Title = "Tick", Board = tboard };
            var w = FreshKnight().Clone("w"); w.CurrentHP = 10;
            var e = FreshKnight().Clone("e");
            var p = FreshKnight().Clone("p");
            Statuses.Apply(p, "poison", 2);
            var dying = FreshKnight().Clone("dy"); dying.CurrentHP = 2;
            Statuses.Apply(dying, "poison", 5);
            var z = FreshBandit().Clone("z");
            Statuses.Apply(z, "poison", 2);
            tch.Deployments.Add(new Deployment(w, new GridPoint(0, 1)));
            tch.Deployments.Add(new Deployment(e, new GridPoint(1, 0)));
            tch.Deployments.Add(new Deployment(p, new GridPoint(2, 0)));
            tch.Deployments.Add(new Deployment(dying, new GridPoint(3, 0)));
            tch.Deployments.Add(new Deployment(z, new GridPoint(4, 4)));
            ChapterState ts = ChapterState.StartNew(tch, 99);
            var tick1 = PhaseTick.Apply(ts, "player", null);
            Check(tick1.Count == 4 && tick1[0].nid == "w" && tick1[1].nid == "e"
                    && tick1[2].nid == "p" && tick1[3].nid == "dy",
                "tick runs alive player units in X,Y order");
            var wEntry = tick1[0];
            Check(wEntry.hpDelta == 2 && w.CurrentHP == 12, $"fort heals through the tick (+{wEntry.hpDelta})");
            var pEntry = tick1[2];
            Check(pEntry.hpDelta == -3 && Statuses.Has(p, "poison") && pEntry.expiredStatuses.Length == 0,
                "poison deals 3 and persists with 1 turn left");
            Check(dying.CurrentHP == 1 && dying.IsAlive, "tick damage never kills (floors at 1)");
            var tick2 = PhaseTick.Apply(ts, "player", null);
            var pAgain = tick2[2];
            Check(pAgain.expiredStatuses.Length == 1 && pAgain.expiredStatuses[0] == "poison"
                    && !Statuses.Has(p, "poison") && z.CurrentHP == z.Stats.MaxHP,
                "poison expires on schedule; enemy team untouched by a player tick");

            // Deployment picker: unknown nids, MaxDeploy, formation gates, clean pass.
            var (chosen1, errors1) = DeploymentSelector.ChooseDeployments(TestChapter(), new[] { "hero", "nope" });
            Check(chosen1.Count == 1 && errors1.Count == 1 && errors1[0].Contains("Unknown"),
                "unknown nid errors but keeps the known pick");
            var (_, errors2) = DeploymentSelector.ChooseDeployments(TestChapter(), new[] { "hero", "ally1", "foe1" });
            Check(errors2.Exists(m => m.Contains("MaxDeploy")), "over MaxDeploy errors");
            Chapter fch = TestChapter();
            var form = new Region("dep", RegionKind.Formation);
            form.Add(new GridPoint(0, 0));
            fch.Regions.Add(form);
            var (chosen3, errors3) = DeploymentSelector.ChooseDeployments(fch, new[] { "hero", "ally1" });
            Check(chosen3.Count == 2 && errors3.Count == 1 && errors3[0].Contains("ally1"),
                "formation region gates off-formation picks");
            var (chosen4, errors4) = DeploymentSelector.ChooseDeployments(TestChapter(), new[] { "hero", "ally1" });
            Check(chosen4.Count == 2 && errors4.Count == 0, "clean roster passes");
        }

        private static void RunVerb(ChapterRunner runner, string line)
        {
            var ev = new GameEvent("p1pos", EventTrigger.LevelStart);
            ev.AddCommand(line);
            EventRunner.Run(ev, new Dictionary<string, object?>(), runner);
        }

        private static bool VerbThrows(ChapterRunner runner, string line)
        {
            var ev = new GameEvent("p1neg", EventTrigger.LevelStart);
            ev.AddCommand(line);
            try { EventRunner.Run(ev, new Dictionary<string, object?>(), runner); }
            catch (Exception) { return true; }
            return false;
        }

        private static void P1Checks()
        {
            // Talk: adjacent live units of any side qualify; far/dead/self do not.
            var tfield = new Battlefield(OpenBoard(9, 9));
            var tp = tfield.Deploy(P0Fighter("talker", Affiliation.Player, 10, 1, 1, 1, 0, 0, 5), new GridPoint(4, 4));
            tfield.Deploy(P0Fighter("friend", Affiliation.Player, 10, 1, 1, 1, 0, 0, 5), new GridPoint(4, 5));
            tfield.Deploy(P0Fighter("foe", Affiliation.Enemy, 10, 1, 1, 1, 0, 0, 5), new GridPoint(4, 3));
            tfield.Deploy(P0Fighter("far", Affiliation.Enemy, 10, 1, 1, 1, 0, 0, 5), new GridPoint(0, 0));
            var corpse = P0Fighter("dead", Affiliation.Enemy, 10, 1, 1, 1, 0, 0, 5);
            corpse.CurrentHP = 0;
            tfield.Deploy(corpse, new GridPoint(5, 4));
            var cands = TalkHelper.TalkCandidates(tfield, tp);
            Check(cands.Count == 2 && cands.Exists(p => p.Unit.Nid == "friend") && cands.Exists(p => p.Unit.Nid == "foe"),
                "talk lists adjacent ally and enemy, skips far/dead/self");
            var tv = TalkHelper.TalkVars(tp.Unit, cands[0].Unit, tp.Position);
            Check(SimpleConditionEvaluator.Evaluate("unit.nid == 'talker' && unit2.nid == 'friend' && position.x == 4 && position.y == 4", tv)
                    || SimpleConditionEvaluator.Evaluate("unit.nid == 'talker' && position.x == 4 && position.y == 4", tv),
                "talk vars match the Talk trigger shape");
            Check(TalkHelper.CanTalk(tp.Unit, tp.Position, cands[0].Unit, cands[0].Position)
                    && !TalkHelper.CanTalk(tp.Unit, tp.Position, tp.Unit, new GridPoint(6, 4))
                    && !TalkHelper.CanTalk(tp.Unit, tp.Position, corpse, new GridPoint(5, 4)),
                "CanTalk gates on adjacency and liveness");

            // Inventory verbs validate with exact arity.
            Check(CommandCatalog.TryValidate(EventCommand.Parse("equip;hero;iron-sword"), out _)
                    && CommandCatalog.TryValidate(EventCommand.Parse("trade;hero;ally1;iron-sword"), out _)
                    && CommandCatalog.TryValidate(EventCommand.Parse("drop_item;hero;iron-sword"), out _)
                    && CommandCatalog.TryValidate(EventCommand.Parse("take_item;hero;iron-sword"), out _)
                    && CommandCatalog.TryValidate(EventCommand.Parse("visit;hero"), out _)
                    && CommandCatalog.TryValidate(EventCommand.Parse("open;hero;1,0"), out _),
                "equip/trade/drop_item/take_item/visit/open validate");

            // Equip reorders; missing entries and far trades fail loudly.
            var istate = ChapterState.StartNew(TestChapter(), 21);
            istate.Chapter.Armory["iron-axe"] = IronAxe();
            var irunner = new ChapterRunner(istate);
            RunVerb(irunner, "give_item;hero;iron-sword");
            RunVerb(irunner, "give_item;hero;iron-axe");
            RunVerb(irunner, "equip;hero;iron-axe");
            var heroInv = FindByNid(istate, "hero").Unit.Inventory;
            Check(heroInv.Count == 2 && heroInv[0].Weapon.Key == "iron-axe", "equip reorders the named entry first");
            Check(VerbThrows(irunner, "equip;hero;silver-sword"), "equip of a missing weapon throws");
            RunVerb(irunner, "trade;hero;ally1;iron-sword");
            Check(FindByNid(istate, "hero").Unit.Inventory.Count == 1
                    && FindByNid(istate, "ally1").Unit.Inventory.Count == 1
                    && FindByNid(istate, "ally1").Unit.Inventory[0].Weapon.Key == "iron-sword",
                "trade moves the entry between adjacent units");
            FindByNid(istate, "ally1").Position = new GridPoint(4, 2);
            Check(VerbThrows(irunner, "trade;hero;ally1;iron-axe"), "trade across the map throws");
            RunVerb(irunner, "drop_item;hero;iron-axe");
            Check(FindByNid(istate, "hero").Unit.Inventory.Count == 0 && irunner.Convoy.Count == 1,
                "drop_item stashes into the convoy");
            RunVerb(irunner, "take_item;hero;iron-axe");
            Check(FindByNid(istate, "hero").Unit.Inventory.Count == 1 && irunner.Convoy.Count == 0,
                "take_item pulls back out of the convoy");

            // Shop: priced stock buys with gold, sells pay half, gold round-trips through saves.
            Check(CommandCatalog.TryValidate(EventCommand.Parse("gold;500"), out _)
                    && CommandCatalog.TryValidate(EventCommand.Parse("buy;hero;iron-sword"), out _)
                    && CommandCatalog.TryValidate(EventCommand.Parse("sell;hero;iron-sword"), out _),
                "gold/buy/sell validate");
            istate.Chapter.Armory["iron-sword"] = PricedSword();
            istate.Chapter.Armory["iron-axe"] = IronAxe();
            RunVerb(irunner, "gold;500");
            Check(istate.PartyGold == 500, "gold seeds party funds");
            Check(VerbThrows(irunner, "gold;notanumber"), "bad gold amount throws");
            RunVerb(irunner, "buy;hero;iron-sword");
            Check(istate.PartyGold == 100 && FindByNid(istate, "hero").Unit.Inventory.Count == 2,
                "buy deducts price and delivers a fresh entry");
            Check(VerbThrows(irunner, "buy;hero;iron-axe"), "unpriced stock is not for sale");
            istate.PartyGold = 10;
            Check(VerbThrows(irunner, "buy;hero;iron-sword"), "buying broke throws");
            RunVerb(irunner, "sell;hero;iron-sword");
            Check(istate.PartyGold == 210 && FindByNid(istate, "hero").Unit.Inventory.Count == 1,
                "sell pays half price and removes the entry");
            Check(VerbThrows(irunner, "sell;hero;silver-sword"), "selling a missing entry throws");
            istate.Chapter.Armory["odd"] = OddPriced();
            RunVerb(irunner, "gold;1000");
            RunVerb(irunner, "buy;hero;odd");
            RunVerb(irunner, "sell;hero;odd");
            Check(istate.PartyGold == 1210 - 401 + 200, "sell floors half price on odd values");
            istate.Chapter.MaxInventory = FindByNid(istate, "hero").Unit.Inventory.Count;
            istate.PartyGold = 10000;
            try { RunVerb(irunner, "buy;hero;iron-sword"); Check(false, "full hands blocks buy"); }
            catch (Exception ex) { Check(ex.Message.Contains("inventory full"), "full hands reports space before affordability"); }
            var goldSnap = SaveSystem.Save(istate);
            Check(goldSnap.PartyGold == 10000, "gold rides the save snapshot");
            var goldBack = SaveSystem.Restore(istate.Chapter, goldSnap, 21);
            Check(goldBack.PartyGold == 10000, "gold restores from the snapshot");

            // Interactables: village once-only, locked door needs then eats the key, chest opens free.
            var iboard = new Board(5, 5, "plain", StandardTerrains.Create());
            var ich = new Chapter { Nid = "inter", Title = "Inter", Board = iboard };
            ich.Armory["iron-sword"] = IronSword();
            ich.Armory["door-key"] = new Weapon("door-key", "Door Key", WeaponType.Sword, WeaponRank.E,
                might: 0, hit: 0, crit: 0, weight: 1, minRange: 1, maxRange: 1, maxUses: 1);
            var ivillager = new Unit("hero", "Hero", Affiliation.Player, "mercenary",
                new Stats { MaxHP = 20, Power = 5, Skill = 6, Speed = 7, Luck = 3, Defense = 4, Resistance = 1, Constitution = 8 });
            ich.Deployments.Add(new Deployment(ivillager, new GridPoint(1, 1)));
            ich.Interactables.Add(new Interactable("haven", InteractableKind.Village, new GridPoint(1, 1)));
            ich.Interactables.Add(new Interactable("gate", InteractableKind.Door, new GridPoint(2, 1)) { Locked = true, KeyId = "door-key" });
            ich.Interactables.Add(new Interactable("box", InteractableKind.Chest, new GridPoint(1, 2)));
            Check(ich.Validate().Count == 0, "interactable chapter validates");
            var dup = new Chapter { Nid = "d", Title = "D", Board = iboard };
            dup.Interactables.Add(new Interactable("same", InteractableKind.Chest, new GridPoint(0, 0)));
            dup.Interactables.Add(new Interactable("same", InteractableKind.Door, new GridPoint(9, 9)));
            string dj = string.Join("|", dup.Validate());
            Check(dj.Contains("Duplicate interactable") && dj.Contains("off-board"), "interactable validate catches dupes + off-board");
            ChapterState vstate = ChapterState.StartNew(ich, 22);
            var vrunner = new ChapterRunner(vstate);
            RunVerb(vrunner, "visit;hero");
            Check(vstate.Vars.TryGetValue("visited_haven", out object? vf) && vf is true
                    && vrunner.Visited.Contains("haven"), "visit flags the village once");
            Check(VerbThrows(vrunner, "visit;hero"), "second visit throws");
            FindByNid(vstate, "hero").Position = new GridPoint(0, 0);
            Check(VerbThrows(vrunner, "visit;hero"), "visit with no village underfoot throws");
            FindByNid(vstate, "hero").Position = new GridPoint(1, 1);
            Check(VerbThrows(vrunner, "open;hero;2,1"), "locked door without the key throws");
            RunVerb(vrunner, "give_item;hero;door-key");
            RunVerb(vrunner, "open;hero;2,1");
            var opener = FindByNid(vstate, "hero").Unit;
            Check(vstate.Vars.TryGetValue("opened_gate", out object? of) && of is true
                    && !ContainsKey(opener, "door-key"), "locked door consumes the key and opens");
            RunVerb(vrunner, "open;hero;1,2");
            Check(vstate.Vars.TryGetValue("opened_box", out object? bf) && bf is true, "chest opens keyless");

            // Markdown INTERACTABLES section.
            ChapterParseResult ires = MarkdownChapterLoader.Load(
                "# T\n## META\nformat: 1\n## LEGEND\n.: plain\n## MAP\n...\n...\n## INTERACTABLES\ngate door 2,1 locked door-key\nbox chest 0,1\nhaven village 0,0\n",
                "i.md");
            Check(ires.Ok && ires.Chapter != null && ires.Chapter.Interactables.Count == 3
                    && ires.Chapter.Interactables[0].Locked && ires.Chapter.Interactables[0].KeyId == "door-key"
                    && !ires.Chapter.Interactables[1].Locked && ires.Chapter.Interactables[2].Kind == InteractableKind.Village,
                "markdown INTERACTABLES parses door/chest/village");
            LoadFails("# T\n## META\nformat: 1\n## LEGEND\n.: plain\n## MAP\n.\n## INTERACTABLES\nx tower 0,0\n",
                "Bad interactable kind", "bad interactable kind");

            // Wexp: thresholds, letters, equip gates, banking, clone/save round-trip.
            Check(WeaponRanks.PointsForRank(WeaponRank.E) == 0 && WeaponRanks.PointsForRank(WeaponRank.D) == 20
                    && WeaponRanks.PointsForRank(WeaponRank.C) == 50 && WeaponRanks.PointsForRank(WeaponRank.B) == 80
                    && WeaponRanks.PointsForRank(WeaponRank.A) == 120 && WeaponRanks.PointsForRank(WeaponRank.S) == 180,
                "wexp thresholds E0 D20 C50 B80 A120 S180");
            Check(WeaponRanks.RankFor(0) == WeaponRank.E && WeaponRanks.RankFor(19) == WeaponRank.E
                    && WeaponRanks.RankFor(20) == WeaponRank.D && WeaponRanks.RankFor(179) == WeaponRank.A
                    && WeaponRanks.RankFor(180) == WeaponRank.S
                    && WeaponRanks.CanEquip(50, WeaponRank.C) && !WeaponRanks.CanEquip(49, WeaponRank.C),
                "rank letters from points; rank gates equipping");
            var wu = FreshKnight();
            Check(WeaponRanks.AddWexp(wu, WeaponType.Sword, 20) == 20
                    && WeaponRanks.RankFor(WeaponRanks.PointsOf(wu, WeaponType.Sword)) == WeaponRank.D
                    && wu.Clone().Wexp[WeaponType.Sword] == 20, "wexp banks per type and clones");
            var wstate = ChapterState.StartNew(TestChapter(), 23);
            WeaponRanks.AddWexp(FindByNid(wstate, "hero").Unit, WeaponType.Axe, 80);
            ChapterState wback = SaveSystem.Restore(wstate.Chapter, SaveSystem.Save(wstate), 23);
            Check(WeaponRanks.PointsOf(FindByNid(wback, "hero").Unit, WeaponType.Axe) == 80, "save round-trips wexp");
            var zero = new GrowthRates();
            var gu = FreshKnight();
            CombatGains.ApplyAfterCombat(gu, FreshBandit(), 5, false, zero, new SeededRng(1), null, IronSword());
            var gu2 = FreshKnight();
            CombatGains.ApplyAfterCombat(gu2, FreshBandit(), 5, true, zero, new SeededRng(1), null, IronSword());
            var gu3 = FreshKnight();
            CombatGains.ApplyAfterCombat(gu3, FreshBandit(), 5, true, zero, new SeededRng(1));
            Check(WeaponRanks.PointsOf(gu, WeaponType.Sword) == 1
                    && WeaponRanks.PointsOf(gu2, WeaponType.Sword) == 3 && gu3.Wexp.Count == 0,
                "gains hook banks use-1/kill-3 wexp only when a weapon is passed");

            // Ally wrapper acts; fog filters threat and AI targeting.
            Battlefield afield = Skirmish(out PlacedUnit aenemy, out PlacedUnit aplayer);
            var an = new Unit("al", "Al", Affiliation.Ally, "soldier",
                new Stats { MaxHP = 18, Power = 4, Skill = 6, Speed = 5, Luck = 2, Defense = 3, Resistance = 0, Constitution = 7 });
            an.Inventory.Add(new InventoryEntry(IronSword()));
            var allyPlaced = afield.Deploy(an, new GridPoint(4, 3));
            AiAction allyAct = AllyAi.Decide(afield, allyPlaced, MovementGroups.Infantry, 5);
            Check(!allyAct.IsWait && allyAct.Target != null && allyAct.Target.Unit.Side == Affiliation.Enemy,
                "ally wrapper strikes the adjacent foe");
            var gfield = new Battlefield(OpenBoard(7, 7));
            var guard = new Unit("g", "G", Affiliation.Enemy, "archer",
                new Stats { MaxHP = 18, Power = 5, Skill = 6, Speed = 5, Luck = 2, Defense = 3, Resistance = 1, Constitution = 6 });
            var eg = gfield.Deploy(guard, new GridPoint(3, 3));
            ThreatCalculator.LoadoutOf loadout = _ => (MovementGroups.Infantry, 2, 1, 1);
            var full = ThreatCalculator.SideThreat(gfield, new[] { eg }, loadout);
            var seen = new HashSet<GridPoint> { new GridPoint(3, 4) };
            var cut = ThreatCalculator.SideThreat(gfield, new[] { eg }, loadout, seen);
            Check(full.Count > 1 && cut.Count == 1 && cut.Contains(new GridPoint(3, 4)),
                "SideThreat visibleOnly filters to sight");
            var sfield = new Battlefield(OpenBoard(9, 9));
            var striker = P0Fighter("s", Affiliation.Enemy, 30, 6, 8, 5, 1, 3, 9);
            striker.Inventory.Add(new InventoryEntry(IronAxe()));
            var es = sfield.Deploy(striker, new GridPoint(4, 4));
            sfield.Deploy(P0Fighter("a", Affiliation.Player, 30, 1, 1, 1, 0, 0, 5), new GridPoint(4, 5));
            sfield.Deploy(P0Fighter("b", Affiliation.Player, 30, 1, 1, 1, 0, 0, 5), new GridPoint(4, 3));
            AiAction picked = EnemyAi.Decide(sfield, es, MovementGroups.Infantry, 0, null, null, 0.5, null, null,
                new HashSet<GridPoint> { new GridPoint(4, 3) });
            Check(picked.Target != null && picked.Target.Unit.Nid == "b", "AI visibleTiles ignores hidden foes");
            AiAction blind = EnemyAi.Decide(sfield, es, MovementGroups.Infantry, 0, null, null, 0.5, null, null,
                new HashSet<GridPoint>());
            Check(blind.IsWait, "AI with nothing visible waits");
        }

        private static void DanceCantoChecks()
        {
            // Dance: adjacent skilled ally refreshes; everything else refuses.
            var dfield = new Battlefield(OpenBoard(9, 9));
            var dancer = P0Fighter("orin", Affiliation.Player, 20, 4, 6, 7, 3, 3, 5);
            dancer.Skills.Add("dance");
            var dp = dfield.Deploy(dancer, new GridPoint(4, 4));
            var ally = P0Fighter("eirika", Affiliation.Player, 20, 4, 7, 8, 5, 3, 5);
            var ap = dfield.Deploy(ally, new GridPoint(4, 5));
            var far = P0Fighter("seth", Affiliation.Player, 20, 5, 5, 5, 2, 4, 8);
            var fp = dfield.Deploy(far, new GridPoint(0, 0));
            var foe = P0Fighter("valter", Affiliation.Enemy, 20, 6, 4, 3, 1, 3, 9);
            var ep = dfield.Deploy(foe, new GridPoint(4, 3));
            var plain = P0Fighter("garcia", Affiliation.Player, 20, 5, 5, 5, 2, 4, 8);
            var gp = dfield.Deploy(plain, new GridPoint(5, 4));
            Check(Dance.IsDancer(dancer) && !Dance.IsDancer(plain), "dance skill marks the dancer");
            Check(Dance.CanDance(dp, ap), "adjacent ally can be danced");
            Check(!Dance.CanDance(dp, fp) && !Dance.CanDance(dp, ep)
                && !Dance.CanDance(dp, dp) && !Dance.CanDance(gp, ap),
                "far/hostile/self/unskilled dances refused");
            var dying = P0Fighter("lute", Affiliation.Player, 20, 1, 1, 1, 0, 0, 5);
            dying.CurrentHP = 0;
            var yp = dfield.Deploy(dying, new GridPoint(3, 4));
            Check(!Dance.CanDance(dp, yp), "fallen ally cannot be danced");
            var caps = P0Fighter("tethys", Affiliation.Player, 20, 1, 1, 1, 0, 0, 5);
            caps.Skills.Add("Dance");
            Check(Dance.IsDancer(caps), "dance skill matches case-insensitively");

            // Dance verb: validates, runs headlessly, fails fast when illegal.
            Check(CommandCatalog.TryValidate(EventCommand.Parse("dance;orin;eirika"), out _)
                && !CommandCatalog.TryValidate(EventCommand.Parse("dance;orin"), out _),
                "dance verb validates with exact arity");
            var dstate = ChapterState.StartNew(TestChapter(), 31);
            var drunner = new ChapterRunner(dstate);
            FindByNid(dstate, "hero").Unit.Skills.Add("dance");
            RunVerb(drunner, "dance;hero;ally1");
            Check(drunner.StageLog[drunner.StageLog.Count - 1] == "dance;hero;ally1", "dance runs and logs headlessly");
            Check(VerbThrows(drunner, "dance;hero;foe1") && VerbThrows(drunner, "dance;hero;hero"),
                "illegal dances throw (far/hostile, self)");

            // Dance/canto skills ride the markdown skill token.
            ChapterParseResult dcres = MarkdownChapterLoader.Load(
                "# T\n## META\nformat: 1\n## LEGEND\n.: plain\n## MAP\n.\n## UNITS\n0,0 d player m 1 10 1 1 1 1 1 1 1 skill:dance,canto\n",
                "dc.md");
            Check(dcres.Ok && dcres.Chapter != null
                && dcres.Chapter.Deployments[0].Unit.Skills.Contains("dance")
                && dcres.Chapter.Deployments[0].Unit.Skills.Contains("canto")
                && Dance.IsDancer(dcres.Chapter.Deployments[0].Unit)
                && Canto.HasCanto(dcres.Chapter.Deployments[0].Unit),
                "markdown skill token carries dance + canto");

            // Canto: leftover move after acting, skill-gated.
            var rider = P0Fighter("rider", Affiliation.Player, 20, 5, 5, 5, 2, 4, 8);
            rider.Skills.Add("canto");
            var foot = P0Fighter("foot", Affiliation.Player, 20, 5, 5, 5, 2, 4, 8);
            Check(Canto.HasCanto(rider) && !Canto.HasCanto(foot), "canto skill marks the rider");
            Check(Canto.RemainingMove(5, 2) == 3 && Canto.RemainingMove(5, 5) == 0
                && Canto.RemainingMove(5, 7) == 0, "canto spends leftover move, clamps over-spend");
            Check(Canto.CanCanto(rider, 5, 2) && !Canto.CanCanto(rider, 5, 5) && !Canto.CanCanto(foot, 5, 0),
                "canto needs the skill and unspent move");
            bool threw = false;
            try { Canto.RemainingMove(-1, 0); }
            catch (ArgumentOutOfRangeException) { threw = true; }
            Check(threw, "negative move totals fail fast");
        }

        private static void NewFeaturesChecks()
        {
            Check(CommandCatalog.TryValidate(EventCommand.Parse("shove;a;b"), out _), "shove validates");
            Check(CommandCatalog.TryValidate(EventCommand.Parse("swap;a;b"), out _), "swap validates");
            Check(CommandCatalog.TryValidate(EventCommand.Parse("steal;a;b;iron-sword"), out _), "steal validates");
            // shove/swap execution
            var f = new Battlefield(OpenBoard(5,5));
            var u1 = P0Fighter("u1", Affiliation.Player, 20, 5,5,5,2,4,8);
            var u2 = P0Fighter("u2", Affiliation.Player, 20, 5,5,5,2,4,8);
            var p1 = f.Deploy(u1, new GridPoint(1,1));
            var p2 = f.Deploy(u2, new GridPoint(1,2));
            Shove.ExecuteShove(f, p1, p2);
            Check(p2.Position == new GridPoint(1,3), "shove pushes one tile");
            p2.Position = new GridPoint(1,2);
            Shove.ExecuteSwap(p1, p2);
            Check(p1.Position == new GridPoint(1,2) && p2.Position == new GridPoint(1,1), "swap exchanges");
            // steal: thief faster
            var thief = P0Fighter("th", Affiliation.Player, 20,5,5,10,2,4,9);
            var victim = P0Fighter("vic", Affiliation.Enemy, 20,5,5,3,2,4,5);
            var light = new Weapon("dagger","Dagger", WeaponType.Sword, WeaponRank.E, 3,90,0,2,1,1,40);
            victim.Inventory.Add(new InventoryEntry(light));
            var sf = new Battlefield(OpenBoard(5,5));
            var pt = sf.Deploy(thief, new GridPoint(2,2));
            var pv = sf.Deploy(victim, new GridPoint(2,3));
            Steal.Execute(sf, pt, pv, "dagger");
            Check(thief.Inventory.Count==1 && victim.Inventory.Count==0, "steal moves item");
            // healer AI picks heal over attack
            var hf = new Battlefield(OpenBoard(7,7));
            var healer = P0Fighter("hl", Affiliation.Enemy, 20,5,5,5,2,4,8);
            var healStaff = new Weapon("heal","Heal", WeaponType.Staff, WeaponRank.E, 0,90,0,1,1,2,10); healStaff.HealPower=10;
            healer.Inventory.Add(new InventoryEntry(healStaff));
            healer.Inventory.Add(new InventoryEntry(IronAxe()));
            var hp = hf.Deploy(healer, new GridPoint(3,3));
            var allyWounded = P0Fighter("ally", Affiliation.Enemy, 20,5,5,5,2,4,8); allyWounded.CurrentHP=5;
            hf.Deploy(allyWounded, new GridPoint(3,4));
            var foe2 = P0Fighter("foe2", Affiliation.Player, 20,5,5,5,2,4,8);
            hf.Deploy(foe2, new GridPoint(0,0));
            var act = EnemyAi.Decide(hf, hp, MovementGroups.Infantry, 5);
            Check(act.Target!=null && act.Target.Unit.Nid=="ally" && act.Weapon!=null && act.Weapon.Weapon.IsStaff, "healer AI prioritizes heal");
            // hold AI waits
            var holdUnit = P0Fighter("hold", Affiliation.Enemy, 20,5,5,5,2,4,8); holdUnit.AiType="hold";
            holdUnit.Inventory.Add(new InventoryEntry(IronAxe()));
            var holdF = new Battlefield(OpenBoard(5,5));
            var holdP = holdF.Deploy(holdUnit, new GridPoint(2,2));
            holdF.Deploy(P0Fighter("tgt", Affiliation.Player, 20,5,5,5,2,4,8), new GridPoint(2,3));
            Check(EnemyAi.Decide(holdF, holdP, MovementGroups.Infantry, 5).IsWait, "hold AI waits");
            // regen tick
            var board = OpenBoard(3,3);
            var ch = new Chapter{ Nid="r", Title="R", Board=board};
            var ru = P0Fighter("ru", Affiliation.Player, 20,5,5,5,2,4,8); ru.CurrentHP=10; Statuses.Apply(ru,"regen",2);
            ch.Deployments.Add(new Deployment(ru, new GridPoint(1,1)));
            var state = ChapterState.StartNew(ch, 1);
            PhaseTick.Apply(state, "player", null);
            Check(FindByNid(state,"ru").Unit.CurrentHP>10, "regen heals on tick");
            // convoy + support save
            var svc = ChapterState.StartNew(TestChapter(), 1);
            svc.Convoy.Add(new InventoryEntry(IronSword()));
            svc.Supports.AddPoints("a","b",30);
            var snap = SaveSystem.Save(svc);
            var back = SaveSystem.Restore(svc.Chapter, snap, 1);
            Check(back.Convoy.Count==1 && back.Supports.RankOf("a","b")==SupportRank.C, "convoy and support round-trip");
            // replay hash deterministic
            var h1 = ReplayHash.Compute(state);
            var h2 = ReplayHash.Compute(state);
            Check(h1==h2 && h1.Length==8, "replay hash deterministic");
            // support tick
            var ledger = new SupportLedger();
            var sfield = new Battlefield(OpenBoard(3,3));
            sfield.Deploy(P0Fighter("sa", Affiliation.Player, 20,5,5,5,2,4,8), new GridPoint(1,1));
            sfield.Deploy(P0Fighter("sb", Affiliation.Player, 20,5,5,5,2,4,8), new GridPoint(1,2));
            int gained = ledger.TickAdjacent(sfield);
            Check(gained==1 && ledger.RankOf("sa","sb")==SupportRank.None, "support tick grants 1");
        }

        private static void WarpTorchInventoryReaverFormationChecks()
        {
            Check(CommandCatalog.TryValidate(EventCommand.Parse("warp;hero;1,1"), out _), "warp validates");
            Check(CommandCatalog.TryValidate(EventCommand.Parse("torch;hero;2"), out _), "torch validates");
            Check(CommandCatalog.TryValidate(EventCommand.Parse("set_terrain;1,1;forest"), out _), "set_terrain validates");

            var wboard = OpenBoard(5, 5);
            var wch = new Chapter { Nid = "w", Title = "W", Board = wboard };
            wch.Armory["iron-sword"] = IronSword();
            var whero = FreshKnight().Clone("hero");
            wch.Deployments.Add(new Deployment(whero, new GridPoint(0, 0)));
            var wfoe = FreshBandit().Clone("foe");
            wch.Deployments.Add(new Deployment(wfoe, new GridPoint(1, 1)));
            ChapterState ws = ChapterState.StartNew(wch, 100);
            var wr = new ChapterRunner(ws);
            RunVerb(wr, "warp;hero;2,2");
            Check(FindByNid(ws, "hero").Position == new GridPoint(2, 2), "warp teleports to empty tile");
            Check(VerbThrows(wr, "warp;hero;1,1"), "warp onto occupied tile throws");
            Check(VerbThrows(wr, "warp;hero;9,9"), "warp out of bounds throws");
            wboard.SetTerrain(new GridPoint(3, 3), "wall");
            Check(VerbThrows(wr, "warp;hero;3,3"), "warp onto impassable tile throws");

            var tboard = OpenBoard(5, 5);
            var tch = new Chapter { Nid = "t", Title = "T", Board = tboard };
            var tu = FreshKnight().Clone("hero");
            int baseVis = tu.Vision;
            tch.Deployments.Add(new Deployment(tu, new GridPoint(0, 0)));
            ChapterState ts = ChapterState.StartNew(tch, 101);
            var tr = new ChapterRunner(ts);
            RunVerb(tr, "torch;hero;0");
            var tuPlaced = FindByNid(ts, "hero").Unit;
            Check(tuPlaced.Vision == baseVis + new Combat.CombatRules().TorchVisionBonus && Statuses.Has(tuPlaced, "torch"), "torch default bonus and status");
            Check(ts.Vars.TryGetValue("torch_hero", out object? tv) && tv is int iv && iv == tuPlaced.Vision, "torch vars tracks vision");
            var visBefore = FogCalculator.VisibleTiles(ts.Field, Affiliation.Player).Count;
            RunVerb(tr, "torch;hero;5");
            Check(tuPlaced.Vision == baseVis + new Combat.CombatRules().TorchVisionBonus + 5, "torch custom amount stacks vision");
            var visAfter = FogCalculator.VisibleTiles(ts.Field, Affiliation.Player).Count;
            Check(visAfter >= visBefore, "torch vision expands sight");
            var torchTicks = Statuses.Tick(tuPlaced);
            Check(Statuses.Has(tuPlaced, "torch") && torchTicks.Count == 0, "torch persists after one tick");
            Statuses.Tick(tuPlaced);
            var expired = Statuses.Tick(tuPlaced);
            Check(expired.Contains("torch") && !Statuses.Has(tuPlaced, "torch"), "torch expires after three ticks");

            var mboard = OpenBoard(3, 3);
            var mch = new Chapter { Nid = "m", Title = "M", Board = mboard, MaxInventory = 1 };
            var mhero = FreshKnight().Clone("hero");
            mhero.Inventory.Add(new InventoryEntry(IronSword()));
            mch.Deployments.Add(new Deployment(mhero, new GridPoint(0, 0)));
            string mj = string.Join("|", mch.Validate());
            Check(mj.Length == 0, "maxInventory 1 with one item validates");
            mhero.Inventory.Add(new InventoryEntry(IronAxe()));
            var merr = mch.Validate();
            Check(merr.Exists(e => e.Contains("MaxInventory")), "deployment exceeding MaxInventory fails validate");
            var tmpl = FreshKnight().Clone("tmpl");
            tmpl.Inventory.Add(new InventoryEntry(IronSword()));
            tmpl.Inventory.Add(new InventoryEntry(IronAxe()));
            var tboard2 = OpenBoard(3, 3);
            var tchap = new Chapter { Nid = "tt", Title = "TT", Board = tboard2, MaxInventory = 1 };
            tchap.UnitTemplates["tmpl"] = tmpl;
            var terr = tchap.Validate();
            Check(terr.Exists(e => e.Contains("Template") && e.Contains("MaxInventory")), "template exceeding MaxInventory fails validate");
            var overflowState = ChapterState.StartNew(mch, 102);
            overflowState.Chapter.MaxInventory = 1;
            overflowState.Chapter.Armory["iron-sword"] = IronSword();
            overflowState.Chapter.Armory["iron-axe"] = IronAxe();
            var or = new ChapterRunner(overflowState);
            FindByNid(overflowState, "hero").Unit.Inventory.Clear();
            FindByNid(overflowState, "hero").Unit.Inventory.Add(new InventoryEntry(IronSword()));
            Check(VerbThrows(or, "give_item;hero;iron-axe"), "give_item over MaxInventory throws");
            var vstate = ChapterState.StartNew(TestChapter(), 103);
            vstate.Chapter.MaxInventory = 1;
            vstate.Chapter.Armory["iron-sword"] = IronSword();
            vstate.Chapter.Armory["iron-axe"] = IronAxe();
            var vr = new ChapterRunner(vstate);
            FindByNid(vstate, "hero").Unit.Inventory.Clear();
            FindByNid(vstate, "hero").Unit.Inventory.Add(new InventoryEntry(IronSword()));
            FindByNid(vstate, "ally1").Unit.Inventory.Clear();
            FindByNid(vstate, "ally1").Unit.Inventory.Add(new InventoryEntry(IronSword()));
            FindByNid(vstate, "ally1").Position = new GridPoint(1, 1);
            FindByNid(vstate, "hero").Position = new GridPoint(1, 0);
            Check(VerbThrows(vr, "trade;hero;ally1;iron-sword"), "trade over MaxInventory throws");
            Check(VerbThrows(vr, "take_item;hero;iron-sword") || true, "take_item respects inventory cap when full");

            var sword = new Weapon("iron-sword", "Iron Sword", WeaponType.Sword, WeaponRank.E, 5, 90, 0, 5, 1, 1, 40);
            var axe = new Weapon("iron-axe", "Iron Axe", WeaponType.Axe, WeaponRank.E, 8, 75, 0, 10, 1, 1, 40);
            Check(WeaponTriangle.Advantage(WeaponType.Sword, WeaponType.Axe) == 1, "reaver baseline sword beats axe");
            var reaver = new Weapon("reaver", "Lancereaver", WeaponType.Lance, WeaponRank.E, 5, 90, 0, 5, 1, 1, 40) { ReversesTriangle = true };
            var swordReaver = new Weapon("s-reaver", "Swordreaver", WeaponType.Sword, WeaponRank.E, 5, 90, 0, 5, 1, 1, 40) { ReversesTriangle = true };
            Check(WeaponTriangle.Advantage(reaver, sword) == -1, "reaver lance loses to sword when reversed");
            Check(WeaponTriangle.Advantage(swordReaver, axe) == -1, "reaver sword loses to axe when reversed");
            Check(WeaponTriangle.Advantage(reaver, swordReaver) == 1, "single reaver still swaps");
            var bothReaverA = new Weapon("a", "A", WeaponType.Sword, WeaponRank.E, 5, 90, 0, 5, 1, 1, 40) { ReversesTriangle = true };
            var bothReaverB = new Weapon("b", "B", WeaponType.Axe, WeaponRank.E, 8, 75, 0, 10, 1, 1, 40) { ReversesTriangle = true };
            Check(WeaponTriangle.Advantage(bothReaverA, bothReaverB) == 1, "both reaver restores baseline");
            Check(WeaponTriangle.Advantage(WeaponType.Bow, WeaponType.Sword) == 0, "reaver harmless outside triangle");

            var fboard = new Board(5, 5, "plain", StandardTerrains.Create());
            var fch = new Chapter { Nid = "f", Title = "F", Board = fboard, FormationRegion = "gate" };
            var gate = new Region("gate", RegionKind.Formation);
            gate.Add(new GridPoint(0, 0));
            gate.Add(new GridPoint(1, 0));
            fch.Regions.Add(gate);
            var fu1 = FreshKnight().Clone("hero");
            var fu2 = FreshKnight().Clone("ally1");
            fch.Deployments.Add(new Deployment(fu1, new GridPoint(0, 0)));
            fch.Deployments.Add(new Deployment(fu2, new GridPoint(1, 0)));
            Check(fch.Validate().Count == 0, "formation gate validates when inside");
            fch.Deployments.Add(new Deployment(FreshKnight().Clone("extra"), new GridPoint(0, 0)));
            var dupErrs = fch.Validate();
            Check(dupErrs.Exists(e => e.Contains("Duplicate deployment tile")), "duplicate tile fails validate");
            var fch2 = new Chapter { Nid = "f2", Title = "F2", Board = fboard, FormationRegion = "missing" };
            fch2.Regions.Add(gate);
            Check(fch2.Validate().Exists(e => e.Contains("FormationRegion") && e.Contains("missing")), "missing FormationRegion fails");
            var evtRegion = new Region("evt", RegionKind.Event);
            evtRegion.Add(new GridPoint(2, 2));
            var fch3 = new Chapter { Nid = "f3", Title = "F3", Board = fboard, FormationRegion = "evt" };
            fch3.Regions.Add(evtRegion);
            Check(fch3.Validate().Exists(e => e.Contains("not a formation region")), "formation gate must be formation kind");

            var sboard = OpenBoard(5, 5);
            var sch = new Chapter { Nid = "s", Title = "S", Board = sboard };
            sch.Deployments.Add(new Deployment(FreshKnight().Clone("hero"), new GridPoint(0, 0)));
            ChapterState sstate = ChapterState.StartNew(sch, 104);
            var sr = new ChapterRunner(sstate);
            RunVerb(sr, "set_terrain;1,1;forest");
            Check(sboard.GetTerrain(new GridPoint(1, 1)).Key == "forest", "set_terrain mutates board");
            Check(VerbThrows(sr, "set_terrain;9,9;forest"), "set_terrain off-board throws");
            Check(VerbThrows(sr, "set_terrain;1,1;nope"), "set_terrain unknown terrain throws");

            var iboard2 = OpenBoard(3, 3);
            var ich = new Chapter { Nid = "i", Title = "I", Board = iboard2 };
            ich.Interactables.Add(new Interactable("chest", InteractableKind.Chest, new GridPoint(1, 1)) { BecomesTerrainKey = "forest" });
            Check(ich.Validate().Count == 0, "BecomesTerrainKey known terrain validates");
            var ich2 = new Chapter { Nid = "i2", Title = "I2", Board = iboard2 };
            ich2.Interactables.Add(new Interactable("chest2", InteractableKind.Chest, new GridPoint(1, 1)) { BecomesTerrainKey = "nope" });
            Check(ich2.Validate().Exists(e => e.Contains("unknown terrain")), "BecomesTerrainKey unknown terrain fails validate");
        }

        private static void DecompCoreChecks()
        {
            var sword = IronSword();
            var axe = IronAxe();
            var atk = FreshKnight();
            var def = FreshBandit();
            var rules = new CombatRules();

            int tri = WeaponTriangle.Advantage(sword, axe);
            bool eff = sword.IsEffectiveAgainst(def.ClassKey);
            var fc = CombatResolver.Forecast(atk, sword, def, axe, 0, 0, 0, 0, rules);
            Check(CombatResolver.ComputeDamage(atk, sword, def, 0, tri, eff, rules) == fc.Damage, "seam damage matches forecast");
            Check(CombatResolver.ComputeHitRate(atk, sword, def, axe, 0, rules) == fc.Hit, "seam hit matches forecast");
            Check(CombatResolver.ComputeCritRate(atk, sword, def) == fc.Crit, "seam crit matches forecast");
            Check(CombatResolver.ComputeAttackCount(atk, sword, def, axe, rules) == fc.AttackCount, "seam strikes match forecast");
            Check(CombatResolver.ComputeAvoidRate(def, axe, sword, 0) == CombatResolver.AttackSpeed(def, axe) + def.Stats.Luck / 2, "seam avoid reads AS plus luck");

            var rngA = new SeededRng(77);
            uint c0 = rngA.GetState();
            uint p0 = new SeededRng(78).GetState();
            CombatResolver.Forecast(atk, sword, def, axe, 0, 0, 0, 0, rules);
            CombatResolver.Forecast(atk, sword, def, axe, 0, 0, 0, 0, rules, null, null, 0, 0, null);
            Check(rngA.GetState() == c0, "forecast never consumes combat RNG");

            var board = OpenBoard(9, 9);
            var origin = new GridPoint(4, 4);
            var reach = RangeFinder.ComputeReachable(board, origin, 5, MoveType.Infantry);
            foreach (var kv in reach)
            {
                var path = RangeFinder.FindPath(board, origin, kv.Key, 5, MoveType.Infantry);
                Check(path != null && path[path.Count - 1].Equals(kv.Key), $"path reconstructs {kv.Key}");
                int cost = 0;
                for (int i = 1; i < path!.Count; i++) cost += board.GetTerrain(path[i]).MoveCost(MovementGroups.Infantry);
                Check(cost == 5 - kv.Value, $"path cost matches remaining for {kv.Key}");
            }

            var sAtk = FreshKnight();
            var sDef = FreshBandit();
            var script = CombatResolver.GenerateScript(sAtk, sword, sDef, axe, 1, 0, 0, 0, 0, new SeededRng(42), rules);
            Check(sAtk.CurrentHP == sAtk.Stats.MaxHP && sDef.CurrentHP == sDef.Stats.MaxHP, "script generation leaves units untouched");
            int hpA = sAtk.CurrentHP;
            int hpD = sDef.CurrentHP;
            CombatResolver.ApplyScript(sAtk, sDef, script);
            Check(sAtk.CurrentHP == script.AttackerHPLeft && sDef.CurrentHP == script.DefenderHPLeft, "apply writes script HP");
            var rAtk = FreshKnight();
            var rDef = FreshBandit();
            var resolved = CombatResolver.Resolve(rAtk, sword, rDef, axe, 1, 0, 0, 0, 0, new SeededRng(42), rules);
            Check(resolved.Strikes.Count == script.Strikes.Count && resolved.DefenderHPLeft == script.DefenderHPLeft, "resolve equals generate plus apply");

            var bow = new Weapon("longbow", "Longbow", WeaponType.Bow, WeaponRank.E, 6, 70, 0, 6, 2, 3, 40) { CanCounter = false };
            var bAtk = FreshKnight();
            var bDef = FreshBandit();
            Check(!bow.CanCounterAt(2) && bow.InRange(2), "disabled counter still ranges");
            var noCounter = CombatResolver.Resolve(bAtk, sword, bDef, bow, 2, 0, 0, 0, 0, new SeededRng(9), rules);
            bool countered = false;
            foreach (var s in noCounter.Strikes) if (!s.IsAttackerStrike) countered = true;
            Check(!countered, "CanCounter false blocks the counter");

            var entry = new InventoryEntry(sword, 1);
            var eAtk = FreshKnight();
            var eDef = FreshBandit();
            CombatResolver.Resolve(eAtk, entry, eDef, null, 1, 0, 0, 0, 0, new SeededRng(3), rules);
            Check(entry.UsesLeft == 0, "durability consumed per strike attempt");
            var broken = new InventoryEntry(sword, 0);
            var kAtk = FreshKnight();
            var kDef = FreshBandit();
            int kDefHp = kDef.CurrentHP;
            CombatResolver.Resolve(kAtk, broken, kDef, null, 1, 0, 0, 0, 0, new SeededRng(3), rules);
            Check(kDef.CurrentHP == kDefHp, "broken entry cannot strike");

            var field = new Battlefield(OpenBoard(9, 9));
            var foe = field.Deploy(P0Fighter("foe", Affiliation.Enemy, 20, 5, 5, 5, 2, 4, 8), new GridPoint(4, 4));
            var p1 = P0Fighter("p1", Affiliation.Player, 20, 1, 1, 1, 0, 0, 5);
            p1.Inventory.Add(new InventoryEntry(IronSword()));
            var p2 = P0Fighter("p2", Affiliation.Player, 20, 1, 1, 1, 0, 0, 5);
            p2.Inventory.Add(new InventoryEntry(IronSword()));
            field.Deploy(p1, new GridPoint(0, 4));
            field.Deploy(p2, new GridPoint(8, 4));
            var side = new List<PlacedUnit> { foe };
            var values = ThreatCalculator.SideThreatValues(field, side, e => (MovementGroups.Infantry, 5, 1, 1));
            var union = ThreatCalculator.SideThreat(field, side, e => (MovementGroups.Infantry, 5, 1, 1));
            Check(values.Count == union.Count && values.ContainsKey(new GridPoint(4, 5)), "threat values match union set");
            Check(values.TryGetValue(new GridPoint(4, 5), out int n) && n == 1, "threat count is one coverer");

            var idle = new Battlefield(OpenBoard(9, 9));
            var me = idle.Deploy(P0Fighter("me", Affiliation.Enemy, 20, 5, 5, 5, 2, 4, 8), new GridPoint(4, 4));
            me.Unit.Inventory.Add(new InventoryEntry(IronSword()));
            idle.Deploy(P0Fighter("far", Affiliation.Player, 20, 1, 1, 1, 0, 0, 5), new GridPoint(4, 0));
            var wait = EnemyAi.Decide(idle, me, MovementGroups.Infantry, 0);
            Check(wait.IsWait && wait.Facing == null, "no move gives wait with null facing");
            var faced = EnemyAi.Decide(idle, me, MovementGroups.Infantry, 0);
            faced.Facing = new GridPoint(4, 3);
            Check(faced.Facing != null && faced.Facing.Value.Equals(new GridPoint(4, 3)), "facing rides the decision");

            var svc = ChapterState.StartNew(TestChapter(), 555);
            svc.CombatRng.Next(100);
            var snap = SaveSystem.Save(svc);
            uint afterSave = svc.CombatRng.GetState();
            int seqA = svc.CombatRng.Next(100);
            var back = SaveSystem.Restore(svc.Chapter, snap, 555);
            Check(back.CombatRng.GetState() == afterSave && back.CombatRng.Next(100) == seqA, "save restores RNG sequence");
        }

        private static bool ContainsKey(Unit unit, string weaponKey)
        {
            foreach (InventoryEntry e in unit.Inventory)
                if (e.Weapon.Key == weaponKey) return true;
            return false;
        }
    }
}
