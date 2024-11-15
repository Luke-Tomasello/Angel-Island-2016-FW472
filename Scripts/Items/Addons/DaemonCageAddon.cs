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

/////////////////////////////////////////////////
//
// Automatically generated by the
// AddonGenerator script by Arya
//
/////////////////////////////////////////////////
namespace Server.Items
{
    public class DaemonCageAddon : BaseAddon
    {
        public override BaseAddonDeed Deed
        {
            get
            {
                return new DaemonCageAddonDeed();
            }
        }

        [Constructable]
        public DaemonCageAddon()
        {
            AddonComponent ac = null;
            ac = new AddonComponent(1314);
            AddComponent(ac, -2, -1, 0);
            ac = new AddonComponent(1824);
            AddComponent(ac, 2, -1, 11);
            ac = new AddonComponent(1981);
            AddComponent(ac, 1, -1, 6);
            ac = new AddonComponent(1824);
            AddComponent(ac, -1, 2, 7);
            ac = new AddonComponent(1781);
            AddComponent(ac, 0, 2, 8);
            ac = new AddonComponent(3685);
            ac.Light = LightType.Circle225;
            AddComponent(ac, 0, 0, 8);
            ac = new AddonComponent(6942);
            AddComponent(ac, 2, 3, 0);
            ac = new AddonComponent(1824);
            AddComponent(ac, -1, -1, 8);
            ac = new AddonComponent(1824);
            AddComponent(ac, 2, 2, 13);
            ac = new AddonComponent(1987);
            AddComponent(ac, -1, -1, 6);
            ac = new AddonComponent(6941);
            AddComponent(ac, 3, -1, 0);
            ac = new AddonComponent(6941);
            AddComponent(ac, 3, 2, 0);
            ac = new AddonComponent(6942);
            AddComponent(ac, -1, 3, 0);
            ac = new AddonComponent(3676);
            ac.Light = LightType.Circle150;
            AddComponent(ac, -1, -1, 28);
            ac = new AddonComponent(3676);
            ac.Light = LightType.Circle150;
            AddComponent(ac, 2, -1, 26);
            ac = new AddonComponent(3676);
            ac.Light = LightType.Circle150;
            AddComponent(ac, 2, 2, 28);
            ac = new AddonComponent(3676);
            ac.Light = LightType.Circle150;
            AddComponent(ac, -1, 2, 27);
            ac = new AddonComponent(1314);
            AddComponent(ac, -2, 2, 0);
            ac = new AddonComponent(1314);
            AddComponent(ac, -2, -2, 0);
            ac = new AddonComponent(1314);
            AddComponent(ac, 2, -2, 0);
            ac = new AddonComponent(1314);
            AddComponent(ac, 3, -1, 0);
            ac = new AddonComponent(1314);
            AddComponent(ac, 3, 2, 0);
            ac = new AddonComponent(1314);
            AddComponent(ac, 2, 3, 0);
            ac = new AddonComponent(1314);
            AddComponent(ac, -1, 3, 0);
            ac = new AddonComponent(1782);
            AddComponent(ac, 2, 1, 8);
            ac = new AddonComponent(1824);
            AddComponent(ac, -1, 2, 0);
            ac = new AddonComponent(1824);
            AddComponent(ac, 2, -1, 0);
            ac = new AddonComponent(1824);
            AddComponent(ac, 2, 2, 1);
            ac = new AddonComponent(1782);
            AddComponent(ac, -2, 0, 8);
            ac = new AddonComponent(1782);
            AddComponent(ac, -2, 1, 8);
            ac = new AddonComponent(1782);
            AddComponent(ac, 2, 0, 8);
            ac = new AddonComponent(1824);
            AddComponent(ac, -1, 2, 22);
            ac = new AddonComponent(1824);
            AddComponent(ac, -1, 2, 17);
            ac = new AddonComponent(1824);
            AddComponent(ac, -1, 2, 12);
            ac = new AddonComponent(1824);
            AddComponent(ac, 2, 2, 23);
            ac = new AddonComponent(1824);
            AddComponent(ac, 2, 2, 18);
            ac = new AddonComponent(1824);
            AddComponent(ac, 2, -1, 21);
            ac = new AddonComponent(1824);
            AddComponent(ac, 2, -1, 16);
            ac = new AddonComponent(1824);
            AddComponent(ac, 2, -1, 6);
            ac = new AddonComponent(1824);
            AddComponent(ac, -1, -1, 23);
            ac = new AddonComponent(1824);
            AddComponent(ac, -1, -1, 18);
            ac = new AddonComponent(1824);
            AddComponent(ac, -1, -1, 13);
            ac = new AddonComponent(1781);
            AddComponent(ac, 1, -2, 8);
            ac = new AddonComponent(1781);
            AddComponent(ac, 0, -2, 8);
            ac = new AddonComponent(1781);
            AddComponent(ac, 1, 2, 8);
            ac = new AddonComponent(3688);
            ac.Light = LightType.Circle225;
            AddComponent(ac, 1, 1, 8);
            ac = new AddonComponent(3682);
            ac.Light = LightType.Circle225;
            AddComponent(ac, 1, 0, 8);
            ac = new AddonComponent(3676);
            ac.Light = LightType.Circle225;
            AddComponent(ac, 0, 1, 8);
            ac = new AddonComponent(1986);
            AddComponent(ac, 2, 2, 6);
            ac = new AddonComponent(1985);
            AddComponent(ac, -1, 2, 6);
            ac = new AddonComponent(1984);
            AddComponent(ac, 2, -1, 5);
            ac = new AddonComponent(1981);
            AddComponent(ac, 0, 0, 6);
            ac = new AddonComponent(1981);
            AddComponent(ac, 1, 1, 6);
            ac = new AddonComponent(1981);
            AddComponent(ac, 1, 0, 6);
            ac = new AddonComponent(1981);
            AddComponent(ac, 0, 1, 6);
            ac = new AddonComponent(1981);
            AddComponent(ac, 0, -1, 6);
            ac = new AddonComponent(1981);
            AddComponent(ac, 2, 0, 6);
            ac = new AddonComponent(1981);
            AddComponent(ac, 2, 1, 6);
            ac = new AddonComponent(1981);
            AddComponent(ac, 1, 2, 6);
            ac = new AddonComponent(1981);
            AddComponent(ac, 0, 2, 6);
            ac = new AddonComponent(1981);
            AddComponent(ac, -1, 0, 6);
            ac = new AddonComponent(1981);
            AddComponent(ac, -1, 1, 6);
            ac = new AddonComponent(1824);
            AddComponent(ac, 2, 2, 8);
            ac = new AddonComponent(3633);
            ac.Light = LightType.Circle300;
            AddComponent(ac, 1, 2, 0);

        }

        public DaemonCageAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // Version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class DaemonCageAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new DaemonCageAddon();
            }
        }

        [Constructable]
        public DaemonCageAddonDeed()
        {
            Name = "DaemonCage";
        }

        public DaemonCageAddonDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // Version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
