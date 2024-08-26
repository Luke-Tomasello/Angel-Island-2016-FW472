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

/* Scripts\Engines\Township\Fortifications\StoneWalls.cs
 * CHANGELOG:
 *  8/26/2024, Adam
 *      Construction now passes the TownshipStone to the wall constructed.
 *      We use this to cleanup all ITownshipItems when the stone is deleted.
 */

using Server.Items;
using System;

namespace Server.Township
{
    public class StoneFortificationWall : BaseFortificationWall
    {
        protected static int[] m_ids = new int[]
            {
				//full walls:
				0x001B, //stone wall
				0x001C, //stone wall other direction
				0x001A, //stone wall corner
				//half walls:
				0x0025, //stone half-wall
				0x0026, //stone half-wall other direction
				0x0024 //stone half-wall corner
			};

        public StoneFortificationWall(TownshipStone stone)
            : base(stone, 0x001B)
        {
            this.RepairSkill = SkillName.Tinkering;
            this.Weight = 200;
        }

        public StoneFortificationWall(Serial serial)
            : base(serial)
        {
        }

        public override int GetBaseInitialHits()
        {
            int hits = base.GetBaseInitialHits();
            hits = hits * 2;
            return hits;
        }

        public override int GetRepairAmount(int damagetorepair)
        {
            return (base.GetRepairAmount(damagetorepair) / 2) + 1;
        }
        public override Type GetRepairType()
        {
            return typeof(IronIngot);
            //return base.GetRepairType();
        }
        public override string GetRepairTypeDesc()
        {
            return "iron ingot";
            //return base.GetRepairTypeDesc();
        }


        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }

        public override void Change(Mobile from)
        {
            if (CanChange(from))
            {
                int index = -1;
                for (int i = 0; i < m_ids.Length; i++)
                {
                    if (m_ids[i] == this.ItemID)
                    {
                        index = i;
                        break;
                    }
                }

                if (index == -1)
                {
                    this.ItemID = m_ids[0];
                }
                else
                {
                    index++;
                    if (index >= m_ids.Length)
                    {
                        index = 0;
                    }
                    this.ItemID = m_ids[index];
                }
            }
            else
            {
                from.SendMessage("You can't change this wall.");
            }
        }
    }
}
