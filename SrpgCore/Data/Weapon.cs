using System;
using System.Collections.Generic;

namespace SrpgCore.Data
{
    /// <summary>
    /// Weapon definition (database entry): Key, Name, Type, Rank (E-S gate via
    /// WeaponRanks), combat numbers (Might, Hit, Crit, Weight, MinRange/MaxRange in
    /// Manhattan tiles, MaxUses durability), and flags: IsMagic (targets Res),
    /// EffectiveAgainst class keys, HealPower and StatusEffect/StatusDuration for
    /// staves. Instances with durability live on Unit as InventoryEntry values;
    /// InRange tests a distance, IsStaff and IsEffectiveAgainst read the flags.
    /// </summary>
    public sealed class Weapon
    {
        /// <summary>Database key referenced by chapter armory and unit items lists.</summary>
        public string Key { get; }
        /// <summary>Display name for menus and inventory screens.</summary>
        public string Name { get; }
        /// <summary>Damage family driving triangle bonus and magic selection.</summary>
        public WeaponType Type { get; }
        /// <summary>Minimum rank letter required to equip this weapon.</summary>
        public WeaponRank Rank { get; }
        /// <summary>Base damage added before triangle and effectiveness bonuses.</summary>
        public int Might { get; }
        /// <summary>Base accuracy before skill, luck, and terrain avoid.</summary>
        public int Hit { get; }
        /// <summary>Base critical rate before skill bonus.</summary>
        public int Crit { get; }
        /// <summary>Heft slowing attack speed after constitution relief.</summary>
        public int Weight { get; }
        /// <summary>Minimum Manhattan attack distance.</summary>
        public int MinRange { get; }
        /// <summary>Maximum Manhattan attack distance.</summary>
        public int MaxRange { get; }
        /// <summary>Full durability for fresh inventory entries.</summary>
        public int MaxUses { get; }
        /// <summary>True when damage targets resistance instead of defense.</summary>
        public bool IsMagic { get; }
        /// <summary>True when this weapon can be used to counterattack (default true; set false for long-range siege or pacifist staves).</summary>
        public bool CanCounter { get; set; } = true;

        /// <summary>Class keys or tags this weapon is effective against (armor, flier, ...). Empty = none.</summary>
        public List<string> EffectiveAgainst { get; } = new List<string>();

        /// <summary>Staff heal power (&gt;0 = healing staff, uses Might-style base). 0 = damaging weapon.</summary>
        public int HealPower { get; set; }

        /// <summary>Status staff effect name (sleep/silence/...) applied on hit. "" = none.</summary>
        public string StatusEffect { get; set; } = "";

        /// <summary>Duration in turns for StatusEffect.</summary>
        public int StatusDuration { get; set; }

        /// <summary>True when this weapon can be stolen (light items; heavy/legendary set false).</summary>
        public bool CanSteal { get; set; } = true;

        /// <summary>True when this weapon reverses weapon-triangle advantage (reaver weapons, default false).</summary>
        public bool ReversesTriangle { get; set; } = false;

        /// <summary>Shop price in gold. 0 = not for sale (spawn pool only).</summary>
        public int Price { get; set; }

        /// <summary>True for staff type or any healing or status payload (legacy broad check).</summary>
        public bool IsStaff => Type == WeaponType.Staff || HealPower > 0 || StatusEffect.Length > 0;

        /// <summary>True when HealPower is positive (healing staff payload).</summary>
        public bool IsHealStaff => HealPower > 0;

        /// <summary>True when StatusEffect is set (status staff payload).</summary>
        public bool IsStatusStaff => StatusEffect.Length > 0;

        /// <summary>Defines one weapon database entry with combat and durability stats.</summary>
        /// <param name="key">Database key referenced by armory rows.</param>
        /// <param name="name">Display name for menus.</param>
        /// <param name="type">Damage family driving triangle and magic selection.</param>
        /// <param name="rank">Minimum rank letter required to equip.</param>
        /// <param name="might">Base damage.</param>
        /// <param name="hit">Base accuracy.</param>
        /// <param name="crit">Base critical rate.</param>
        /// <param name="weight">Heft slowing attack speed.</param>
        /// <param name="minRange">Minimum Manhattan attack distance.</param>
        /// <param name="maxRange">Maximum Manhattan attack distance.</param>
        /// <param name="maxUses">Full durability for fresh entries.</param>
        /// <param name="isMagic">True when damage targets resistance.</param>
        public Weapon(string key, string name, WeaponType type, WeaponRank rank,
            int might, int hit, int crit, int weight, int minRange, int maxRange, int maxUses, bool isMagic = false)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type;
            Rank = rank;
            Might = might;
            Hit = hit;
            Crit = crit;
            Weight = weight;
            MinRange = minRange;
            MaxRange = maxRange;
            MaxUses = maxUses;
            IsMagic = isMagic;
        }

        /// <summary>Tests whether a Manhattan distance falls inside the weapon range.</summary>
        /// <param name="distance">Manhattan distance between attacker and target.</param>
        /// <returns>True when distance sits between MinRange and MaxRange inclusive.</returns>
        public bool InRange(int distance) => distance >= MinRange && distance <= MaxRange;

        /// <summary>Tests whether this weapon can counterattack at a distance.</summary>
        /// <param name="distance">Manhattan distance of the exchange.</param>
        /// <returns>True when countering is enabled and distance is in range.</returns>
        public bool CanCounterAt(int distance) => CanCounter && InRange(distance);

        /// <summary>Tests effectiveness bonus against a defender class key.</summary>
        /// <param name="defenderClassKey">Defender class key or tag, null never matches.</param>
        /// <returns>True when the key matches an EffectiveAgainst entry ignoring case.</returns>
        public bool IsEffectiveAgainst(string? defenderClassKey)
        {
            if (defenderClassKey == null || EffectiveAgainst.Count == 0) return false;
            foreach (string tag in EffectiveAgainst)
                if (string.Equals(tag, defenderClassKey, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
