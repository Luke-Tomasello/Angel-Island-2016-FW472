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

/* Scripts\Engines\ClanSystem\Mobiles\ClanOrc.cs
 * ChangeLog
 *	2/8/11, Adam
 *      Initial version - base off Orc Captain + brigand weapons
 */

using Server.Diagnostics;
using Server.Engines.ChampionSpawn;
using Server.Engines.ClanSystem;
using Server.Engines.IOBSystem;
using Server.Items;
using Server.Misc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Mobiles
{
    [CorpseName("an orcish corpse")]
    public class ClanOrc : BaseCreature
    {
        public override InhumanSpeech SpeechType { get { return InhumanSpeech.Orc; } }
        [CommandProperty(AccessLevel.GameMaster)]
        public override bool IsScaryToPets { get { return true; } set {; } }

        [Constructable]
        public ClanOrc()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            BaseSoundID = 0x45A;
            IOBAlignment = IOBAlignment.None;
            ControlSlots = 1;
            
            SetStr(111, 145);
            SetDex(101, 135);
            SetInt(86, 110);

            SetHits(67, 87);

            SetDamage(5, 15);

            SetSkill(SkillName.Fencing, 70.1, 95.0);
            SetSkill(SkillName.Macing, 70.1, 95.0);
            SetSkill(SkillName.Poisoning, 60.0, 82.5);
            SetSkill(SkillName.MagicResist, 70.1, 85.0);
            SetSkill(SkillName.Swords, 70.1, 95.0);
            SetSkill(SkillName.Tactics, 85.1, 100.0);

            InitBody();
            InitOutfit();

            Fame = 1000;
            Karma = -1000;

            VirtualArmor = 34;
        }
        public override bool PlayerRangeSensitive { get { return false; } }
        public override bool CanRummageCorpses { get { return Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules() ? true : true; } }
        public override int Meat { get { return 1; } }

        #region PathTooComplex
        public override bool PathTooComplex(Mobile target)
        {   // return if the path from source to target is too complex
            Mobile source = this;
            return Utility.PathTooComplex(source, target);
        }
        
        #endregion PathTooComplex
        public override bool OnBeforeDeath()
        {
            // make our equipment ephemeral
            if (!AlwaysMurderer) 
                Utility.AttributeLayers(this, Item.ItemBoolTable.DeleteOnLift, state: true);

            // let our clanmaster know we died. Maybe we're not active in a war, and if so, start a war!
            if (this.Engine is ChampMini ce)
                if (ce.RootParent is Clanmaster cm)
                    cm.CheckWar(this);

            return base.OnBeforeDeath();
        }
        public override bool IsEnemy(Mobile m, RelationshipFilter filter)
        {
            if (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.RenaissanceRules())
                if (m.Player && m.FindItemOnLayer(Layer.Helm) is OrcishKinMask)
                    return false;

            return base.IsEnemy(m, filter);
        }

        public override void AggressiveAction(Mobile aggressor, bool criminal)
        {
            base.AggressiveAction(aggressor, criminal);

            if (Engine is ChampMini ce && ce.RootParent is Clanmaster cm)
                if (cm.Commander == aggressor)
                {
                    cm.Say("{0} has been relieved of duty for traitorous acts.", cm.Commander.Name);
                    ClanSystem.SendClanMessage(ClanAlignment, string.Format("{0} has been relieved of duty for traitorous acts.", cm.Commander.Name));
                    cm.RelinquishCommand();
                }


            //if (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.RenaissanceRules())
            //{
            //    Item item = aggressor.FindItemOnLayer(Layer.Helm);

            //    if (item is OrcishKinMask)
            //    {
            //        AOS.Damage(aggressor, 50, 0, 100, 0, 0, 0, this);
            //        item.Delete();
            //        aggressor.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);
            //        aggressor.PlaySound(0x307);
            //    }
            //}
        }

        public override void InitBody()
        {
            Name = NameList.RandomName("orc");
            Body = 0x190;   // male human
        }
        public override void InitOutfit()
        {
            WipeLayers();
            Hue = Utility.RandomBool() ? 0x841C : 0x83F1;

            AddItem(new RingmailChest());

            AddItem(new OrcHelm());

            switch (Utility.Random(7))
            {
                case 0: AddItem(new Longsword()); break;
                case 1: AddItem(new Cutlass()); break;
                case 2: AddItem(new Broadsword()); break;
                case 3: AddItem(new Axe()); break;
                case 4: AddItem(new Club()); break;
                case 5: AddItem(new Dagger()); break;
                case 6: AddItem(new Spear()); break;
            }

        }
        public override void AddItem(Item item)
        {
            if (item == null || item.Deleted)
                return;
            
            item.Movable = false;

            base.AddItem(item);
        }
        public ClanOrc(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {   // now loot generation if clan in involved in this death
            if (!ClanCombat())
            {
                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules())
                {
                    // TODO: Skull?
                    switch (Utility.Random(7))
                    {
                        case 0: PackItem(new Arrow()); break;
                        case 1: PackItem(new Lockpick()); break;
                        case 2: PackItem(new Shaft()); break;
                        case 3: PackItem(new Ribs()); break;
                        case 4: PackItem(new Bandage()); break;
                        case 5: PackItem(new BeverageBottle(BeverageType.Wine)); break;
                        case 6: PackItem(new Jug(BeverageType.Cider)); break;
                    }

                    // Froste: 12% random IOB drop
                    if (0.12 > Utility.RandomDouble())
                    {
                        Item iob = Loot.RandomIOB();
                        PackItem(iob);
                    }

                    PackGold(50, 100);

                    // Category 2 MID
                    PackMagicItem(1, 1, 0.05);

                    if (IOBRegions.GetIOBStronghold(this) == IOBAlignment)
                    {
                        // 30% boost to gold
                        PackGold(base.GetGold() / 3);
                    }
                }
                else
                {   // Adam: the orc captain should have classic loot.. I don't know wtf RunUO is giving
                    if (Core.RuleSets.SiegeRules() || Core.RuleSets.MortalisRules())
                    {   // http://web.archive.org/web/20020607073208/uo.stratics.com/hunters/orccap.shtml
                        // 	50 to 150 Gold, Gems, Two-Handed Axe, Ringmail Tunic, Orc Helm, Thigh Boots, 1 Raw Ribs (carved)
                        if (Spawning)
                        {
                            PackGold(50, 150);
                        }
                        else
                        {
                            PackGem(1, .9);
                            PackGem(1, .05);
                            PackItem(new TwoHandedAxe());
                            PackItem(new RingmailChest());
                            PackItem(new OrcHelm());
                            PackItem(new ThighBoots());
                        }
                    }
                    else
                    {
                        if (Spawning)
                        {
                            // TODO: Skull?
                            switch (Utility.Random(7))
                            {
                                case 0: PackItem(new Arrow()); break;
                                case 1: PackItem(new Lockpick()); break;
                                case 2: PackItem(new Shaft()); break;
                                case 3: PackItem(new Ribs()); break;
                                case 4: PackItem(new Bandage()); break;
                                case 5: PackItem(new BeverageBottle(BeverageType.Wine)); break;
                                case 6: PackItem(new Jug(BeverageType.Cider)); break;
                            }

                            if (Core.RuleSets.AOSRules())
                                PackItem(Loot.RandomNecromancyReagent());
                        }

                        AddLoot(LootPack.Meager, 2);
                    }
                }
            }
        }
        public bool ClanCombat()
        {
            List<AggressorInfo> conflicts = new List<AggressorInfo>(this.Aggressed);
            conflicts.AddRange(this.Aggressors);

            foreach (AggressorInfo info in conflicts)
                if (info.Expired == false)
                    if (!info.Defender.Player && !info.Attacker.Player)
                        if (this.IsEnemy(info.Defender) || this.IsEnemy(info.Attacker))
                            return true;

            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    [CorpseName("an orcish corpse")]
    public class ClanOrcBoss : ClanOrc
    {
        public override bool AlwaysMurderer { get { return true; } }
        public override bool CanBandage { get { return Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules() ? true : base.CanBandage; } }
        public override TimeSpan BandageDelay { get { return Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules() ? TimeSpan.FromSeconds(Utility.RandomMinMax(10, 13)) : base.BandageDelay; } }

        [Constructable]
        public ClanOrcBoss()
            : base()
        {
            SetHits(139, 157);

            SetDamage(9, 19);

            Fame = 1100;
            Karma = -1100;

            VirtualArmor = 54;

            PackItem(new Bandage(Utility.RandomMinMax(7, 15)));
        }

        public ClanOrcBoss(Serial serial)
            : base(serial)
        {
        }
        private DateTime m_RepairCheck = DateTime.MinValue;
        private Memory Traitor = new Memory();
        private Memory Warg = new Memory();
        public override void OnThink()
        {
            // every two minutes repair the clothing, armor, weapon, and shield
            if (DateTime.UtcNow > m_RepairCheck)
            {   
                Utility.RepairLayers(this);
                m_RepairCheck = DateTime.UtcNow + TimeSpan.FromMinutes(2.0);
            }

            base.OnThink();
        }
        public override void AggressiveAction(Mobile aggressor, bool criminal)
        {   
            // same clan attacking their champion
            if (aggressor.Player)
                if (ClanSystem.GetClanAlignment(aggressor) == ClanSystem.GetClanAlignment(this))
                {
                    if (Traitor.Recall(ClanSystem.GetClanAlignment(aggressor)) != null)
                    { /* we already know this guy is attacking us, do nothing */ }
                    else
                    {   // let everyone know
                        string text = string.Format("{0} [{1}] at {2} is attacking your champion!", aggressor.Name,
                            Utility.SentenceCamel(ClanSystem.GetClanAlignmentAsString(aggressor)),
                            Utility.SextantLocationString(aggressor));
                        ClanSystem.SendClanMessage(this.ClanAlignment, text, hue: 0x22);
                        Traitor.Remember(ClanSystem.GetClanAlignment(aggressor), TimeSpan.FromMinutes(2).TotalSeconds);

                        // slap back!
                        object o = Activator.CreateInstance(typeof(ClanOrc));
                        if (o is ClanOrc co)
                        {
                            co.ClanAlignment = this.ClanAlignment;
                            co.Combatant = aggressor;
                            if (Clanmaster.GetEquipment(this) != null)
                                Utility.EquipMobile(co, Clanmaster.GetEquipment(this));
                            co.MoveToWorld(Utility.NearMobileLocation(aggressor), this.Map);
                        }
                    }
                }

            // drop some wargs!
            if (Warg.Recall(this as object) != null)
            { /* blocked by timer */ }
            else
            {
                Warg.Remember(this, TimeSpan.FromMinutes(5).TotalSeconds);
                bool targeted_aggressor = false;
                for (int ix = 0; ix < 3; ix++)
                {
                    object o = Activator.CreateInstance(typeof(Warg));
                    if (o is Warg warg)
                    {
                        warg.ClanAlignment = this.ClanAlignment;
                        if (targeted_aggressor == false)
                        {
                            warg.Combatant = aggressor;
                            targeted_aggressor = true;
                        }
                        warg.MoveToWorld(Utility.NearMobileLocation(aggressor), this.Map);
                    }
                }
            }

            base.AggressiveAction(aggressor, criminal);
        }
        public override bool OnBeforeDeath()
        {
            List<Item> list = new List<Item>() { FindItemOnLayer(Layer.OneHanded), FindItemOnLayer(Layer.TwoHanded) };
            Utility.Shuffle(list);
            Item item = list.FirstOrDefault(i => i != null);

            int minLevel = 3;
            int maxLevel = 4;

            if (item is BaseWeapon weapon)
            {
                if (0.05 > Utility.RandomDouble())
                    weapon.Slayer = SlayerName.Silver;

                weapon.DamageLevel = (WeaponDamageLevel)RandomMinMaxScaled(minLevel, maxLevel);
                weapon.AccuracyLevel = (WeaponAccuracyLevel)RandomMinMaxScaled(minLevel, maxLevel);
                weapon.DurabilityLevel = (WeaponDurabilityLevel)RandomMinMaxScaled(minLevel, maxLevel);

                // repair the weapon
                weapon.HitPoints = weapon.MaxHitPoints;
            }
            else if (item is BaseShield shield)
            {
                shield.ProtectionLevel = (ArmorProtectionLevel)RandomMinMaxScaled(minLevel, maxLevel);
                shield.Durability = (ArmorDurabilityLevel)RandomMinMaxScaled(minLevel, maxLevel);
                // repair the shield
                shield.HitPoints = shield.MaxHitPoints;
            }
            return base.OnBeforeDeath();
        }
        public override void GenerateLoot()
        {   // now loot generation if clan in involved in this death
            if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules())
            {
                switch (Utility.Random(8))
                {
                    case 0: PackItem(new Arrow()); break;
                    case 1: PackItem(new Lockpick()); break;
                    case 2: PackItem(new Shaft()); break;
                    case 3: PackItem(new Ribs()); break;
                    case 4: PackItem(new Bandage()); break;
                    case 5: PackItem(new BeverageBottle(BeverageType.Wine)); break;
                    case 6: PackItem(new Jug(BeverageType.Cider)); break;
                    case 7: PackItem(new Onion()); break;
                }

                for (int i = 0; i < 8; ++i)
                    PackGem();

                PackGold(800, 900);
                PackMagicEquipment(1, 3, 0.50, 0.50);
                PackMagicEquipment(1, 3, 0.15, 0.15);

                // Category 4 MID
                PackMagicItem(2, 3, 0.10);
                PackMagicItem(2, 3, 0.05);
                PackMagicItem(2, 3, 0.02);

                // finally, if we were involved in a war, we will drop a rare
                if (this.Engine is ChampMini ce && ce.RootParent is Clanmaster cm && cm.EnemySpawners.Count > 0)
                {
                    Item rare = Loot.RareFactoryItem(1);
                    LogHelper Logger = new LogHelper("BabyRareFactory.log", false, true);
                    Logger.Log(LogType.Mobile, this, string.Format("Dropped baby rare factory item {0}", rare));
                    Logger.Finish();
                    PackItem(rare);
                }
            }
        }
        #region Serialization
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
        #endregion Serialization
    }
}
