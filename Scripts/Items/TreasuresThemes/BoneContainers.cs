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

/* Scripts/Items/TreasureThemes/BoneContainers.cs
 * CHANGELOG
 *  03/28/06 Taran Kain
 *		Override IDyable.Dye() to disable dying
 *		Why do we inherit Bag instead of BaseContainer? Can't change inherit order without killing serialization.
 *	04/01/05, Kitaras	
 *		Initial	Creation
 */

namespace Server.Items
{

    public class BoneContainer : Bag, IDyable
    {
        public override int MaxWeight { get { return 0; } }
        public override int DefaultDropSound { get { return 0x42; } }

        //3	differnt types 0-2 
        [Constructable]
        public BoneContainer(int type)
        {

            Name = "a pile of bones";
            Movable = true;
            GumpID = 9;

            if (type == 0) ItemID = 3789;
            if (type == 1) ItemID = 3790;
            if (type == 2) ItemID = 3792;
        }

        public BoneContainer(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); //	version	
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        #region	IDyable	Members

        public new bool Dye(Mobile from, DyeTub sender)
        {
            from.SendMessage("You cannot dye that.");
            return false;
        }

        #endregion
    }
}
