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

/* Scripts\Items\Skill Items\Tailor Items\Dyetubs\LeatherArmorDyeTub.cs
 * CHANGELOG:
 *  12/20/06, Adam
 *      Unfortunately certain 'bone' armor is using leather as the resource;
 *        therefore we make an explicit type check.
 *	6/14/06, Adam
 *		Add the color to the 'name'
 *	6/9/06, Adam
 *		Initial Version
 */

using Server.Multis;
using Server.Targeting;
using System;

namespace Server.Items
{
    public class LeatherArmorDyeTub : DyeTub
    {
        public override bool AllowDyables { get { return false; } }
        public override bool AllowRunebooks { get { return false; } }

        private int m_Uses;

        //are differnt resource metal types and weight table
        int[] m_Color = new int[]
            {
                0x8AB,												// Valorite,	1 in 36 chance
				0x89F,0x89F,										// Verite,		2 in 36 chance
				0x979,0x979,0x979,									// Agapite,		3 in 36 chance
				0x8A5,0x8A5,0x8A5,0x8A5,							// Gold,		4 in 36 chance
				0x972,0x972,0x972,0x972,0x972,						// Bronze,		5 in 36 chance
				0x96D,0x96D,0x96D,0x96D,0x96D,0x96D,				// Copper,		6 in 36 chance
				0x966,0x966,0x966,0x966,0x966,0x966,0x966,			// ShadowIron,	7 in 36 chance
				0x973,0x973,0x973,0x973,0x973,0x973,0x973,0x973,	// DullCopper,	8 in 36 chance
			};

        [CommandProperty(AccessLevel.GameMaster)]
        public int Uses
        {
            get { return m_Uses; }
            set { m_Uses = value; }
        }

        [Constructable]
        public LeatherArmorDyeTub()
        {
            m_Uses = 100;
            this.DyedHue = m_Color[Utility.Random(m_Color.Length)];
            Name = String.Format("leather armor dye tub ({0})", ColorNameLookup(this.DyedHue));
            this.Redyable = false;
        }

        public override void OnSingleClick(Mobile from)
        {
            this.LabelTo(from, Name);
            this.LabelTo(from, string.Format("{0} uses remaining", this.m_Uses));
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(this.GetWorldLocation(), 1))
            {
                from.SendMessage("Target the leather armor to dye.");
                from.Target = new InternalTarget(this);
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }

        public LeatherArmorDyeTub(Serial serial)
            : base(serial)
        {
        }

        private string ColorNameLookup(int id)
        {
            switch (id)
            {
                case 0x8AB: return "Valorite";
                case 0x89F: return "Verite";
                case 0x979: return "Agapite";
                case 0x8A5: return "Gold";
                case 0x972: return "Bronze";
                case 0x96D: return "Copper";
                case 0x966: return "Shadow Iron";
                case 0x973: return "Dull Copper";
                default: return "Unknown";
            }
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((int)m_Uses);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Uses = reader.ReadInt();
                        break;
                    }
            }
        }


        private class InternalTarget : Target
        {
            private LeatherArmorDyeTub m_Tub;

            public InternalTarget(LeatherArmorDyeTub tub)
                : base(1, false, TargetFlags.None)
            {
                m_Tub = tub;
            }

            private bool IsLeatherArmor(object item)
            {
                if (item is BaseArmor)
                {
                    BaseArmor ba = item as BaseArmor;
                    if (ba == null)
                        return false;

                    // Unfortunately certain 'bone' armor is using leather as the resource.
                    //  therefore we make an explicit type check.
                    if (ba.GetType().ToString().Contains("LeatherCap") ||
                        ba.GetType().ToString().Contains("FemaleLeatherChest") || ba.GetType().ToString().Contains("FemaleStuddedChest") ||
                        ba.GetType().ToString().Contains("LeatherArms") || ba.GetType().ToString().Contains("StuddedArms") ||
                        ba.GetType().ToString().Contains("LeatherBustierArms") || ba.GetType().ToString().Contains("StuddedBustierArms") ||
                        ba.GetType().ToString().Contains("LeatherChest") || ba.GetType().ToString().Contains("StuddedChest") ||
                        ba.GetType().ToString().Contains("LeatherGloves") || ba.GetType().ToString().Contains("StuddedGloves") ||
                        ba.GetType().ToString().Contains("LeatherGorget") || ba.GetType().ToString().Contains("StuddedGorget") ||
                        ba.GetType().ToString().Contains("LeatherLegs") || ba.GetType().ToString().Contains("StuddedLegs") ||
                        ba.GetType().ToString().Contains("LeatherShorts") ||
                        ba.GetType().ToString().Contains("LeatherSkirt"))
                        if (ba.Resource == CraftResource.RegularLeather ||
                            ba.Resource == CraftResource.SpinedLeather ||
                            ba.Resource == CraftResource.HornedLeather ||
                            ba.Resource == CraftResource.BarbedLeather)
                            return true;
                }

                return false;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is BaseArmor && IsLeatherArmor(targeted))
                {
                    Item item = (Item)targeted;

                    if (!from.InRange(m_Tub.GetWorldLocation(), 1) || !from.InRange(item.GetWorldLocation(), 1))
                    {
                        from.SendLocalizedMessage(500446); // That is too far away.
                    }
                    else
                    {
                        bool okay = (item.IsChildOf(from.Backpack));

                        if (!okay)
                        {
                            if (item.Parent == null)
                            {
                                BaseHouse house = BaseHouse.FindHouseAt(item);

                                if (house == null || !house.IsLockedDown(item))
                                    from.SendMessage("The leather armor must be locked down to dye it.");
                                else if (!house.IsCoOwner(from))
                                    from.SendLocalizedMessage(501023); // You must be the owner to use this item.
                                else
                                    okay = true;
                            }
                            else
                            {
                                from.SendMessage("The leather armor must be in your backpack to be dyed.");
                            }
                        }

                        if (okay)
                        {
                            m_Tub.m_Uses--;
                            item.Hue = m_Tub.DyedHue;
                            from.PlaySound(0x23E);

                            if (m_Tub.m_Uses <= 0)
                            {
                                m_Tub.Delete();
                            }
                        }
                    }
                }
                else
                {
                    from.SendMessage("That is not leather armor.");
                }
            }
        }


    }
}
