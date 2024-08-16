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

/* Scripts/Engines/SDrop/EnchantedScroll.cs
 * ChangeLog:
 * 2/28/09, Adam
 *		Change EnchantedScroll to return a dud scroll 60% of the time (regular boots)
 *		This is a stopgap measure until we recast the SDrop system
 * 2/25/08, Pix
 *		Fixed OnsingleClick order so that the multiple lines of text aren't over eachother.
 *	8/11/05, erlein
 *		- Added two newline characters to name formatting to aid localization formatting
 *		- Moved the type check performed on enhancement attempt to SDrop.cs
 *		- Added OnSingleClick() call to display properties after successful enhancement
 *	7/13/05, erlein
 *		Changed text back to regular labelling display.
 *	7/13/05, erlein
 *		Initial creation.
 */

using Server.Engines;
using Server.Targeting;

namespace Server.Items
{
    public class EnchantedScroll : EnchantedItem
    {
        public override double SuccessAdjust
        {
            get
            {
                return CoreAI.EScrollSuccess;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override bool Identified
        {
            get
            {
                return m_Identified;
            }
            set
            {
                m_Identified = value;

                if (value == true)
                    this.Name = string.Format("an enchanted {0} scroll\n\n", sApproxLabel);
                else
                    this.Name = "an enchanted scroll";
            }
        }

        // Construct from object reference of magical item passed us

        public EnchantedScroll(object miref, int baseimage)
            : base(baseimage)
        {
            Weight = 1.0;
            base.Name = "an enchanted scroll";
            Stackable = false;

            // only a 40% chance to suceed
            if (Server.Utility.RandomChance(40))
                // copy props
                base.GenerateiProps(miref);
            else
                base.GenerateiProps(new Rocks());

            ((Item)miref).Delete();
        }

        public EnchantedScroll(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_Identified)
            {
                // Create target for use
                from.SendMessage("Choose the item you wish to enchant...");
                from.Target = new EnchantedScrollTarget(this);
            }
            else
                from.SendMessage("The scroll must be identified first.");
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);
            this.LabelTo(from, Name);
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


    public class EnchantedScrollTarget : Target
    {
        private EnchantedScroll m_EnchantedScroll;

        public EnchantedScrollTarget(EnchantedScroll escroll)
            : base(1, false, TargetFlags.None)
        {
            m_EnchantedScroll = escroll;
        }

        protected override void OnTarget(Mobile from, object target)
        {
            if (!(target is Item))
            {
                from.SendMessage("You must target the item you wish to enhance.");
                return;
            }

            // Create SDrop instance to handle enhancement operation

            SDrop SDropEI = new SDrop(m_EnchantedScroll, (Item)target, from);
            if (!SDropEI.CanEnhance())
                return;

            // Perform enhancement here

            if (Utility.RandomDouble() < SDropEI.EnhanceChance())
            {
                SDropEI.DoEnhance();
                ((Item)target).OnSingleClick(from);

                from.SendMessage("You successfully enchant the item with the magic of the scroll!");
            }
            else
            {
                SDropEI.DoFailure();
                from.SendMessage("You fail to perform the enchantment! Both the scroll and item have been destroyed!");
            }
        }
    }
}
