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

/* Scripts/Items/Misc/SnowPile.cs
 * ChangeLog
 *	12/25/04, Adam
 *		First time check-in.
 */

using Server.Targeting;
using System;

namespace Server.Items
{

    public class SnowPile : Item
    {
        [Constructable]
        public SnowPile()
            : base(0x913)
        {
            Hue = 0x481;
            Name = "Snow";
            m_NextAbilityTime = DateTime.Now;

        }


        public SnowPile(Serial serial)
            : base(serial)
        {
        }

        private DateTime m_NextAbilityTime;
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version 

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            m_NextAbilityTime = DateTime.Now;
        }

        public override void OnSingleClick(Mobile from)
        {
            this.LabelTo(from, 1005578);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042010); //You must have the object in your backpack to use it. 
                return;
            }
            else
            {
                if (DateTime.Now >= m_NextAbilityTime)
                {
                    from.Target = new SnowTarget(from, this);
                    from.SendLocalizedMessage(1005575); // You carefully pack the snow into a ball... 
                }
                else
                {
                    from.SendLocalizedMessage(1005574);
                }
            }
        }

        private class SnowTarget : Target
        {
            private Mobile m_Thrower;
            private SnowPile m_Snow;

            public SnowTarget(Mobile thrower, SnowPile snow)
                : base(10, false, TargetFlags.None)
            {
                m_Thrower = thrower;
                m_Snow = snow;
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target == from)
                    from.SendLocalizedMessage(1005576);
                else if (target is Mobile)
                {
                    Mobile m = (Mobile)target;
                    from.PlaySound(0x145);
                    from.Animate(9, 1, 1, true, false, 0);
                    from.SendLocalizedMessage(1010573); // You throw the snowball and hit the target! 
                    m.SendLocalizedMessage(1010572); // You have just been hit by a snowball! 
                    Effects.SendMovingEffect(from, m, 0x36E4, 7, 0, false, true, 0x480, 0);
                    m_Snow.m_NextAbilityTime = DateTime.Now + TimeSpan.FromSeconds(5.0);
                }
                else
                {
                    from.SendLocalizedMessage(1005577);
                }
            }

        }
    }


}
// created on 16/11/2002 at 19:27
