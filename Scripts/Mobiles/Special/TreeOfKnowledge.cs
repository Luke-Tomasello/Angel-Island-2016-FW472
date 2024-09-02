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

/* Scripts/Mobiles/Special/TreeOfKnowledge.cs
 * ChangeLog:
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 2 loops updated.
 *  03/09/07, plasma    
 *      Removed cannedevil namespace reference
 *  01/05/07, plasma
 *      Changed CannedEvil namespace to ChampionSpawn for cleanup!
 *  10/10/06, Kit
 *		b1 revert, dont bond pets anymore :/
 *  05/08/06, Kit
 *		Added releaseing bonded pet functionality to TOK, changed bonded msg to "I wish to forge a bond with the spirit of"
 *		Changed msg for release function to warn on bonded pet it will result in death, *2 cost for bonded pet release.
 *  05/07/06, Kit
 *		Extended TOK to now initiate pet bonding, and release unbonded pets, rewrote ValidatePet
 *		function to test based on switch and request type vs if/else if/else.
 *	02/11/06, Adam
 *		Make common the formatting of sextant coords.
 *  1/01/06, Kit
 *		Fixed problem with tok returning stabled pet first and preventing return of pet with same name in world.
 *	9/27/05, Adam
 *		a. Add timer start for the DrainTimer in Deserialize
 *		b. Add Succubus style Aura damage and DrainLife
 *		We now have TWO drain systems + aura. This guy is not really meant to be farmed
 *		And so this level of protection should discourage all but the most persistent.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *	5/7/05, Pix
 *		Fix for crash - the spiritlist and locatelist weren't being instantiated
 *		when the TOK was deserialized.
 *	4/24/05, Adam
 *		Add the Angry() function so that the tree will attack only mobiles that it's angery at.
 *			That is, on it's agro list.
 *	4/21/05, Adam
 *		1. Rename
 *		2. Switch armor/wep generation to ImbueWeaponOrArmor()
 *	4/07/05 Created by smerX
 */

using Server.Items;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [CorpseName("an ancient tree")]
    public class TreeOfKnowledge : BaseCreature
    {
        private ArrayList m_SpiritList;
        private ArrayList m_LocateList;
        private ArrayList m_BondList;
        private ArrayList m_ReleaseList;

        private double m_BaseSpiritPrice = 4000;
        private double m_BaseLocatePrice = 6000;
        private double m_BaseBondPrice = 2000;
        private double m_BaseReleasePrice = 4000;
        private DrainTimer m_Timer;

        public ArrayList SpiritList { get { return m_SpiritList; } }
        public ArrayList LocateList { get { return m_LocateList; } }
        public ArrayList BondList { get { return m_BondList; } }
        public ArrayList ReleaseList { get { return m_ReleaseList; } }

        [Constructable]
        public TreeOfKnowledge()
            : base(AIType.AI_Mage, FightMode.Aggressor, 18, 1, 0.2, 0.4)
        {
            Name = "tree of knowledge";
            BodyValue = 47;
            BardImmune = true;

            SetStr(900, 1000);
            SetDex(125, 135);
            SetInt(1000, 1200);

            SetFameLevel(4);
            SetKarmaLevel(4);

            VirtualArmor = 60;

            SetSkill(SkillName.Wrestling, 93.9, 96.5);
            SetSkill(SkillName.Tactics, 96.9, 102.2);
            SetSkill(SkillName.MagicResist, 131.4, 140.8);
            SetSkill(SkillName.Magery, 156.2, 161.4);
            SetSkill(SkillName.EvalInt, 100.0);
            SetSkill(SkillName.Meditation, 120.0);

            AddItem(new LightSource());

            m_Timer = new DrainTimer(this);
            m_Timer.Start();

            m_SpiritList = new ArrayList();
            m_LocateList = new ArrayList();
            m_BondList = new ArrayList();
            m_ReleaseList = new ArrayList();
        }

        public override bool DisallowAllMoves { get { return true; } }
        // Auto-dispel is UOR - http://forums.uosecondage.com/viewtopic.php?f=8&t=6901
        public override bool AutoDispel { get { return Core.UOAI || Core.UOAR ? false : PublishInfo.PublishDate >= Core.EraREN ? true : false; } }
        public override Poison PoisonImmune { get { return Poison.Lethal; } }

        public override bool CanBandage { get { return true; } }
        public override TimeSpan BandageDelay { get { return TimeSpan.FromSeconds(15.0); } }
        public override int BandageMin { get { return 40; } }
        public override int BandageMax { get { return 85; } }

        public override AuraType MyAura { get { return AuraType.Hate; } }
        public override int AuraRange { get { return 5; } }
        public override int AuraMin { get { return 5; } }
        public override int AuraMax { get { return 10; } }
        public override TimeSpan NextAuraDelay { get { return TimeSpan.FromSeconds(4.0); } }

        public void DrainLife()
        {
            ArrayList list = new ArrayList();

            IPooledEnumerable eable = this.GetMobilesInRange(2);
            foreach (Mobile m in eable)
            {
                // excluse these cases
                if (m == this || !CanBeHarmful(m)) continue;
                if (AuraTarget(m) == false) continue;

                if (m is BaseCreature && (((BaseCreature)m).Controlled || ((BaseCreature)m).Summoned || ((BaseCreature)m).Team != this.Team))
                    list.Add(m);
                else if (m.Player)
                    list.Add(m);
            }
            eable.Free();

            foreach (Mobile m in list)
            {
                DoHarmful(m);

                m.FixedParticles(0x374A, 10, 15, 5013, 0x496, 0, EffectLayer.Waist);
                m.PlaySound(0x231);

                m.SendMessage("You feel the life drain out of you!");

                int toDrain = Utility.RandomMinMax(10, 40);

                Hits += toDrain;
                m.Damage(toDrain, this);
            }
        }

        public override void OnGaveMeleeAttack(Mobile defender)
        {
            base.OnGaveMeleeAttack(defender);

            if (0.1 >= Utility.RandomDouble())
                DrainLife();
        }

        public override void OnGotMeleeAttack(Mobile attacker)
        {
            base.OnGotMeleeAttack(attacker);

            if (0.1 >= Utility.RandomDouble())
                DrainLife();
        }

        public override bool AuraTarget(Mobile aggressor)
        {
            if (aggressor == this)
                return false;

            List<AggressorInfo> list = this.Aggressors;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)list[i];

                if (info.Attacker == aggressor)
                {
                    return true;
                }
            }

            list = aggressor.Aggressors;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)list[i];

                if (info.Attacker == this)
                {
                    return true;
                }
            }

            list = this.Aggressed;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)list[i];

                if (info.Defender == aggressor)
                {
                    return true;
                }
            }

            list = aggressor.Aggressed;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)list[i];

                if (info.Defender == this)
                {
                    return true;
                }
            }

            return false;
        }

        private class DrainTimer : Timer
        {
            private TreeOfKnowledge m_Owner;

            public DrainTimer(TreeOfKnowledge owner)
                : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            {
                m_Owner = owner;
                Priority = TimerPriority.TwoFiftyMS;
            }

            private static ArrayList m_ToDrain = new ArrayList();

            protected override void OnTick()
            {
                if (m_Owner.Deleted)
                {
                    Stop();
                    return;
                }

                if (0.2 < Utility.RandomDouble())
                    return;

                if (m_Owner.Combatant != null)
                {
                    IPooledEnumerable eable = m_Owner.GetMobilesInRange(8);
                    foreach (Mobile m in eable)
                    {   // exclude the obvious
                        if (m == m_Owner) continue;
                        if (m.AccessLevel != AccessLevel.Player) continue;

                        if (m_Owner.CanBeHarmful(m))
                            if (m_Owner.AuraTarget(m) == true)
                                m_ToDrain.Add(m);
                    }
                    eable.Free();

                    foreach (Mobile m in m_ToDrain)
                    {
                        m_Owner.DoHarmful(m);

                        m.FixedParticles(0x374A, 10, 15, 5013, 0x455, 0, EffectLayer.Waist);
                        m.PlaySound(0x231);

                        m.SendMessage("You feel a sharp pain in your head!");

                        if (m_Owner != null)
                            m_Owner.Hits += 20;

                        m.Damage(20, m_Owner);
                    }

                    m_ToDrain.Clear();
                }
            }
            /*
						private bool Angry( Mobile target )
						{
							if ( target == m_Owner )
								return false;

							ArrayList list = m_Owner.Aggressors;

							for ( int i = 0; i < list.Count; ++i )
							{
								AggressorInfo ai = (AggressorInfo)list[i];

								if ( ai.Attacker == target )
									return true;
							}

							return false;
						}
			*/
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int HitsMax { get { return 30000; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int ManaMax { get { return 5000; } }

        public TreeOfKnowledge(Serial serial)
            : base(serial)
        {
            m_SpiritList = new ArrayList();
            m_LocateList = new ArrayList();
            m_ReleaseList = new ArrayList();
            m_BondList = new ArrayList();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            // restart the timer on load!
            m_Timer = new DrainTimer(this);
            m_Timer.Start();
        }

        public override void GenerateLoot()
        {
            if (Core.UOAI || Core.UOAR)
            {
                PackGold(28000, 32000);
                PackItem(new Log(Utility.Random(2500, 3500)));
                GenerateRegs();

                // Use our unevenly weighted table for chance resolution
                Item item;
                item = Loot.RandomArmorOrShieldOrWeapon();
                PackItem(Loot.ImbueWeaponOrArmor(item, 6, 0, false));
            }
            else
            {
                if (Core.UOSP || Core.UOMO)
                {   // ai special
                    if (Spawning)
                    {
                        PackGold(0);
                    }
                    else
                    {
                    }
                }
                else
                {   // Standard RunUO
                    // ai special
                }
            }
        }

        private void GenerateRegs()
        {
            PackItem(new BlackPearl(Utility.Random(80, 150)));
            PackItem(new Garlic(Utility.Random(80, 150)));
            PackItem(new Bloodmoss(Utility.Random(80, 150)));
            PackItem(new Ginseng(Utility.Random(95, 190)));
            PackItem(new MandrakeRoot(Utility.Random(95, 190)));
            PackItem(new SulfurousAsh(Utility.Random(80, 150)));
            PackItem(new SpidersSilk(Utility.Random(80, 150)));
            PackItem(new Nightshade(Utility.Random(80, 150)));
        }

        public override bool OnGoldGiven(Mobile from, Gold dropped)
        {
            if (m_SpiritList != null && m_SpiritList.Count > 0)
            {
                foreach (BaseCreature bc in m_SpiritList)
                {
                    if (bc != null && GetSpiritPrice(bc) != -1 && GetSpiritPrice(bc) == dropped.Amount && from == bc.ControlMaster)
                    {
                        if (bc != null)
                        {
                            Effects.SendLocationParticles(EffectItem.Create(bc.Location, bc.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);

                            if (bc.Alive)
                                bc.Kill();

                            Mobile m = (Mobile)bc;
                            m.MoveToWorld(new Point3D(from.X, from.Y, from.Z), from.Map);
                            Effects.SendLocationParticles(EffectItem.Create(m.Location, m.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);
                            m_SpiritList.Remove(bc);
                        }

                        return true;
                    }
                }
            }

            if (m_LocateList != null && m_LocateList.Count > 0)
            {
                foreach (BaseCreature bc1 in m_LocateList)
                {
                    if (bc1 != null && GetLocatePrice(bc1) != -1 && GetLocatePrice(bc1) == dropped.Amount && from == bc1.ControlMaster)
                    {
                        if (bc1 != null)
                        {
                            String location = String.Format("Your pet, \"{0},\" is at {1}.", bc1.Name, GetSextantLocation(bc1));
                            this.SayTo(from, location);

                            if (0.05 > Utility.RandomDouble())
                            {
                                if (bc1.Alive)
                                    this.SayTo(from, "He seems to be relatively healthy.");
                                else
                                    this.SayTo(from, "Your pet is dead.");
                            }

                            m_LocateList.Remove(bc1);
                        }

                        return true;
                    }
                }
            }

            /*
			if ( m_BondList != null && m_BondList.Count > 0 )
			{
				foreach ( BaseCreature bc2 in m_BondList )
				{
					if ( bc2 != null && GetBondPrice(bc2) != -1 && GetBondPrice(bc2) == dropped.Amount && from == bc2.ControlMaster )
					{
						if ( bc2 != null )
						{
							this.SayTo( from, "The bonding of your pet's spirit to you has now begun." );
							bc2.BondingBegin = DateTime.UtcNow;
							m_BondList.Remove( bc2 );
						}

						return true;
					}
				}			
			}*/

            if (m_ReleaseList != null && m_ReleaseList.Count > 0)
            {
                foreach (BaseCreature bc3 in m_ReleaseList)
                {
                    if (bc3 != null && GetReleasePrice(bc3) != -1 && GetReleasePrice(bc3) == dropped.Amount && from == bc3.ControlMaster)
                    {
                        if (bc3 != null)
                        {
                            this.SayTo(from, "Your connection with {0} has now been severed.", bc3.Name.ToString());
                            //bc3.SetControlMaster(null);
                            bc3.AIObject.DoOrderRelease();
                            m_ReleaseList.Remove(bc3);
                        }
                        return true;
                    }
                }
            }

            return base.OnGoldGiven(from, dropped);
        }

        public override void OnAfterDelete()
        {
            if (m_Timer != null)
                m_Timer.Stop();

            m_Timer = null;

            base.OnAfterDelete();
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return true;
        }


        public override void OnSpeech(SpeechEventArgs e)
        {
            if ((e.Speech.ToLower()).StartsWith("i wish the return of ") && e.Speech.Length > 21 && e.Mobile.CheckAlive())
            {
                String s = e.Speech.Remove(0, 21);
                ReturnRequest(e.Mobile, s);
            }
            else if ((e.Speech.ToLower()).StartsWith("i wish to locate ") && e.Speech.Length > 17 && e.Mobile.CheckAlive())
            {
                String s = e.Speech.Remove(0, 17);
                LocateRequest(e.Mobile, s);
            }
            /*else if ( (e.Speech.ToLower()).StartsWith("i wish to forge a bond with the spirit of ") && e.Speech.Length > 42 && e.Mobile.CheckAlive() )
			{
				String s = e.Speech.Remove( 0, 42 );
				BondRequest( e.Mobile, s );
			}*/
            else if ((e.Speech.ToLower()).StartsWith("i wish to release ") && e.Speech.Length > 18 && e.Mobile.CheckAlive())
            {
                String s = e.Speech.Remove(0, 18);
                ReleaseRequest(e.Mobile, s);
            }
        }

        private bool ValidatePet(Mobile pet, PlayerMobile messageReciever, RequestType type)
        {
            if (pet != null && pet is BaseCreature && !pet.Deleted)
            {
                BaseCreature bc = pet as BaseCreature;

                if (bc.IsStabled)
                {
                    this.SayTo(messageReciever, "That creature is in your stables. If you wish to see it, talk to your stablemaster.");
                    return false;
                }

                switch (type)
                {
                    case RequestType.Spirit:
                        {
                            if (!bc.IsBonded)
                            {
                                this.SayTo(messageReciever, "You have not bonded with that pet, and so I cannot reach it's spirit through you.");
                                return false;
                            }

                            else if (messageReciever.InRange(bc, 12))
                            {
                                this.SayTo(messageReciever, "Your pet is not far from here, you do not require my assistance.");
                                return false;
                            }
                            break;
                        }
                    case RequestType.Locate:
                        {
                            if (messageReciever.InRange(bc, 12))
                            {
                                this.SayTo(messageReciever, "Your pet is not far from here, you do not require my assistance.");
                                return false;
                            }
                            break;
                        }
                    case RequestType.Bond:
                        {
                            if (bc.IsBonded)
                            {
                                this.SayTo(messageReciever, "Your pet is bonded too you already for all time, you do not require my assistance.");
                                return false;
                            }
                            else if (!messageReciever.InRange(bc, 6))
                            {
                                this.SayTo(messageReciever, "Your pet is too far away from here, and the bonding process can not begin.");
                                return false;
                            }
                            else if (bc.BondingBegin != DateTime.MinValue)
                            {
                                this.SayTo(messageReciever, "The bonding of {0}'s spirit has already begun, you must now wait some time for it to complete.", bc.Name.ToString());
                                return false;
                            }
                            else if (bc.MinTameSkill >= 29.1 && messageReciever.Skills[SkillName.AnimalTaming].Value < bc.MinTameSkill)
                            {
                                this.SayTo(messageReciever, "Your connection and control over your pet is too weak, and you may not bond with it.");
                                return false;
                            }
                            break;
                        }
                    case RequestType.Release:
                        {
                            if (messageReciever.InRange(bc, 12))
                            {
                                this.SayTo(messageReciever, "Your pet is not far from here, you do not require my assistance.");
                                return false;
                            }

                            break;
                        }
                }

                if (GetSpiritPrice(bc) == -1)
                    return false;

                return true;
            }
            else
                this.SayTo(messageReciever, "You have no pets by that name.");

            return false;
        }

        private void BondRequest(Mobile from, String petname)
        {
            if (!(from is PlayerMobile))
                return;

            PlayerMobile pm = from as PlayerMobile;
            Mobile pet = FindPet(petname, pm);
            if (pet != null && pet is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)pet;

                if (ValidatePet(pet, pm, RequestType.Bond))
                {
                    if (m_BondList != null && m_BondList.Count > 0)
                    {
                        foreach (BaseCreature listItem in m_BondList)
                        {
                            if (listItem == bc)
                            {
                                this.SayTo(from, "I'll just wait here for that {0}GP..", Convert.ToString(GetBondPrice(bc)));
                                return;
                            }
                        }
                    }
                    String proposition = String.Format("I will forever link the spirit of {0} with you for the penance of {1}GP.", petname, Convert.ToString(GetBondPrice(bc)));
                    this.SayTo(from, proposition);
                    if (m_BondList != null)
                    {
                        m_BondList.Add(bc);
                        new EntryTimer(bc, ListType.Bond, this).Start();
                    }
                }
            }
        }

        private void ReleaseRequest(Mobile from, String petname)
        {
            String proposition;

            if (!(from is PlayerMobile))
                return;

            PlayerMobile pm = from as PlayerMobile;
            Mobile pet = FindPet(petname, pm);
            if (pet != null && pet is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)pet;

                if (ValidatePet(pet, pm, RequestType.Release))
                {
                    if (m_ReleaseList != null && m_ReleaseList.Count > 0)
                    {
                        foreach (BaseCreature listItem in m_ReleaseList)
                        {
                            if (listItem == bc)
                            {
                                this.SayTo(from, "I'll just wait here for that {0}GP..", Convert.ToString(GetReleasePrice(bc)));
                                return;
                            }
                        }
                    }
                    proposition = String.Format("I will sever your connection with {0} for the penance of {1}GP.", petname, Convert.ToString(GetReleasePrice(bc)));

                    this.SayTo(from, proposition);
                    if (m_ReleaseList != null)
                    {
                        m_ReleaseList.Add(bc);
                        new EntryTimer(bc, ListType.Release, this).Start();
                    }
                }
            }
        }
        private void LocateRequest(Mobile from, String petname)
        {
            if (!(from is PlayerMobile))
                return;

            PlayerMobile pm = from as PlayerMobile;
            Mobile pet = FindPet(petname, pm);
            if (pet != null && pet is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)pet;

                if (ValidatePet(pet, pm, RequestType.Locate))
                {
                    if (m_LocateList != null && m_LocateList.Count > 0)
                    {
                        foreach (BaseCreature listItem in m_LocateList)
                        {
                            if (listItem == bc)
                            {
                                this.SayTo(from, "I'll just wait here for that {0}GP..", Convert.ToString(GetLocatePrice(bc)));
                                return;
                            }
                        }
                    }

                    String proposition = String.Format("I will tell you the location of your pet for the penance of {1}GP.", petname, Convert.ToString(GetLocatePrice(bc)));
                    this.SayTo(from, proposition);
                    if (m_LocateList != null)
                    {
                        m_LocateList.Add(bc);
                        new EntryTimer(bc, ListType.Locate, this).Start();
                    }
                }
            }
        }

        private void ReturnRequest(Mobile from, String petname)
        {
            if (!(from is PlayerMobile))
                return;

            PlayerMobile pm = from as PlayerMobile;
            Mobile pet = FindPet(petname, pm);

            if (ValidatePet(pet, pm, RequestType.Spirit))
            {
                BaseCreature bc = (BaseCreature)pet;


                if (m_SpiritList != null && m_SpiritList.Count > 0)
                {
                    foreach (BaseCreature listItem in m_SpiritList)
                    {
                        if (listItem == bc)
                        {
                            this.SayTo(from, "I'll just wait here for that {0}GP..", Convert.ToString(GetSpiritPrice(bc)));
                            return;
                        }
                    }
                }

                String proposition = String.Format("I will return the spirit of your pet to you for the price of {1}GP.", petname, Convert.ToString(GetSpiritPrice(bc)));
                this.SayTo(from, proposition);
                m_SpiritList.Add(bc);
                new EntryTimer(bc, ListType.Spirit, this).Start();
            }
        }

        private int GetSpiritPrice(BaseCreature bc)
        {
            if (bc == null || bc.Deleted)
                return -1;

            double la = (m_BaseSpiritPrice * (bc.MinTameSkill * 0.01));
            int de = Convert.ToInt32(la);
            int da = Math.Abs(de);

            if (da <= 0)
                return -1;

            return da;
        }

        private int GetLocatePrice(BaseCreature bc)
        {
            if (bc == null || bc.Deleted)
                return -1;

            double la = (m_BaseLocatePrice * (bc.MinTameSkill * 0.01));
            int de = Convert.ToInt32(la);
            int da = Math.Abs(de);

            if (da <= 0)
                return -1;

            return da;
        }

        private int GetBondPrice(BaseCreature bc)
        {
            if (bc == null || bc.Deleted)
                return -1;

            double la = (m_BaseBondPrice * (bc.MinTameSkill * 0.01));
            int de = Convert.ToInt32(la);
            int da = Math.Abs(de);

            if (da <= 0)
                return -1;

            return da;
        }

        private int GetReleasePrice(BaseCreature bc)
        {
            if (bc == null || bc.Deleted)
                return -1;

            double la = (m_BaseReleasePrice * (bc.MinTameSkill * 0.01));
            int de = Convert.ToInt32(la);
            int da = Math.Abs(de);

            if (da <= 0)
                return -1;


            if (bc.IsBonded)
                da *= 2;

            return da;
        }

        private Mobile FindPet(String petname, PlayerMobile owner)
        {
            if (owner == null || petname == null)
                return null;

            petname.ToLower();

            foreach (Mobile n in World.Mobiles.Values)
            {
                if (n is BaseCreature)
                {
                    BaseCreature bc = (BaseCreature)n;

                    if (bc.Controlled && bc.ControlMaster == owner)
                    {
                        if (Insensitive.Equals(bc.Name, petname))
                            return bc as Mobile;
                    }
                }
            }

            if (owner.Stabled.Count > 0)
            {
                foreach (Mobile m in owner.Stabled)
                {
                    if (Insensitive.Equals(m.Name, petname))
                        return m as Mobile;
                }
            }

            return null;
        }

        private void Remove(BaseCreature entry, ListType list)
        {
            if (list == ListType.Spirit)
            {
                if (m_SpiritList != null && m_SpiritList.Count > 0)
                {
                    foreach (BaseCreature bc in m_SpiritList)
                    {
                        if (bc == entry)
                        {
                            m_SpiritList.Remove(bc);
                            return;
                        }
                    }
                }
            }

            if (list == ListType.Locate)
            {
                if (m_LocateList != null && m_LocateList.Count > 0)
                {
                    foreach (BaseCreature bc1 in m_LocateList)
                    {
                        if (bc1 == entry)
                        {
                            m_LocateList.Remove(bc1);
                            return;
                        }
                    }
                }
            }
            if (list == ListType.Bond)
            {
                if (m_BondList != null && m_BondList.Count > 0)
                {
                    foreach (BaseCreature bc2 in m_BondList)
                    {
                        if (bc2 == entry)
                        {
                            m_BondList.Remove(bc2);
                            return;
                        }
                    }
                }
            }
            if (list == ListType.Release)
            {
                if (m_ReleaseList != null && m_ReleaseList.Count > 0)
                {
                    foreach (BaseCreature bc3 in m_ReleaseList)
                    {
                        if (bc3 == entry)
                        {
                            m_ReleaseList.Remove(bc3);
                            return;
                        }
                    }
                }
            }
        }

        public String GetSextantLocation(Mobile m) //returns location of mobile in sextant coords
        {

            if (m.Deleted)
                return "Pet Heaven";

            string location;
            int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
            bool xEast = false, ySouth = false;
            Map map = m.Map;
            bool valid = Server.Items.Sextant.Format(m.Location, map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth);

            if (valid)
                location = Sextant.Format(xLong, yLat, xMins, yMins, xEast, ySouth);
            else
                location = "????";

            if (!valid)
                location = string.Format("{0} {1}", m.X, m.Y);

            if (map != null)
            {
                Region reg = m.Region;

                if (reg != map.DefaultRegion)
                {
                    location += (" in " + reg);
                }
            }

            return location;
        }

        private class EntryTimer : Timer
        {
            private BaseCreature m_Entry;
            private ListType m_ListType;
            private TreeOfKnowledge m_Owner;

            public EntryTimer(BaseCreature entry, ListType type, TreeOfKnowledge owner)
                : base(TimeSpan.FromMinutes(5.0))
            {
                m_Entry = entry;
                m_ListType = type;
                m_Owner = owner;
                Priority = TimerPriority.TwoFiftyMS;
            }

            protected override void OnTick()
            {
                if (m_Entry.Deleted || m_Owner == null || m_Owner.Deleted)
                {
                    Stop();
                    return;
                }


                if (m_ListType == ListType.Spirit)
                {
                    foreach (BaseCreature bc in m_Owner.SpiritList)
                    {
                        if (bc == m_Entry)
                        {
                            m_Owner.Remove(bc, m_ListType);
                            break;
                        }
                    }
                }

                if (m_ListType == ListType.Locate)
                {
                    foreach (BaseCreature bc1 in m_Owner.LocateList)
                    {
                        if (bc1 == m_Entry)
                        {
                            m_Owner.Remove(bc1, m_ListType);
                            break;
                        }
                    }
                }
                if (m_ListType == ListType.Bond)
                {
                    foreach (BaseCreature bc2 in m_Owner.BondList)
                    {
                        if (bc2 == m_Entry)
                        {
                            m_Owner.Remove(bc2, m_ListType);
                            break;
                        }
                    }
                }
                if (m_ListType == ListType.Release)
                {
                    foreach (BaseCreature bc3 in m_Owner.ReleaseList)
                    {
                        if (bc3 == m_Entry)
                        {
                            m_Owner.Remove(bc3, m_ListType);
                            break;
                        }
                    }
                }

                Stop();
            }
        }

        public enum ListType
        {
            Spirit,
            Locate,
            Bond,
            Release
        }

        public enum RequestType
        {
            Spirit,
            Locate,
            Bond,
            Release
        }

    }
}
