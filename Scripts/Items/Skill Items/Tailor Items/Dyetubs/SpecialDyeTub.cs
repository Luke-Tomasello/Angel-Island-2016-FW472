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

/* Scripts/Items/Skill Items/Tailor Items/Misc/SpecialDyeTub.cs
 * ChangeLog:
 *  2/4/07, Adam
 *      Add back old style SpecialDyeTub as SpecialDyeTubClassic
 *  01/04/07, plasma
 *      Added two read only properties that indicate if a tub can be lightened/darkened
 *	10/16/05, erlein
 *		Altered use of "Prepped" to define whether tub has been darkened or lightened already.
 *		Added appropriate deserialization to handle old tubs.
 *	10/15/05, erlein
 *		Added checks to ensure dye tub and targetted clothing is in backpack.
 *		Added stack handling for dying of multiple color swatches.
 *		Added check to ensure only clothing is targetted in dying process.
 *	10/15/05, erlein
 *		Initial creation (complete re-write).
 */

using Server.Targeting;

namespace Server.Items
{
    public class SpecialDyeTubClassic : DyeTub, Engines.VeteranRewards.IRewardItem
    {
        public override CustomHuePicker CustomHuePicker { get { return CustomHuePicker.SpecialDyeTub; } }
        public override int LabelNumber { get { return 1041285; } } // Special Dye Tub

        private bool m_IsRewardItem;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRewardItem
        {
            get { return m_IsRewardItem; }
            set { m_IsRewardItem = value; }
        }

        [Constructable]
        public SpecialDyeTubClassic()
        {
            LootType = LootType.Blessed;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_IsRewardItem && !Engines.VeteranRewards.RewardSystem.CheckIsUsableBy(from, this, null))
                return;

            base.OnDoubleClick(from);
        }

        public SpecialDyeTubClassic(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((bool)m_IsRewardItem);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_IsRewardItem = reader.ReadBool();
                        break;
                    }
            }
        }
    }

    public class SpecialDyeTub : DyeTub
    {
        // Stored color (where we've got to in the dying process)
        private int m_StoredColor;
        private string m_StoredColorName;
        private string m_StoredColorNamePrefix;

        private bool m_Prepped;
        private int m_Uses;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Uses
        {
            get { return m_Uses; }
            set { m_Uses = value; }
        }

        // Interfaces

        // Have we used the dye we made and tested with this color swatch yet?

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Prepped
        {
            get
            {
                return m_Prepped;
            }

            set
            {
                m_Prepped = value;

            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int StoredColor
        {
            get
            {
                return m_StoredColor;
            }
            set
            {
                m_StoredColor = value;
                Hue = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string StoredColorName
        {
            get
            {
                return m_StoredColorName;
            }
            set
            {
                m_StoredColorName = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string StoredColorNamePrefix
        {
            get
            {
                return m_StoredColorNamePrefix;
            }
            set
            {
                m_StoredColorNamePrefix = value;
            }
        }

        //pla, 01/04/07
        // Added two readonly bool props to indicate if the tub can be lightened/darkened
        [CommandProperty(AccessLevel.GameMaster)]
        public bool CanLighten
        {
            get
            {
                if (StoredColorNamePrefix.Trim() == "the lightest")
                    return false;
                else
                    return true;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CanDarken
        {
            get
            {
                if (StoredColorNamePrefix.Trim() == "the darkest")
                    return false;
                else
                    return true;
            }
        }


        // Special colors are defined by their starting index and the number
        // of colors in the set
        private static short[,] m_SpecialColors = new short[,]
            {
                { 1230, 6 },			//	Violet			1230 - 1235 (6)
				{ 1501, 8 },			//	Tan             1501 - 1508 (8)
				{ 2013, 5 },			//	Brown           2012 - 2017 (5)
				{ 1303, 6 },			//	Dark Blue		1303 - 1308 (6)
				{ 1420, 7 },			//	Forest Green	1420 - 1426 (7)
				{ 1619, 8 },			//	Pink         	1619 - 1626 (8)
				{ 1640, 5 },			//	Red           	1640 - 1644 (5)
				{ 2001, 5 }				//	Olive			2001 - 2005 (5)
			};

        // Don't consume this if we're using it to mix ;)
        public override void Consume(int amount)
        {
        }

        [Constructable]
        public SpecialDyeTub()
        {
            Redyable = false;
            m_StoredColorName = "";
            m_StoredColorNamePrefix = RelPosPrefix(0);
            m_Uses = 1;
            Prepped = false;
        }

        public SpecialDyeTub(Serial serial)
            : base(serial)
        {
        }

        // Sets the color accordingly
        public bool StoreColor(string sDyeColor)
        {
            // Peel it off the stack
            Stackable = false;

            int index;

            if (sDyeColor == "Violet")
                index = 0;
            else if (sDyeColor == "Tan")
                index = 1;
            else if (sDyeColor == "Brown")
                index = 2;
            else if (sDyeColor == "Dark Blue")
                index = 3;
            else if (sDyeColor == "Forest Green")
                index = 4;
            else if (sDyeColor == "Pink")
                index = 5;
            else if (sDyeColor == "Red")
                index = 6;
            else if (sDyeColor == "Olive")
                index = 7;
            else
                return false;

            StoredColor = m_SpecialColors[index, 0];
            m_StoredColorName = sDyeColor;
            Hue = DyedHue = StoredColor;

            return true;
        }

        // Calculates text
        public string RelPosPrefix(int iRelPos)
        {
            switch (iRelPos)
            {
                case 0:
                    return "the lightest ";
                case 1:
                    return "a very light ";
                case 2:
                    return "a light ";
                case 3:
                    return "a dark ";
                case 4:
                    return "a very dark ";
                case 5:
                    return "the darkest ";
            }

            return "";
        }

        // Darkens the mix
        public bool DarkenMix()
        {
            for (int i = 0; i < 8; i++)
            {
                if (m_StoredColor >= m_SpecialColors[i, 0] &&
                    m_StoredColor <= m_SpecialColors[i, 0] + m_SpecialColors[i, 1] &&
                    m_SpecialColors[i, 0] + m_SpecialColors[i, 1] >= m_StoredColor + 1)
                {
                    StoredColor++;

                    double dRelPos = m_StoredColor - m_SpecialColors[i, 0];
                    dRelPos /= m_SpecialColors[i, 1];
                    dRelPos *= 5;

                    if (dRelPos < 1 && dRelPos != 0)
                        dRelPos = 1;
                    else if (dRelPos > 4 && dRelPos != 5)
                        dRelPos = 4;

                    StoredColorNamePrefix = RelPosPrefix((int)dRelPos);
                    Prepped = true;

                    return true;
                }
            }

            return false;
        }

        // Lightens the mix
        public bool LightenMix()
        {
            for (int i = 0; i < 8; i++)
            {
                if (m_StoredColor >= m_SpecialColors[i, 0] &&
                    m_StoredColor <= m_SpecialColors[i, 0] + m_SpecialColors[i, 1] &&
                    m_SpecialColors[i, 0] <= m_StoredColor - 1)
                {
                    StoredColor--;

                    double dRelPos = m_StoredColor - m_SpecialColors[i, 0];
                    dRelPos /= m_SpecialColors[i, 1];
                    dRelPos *= 5;

                    if (dRelPos < 1 && dRelPos != 0)
                        dRelPos = 1;
                    else if (dRelPos > 4 && dRelPos != 5)
                        dRelPos = 4;

                    StoredColorNamePrefix = RelPosPrefix((int)dRelPos);
                    Prepped = true;

                    return true;
                }
            }

            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
            writer.Write(m_StoredColorName);
            writer.Write(m_StoredColorNamePrefix);
            writer.Write((short)m_StoredColor);
            writer.Write(m_Prepped);
            writer.Write((short)m_Uses);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            m_StoredColorName = reader.ReadString();
            m_StoredColorNamePrefix = reader.ReadString();
            m_StoredColor = (int)reader.ReadShort();
            m_Prepped = reader.ReadBool();
            m_Uses = (int)reader.ReadShort();

            // Handle tubs which have been lightened / darkened and not marked prepped for vice versa
            if (version == 0)
            {
                Prepped = true;
                for (int i = 0; i < 8; i++)
                    if (StoredColor == m_SpecialColors[i, 0])
                        Prepped = false;
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            // Say what colour it is

            if (m_StoredColorName == "")
                from.SendMessage("This tub has not yet been used to make any dye.");
            else
                from.SendMessage("You examine the tub and note it is " + m_StoredColorNamePrefix + "shade of " + m_StoredColorName.ToLower() + ".");

            this.LabelTo(from, "special dye tub");
            this.LabelTo(from, string.Format("{0} uses remaining", this.m_Uses));

            base.OnSingleClick(from);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!this.IsChildOf(from.Backpack))
            {
                from.SendMessage("The special dye tub must be in your backpack.");
                return;
            }

            if (from.InRange(this.GetWorldLocation(), 1))
            {
                from.SendMessage("Target the clothing to dye.");
                from.Target = new InternalTarget(this);
            }
            else
                from.SendLocalizedMessage(500446); // That is too far away.
        }

        private class InternalTarget : Target
        {
            private SpecialDyeTub m_Tub;

            public InternalTarget(SpecialDyeTub tub)
                : base(1, false, TargetFlags.None)
            {
                m_Tub = tub;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                // Use the same targetting logic as a regular dye tub, only with uses
                // decrementation (providing not just a color swatch)

                if (targeted is ColorSwatch)
                {
                    ColorSwatch cs = (ColorSwatch)targeted;
                    if (cs.Amount > 1)
                    {
                        cs.Amount--;
                        cs = null;

                        cs = new ColorSwatch();
                        cs.Stackable = false;

                        from.AddToBackpack(cs);
                    }
                    cs.StoredColorName = m_Tub.StoredColorNamePrefix + "shade of " + m_Tub.StoredColorName;
                    cs.Hue = m_Tub.Hue;
                }
                else if (targeted is Item)
                {
                    Item item = (Item)targeted;

                    if (item is BaseClothing)
                    {
                        if (!from.InRange(m_Tub.GetWorldLocation(), 1) || !from.InRange(item.GetWorldLocation(), 1))
                            from.SendLocalizedMessage(500446);      // That is too far away.

                        else if (!item.IsChildOf(from.Backpack))
                            from.SendMessage("The item you are dying must be in your backpack.");

                        else if (item.Parent is Mobile)
                            from.SendLocalizedMessage(500861);      // Can't Dye clothing that is being worn.

                        else if (((IDyable)item).Dye(from, m_Tub))
                        {
                            from.PlaySound(0x23E);
                            if (--m_Tub.m_Uses == 0)
                            {
                                m_Tub.Delete();
                                from.AddToBackpack(new DyeTub());
                            }

                            item.Hue = m_Tub.Hue;
                        }
                    }
                    else
                    {
                        from.SendMessage("You can only dye clothing with special dyes. You're trying to dye {0}", targeted);

                    }
                }
            }
        }
    }

    // Seeing as the craft system requires a type of item to craft,
    // we need a SpecialDye() class that clears itself up

    public class SpecialDye : Item
    {
        public SpecialDye()
            : base(0xFA9)
        {
            this.Delete();
        }

        public SpecialDye(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
        }

        public override void Deserialize(GenericReader reader)
        {
        }
    }
}
