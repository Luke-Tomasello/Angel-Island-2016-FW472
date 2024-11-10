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

/* Scripts/Multis/StaticHousing/StaticHouse.cs
 *  Changelog:
 *	12/28/07 Taran Kain
 *		Added BuildFixerAddon() to take care of doubled-up tiles
 *	8/25/07, Adam
 *		Override CheckSignpost() to ensure we have one
 *	6/25/07, Adam
 *      Major changes, please SR/MR for full details
 *  6/11/07, Pix
 *	    Added GetDeed() override so you can demolish the house nicely and get a deed back.
 *		Added versioning to the Serialize/Deserialize.
 *  06/08/2007, plasma
 *      Initial creation
 */
using Server.Diagnostics;
using Server.Commands;
using Server.Items;
using System;
using System.Collections;

namespace Server.Multis.StaticHousing
{
    public class StaticHouse : HouseFoundation
    {
        private int m_DefaultPrice;
        [CommandProperty(AccessLevel.GameMaster)]
        public override int DefaultPrice { get { return m_DefaultPrice; } set { m_DefaultPrice = value; } }

        private double m_BlueprintVersion;
        [CommandProperty(AccessLevel.GameMaster)]
        public double BlueprintVersion { get { return m_BlueprintVersion; } set { m_BlueprintVersion = value; } }

        private string m_HouseBlueprintID = String.Empty;
        [CommandProperty(AccessLevel.GameMaster)]
        public string HouseBlueprintID { get { return m_HouseBlueprintID; } }

        private string m_Description = String.Empty;
        [CommandProperty(AccessLevel.GameMaster)]
        public string Description { get { return m_Description; } set { m_Description = value; } }

        private string m_OriginalOwnerName = String.Empty;
        [CommandProperty(AccessLevel.GameMaster)]
        public string OriginalOwnerName { get { return m_OriginalOwnerName; } set { m_OriginalOwnerName = value; } }

        private string m_OriginalOwnerAccount = String.Empty;
        [CommandProperty(AccessLevel.GameMaster)]
        public string OriginalOwnerAccount { get { return m_OriginalOwnerAccount; } set { m_OriginalOwnerAccount = value; } }

        private Serial m_OriginalOwnerSerial = Serial.MinusOne;
        [CommandProperty(AccessLevel.GameMaster)]
        public Serial OriginalOwnerSerial { get { return m_OriginalOwnerSerial; } set { m_OriginalOwnerSerial = value; } }

        private DateTime m_RevisionDate;
        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime RevisionDate { get { return m_RevisionDate; } set { m_RevisionDate = value; } }

        public StaticHouse(Mobile owner, string houseID)
            : base(owner, StaticHouseHelper.GetFoundationID(houseID), 270, 2, 2)
        {
            m_HouseBlueprintID = houseID;
            // setup basic multi structure
            SetInitialState();
            // apply FixerAddon
            BuildFixerAddon();
            // now transfer over the data from the original blueprint house
            StaticHouseHelper.TransferData(this);
        }

        public StaticHouse(Serial serial)
            : base(serial)
        {
        }

        // area passed via blueprint will default to base.area if there is no area in blueprint
        private ArrayList m_AreaArray = new ArrayList();
        public ArrayList AreaList { get { return m_AreaArray; } }
        public override Rectangle2D[] Area
        {
            get
            {   // if we don't have a defined area, just use that of the base class (usually plot size - yuck)
                if (m_AreaArray.Count == 0)
                    return base.Area;

                // Copies the elements of the ArrayList to a Rectangle2D array.
                Rectangle2D[] temp = (Rectangle2D[])m_AreaArray.ToArray(typeof(Rectangle2D));
                return temp;
            }
        }

        public override DesignState CurrentState
        {
            get { return m_Current; }
            set { m_Current = value; }
        }

        public override DesignState DesignState
        {
            get { DesignStateError("DesignState DesignState called."); return m_Design; }
        }

        public override DesignState BackupState
        {
            get { DesignStateError("DesignState BackupState called."); return m_Backup; }
        }

        // we want to know if either m_Backup or m_Design are ever used.
        private void DesignStateError(string text)
        {
            try { throw new ApplicationException(text); }
            catch (Exception ex) { LogHelper.LogException(ex); }
        }

        public override bool IsAosRules { get { return false; } }

        public override void SetInitialState()
        {
            m_Current = new DesignState(this, GetEmptyFoundation());

            // explicitly unused in StaticHousing
            m_Design = null;
            m_Backup = null;

            //init the other two design states just so they don't crash the base's serilization      
            MultiComponentList y = new MultiComponentList(m_Current.Components);
            MultiComponentList x = new MultiComponentList(StaticHouseHelper.GetComponents(m_HouseBlueprintID));

            //merge x into y.
            //first, remove all in y
            for (int i = y.List.Length - 1; i >= 0; i--)
            {
                y.Remove(y.List[i].m_ItemID, y.List[i].m_OffsetX, y.List[i].m_OffsetY, y.List[i].m_OffsetZ);
            }

            //then add all the ones we want to the list
            for (int i = 0; i < x.List.Length; ++i)
            {
                y.Add(x.List[i].m_ItemID, x.List[i].m_OffsetX, x.List[i].m_OffsetY, x.List[i].m_OffsetZ, true);
            }

            m_Current.Components = y;

            return;
        }

        public override void CheckSignpost()
        {
            MultiComponentList mcl = this.Components;

            int x = mcl.Min.X;
            int y = mcl.Height - 2 - mcl.Center.Y;

            if (m_Signpost == null)
            {
                m_Signpost = new Static(m_SignpostGraphic);
                m_Signpost.MoveToWorld(new Point3D(X + x, Y + y, Z + 7), Map);
            }
            else
            {
                m_Signpost.ItemID = m_SignpostGraphic;
                m_Signpost.MoveToWorld(new Point3D(X + x, Y + y, Z + 7), Map);
            }
        }
        /*
		public static void Initialize()
		{	
			Server.Commands.Register("BuildFixerAddon", AccessLevel.Administrator, new CommandEventHandler(OnBuildFixerAddon));
		}

		[Usage("BuildFixerAddon")]
		[Description("Add patchtiles to the static house.")]
		private static void OnBuildFixerAddon(CommandEventArgs e)
		{
			e.Mobile.SendMessage("Build patch addon for which house?");
			e.Mobile.SendMessage("Please target the house sign.");
			e.Mobile.Target = new BuildFixerAddonTarget();
		}

		public class BuildFixerAddonTarget : Target
		{

			public BuildFixerAddonTarget()
				: base(8, false, TargetFlags.None)
			{

			}

			protected override void OnTarget(Mobile from, object target)
			{
				if (target is HouseSign && (target as HouseSign).Owner != null)
				{
					HouseSign sign = target as HouseSign;

					if (sign.Owner is StaticHouse == false)
					{
						from.SendMessage("This is not a StaticHouse.");
						return;
					}

					StaticHouse sh = sign.Owner as StaticHouse;
					sh.BuildFixerAddon();
					from.SendMessage("Patch applied.");
				}
				else
				{
					from.SendMessage("That is not a house sign.");
				}
			}
		}*/

        public void BuildFixerAddon()
        {
            StaticHouseHelper.FixerAddon fa = StaticHouseHelper.BuildFixerAddon(m_HouseBlueprintID);
            ArrayList components = fa.Components;

            /* abandoned effort to remove doubled tiles before adding in the fixer addon.
			 * Problem: We seem to get missing tiles like we do in original construction, which is the whole reason
			 * for jumping through all of these hoops.
			for (int ix=0; ix < fa.Components.Count; ix++)
			{
				AddonComponent mx = (AddonComponent)fa.Components[ix];
				this.Components.Remove(mx.ItemID, mx.Offset.X, mx.Offset.Y, mx.Offset.Z);
			}*/

            /* Abandoned:
			 * This code simply proves that the "missing" tiles in a design are not only there in the MCL, but a acll to 
			 * Map.GetTilesAt() that location shows the (invisible) are actually there. We must therefore conclude there is
			 * some (as yet) unsolavable Z Order issue when you have multiple tiles at the same x/y/z
			 * This code is worth keeping as it is the basis for a tool to query missing tiles in a house.
			Point2D center;
			center = this.Components.Center;
			int X0 = (this.X + this.Components.Min.X);
			int Y0 = (this.Y + this.Components.Min.Y);
			int xOffset =  X0 + center.X;
			int yOffset = Y0 + center.Y;
			int count = 0;

			// just make sure things are what we expect
			foreach (AddonComponent ms in components)
			{
				Point3D msp = ms.Offset;
				Point2D px = new Point2D(xOffset + msp.X, yOffset + msp.Y);
				ArrayList list = this.Map.GetTilesAt(px, false, false, true);
				ArrayList foo = new ArrayList(this.Components.Tiles[center.X + msp.X][center.Y + msp.Y]);
				if (foo.Count == list.Count)
				{
					for (int ix = 0; ix < foo.Count; ix++)
					{
						Tile tx1 = (Tile)foo[ix];
						Tile tx2 = (Tile)list[ix];
						if (tx1.Height != tx2.Height ||
							tx1.ID != tx2.ID ||
							tx1.Ignored != tx2.Ignored ||
							tx1.Z != tx2.Z)
							continue;
					}
				}
				else
					continue;
			}*/

            fa.MoveToWorld(Location, this.Map);
            Addons.Add(fa);
            fa.OnPlaced(null, this);
            //m_Current.OnRevised();	// do we need this?
        }

        public override Server.Multis.Deeds.HouseDeed GetDeed()
        {
            return new StaticDeed(m_HouseBlueprintID, m_Description);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)5); // version

            // verson 5
            writer.Write(m_RevisionDate);

            // version 4
            writer.Write(m_AreaArray.Count);
            for (int i = 0; i < m_AreaArray.Count; ++i)
                writer.Write((Rectangle2D)m_AreaArray[i]);

            // version 3
            writer.Write(m_Description);
            writer.Write(m_OriginalOwnerName);
            writer.Write(m_OriginalOwnerAccount);
            writer.Write((int)m_OriginalOwnerSerial);

            // version 2
            writer.Write(m_DefaultPrice);
            writer.Write(m_BlueprintVersion);

            // version 1
            writer.Write(m_HouseBlueprintID);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 5:
                    {
                        m_RevisionDate = reader.ReadDateTime();
                        goto case 4;
                    }
                case 4:
                    {
                        int count = reader.ReadInt();
                        m_AreaArray = new ArrayList(count);
                        for (int i = 0; i < count; ++i)
                        {
                            Rectangle2D rect = reader.ReadRect2D();
                            m_AreaArray.Add(rect);
                        }
                        base.UpdateRegionArea();
                        goto case 3;
                    }
                case 3:
                    {
                        m_Description = reader.ReadString();
                        m_OriginalOwnerName = reader.ReadString();
                        m_OriginalOwnerAccount = reader.ReadString();
                        m_OriginalOwnerSerial = reader.ReadInt();
                        goto case 2;
                    }
                case 2:
                    {
                        m_DefaultPrice = reader.ReadInt();
                        m_BlueprintVersion = reader.ReadDouble();
                        goto case 1;
                    }
                case 1:
                    {
                        m_HouseBlueprintID = reader.ReadString();
                        break;
                    }
            }
        }

    }
}
