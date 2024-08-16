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

/* Items/SkillItems/Tools/BaseTool.cs
 * CHANGELOG:
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Engines.Craft;
using Server.Engines.OldSchoolCraft;
using Server.Network;
using System;

namespace Server.Items
{
    public enum ToolQuality
    {
        Low,
        Regular,
        Exceptional
    }

    public abstract class BaseTool : Item, IUsesRemaining, ICraftable
    {
        private Mobile m_Crafter;
        private ToolQuality m_Quality;
        private int m_UsesRemaining;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Crafter
        {
            get { return m_Crafter; }
            set { m_Crafter = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public ToolQuality Quality
        {
            get { return m_Quality; }
            set { UnscaleUses(); m_Quality = value; InvalidateProperties(); ScaleUses(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get { return m_UsesRemaining; }
            set { m_UsesRemaining = value; InvalidateProperties(); }
        }

        public void ScaleUses()
        {
            m_UsesRemaining = (m_UsesRemaining * GetUsesScalar()) / 100;
            InvalidateProperties();
        }

        public void UnscaleUses()
        {
            m_UsesRemaining = (m_UsesRemaining * 100) / GetUsesScalar();
        }

        public int GetUsesScalar()
        {
            if (m_Quality == ToolQuality.Exceptional)
                return 200;

            return 100;
        }

        public bool ShowUsesRemaining { get { return true; } set { } }

        public abstract CraftSystem CraftSystem { get; }

        public BaseTool(int itemID)
            : this(50, itemID)
        {
        }

        public BaseTool(int uses, int itemID)
            : base(itemID)
        {
            m_UsesRemaining = uses;
            m_Quality = ToolQuality.Regular;
        }

        public BaseTool(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            // Makers mark not displayed on OSI
            //if ( m_Crafter != null )
            //	list.Add( 1050043, m_Crafter.Name ); // crafted by ~1_NAME~

            if (m_Quality == ToolQuality.Exceptional)
                list.Add(1060636); // exceptional

            list.Add(1060584, m_UsesRemaining.ToString()); // uses remaining: ~1_val~
        }

        public virtual void DisplayDurabilityTo(Mobile m)
        {
            LabelToAffix(m, 1017323, AffixType.Append, ": " + m_UsesRemaining.ToString()); // Durability
        }

        public static bool CheckAccessible(Item tool, Mobile m)
        {
            return (tool.IsChildOf(m) || tool.Parent == m);
        }

        public static bool CheckTool(Item tool, Mobile m)
        {
            Item check = m.FindItemOnLayer(Layer.OneHanded);

            if (check is BaseTool && check != tool)
                return false;

            check = m.FindItemOnLayer(Layer.TwoHanded);

            if (check is BaseTool && check != tool)
                return false;

            return true;
        }

        public override void OnSingleClick(Mobile from)
        {
            // we would like a date here but we do not know when Durability display appeared.
            //	for now we will just condition on SP
            if (!Core.UOSP)
                DisplayDurabilityTo(from);

            base.OnSingleClick(from);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack) || Parent == from)
            {
                CraftSystem system = this.CraftSystem;

                int num = system.CanCraft(from, this, null);

                if (num > 0)
                {
                    from.SendLocalizedMessage(num);
                }
                else
                {
                    // What good is this? Just to ensure creation?
                    CraftContext context = system.GetContext(from);

                    // add UOSP old school craft system hook here
                    if (Core.UOSP)
                    {
                        if (new OldSchoolCraft(from, system, this, null).DoOldSchoolCraft() == false)
                            from.SendGump(new CraftGump(from, system, this, null)); // call the old system as the old-school system is not impl
                    }
                    else
                        from.SendGump(new CraftGump(from, system, this, null));
                }
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            // version 1
            writer.Write((Mobile)m_Crafter);
            writer.Write((int)m_Quality);

            // version 0
            writer.Write((int)m_UsesRemaining);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Crafter = reader.ReadMobile();
                        m_Quality = (ToolQuality)reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        m_UsesRemaining = reader.ReadInt();
                        break;
                    }
            }
        }

        #region ICraftable Members

        public int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue)
        {
            Quality = (ToolQuality)quality;

            if (makersMark)
                Crafter = from;

            return quality;
        }

#if old
		else if (item is BaseTool || item is BaseHarvestTool && quality == 2)
					{
						endquality = quality;

						if (item is BaseTool)
							((BaseTool)item).UsesRemaining *= 3;
						else
							((BaseHarvestTool)item).UsesRemaining *= 3;
					}
#endif

        #endregion
    }
}
