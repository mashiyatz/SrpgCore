using System;
using SrpgCore.Data;

namespace SrpgCore.Combat
{
    /// <summary>
    /// Weapon-triangle (sword &gt; axe &gt; lance &gt; sword; anima &gt; light &gt; dark &gt; anima).
    /// Bows and staves sit outside the triangle. Bonuses are injectable (GBA: +1 Mt, +15 Hit).
    /// Reaver weapons (Weapon.ReversesTriangle) swap advantage for their wielder.
    /// </summary>
    public static class WeaponTriangle
    {
        /// <summary>Rates triangle advantage: 1 for advantage, -1 for disadvantage, 0 otherwise.</summary>
        /// <param name="attacker">The attacking weapon type.</param>
        /// <param name="defender">The defending weapon type.</param>
        /// <returns>1 when the attacker wins, -1 when the defender wins, else 0.</returns>
        public static int Advantage(WeaponType attacker, WeaponType defender)
        {
            if (Beats(attacker, defender)) return 1;
            if (Beats(defender, attacker)) return -1;
            return 0;
        }

        /// <summary>Rates triangle advantage respecting reaver-weapon reversal.</summary>
        /// <param name="attacker">The attacking weapon (ReversesTriangle swaps advantage).</param>
        /// <param name="defender">The defending weapon (ReversesTriangle swaps advantage).</param>
        /// <returns>1 when the attacker wins, -1 when the defender wins, else 0, with reaver swap applied.</returns>
        public static int Advantage(Weapon attacker, Weapon defender)
        {
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (defender == null) throw new ArgumentNullException(nameof(defender));
            int baseAdv = Advantage(attacker.Type, defender.Type);
            if (baseAdv == 0) return 0;
            bool rev = attacker.ReversesTriangle ^ defender.ReversesTriangle;
            return rev ? -baseAdv : baseAdv;
        }

        private static bool Beats(WeaponType a, WeaponType d)
        {
            return (a == WeaponType.Sword && d == WeaponType.Axe)
                || (a == WeaponType.Axe && d == WeaponType.Lance)
                || (a == WeaponType.Lance && d == WeaponType.Sword)
                || (a == WeaponType.Anima && d == WeaponType.Light)
                || (a == WeaponType.Light && d == WeaponType.Dark)
                || (a == WeaponType.Dark && d == WeaponType.Anima);
        }
    }
}
