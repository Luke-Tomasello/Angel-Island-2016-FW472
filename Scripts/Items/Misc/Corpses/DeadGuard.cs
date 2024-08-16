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

/* Scripts/Mobiles/Guards/DeadGuard.cs
 * CHANGELOG
 *  09/06/05 Taran Kain
 *		Set StaticCorpse to true in OnDeath to prevent looting.
 */

using Server.Items;
using System;

namespace Server.Mobiles
{
    [CorpseName("a dead guard")]
    public class DeadGuard : BaseGuard
    {
        [Constructable]
        public DeadGuard()
            : base(null)
        {
            this.Direction = (Direction)Utility.Random(8);

            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i] is Halberd)
                    ((Item)Items[i]).Movable = true;
            }

            //AddItem(new Halberd());

            Timer.DelayCall(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.0), new TimerCallback(Kill));
        }

        public DeadGuard(Serial serial)
            : base(serial)
        {
        }

        public override Mobile Focus
        {
            get { return null; }
            set {; }
        }

        public override bool OnBeforeDeath()
        {
            return true;
        }

        public override void OnDeath(Server.Items.Container c)
        {
            base.OnDeath(c);

            Corpse corpse = c as Corpse;
            corpse.BeginDecay(TimeSpan.FromHours(24.0));
            corpse.StaticCorpse = true;
            for (int i = 0; i < 3; i++)
            {
                Point3D p = new Point3D(Location);
                p.X += Utility.RandomMinMax(-1, 1);
                p.Y += Utility.RandomMinMax(-1, 1);
                new Blood(Utility.Random(0x122A, 5), 86400.0).MoveToWorld(p, c.Map);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
        }
    }
}
