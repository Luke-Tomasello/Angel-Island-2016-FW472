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

/* Scripts/Engines/AngelIsland/AIGuardSpawn/AIPostGuard.cs
 * Created 4/1/04 by mith
 * ChangeLog
 *	3/17/10, adam
 *		Every once ana while a guard will drop a key the the guard's after hours club.
 *		Logic:
 *			Dying guards rekey lock and generate+drop a key IF the door is locked
 *			Spawned guards lock and rekey lock IF the door is unlocked
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 1 lines removed.
 *  7/21/04, Adam
 *		1. Redo the setting of skills and setting of Damage 
 *  7/17/04, Adam
 *		1. Add NightSightScroll to drop
 *		2. Replace MindBlastScroll with FireballScroll
 *	5/23/04 smerX
 *		Enabled healing
 *	5/14/04, mith
 *		Modified FightMode from Aggressor to Closest.
 *		Added Speech.
 *	4/12/04 mith
 *		Converted stats/skills to use dynamic values defined in CoreAI.
 *	4/10/04 changes by mith
 * 		Added bag of reagents and scrolls to loot.
 *	4/1/04
 * 		Changed starting skills to be from a range of 70-80 rather than flat 75.0.
 */

using Server.Items;
using System;

namespace Server.Mobiles
{
    public class AIPostGuard : BaseAIGuard
    {
        private TimeSpan m_SpeechDelay = TimeSpan.FromSeconds(120.0); // time between speech
        public DateTime m_NextSpeechTime;

        [Constructable]
        public AIPostGuard()
            : base()
        {
            FightMode = FightMode.All | FightMode.Closest;

            InitStats(CoreAI.PostGuardStrength, 100, 100);

            // Set the BroadSword damage
            SetDamage(14, 25);

            SetSkill(SkillName.Anatomy, CoreAI.PostGuardSkillLevel);
            SetSkill(SkillName.Tactics, CoreAI.PostGuardSkillLevel);
            SetSkill(SkillName.Swords, CoreAI.PostGuardSkillLevel);
            SetSkill(SkillName.MagicResist, CoreAI.PostGuardSkillLevel);

            // all spawned guards lock the door to the "after hours club"
            //	only lock it if the playe has had a chance to get in
            Serial ds = 0x400139E7;
            if (World.FindItem(ds) != null && World.FindItem(ds) is BaseDoor && (World.FindItem(ds) as BaseDoor).Locked == false)
            {   // relock and rekey
                (World.FindItem(ds) as BaseDoor).Locked = true;
                (World.FindItem(ds) as BaseDoor).KeyValue = Key.RandomValue();
            }
        }

        public AIPostGuard(Serial serial)
            : base(serial)
        {
        }

        public override bool CanBandage { get { return true; } }
        public override TimeSpan BandageDelay { get { return TimeSpan.FromSeconds(12.0); } }
        public override int BandageMin { get { return 15; } }
        public override int BandageMax { get { return 30; } }

        public override void GenerateLoot()
        {
            if (Core.UOAI || Core.UOAR)
            {
                DropWeapon(1, 1);
                DropWeapon(1, 1);

                DropItem(new BagOfReagents(CoreAI.PostGuardNumRegDrop));
                DropItem(new Bandage(CoreAI.PostGuardNumBandiesDrop));
                DropItem(new ParalyzeScroll());
                DropItem(new FireballScroll());
                DropItem(new NightSightScroll());

                // once in a while allow the player to enter the guards after hours club
                //	in here they can buy rare items from the therapist there.
                //	the items are only rare due to the difficulity in getting in the room 
                Serial ds = 0x400139E7;
                if (Utility.RandomChance(1) && World.FindItem(ds) != null && World.FindItem(ds) is BaseDoor && (World.FindItem(ds) as BaseDoor).Locked == true)
                {
                    Key key = new Key(Key.RandomValue());
                    key.Description = "After hours club";
                    (World.FindItem(ds) as BaseDoor).KeyValue = key.KeyValue;
                    DropItem(key);
                }
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

        public override bool OnBeforeDeath()
        {
            return base.OnBeforeDeath();
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (DateTime.UtcNow >= m_NextSpeechTime)
            {
                PlayerMobile pm = m as PlayerMobile;
                if (pm != null && pm.AccessLevel == AccessLevel.Player && !m.Hidden && m.Alive && m.Location != oldLocation && m.InRange(this, 8))
                {
                    if (Utility.RandomBool())
                    {
                        switch (Utility.Random(5))
                        {
                            case 0:
                                {
                                    this.Say("Back to your cage wretched dog!");
                                    break;
                                }
                            case 1:
                                {
                                    this.Say("Thinking of escape eh?");
                                    this.Say("We�ll just see about that!");
                                    break;
                                }
                            case 2:
                                {
                                    this.Say("*blows whistle*");
                                    this.Say("Escape! Escape!");
                                    break;
                                }
                            case 3:
                                {
                                    this.Say("I see you�ve lost your way.");
                                    this.Say("Shall I see you to the prison cemetery?");
                                    break;
                                }
                            case 4:
                                {
                                    this.Say("Yes, run away!");
                                    this.Say("Ah, hah hah hah!");
                                    break;
                                }
                        }

                        m_NextSpeechTime = DateTime.UtcNow + m_SpeechDelay;
                    }
                }
            }

            base.OnMovement(m, oldLocation);
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
        }
    }
}
