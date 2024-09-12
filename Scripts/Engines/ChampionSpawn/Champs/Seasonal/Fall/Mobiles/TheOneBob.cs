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

/* Scripts\Engines\ChampionSpawn\Champs\Seasonal\Fall\Mobiles\TheOneBob.cs	
 * ChangeLog:
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

using Server.Items;
using System;
using System.Collections;

namespace Server.Mobiles
{
    public class TheOneBob : BaseCreature
    {
        private TimeSpan m_SpeechDelay = TimeSpan.FromSeconds(45.0); // time between pirate speech
        public DateTime m_NextSpeechTime;

        [Constructable]
        public TheOneBob()
            : base(AIType.AI_BaseHybrid, FightMode.All | FightMode.Weakest, 10, 1, 0.2, 0.4)
        {
            BardImmune = true;
            FightStyle = FightStyle.Magic | FightStyle.Smart | FightStyle.Bless | FightStyle.Curse;
            UsesHumanWeapons = false;
            UsesBandages = true;
            UsesPotions = true;
            CanRun = true;
            CanReveal = true; // magic and smart

            SpeechHue = Utility.RandomDyedHue();
            Hue = 33770;

            SetStr(305, 425);
            SetDex(72, 150);
            SetInt(505, 750);

            SetHits(4800);
            SetStam(102, 300);

            SetDamage(25, 35);

            SetSkill(SkillName.EvalInt, 120.0);
            SetSkill(SkillName.Magery, 120.0);
            SetSkill(SkillName.Meditation, 120.0);
            SetSkill(SkillName.MagicResist, 150.0);
            SetSkill(SkillName.Swords, 97.6, 100.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 97.6, 100.0);

            Fame = 22500;
            Karma = -22500;

            InitBody();
            InitOutfit();

            VirtualArmor = 30;

            m_NextSpeechTime = DateTime.UtcNow + m_SpeechDelay;

            PackItem(new Bandage(Utility.RandomMinMax(VirtualArmor, VirtualArmor * 2)));
            PackStrongPotions(6, 12);
            PackItem(new Pouch());
        }

        public TheOneBob(Serial serial)
            : base(serial)
        {
        }

        public override int TreasureMapLevel { get { return Core.UOAI || Core.UOREN ? 5 : 0; } }
        public override bool AlwaysMurderer { get { return true; } }
        public override bool CanRummageCorpses { get { return Core.UOAI || Core.UOREN ? true : false; } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool ClickTitle { get { return true; } }

        public override void InitBody()
        {
            this.Female = false;
            Body = 0x190;
            Name = "The One Bob";
        }

        public override void InitOutfit()
        {
            WipeLayers();

            Robe robe = new Robe(23);
            AddItem(robe);
        }

        public override void OnThink()
        {
            if (DateTime.UtcNow >= m_NextSpeechTime)
            {
                Mobile combatant = this.Combatant;

                if (combatant != null && combatant.Map == this.Map && combatant.InRange(this, 8))
                {
                    int phrase = Utility.Random(5);

                    switch (phrase)
                    {
                        case 0: this.Say(true, "Bring it."); break;
                        case 1: this.Say(true, "As long as you die in the end, I win."); break;
                        case 2: this.Say(true, "You're lucky I don't have purple potions."); break;
                        case 3: this.Say(true, "Bobs > you newbs"); break;
                        case 4: this.Say(true, "Yeah, lol"); break;
                    }

                    m_NextSpeechTime = DateTime.UtcNow + m_SpeechDelay;
                }

                base.OnThink();
            }

        }

        /*private DateTime m_NextBomb;
		private int m_Thrown;

		public override void OnActionCombat()
		{
			Mobile combatant = Combatant;

			if (combatant == null || combatant.Deleted || combatant.Map != Map || !InRange(combatant, 12) || !CanBeHarmful(combatant) || !InLOS(combatant))
				return;

			if (DateTime.UtcNow >= m_NextBomb)
			{
				ThrowBomb(combatant);

				m_Thrown++;

				if (0.75 >= Utility.RandomDouble() && (m_Thrown % 2) == 1) // 75% chance to quickly throw another bomb
					m_NextBomb = DateTime.UtcNow + TimeSpan.FromSeconds(3.0);
				else
					m_NextBomb = DateTime.UtcNow + TimeSpan.FromSeconds(5.0 + (10.0 * Utility.RandomDouble())); // 5-15 seconds
			}
		}

		public void ThrowBomb(Mobile m)
		{
			DoHarmful(m);

			this.MovingParticles(m, 0x1C19, 1, 0, false, true, 0, 0, 9502, 6014, 0x11D, EffectLayer.Waist, 0);

			new InternalTimer(m, this).Start();
		}

		private class InternalTimer : Timer
		{
			private Mobile m_Mobile, m_From;

			public InternalTimer(Mobile m, Mobile from)
				: base(TimeSpan.FromSeconds(1.0))
			{
				m_Mobile = m;
				m_From = from;
				Priority = TimerPriority.TwoFiftyMS;
			}

			protected override void OnTick()
			{
				m_Mobile.PlaySound(0x11D);
				AOS.Damage(m_Mobile, m_From, Utility.RandomMinMax(10, 20), 0, 100, 0, 0, 0);
			}
		}*/

        public override void OnDamagedBySpell(Mobile caster)
        {
            if (caster == this)
                return;

            // Adam: 12% chance to spawn a bob
            if (Utility.RandomChance(12))
                SpawnBob(caster);
        }

        public void SpawnBob(Mobile caster)
        {
            Mobile target = caster;

            if (Map == null || Map == Map.Internal)
                return;

            int helpers = 0;
            ArrayList mobs = new ArrayList();
            IPooledEnumerable eable = this.GetMobilesInRange(10);
            foreach (Mobile m in eable)
            {
                if (m is Bob)
                    ++helpers;

                if (m is PlayerMobile && m.Alive == true && m.Hidden == false && m.AccessLevel <= AccessLevel.Player)
                    mobs.Add(m);
            }
            eable.Free();

            if (helpers < 5)
            {
                BaseCreature helper = new Bob();

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

        public override void DistributedLoot()
        {
            if (!NoKillAwards)
            {
                Map map = this.Map;

                if (map != null)
                {
                    for (int x = -2; x <= 2; ++x)
                    {
                        for (int y = -2; y <= 2; ++y)
                        {
                            double dist = Math.Sqrt(x * x + y * y);

                            if (dist <= 12)
                                new GoodiesTimer(map, X + x, Y + y).Start();
                        }
                    }
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

        public override void GenerateLoot()
        {
            int phrase = Utility.Random(2);
            switch (phrase)
            {
                case 0: this.Say(true, "Bobs for Bob!"); break;
                case 1: this.Say(true, "Bob will rise!"); break;
            }

            // make the magic key
            Key key = new Key(KeyType.Magic);
            key.KeyValue = Key.RandomValue();

            // make the magic box
            MagicBox MagicBox = new MagicBox();
            MagicBox.Movable = true;
            MagicBox.KeyValue = key.KeyValue;
            MagicBox.DropItem(key);

            PackItem(MagicBox);

            // add bob's pillow
            Item pillow = new Item(Utility.RandomList(5029, 5030, 5031, 5032, 5033, 5034, 5035, 5036, 5037, 5038));
            pillow.Hue = 23;
            pillow.Name = "Pillow of The One Bob";
            pillow.Weight = 1.0;

            PackItem(pillow);
        }

        public override void Damage(int amount, Mobile from, object source_weapon)
        {
            Mobile combatant = this.Combatant;

            if (combatant != null && combatant.Map == this.Map && combatant.InRange(this, 8))
            {
                if (Utility.RandomBool())
                {

                    /*                    int phrase = Utility.Random(4);

										switch (phrase)
										{
											case 0: this.Say(true, "Har! The mackerel wiggles!"); break;
											case 1: this.Say(true, "Ye stink like a rotten clam! Bring it on yet!?"); break;
											case 2: this.Say(true, "Arr, treacherous monkey!"); break;
											case 3: this.Say(true, "Ye'll not get my swag!"); break;
										}
                    
										m_NextSpeechTime = DateTime.UtcNow + m_SpeechDelay;*/
                }
            }

            base.Damage(amount, from, source_weapon: source_weapon);
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i] is ContextMenus.PaperdollEntry)
                    list.RemoveAt(i--);
            }
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
}
