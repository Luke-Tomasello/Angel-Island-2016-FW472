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

/* Scripts\Engines\ChampionSpawn\Champs\Seasonal\Spring\Mobiles\PirateChamp.cs	
 * ChangeLog:
 *	7/9/10, adam
 *		o Merge pirate class hierarchy (all pirates are now derived from class Pirate)
 *	1/1/09, Adam
 *		- Add potions and bandages
 *			Now uses real potions and real bandages
 *		- Cross heals is now turned off
 *		- Smart AI upgrade (adds healing with bandages)
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *  3/22/07, Adam
 *      Created; based largely on Neira stats
 */

using Server.Commands;
using Server.Items;
using System;
using System.Collections;

namespace Server.Mobiles
{
    [CorpseName("corpse of a salty seadog")]
    public class PirateChamp : Pirate
    {
        private MetalChest m_MetalChest = null;
        public override int TreasureMapLevel { get { return Core.UOAI || Core.UOREN ? 5 : 0; } }

        [Constructable]
        public PirateChamp()
            : base(AIType.AI_Hybrid)
        {
            BardImmune = true;
        }

        public override void InitClass()
        {

            SetStr(305, 425);
            SetDex(72, 150);
            SetInt(505, 750);

            SetHits(4800);
            SetStam(102, 300);

            VirtualArmor = 30;

            SetDamage(25, 35);

            SetSkill(SkillName.EvalInt, 120.0);
            SetSkill(SkillName.Magery, 120.0);
            SetSkill(SkillName.Meditation, 120.0);
            SetSkill(SkillName.MagicResist, 150.0);
            SetSkill(SkillName.Swords, 97.6, 100.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 97.6, 100.0);
            SetSkill(SkillName.Healing, 97.6, 100.0);

            Fame = 22500;
            Karma = -22500;

        }

        public PirateChamp(Serial serial)
            : base(serial)
        {
        }

        public override void InitBody()
        {
            base.InitBody();
            Title = "the hoard guardian";
        }

        public override void InitOutfit()
        {
            base.InitOutfit();
            Item hat = FindItemOnLayer(Layer.Helm);
            if (hat != null)
                hat.Delete();

            AddItem(CaptainsHat("a pirate hat"));
        }

        public override void OnDamagedBySpell(Mobile caster)
        {
            if (caster == this)
                return;

            // Adam: 12% chance to spawn a Bone Knight
            if (Utility.RandomChance(12))
                SpawnBoneKnight(caster);
        }

        public void SpawnBoneKnight(Mobile caster)
        {
            Mobile target = caster;

            if (Map == null || Map == Map.Internal)
                return;

            int helpers = 0;
            ArrayList mobs = new ArrayList();
            IPooledEnumerable eable = this.GetMobilesInRange(10);
            foreach (Mobile m in eable)
            {
                if (m is BoneKnight)
                    ++helpers;

                if (m is PlayerMobile && m.Alive == true && m.Hidden == false && m.AccessLevel <= AccessLevel.Player)
                    mobs.Add(m);
            }
            eable.Free();

            if (helpers < 5)
            {
                BaseCreature helper = new BoneKnight();

                helper.Team = this.Team;
                helper.Map = Map;

                bool validLocation = false;

                // pick a random player to focus on
                //  if there are no players, we will stay with the caster
                if (mobs.Count > 0)
                    target = mobs[Utility.Random(mobs.Count)] as Mobile;

                for (int j = 0; !validLocation && j < 10; ++j)
                {
                    int x = target.X + Utility.Random(3) - 1;
                    int y = target.Y + Utility.Random(3) - 1;
                    int z = Map.GetAverageZ(x, y);

                    if (validLocation = Map.CanFit(x, y, this.Z, 16, CanFitFlags.requireSurface))
                        helper.Location = new Point3D(x, y, Z);
                    else if (validLocation = Map.CanFit(x, y, z, 16, CanFitFlags.requireSurface))
                        helper.Location = new Point3D(x, y, z);
                }

                if (!validLocation)
                    helper.Location = target.Location;

                helper.Combatant = target;
            }
        }

        public override void GenerateLoot()
        {
            // build 'hoard' loot
            BuildChest();
        }

        public void BuildChest()
        {
            m_MetalChest = new MetalChest();
            m_MetalChest.Name = "Dead Man's Chest";
            m_MetalChest.Hue = Utility.RandomMetalHue();
            m_MetalChest.Movable = false;

            // level 5 chest logic
            m_MetalChest.RequiredSkill = 100;
            m_MetalChest.LockLevel = m_MetalChest.RequiredSkill - 10;
            m_MetalChest.MaxLockLevel = m_MetalChest.RequiredSkill + 40;

            // reset the trap
            m_MetalChest.TrapEnabled = true;
            m_MetalChest.TrapPower = 5 * 25;    // level 5
            m_MetalChest.TrapLevel = 5;
            m_MetalChest.Locked = true;
            m_MetalChest.TrapType = Utility.RandomBool() ? TrapType.PoisonTrap : TrapType.ExplosionTrap;

            // setup timmed release logic
            string[] lines = new string[4];
            lines[0] = "Movable true";
            lines[1] = "TrapEnabled false";
            lines[2] = "TrapPower 0";
            lines[3] = "Locked true";

            // the chest will become movable in 10-25 minutes
            DateTime SetTime = DateTime.UtcNow + TimeSpan.FromMinutes(Utility.RandomMinMax(10, 25));
            new TimedSet(m_MetalChest, SetTime, lines).MoveItemToIntStorage();

            // add loot
            FillChest();

            // move the chest to world;
            m_MetalChest.MoveToWorld(Location, Map);

        }

        private void FillChest()
        {
            int RaresDropped = 0;
            LogHelper Logger = new LogHelper("PirateChampChest.log", false);

            // 25 piles * 1200 = 30K gold
            for (int ix = 0; ix < 25; ix++)
            {   // force the separate piles
                Gold gold = new Gold(800, 1200);
                gold.Stackable = false;
                m_MetalChest.DropItem(gold);
                gold.Stackable = true;
            }

            // "a smelly old mackerel"
            if (Utility.RandomChance(10))
            {
                Item ii;
                ii = new BigFish();
                ii.Name = "a smelly old mackerel";
                ii.Weight = 5;
                m_MetalChest.DropItem(ii);
                RaresDropped++;
                Logger.Log(LogType.Item, ii);
            }

            // single gold ingot weight 12
            if (Utility.RandomChance(10 * 2))
            {
                Item ii;
                if (Utility.RandomBool())
                    ii = new Item(7145);
                else
                    ii = new Item(7148);

                ii.Weight = 12;
                m_MetalChest.DropItem(ii);
                RaresDropped++;
                Logger.Log(LogType.Item, ii);
            }

            // 3 gold ingots 12*3
            if (Utility.RandomChance(5 * 2))
            {
                Item ii;
                if (Utility.RandomBool())
                    ii = new Item(7146);
                else
                    ii = new Item(7149);

                ii.Weight = 12 * 3;
                m_MetalChest.DropItem(ii);
                RaresDropped++;
                Logger.Log(LogType.Item, ii);
            }

            // 5 gold ingots 12*5
            if (Utility.RandomChance(1 * 2))
            {
                Item ii;
                if (Utility.RandomBool())
                    ii = new Item(7147);
                else
                    ii = new Item(7150);

                ii.Weight = 12 * 5;
                m_MetalChest.DropItem(ii);
                RaresDropped++;
                Logger.Log(LogType.Item, ii);
            }

            // single silver ingot weight 6
            if (Utility.RandomChance(10 * 2))
            {
                Item ii;
                if (Utility.RandomBool())
                    ii = new Item(7157);
                else
                    ii = new Item(7160);

                ii.Weight = 6;
                m_MetalChest.DropItem(ii);
                RaresDropped++;
                Logger.Log(LogType.Item, ii);
            }

            // 3 silver ingots 6*3
            if (Utility.RandomChance(5 * 2))
            {
                Item ii;
                if (Utility.RandomBool())
                    ii = new Item(7158);
                else
                    ii = new Item(7161);

                ii.Weight = 6 * 3;
                m_MetalChest.DropItem(ii);
                RaresDropped++;
                Logger.Log(LogType.Item, ii);
            }

            // 5 silver ingots 6*5
            if (Utility.RandomChance(1 * 2))
            {
                Item ii;
                if (Utility.RandomBool())
                    ii = new Item(7159);
                else
                    ii = new Item(7162);

                ii.Weight = 6 * 5;
                m_MetalChest.DropItem(ii);
                RaresDropped++;
                Logger.Log(LogType.Item, ii);
            }

            // rolled map w1
            if (Utility.RandomChance(20 * 2))
            {
                Item ii;
                if (Utility.RandomBool())
                    ii = new Item(5357);
                else
                    ii = new Item(5358);

                ii.Weight = 1;
                m_MetalChest.DropItem(ii);
                RaresDropped++;
                Logger.Log(LogType.Item, ii);
            }

            // ship plans
            if (Utility.RandomChance(10 * 2))
            {
                Item ii;
                if (Utility.RandomBool())
                    ii = new Item(5361);
                else
                    ii = new Item(5362);

                ii.Weight = 1;
                m_MetalChest.DropItem(ii);
                RaresDropped++;
                Logger.Log(LogType.Item, ii);
            }

            // ship model
            if (Utility.RandomChance(5 * 2))
            {
                Item ii;
                if (Utility.RandomBool())
                    ii = new Item(5363);
                else
                    ii = new Item(5364);

                ii.Weight = 3;
                m_MetalChest.DropItem(ii);
                RaresDropped++;
                Logger.Log(LogType.Item, ii);
            }

            // "scale shield" w6
            if (Utility.RandomChance(1))
            {
                Item ii;
                if (Utility.RandomBool())
                    ii = new Item(7110);
                else
                    ii = new Item(7111);

                ii.Name = "scale shield";
                ii.Weight = 6;
                m_MetalChest.DropItem(ii);
                RaresDropped++;
                Logger.Log(LogType.Item, ii);
            }

            // level 5 chest regs & gems
            TreasureMapChest.PackRegs(m_MetalChest, 5 * 10);
            TreasureMapChest.PackGems(m_MetalChest, 5 * 5);

            // level 5 magic items
            DungeonTreasureChest.PackMagicItem(m_MetalChest, 3, 3, 0.20);
            DungeonTreasureChest.PackMagicItem(m_MetalChest, 3, 3, 0.10);
            DungeonTreasureChest.PackMagicItem(m_MetalChest, 3, 3, 0.05);

            // an a level 5 treasure map
            m_MetalChest.DropItem(new TreasureMap(5, Map.Felucca));

            Logger.Log(LogType.Text, string.Format("There were a total of {0} rares dropped.", RaresDropped));
            Logger.Finish();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            if (base.Version == 0)
                return;

            int version = reader.ReadInt();

        }
    }
}
