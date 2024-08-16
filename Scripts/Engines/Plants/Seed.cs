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

/* Scripts/Engines/Plants/PlantTypes.cs
 *	10/10/05, Pix
 *		Fix for Hedge hue.
 *  04/07/05, Kitaras
 *	 Added check to set seed name to a solen seed if is a hedge plant type.
 */

using Server.Targeting;

namespace Server.Engines.Plants
{
    public class Seed : Item
    {
        private PlantType m_PlantType;
        private PlantHue m_PlantHue;
        private bool m_ShowType;

        [CommandProperty(AccessLevel.GameMaster)]
        public PlantType PlantType
        {
            get { return m_PlantType; }
            set
            {
                m_PlantType = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public PlantHue PlantHue
        {
            get { return m_PlantHue; }
            set
            {
                m_PlantHue = value;
                Hue = PlantHueInfo.GetInfo(value).Hue;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ShowType
        {
            get { return m_ShowType; }
            set
            {
                m_ShowType = value;
                InvalidateProperties();
            }
        }

        public override int LabelNumber { get { return 1060810; } } // seed

        [Constructable]
        public Seed()
            : this(PlantTypeInfo.RandomFirstGeneration(), PlantHueInfo.RandomFirstGeneration(), false)
        {
            Weight = 1.0;
        }

        [Constructable]
        public Seed(PlantType plantType, PlantHue plantHue, bool showType)
            : base(0xDCF)
        {
            m_PlantType = plantType;
            m_PlantHue = plantHue;
            m_ShowType = showType;
            if (PlantType == PlantType.Hedge)
            {
                Name = "a solen seed";
                m_PlantHue = PlantHue.None; //for safety
            }
            Hue = PlantHueInfo.GetInfo(plantHue).Hue;
        }

        public Seed(Serial serial)
            : base(serial)
        {
        }

        public override bool ForceShowProperties { get { return ObjectPropertyList.Enabled; } }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            PlantHueInfo hueInfo = PlantHueInfo.GetInfo(m_PlantHue);

            if (m_ShowType)
            {
                PlantTypeInfo typeInfo = PlantTypeInfo.GetInfo(m_PlantType);
                list.Add(hueInfo.IsBright() ? 1061918 : 1061917, "#" + hueInfo.Name.ToString() + "\t#" + typeInfo.Name.ToString()); // [bright] ~1_COLOR~ ~2_TYPE~ seed
            }
            else
            {
                list.Add(hueInfo.IsBright() ? 1060839 : 1060838, "#" + hueInfo.Name.ToString()); // [bright] ~1_val~ seed
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
                return;
            }

            from.Target = new InternalTarget(this);
            LabelTo(from, 1061916); // Choose a bowl of dirt to plant this seed in.
        }

        private class InternalTarget : Target
        {
            private Seed m_Seed;

            public InternalTarget(Seed seed)
                : base(3, false, TargetFlags.None)
            {
                m_Seed = seed;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Seed.Deleted)
                    return;

                if (!m_Seed.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
                    return;
                }

                if (targeted is PlantItem)
                {
                    PlantItem plant = (PlantItem)targeted;

                    plant.PlantSeed(from, m_Seed);
                }
                else if (targeted is Item)
                {
                    ((Item)targeted).LabelTo(from, 1061919); // You must use a seed on a bowl of dirt!
                }
                else
                {
                    from.SendLocalizedMessage(1061919); // You must use a seed on a bowl of dirt!
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_PlantType);
            writer.Write((int)m_PlantHue);
            writer.Write((bool)m_ShowType);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_PlantType = (PlantType)reader.ReadInt();
            m_PlantHue = (PlantHue)reader.ReadInt();
            m_ShowType = reader.ReadBool();

            //Pix: Fix for hedge hue (needs to be none to be mutant)
            if (m_PlantType == PlantType.Hedge)
            {
                m_PlantHue = PlantHue.None;
            }
        }
    }
}
