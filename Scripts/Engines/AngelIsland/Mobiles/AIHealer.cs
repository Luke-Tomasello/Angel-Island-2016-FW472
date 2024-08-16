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

// /Scripts/Mobiles/Healers/AIHealer.cs
// Created 3/28/04 by mith, copied from EvilHealer.cs
// ChangeLog
// 4/1/04, changes by mith
//	Added Frozen and Direction properties so healer won't wander and always faces south.
// 3/28/04
//	Removed code to generate AIStinger, this has been moved to the AITeleporter.
//	Removed ability to buy/sell items and teach skills.
//	Set "AlwaysMurderer" flag to false so that NPC shows as blue.

namespace Server.Mobiles
{
    public class AIHealer : BaseHealer
    {
        public override bool CanTeach { get { return false; } }
        public override bool CheckTeach(SkillName skill, Mobile from)
        {
            if (!base.CheckTeach(skill, from))
                return false;

            return (skill == SkillName.Forensics)
                || (skill == SkillName.Healing)
                || (skill == SkillName.SpiritSpeak)
                || (skill == SkillName.Swords);
        }

        [Constructable]
        public AIHealer()
        {
            Title = "the healer";

            Karma = -10000;

            SetSkill(SkillName.Forensics, 80.0, 100.0);
            SetSkill(SkillName.SpiritSpeak, 80.0, 100.0);
            SetSkill(SkillName.Swords, 80.0, 100.0);

            Frozen = true;
            Direction = Direction.South;
        }

        public override bool AlwaysMurderer { get { return false; } }
        public override bool IsActiveVendor { get { return false; } }

        public override void InitSBInfo()
        {
            SBInfos.Add(new SBHealer());
        }

        public override bool CheckResurrect(Mobile m)
        {
            // This code moved to AITeleporter so users only get one per visit.
            //Item aiStinger = new Server.Items.AIStinger();
            //if ( !m.AddToBackpack( aiStinger ) )
            //aiStinger.Delete();

            return true;
        }

        public AIHealer(Serial serial)
            : base(serial)
        {
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
