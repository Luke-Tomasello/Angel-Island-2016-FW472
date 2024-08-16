/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shard   : March 25, 2004
 *   portions copyright      : (C) 2004-2024 Tomasello Software LLC.
 *   email                   : luke@tomasello.com
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

/* Scripts/Misc/ChampLoot.cs
 * ChangeLog
 *  3/15/08, Adam 
 *      Initial Creation
 *      Moved the Champ Loot generation from BasChampion to here for generic use.
 *		(building champ level creatures that are not BasChampion)
 */

using Server.Items;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server
{

    public class ChampLootPack
    {
        private Mobile m_mobile;
        int m_X;
        int m_Y;
        public ChampLootPack(Mobile m)
        {
            m_mobile = m;
            m_X = m.X;
            m_Y = m.Y;
        }
        public ChampLootPack(Mobile m, int X, int Y)
        {
            m_mobile = m;
            m_X = X;
            m_Y = Y;
        }

        public virtual void DistributedLoot()
        {
            if (m_mobile is BaseCreature && !(m_mobile as BaseCreature).NoKillAwards)
            {
                GiveMagicItems();

                Map map = m_mobile.Map;

                if (map != null)
                {
                    for (int x = -2; x <= 2; ++x)
                    {
                        for (int y = -2; y <= 2; ++y)
                        {
                            double dist = Math.Sqrt(x * x + y * y);

                            if (dist <= 12)
                                new GoodiesTimer(map, m_X + x, m_Y + y).Start();
                        }
                    }
                }
            }

        }

        private void GiveMagicItems()
        {
            ArrayList toGive = new ArrayList();

            List<AggressorInfo> list = m_mobile.Aggressors;
            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)list[i];

                if (info.Attacker.Player && info.Attacker.Alive && (DateTime.Now - info.LastCombatTime) < TimeSpan.FromSeconds(30.0) && !toGive.Contains(info.Attacker))
                    toGive.Add(info.Attacker);
            }

            list = m_mobile.Aggressed;
            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)list[i];

                if (info.Defender.Player && info.Defender.Alive && (DateTime.Now - info.LastCombatTime) < TimeSpan.FromSeconds(30.0) && !toGive.Contains(info.Defender))
                    toGive.Add(info.Defender);
            }

            if (toGive.Count == 0)
                return;

            // Randomize
            for (int i = 0; i < toGive.Count; ++i)
            {
                int rand = Utility.Random(toGive.Count);
                object hold = toGive[i];
                toGive[i] = toGive[rand];
                toGive[rand] = hold;
            }

            for (int i = 0; i < CoreChamp.AmountOfMagicLoot; ++i)
            {
                int level;
                double random = Utility.RandomDouble();

                if (0.1 >= random)
                    level = CoreChamp.MaxMagicDropLevel;    // Vanquishing Weapons/Invulnerability Armor
                else if (0.4 >= random)
                    level = (CoreChamp.MinMagicDropLevel + CoreChamp.MaxMagicDropLevel) / 2;    // Power Weapons/Fortification Armor
                else
                    level = CoreChamp.MinMagicDropLevel;    // Force Weapons/Hardening Armor

                Mobile m = (Mobile)toGive[i % toGive.Count];

                Item reward;
                if (Utility.RandomBool())
                    reward = CreateWeapon(level);
                else
                    reward = CreateArmor(level);

                if (reward != null)
                {
                    m.SendMessage("You have received a magic item!");
                    m.AddToBackpack(reward);
                }
            }
        }

        private BaseWeapon CreateWeapon(int level)
        {
            BaseWeapon weapon = Loot.RandomWeapon();

            if (0.05 > Utility.RandomDouble())
                weapon.Slayer = SlayerName.Silver;

            weapon.DamageLevel = (WeaponDamageLevel)level;
            weapon.AccuracyLevel = (WeaponAccuracyLevel)BaseCreature.RandomMinMaxScaled(0, 5);
            weapon.DurabilityLevel = (WeaponDurabilityLevel)BaseCreature.RandomMinMaxScaled(0, 5);

            return (weapon);
        }

        private BaseArmor CreateArmor(int level)
        {
            BaseArmor armor = Loot.RandomArmorOrShield();

            armor.ProtectionLevel = (ArmorProtectionLevel)level;
            armor.Durability = (ArmorDurabilityLevel)BaseCreature.RandomMinMaxScaled(0, 5);

            return (armor);
        }

        private class GoodiesTimer : Timer
        {
            private Map m_Map;
            private int m_X, m_Y;

            public GoodiesTimer(Map map, int x, int y)
                : base(TimeSpan.FromSeconds(Utility.RandomDouble() * 10.0))
            {
                m_Map = map;
                m_X = x;
                m_Y = y;
            }

            protected override void OnTick()
            {
                int z = m_Map.GetAverageZ(m_X, m_Y);
                bool canFit = m_Map.CanFit(m_X, m_Y, z, 6, CanFitFlags.requireSurface);

                for (int i = -3; !canFit && i <= 3; ++i)
                {
                    canFit = m_Map.CanFit(m_X, m_Y, z + i, 6, CanFitFlags.requireSurface);

                    if (canFit)
                        z += i;
                }

                if (!canFit)
                    return;

                // Adam: 25 piles * 1200 = 30K gold
                Gold g = new Gold(800, 1200);

                g.MoveToWorld(new Point3D(m_X, m_Y, z), m_Map);

                if (0.5 >= Utility.RandomDouble())
                {
                    switch (Utility.Random(3))
                    {
                        case 0: // Fire column
                            {
                                Effects.SendLocationParticles(EffectItem.Create(g.Location, g.Map, EffectItem.DefaultDuration), 0x3709, 10, 30, 5052);
                                Effects.PlaySound(g, g.Map, 0x208);

                                break;
                            }
                        case 1: // Explosion
                            {
                                Effects.SendLocationParticles(EffectItem.Create(g.Location, g.Map, EffectItem.DefaultDuration), 0x36BD, 20, 10, 5044);
                                Effects.PlaySound(g, g.Map, 0x307);

                                break;
                            }
                        case 2: // Ball of fire
                            {
                                Effects.SendLocationParticles(EffectItem.Create(g.Location, g.Map, EffectItem.DefaultDuration), 0x36FE, 10, 10, 5052);

                                break;
                            }
                    }
                }
            }
        }

    }

}
