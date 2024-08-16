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

/* Scripts/Items/Skill Items/Specialized/StoneGraver.cs
 * ChangeLog:
 *	04/07/05, Kitaras
 *		Initial creation
 */

using Server.Misc;
using Server.Mobiles;
using Server.Network;
using Server.Prompts;
using Server.Targeting;
using System.Text.RegularExpressions;

namespace Server.Items
{

    // Graver target class

    public class StoneGraverTarget : Target
    {
        private StoneGraver m_Graver;

        public StoneGraverTarget(StoneGraver graver)
            : base(1, false, TargetFlags.None)
        {
            m_Graver = graver;
        }

        protected override void OnTarget(Mobile from, object target) // Override the protected OnTarget() for our feature
        {
            // Check targetted thing is a container

            if (target is BaseGraveStone)
            {

                // Is a container, so cast

                BaseGraveStone bc = (BaseGraveStone)target;

                // Check player crafted

                if (!bc.IsChildOf(from.Backpack))
                {
                    from.SendMessage("The gravestone you wish to engrave must be in your backpack.");
                    return;
                }

                from.SendMessage("Please enter the words you wish to engrave :");
                from.Prompt = new RenamePrompt(from, bc, m_Graver);
            }
            else
            {
                // Not a container

                from.SendMessage("This tool can only be used on a gravestone.");
            }

        }

        // Handles the renaming prompt and associated validation

        private class RenamePrompt : Prompt
        {
            private Mobile m_from;
            private BaseGraveStone m_container;
            private StoneGraver m_graver;

            public RenamePrompt(Mobile from, BaseGraveStone container, StoneGraver graver)
            {
                m_from = from;
                m_container = container;
                m_graver = graver;
            }

            public override void OnResponse(Mobile from, string text)
            {

                // Pattern match for invalid characters
                Regex InvalidPatt = new Regex("[^-a-zA-Z0-9' ]");

                if (InvalidPatt.IsMatch(text))
                {

                    // Invalid chars
                    from.SendMessage("You may only engrave numbers, letters, apostrophes and hyphens.");

                }
                else if (!NameVerification.Validate(text, 2, 30, true, true, true, 1, NameVerification.SpaceDashPeriodQuote))
                {

                    // Invalid for some other reason
                    from.SendMessage("You may not name it this here.");

                }
                else
                {

                    // Make the change
                    m_container.Name = text;
                    from.SendMessage("You successfully engrave the container.");

                    // Decrement UsesRemaining of graver
                    m_graver.UsesRemaining--;

                    // Check for 0 charges and delete if has none left
                    if (m_graver.UsesRemaining == 0)
                    {
                        m_graver.Delete();
                        from.SendMessage("You have worn out your tool!");
                    }

                }

            }

        }

    }

    // Main type class, including WoodEngraving check on PlayerMobile

    public class StoneGraver : Item
    {

        private int m_UsesRemaining;

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get { return m_UsesRemaining; }
            set
            {
                m_UsesRemaining = value;
                InvalidateProperties();
            }
        }

        [Constructable]
        public StoneGraver()
            : base(4135)
        {
            base.Weight = 1.0;
            base.Name = "a stone graver";
            UsesRemaining = 10;
        }

        public StoneGraver(Serial serial)
            : base(serial)
        {
        }

        // UsesRemaining property handling

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(1060584, m_UsesRemaining.ToString()); // uses remaining: ~1_val~
        }

        public virtual void DisplayDurabilityTo(Mobile m)
        {
            LabelToAffix(m, 1017323, AffixType.Append, ": " + m_UsesRemaining.ToString()); // Durability
        }

        public override void OnSingleClick(Mobile from)
        {
            DisplayDurabilityTo(from);
            base.OnSingleClick(from);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
            writer.Write((int)m_UsesRemaining); // Uses remaining
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_UsesRemaining = reader.ReadInt(); // Uses remaining
                        break;
                    }

            }

        }

        public override void OnDoubleClick(Mobile from)
        {
            // Make sure is in pack
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            PlayerMobile pm = (PlayerMobile)from;

            // Confirm person using it has learn(t||ed) he engraving skill!
            if (pm.Masonry == false)
            {
                pm.SendMessage("You have not learned how to use this tool.");
                return;
            }

            // Create target and call it
            pm.SendMessage("Choose the gravestone you wish to engrave");
            pm.Target = new StoneGraverTarget(this);

        }

    }

}
