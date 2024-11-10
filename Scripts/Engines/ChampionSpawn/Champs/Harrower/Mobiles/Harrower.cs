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

/* Scripts\Engines\ChampionSpawn\Champs\Harrower\Mobiles\Harrower.cs
 * ChangeLog:
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *  03/09/07, plasma    
 *      Removed cannedevil namespace reference
 *  01/05/07, plasma
 *      Changed CannedEvil namespace to ChampionSpawn for cleanup!
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	5/20/06, Pix
 *		Override new DamageEntryExpireTimeSeconds to be 10 minutes.
 *		Removed CoreAI.TempInt condition around new loot distribution.
 *		Removed old loot distribution code.
 *		Added distance check to loot distribution.
 *	5/15/06, Adam
 *		Improve rare drop chance slightly.
 *	4/08/06, Pix
 *		Coded new loot algorithm.  Still under (CoreAI.TempInt == 1) for test.
 *	3/23/06, Pix
 *		Put (CoreAI.TempInt == 1) around new loot distribution part until we can perfect it.
 *	3/19/06, Adam
 *		1. Add complete loot logging
 *		2. make sure the player is alive to get a reward
 *	3/19/06, Pix
 *		Changed algorithm to determine who gets loot in GiveMagicItems()
 *	3/18/06, Adam
 *		replace the 'special dye tub' colors for rare cloth with the best ore hues 
 *		basically vet rewards + a really dark 'evil cloth'
 *  11/14/05, erlein
 *		Fixed loot generation so it correctly randomizes accuracy and durability levels.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *	12/27/04, Pigpen
 *		Fixed problem with harrower's spawn points and gate location being reversed.
 *	12/24/04, Pigpen
 *		Removed Destard as possible dynamic spawn location.
 *		Added Shame level 2, Deceit level 2, and Despise level 3 to list of dynamic spawn points.
 *	12/18/05, Adam
 *		1. Remove Force/Hardening from the distribution
 *		2. Bump up gold to 90,000 - 100,000
 *		3. add "special dye tub" colored cloth
 *		4. add black colored cloth
 *		5. special hair/beard dye
 *		6. high-end magic clothing items
 *		7. increase dropped items to 24 (MaxGifts)
 *		8. Add potted plants
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	4/29/04, mith
 *		Removed Justice rewards now that virtues are disabled.
 *		Modified the amount of items given. Modified weapon rewards to also reward with armor.
 *	3/23/04 code changes by mith:
 *		OnBeforeDeath() - replaced GivePowerScrolls with GiveMagicItems
 *		GiveMagicItems() - new function to award players with magic items upon death of champion
 *		CreateWeapon()/CreateArmor() - called by GiveMagicItems to create random item to be awarded to player
 */

using Server.Diagnostics;
using Server.Commands;
using Server.Items;
using System;
using System.Collections;

namespace Server.Mobiles
{
    public class Harrower : BaseCreature
    {
        private bool m_TrueForm;
        private Item m_GateItem;
        private ArrayList m_Tentacles;
        private Timer m_Timer;
        private int MaxGifts = 24;      // number of gifts to award of each class

        //Damage Entries for the Harrower Expire in 10 minutes.
        public override double DamageEntryExpireTimeSeconds
        {
            get { return 10 * 60.0; }
        }

        private class SpawnEntry
        {
            public Point3D m_Location;
            public Point3D m_Entrance;

            public SpawnEntry(Point3D loc, Point3D ent)
            {
                m_Location = loc;
                m_Entrance = ent;
            }
        }

        private static SpawnEntry[] m_Entries = new SpawnEntry[]
            {
				//new SpawnEntry( new Point3D( 5284, 798, 0 ), new Point3D( 1176, 2638, 0 ) ), //Back of destard level 1. Removed at Jade's Request, Pigpen.
				new SpawnEntry( new Point3D( 5607, 17, 10 ), new Point3D( 513, 1562, 0 ) ), // Shame level
				new SpawnEntry( new Point3D( 5301, 606, 0 ), new Point3D( 4111, 433, 5 ) ), // Deceit level 2
				new SpawnEntry( new Point3D( 5606, 786, 60 ), new Point3D( 1299, 1081, 0 ) ) // Despise level 3
			};

        private static ArrayList m_Instances = new ArrayList();

        public static ArrayList Instances { get { return m_Instances; } }

        public static Harrower Spawn(Point3D platLoc, Map platMap)
        {
            if (m_Instances.Count > 0)
                return null;

            SpawnEntry entry = m_Entries[Utility.Random(m_Entries.Length)];

            Harrower harrower = new Harrower();

            harrower.MoveToWorld(entry.m_Location, Map.Felucca);

            harrower.m_GateItem = new HarrowerGate(harrower, platLoc, platMap, entry.m_Entrance, Map.Felucca);

            return harrower;
        }

        public static bool CanSpawn
        {
            get
            {
                return (m_Instances.Count == 0);
            }
        }

        [Constructable]
        public Harrower()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 18, 1, 0.25, 0.5)
        {
            m_Instances.Add(this);

            Name = "the harrower";
            BodyValue = 146;

            SetStr(900, 1000);
            SetDex(125, 135);
            SetInt(1000, 1200);

            SetFameLevel(5);
            SetKarmaLevel(5);

            VirtualArmor = 60;

            SetSkill(SkillName.Wrestling, 93.9, 96.5);
            SetSkill(SkillName.Tactics, 96.9, 102.2);
            SetSkill(SkillName.MagicResist, 131.4, 140.8);
            SetSkill(SkillName.Magery, 156.2, 161.4);
            SetSkill(SkillName.EvalInt, 100.0);
            SetSkill(SkillName.Meditation, 120.0);

            m_Tentacles = new ArrayList();

            m_Timer = new TeleportTimer(this);
            m_Timer.Start();
        }

        public override void GenerateLoot()
        {
            if (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.RenaissanceRules())
            {
                AddLoot(LootPack.SuperBoss, 2);
                AddLoot(LootPack.Meager);
            }
        }

        public override bool Unprovokable { get { return true; } }
        public override Poison PoisonImmune { get { return Poison.Lethal; } }

        private double[] m_Offsets = new double[]
            {
                Math.Cos( 000.0 / 180.0 * Math.PI ), Math.Sin( 000.0 / 180.0 * Math.PI ),
                Math.Cos( 040.0 / 180.0 * Math.PI ), Math.Sin( 040.0 / 180.0 * Math.PI ),
                Math.Cos( 080.0 / 180.0 * Math.PI ), Math.Sin( 080.0 / 180.0 * Math.PI ),
                Math.Cos( 120.0 / 180.0 * Math.PI ), Math.Sin( 120.0 / 180.0 * Math.PI ),
                Math.Cos( 160.0 / 180.0 * Math.PI ), Math.Sin( 160.0 / 180.0 * Math.PI ),
                Math.Cos( 200.0 / 180.0 * Math.PI ), Math.Sin( 200.0 / 180.0 * Math.PI ),
                Math.Cos( 240.0 / 180.0 * Math.PI ), Math.Sin( 240.0 / 180.0 * Math.PI ),
                Math.Cos( 280.0 / 180.0 * Math.PI ), Math.Sin( 280.0 / 180.0 * Math.PI ),
                Math.Cos( 320.0 / 180.0 * Math.PI ), Math.Sin( 320.0 / 180.0 * Math.PI ),
            };

        public void Morph()
        {
            if (m_TrueForm)
                return;

            m_TrueForm = true;

            Name = "the true harrower";
            BodyValue = 780;
            Hue = 0x497;

            Hits = HitsMax;
            Stam = StamMax;
            Mana = ManaMax;

            ProcessDelta();

            Say(1049499); // Behold my true form!

            Map map = this.Map;

            if (map != null)
            {
                for (int i = 0; i < m_Offsets.Length; i += 2)
                {
                    double rx = m_Offsets[i];
                    double ry = m_Offsets[i + 1];

                    int dist = 0;
                    bool ok = false;
                    int x = 0, y = 0, z = 0;

                    while (!ok && dist < 10)
                    {
                        int rdist = 10 + dist;

                        x = this.X + (int)(rx * rdist);
                        y = this.Y + (int)(ry * rdist);
                        z = map.GetAverageZ(x, y);

                        if (!(ok = map.CanFit(x, y, this.Z, 16, CanFitFlags.requireSurface)))
                            ok = map.CanFit(x, y, z, 16, CanFitFlags.requireSurface);

                        if (dist >= 0)
                            dist = -(dist + 1);
                        else
                            dist = -(dist - 1);
                    }

                    if (!ok)
                        continue;

                    BaseCreature spawn = new HarrowerTentacles(this);

                    spawn.Team = this.Team;

                    spawn.MoveToWorld(new Point3D(x, y, z), map);

                    m_Tentacles.Add(spawn);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int HitsMax { get { return m_TrueForm ? 65000 : 30000; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int ManaMax { get { return 5000; } }

        public Harrower(Serial serial)
            : base(serial)
        {
            m_Instances.Add(this);
        }

        public override void OnAfterDelete()
        {
            m_Instances.Remove(this);

            base.OnAfterDelete();
        }

        public override bool DisallowAllMoves { get { return m_TrueForm; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_TrueForm);
            writer.Write(m_GateItem);
            writer.WriteMobileList(m_Tentacles);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_TrueForm = reader.ReadBool();
                        m_GateItem = reader.ReadItem();
                        m_Tentacles = reader.ReadMobileList();

                        m_Timer = new TeleportTimer(this);
                        m_Timer.Start();

                        break;
                    }
            }
        }


        public override bool OnBeforeDeath()
        {
            if (m_TrueForm)
            {
                return base.OnBeforeDeath();
            }
            else
            {
                Morph();
                return false;
            }
        }

        public override void DistributedLoot()
        {
            if (!NoKillAwards)
            {
                GiveMagicItems();

                Map map = this.Map;

                if (map != null)
                {
                    for (int x = -4; x <= 4; ++x)
                    {
                        for (int y = -4; y <= 4; ++y)
                        {
                            double dist = Math.Sqrt(x * x + y * y);

                            if (dist <= 16)
                                new GoodiesTimer(map, X + x, Y + y).Start();
                        }
                    }
                }

                for (int i = 0; i < m_Tentacles.Count; ++i)
                {
                    Mobile m = (Mobile)m_Tentacles[i];

                    if (!m.Deleted)
                        m.Kill();
                }

                m_Tentacles.Clear();

                if (m_GateItem != null)
                    m_GateItem.Delete();
            }
        }

        private void GiveMagicItems()
        {
            ArrayList toGive = new ArrayList();

            LogHelper Logger = new LogHelper("HarrowerLoot.log", false);


            ArrayList allNonExpiredPMDamageEntries = new ArrayList();
            //New Looting method (Pix: 4/8/06)
            for (int i = 0; i < DamageEntries.Count; i++)
            {
                DamageEntry de = DamageEntries[i] as DamageEntry;
                if (de != null)
                {
                    Logger.Log(LogType.Text, string.Format("DE[{0}]: {1} ({2})", i, de.Damager, de.Damager != null ? de.Damager.Name : ""));
                    if (de.HasExpired)
                    {
                        Logger.Log(LogType.Text, string.Format("DE[{0}]: Expired", i));
                    }
                    else
                    {
                        if (de.Damager != null && !de.Damager.Deleted)
                        {
                            if (de.Damager is BaseCreature)
                            {
                                Logger.Log(LogType.Text, string.Format("DE[{0}]: BaseCreature", i));
                                BaseCreature bc = (BaseCreature)de.Damager;
                                if (bc.ControlMaster != null && !bc.ControlMaster.Deleted)
                                {
                                    //de.Damager = bc.ControlMaster;
                                    DamageEntry cmde = new DamageEntry(bc.ControlMaster);
                                    cmde.DamageGiven = de.DamageGiven;
                                    de = cmde;
                                    Logger.Log(LogType.Text, string.Format("DE[{0}]: New Damager: {1}", i, de.Damager.Name));
                                }
                            }

                            if (de.Damager is PlayerMobile)
                            {
                                Logger.Log(LogType.Text, string.Format("DE[{0}]: PlayerMobile", i));

                                if (de.Damager.Alive)
                                {
                                    Logger.Log(LogType.Text, string.Format("DE[{0}]: PM Alive", i));

                                    bool bFound = false;
                                    for (int j = 0; j < allNonExpiredPMDamageEntries.Count; j++)
                                    {
                                        DamageEntry de2 = (DamageEntry)allNonExpiredPMDamageEntries[j];
                                        if (de2.Damager == de.Damager)
                                        {
                                            Logger.Log(LogType.Text, string.Format("DE[{0}]: PM Found, adding damage", i));

                                            de2.DamageGiven += de.DamageGiven;
                                            bFound = true;
                                            break;
                                        }
                                    }

                                    if (!bFound)
                                    {
                                        Logger.Log(LogType.Text, string.Format("DE[{0}]: PM not found, adding", i));
                                        allNonExpiredPMDamageEntries.Add(de);
                                    }
                                }
                            }

                        }
                    }
                }
            }

            //Remove any PMs that are over 100 tiles away
            ArrayList toRemove = new ArrayList();
            for (int i = 0; i < allNonExpiredPMDamageEntries.Count; i++)
            {
                DamageEntry de = (DamageEntry)allNonExpiredPMDamageEntries[i];
                if (de.Damager.GetDistanceToSqrt(this.Location) > 100)
                {
                    Logger.Log(LogType.Text, string.Format("Removing {0} for being too far away at death", de.Damager.Name));
                    toRemove.Add(allNonExpiredPMDamageEntries[i]);
                }
            }
            for (int i = 0; i < toRemove.Count; i++)
            {
                allNonExpiredPMDamageEntries.Remove(toRemove[i]);
            }

            int topDamage = 0;
            int minDamage = 0;
            for (int i = 0; i < allNonExpiredPMDamageEntries.Count; i++)
            {
                DamageEntry de = (DamageEntry)allNonExpiredPMDamageEntries[i];
                if (de.DamageGiven > topDamage) topDamage = de.DamageGiven;

                Logger.Log(LogType.Text, string.Format("Non-Expired[{0}]: {1} (damage: {2})", i, de.Damager.Name, de.DamageGiven));

            }

            //Now filter on 'enough' damage
            if (HitsMax >= 3000) minDamage = topDamage / 16;
            else if (HitsMax >= 1000) minDamage = topDamage / 8;
            else if (HitsMax >= 200) minDamage = topDamage / 4;
            else minDamage = topDamage / 2;

            Logger.Log(LogType.Text, string.Format("HitsMax: {0}, TopDamage: {1}, MinDamage: {2}", HitsMax, topDamage, minDamage));


            for (int i = 0; i < allNonExpiredPMDamageEntries.Count; i++)
            {
                DamageEntry de = (DamageEntry)allNonExpiredPMDamageEntries[i];
                if (de.DamageGiven >= minDamage)
                {
                    toGive.Add(de.Damager);
                }
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

            Logger.Log(LogType.Text, ""); // new line
            Logger.Log(LogType.Text, "Randomized list of players:");
            for (int i = 0; i < toGive.Count; ++i)
            {
                Mobile mob = toGive[i] as Mobile;
                Logger.Log(LogType.Mobile, mob, "alive:" + mob.Alive.ToString());
            }

            Logger.Log(LogType.Text, ""); // new line
            Logger.Log(LogType.Text, "Begin loot distribution: Who/What:");

            // Loop goes until we've generated MaxGifts items.
            for (int i = 0; i < MaxGifts; ++i)
            {
                Item reward = null;
                Mobile m = (Mobile)toGive[i % toGive.Count];

                switch (Utility.Random(10))
                {
                    case 0:         // Power/Vanq Weapon
                    case 1:
                    case 2: // 3 in 10 chance	
                        {   // 33% chance at best
                            reward = CreateWeapon((0.32 >= Utility.RandomDouble()) ? 5 : 4);
                            break;
                        }
                    case 3:         // Fort/Invul Armor
                    case 4:
                    case 5: // 3 in 10 chance 
                        {   // 33% chance at best
                            reward = CreateArmor((0.32 >= Utility.RandomDouble()) ? 5 : 4);
                            break;
                        }
                    case 6:     // hair/beard dye
                        {       // 1 in 10 chance
                            if (Utility.RandomBool())
                                reward = new SpecialHairDye();
                            else
                                reward = new SpecialBeardDye();
                            break;
                        }
                    case 7:     // special cloth
                        {       // 1 in 10 chance
                            reward = new UncutCloth(50);
                            if (Utility.RandomBool())
                                // best ore hues (vet rewards) + really dark 'evil cloth'
                                reward.Hue = Utility.RandomList(2213, 2219, 2207, 2425, 1109);
                            else
                                reward.Hue = 0x01;  // black cloth
                            break;
                        }

                    case 8:     // potted plant
                        {       // 1 in 10 chance
                            switch (Utility.Random(11))
                            {
                                default:    // should never happen
                                case 0: reward = new PottedCactus(); break;
                                case 1: reward = new PottedCactus1(); break;
                                case 2: reward = new PottedCactus2(); break;
                                case 3: reward = new PottedCactus3(); break;
                                case 4: reward = new PottedCactus4(); break;
                                case 5: reward = new PottedCactus5(); break;
                                case 6: reward = new PottedPlant(); break;
                                case 7: reward = new PottedPlant1(); break;
                                case 8: reward = new PottedPlant2(); break;
                                case 9: reward = new PottedTree(); break;
                                case 10: reward = new PottedTree1(); break;
                            }
                            break;
                        }

                    default:    // Should never happen
                    /* fall through*/

                    case 9:     // Magic Item Drop
                        {           // 1 in 10 chance
                            reward = Loot.RandomClothingOrJewelry();
                            if (reward != null)
                            {
                                int minLevel = 3;
                                int maxLevel = 3;
                                if (reward is BaseClothing)
                                    ((BaseClothing)reward).SetRandomMagicEffect(minLevel, maxLevel);
                                else if (reward is BaseJewel)
                                    ((BaseJewel)reward).SetRandomMagicEffect(minLevel, maxLevel);
                            }
                            break;
                        }
                }

                if (reward != null)
                {
                    // Drop the new weapon into their backpack and send them a message.
                    m.SendMessage("You have received a special item!");
                    m.AddToBackpack(reward);

                    Logger.Log(LogType.Mobile, m, "alive:" + m.Alive.ToString());
                    Logger.Log(LogType.Item, reward, string.Format("Hue:{0}:Rare:{1}",
                        reward.Hue,
                        (reward is BaseWeapon || reward is BaseArmor || reward is BaseClothing || reward is BaseJewel) ? "False" : "True"));
                }
            }

            // done logging
            Logger.Finish();
        }

        private BaseWeapon CreateWeapon(int level)
        {
            BaseWeapon weapon = Loot.RandomWeapon();

            if (0.05 > Utility.RandomDouble())
                weapon.Slayer = SlayerName.Silver;

            weapon.DamageLevel = (WeaponDamageLevel)level;
            weapon.AccuracyLevel = (WeaponAccuracyLevel)RandomMinMaxScaled(3, 5);
            weapon.DurabilityLevel = (WeaponDurabilityLevel)RandomMinMaxScaled(3, 5);

            return (weapon);
        }

        private BaseArmor CreateArmor(int level)
        {
            BaseArmor armor = Loot.RandomArmorOrShield();

            armor.ProtectionLevel = (ArmorProtectionLevel)level;
            armor.Durability = (ArmorDurabilityLevel)RandomMinMaxScaled(3, 5);

            return (armor);
        }
        private class TeleportTimer : Timer
        {
            private Mobile m_Owner;

            private static int[] m_Offsets = new int[]
            {
                -1, -1,
                -1,  0,
                -1,  1,
                0, -1,
                0,  1,
                1, -1,
                1,  0,
                1,  1
            };

            public TeleportTimer(Mobile owner)
                : base(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(5.0))
            {
                m_Owner = owner;
            }

            protected override void OnTick()
            {
                if (m_Owner.Deleted)
                {
                    Stop();
                    return;
                }

                Map map = m_Owner.Map;

                if (map == null)
                    return;

                if (0.25 < Utility.RandomDouble())
                    return;

                Mobile toTeleport = null;

                IPooledEnumerable eable = m_Owner.GetMobilesInRange(16);
                foreach (Mobile m in eable)
                {
                    if (m != m_Owner && m.Player && m_Owner.CanBeHarmful(m) && m_Owner.CanSee(m))
                    {
                        toTeleport = m;
                        break;
                    }
                }
                eable.Free();

                if (toTeleport != null)
                {
                    int offset = Utility.Random(8) * 2;

                    Point3D to = m_Owner.Location;

                    for (int i = 0; i < m_Offsets.Length; i += 2)
                    {
                        int x = m_Owner.X + m_Offsets[(offset + i) % m_Offsets.Length];
                        int y = m_Owner.Y + m_Offsets[(offset + i + 1) % m_Offsets.Length];

                        if (map.CanSpawnMobile(x, y, m_Owner.Z))
                        {
                            to = new Point3D(x, y, m_Owner.Z);
                            break;
                        }
                        else
                        {
                            int z = map.GetAverageZ(x, y);

                            if (map.CanSpawnMobile(x, y, z))
                            {
                                to = new Point3D(x, y, z);
                                break;
                            }
                        }
                    }

                    Mobile m = toTeleport;

                    Point3D from = m.Location;

                    m.Location = to;

                    Server.Spells.SpellHelper.Turn(m_Owner, toTeleport);
                    Server.Spells.SpellHelper.Turn(toTeleport, m_Owner);

                    m.ProcessDelta();

                    Effects.SendLocationParticles(EffectItem.Create(from, m.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);
                    Effects.SendLocationParticles(EffectItem.Create(to, m.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 5023);

                    m.PlaySound(0x1FE);

                    m_Owner.Combatant = toTeleport;
                }
            }
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

                // Adam: 90,000 - 100,000
                Gold g = new Gold(1111, 1234);

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
