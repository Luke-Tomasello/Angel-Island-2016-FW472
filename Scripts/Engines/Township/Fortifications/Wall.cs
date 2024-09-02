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

/* Engines/Township/Fortifications/Walls.cs
 * CHANGELOG:
 *  8/26/2024, Adam
 *      1. BaseFortificationWall now saves a reference to the TownshipStone to which it belongs.
 *      2. BaseFortificationWall is now ITownshipItem
 *      We use this to cleanup all ITownshipItems when the stone is deleted.
 * 2010.05.24 - Pix
 *      Code cleanup (renaming functions/classes to make sense, reorganizing for easier reading)
 *      Moves Stone and Spear walls to separate files.
 * 4/23/10, adam
 *		1. Add CanDamageWall & DamageWeapon to insure the player has the right tool for the job
 *		2. adam hack until pixie reviews
 * 			The problem is twofold. Firstly a GM carpenter can do 20+25=42 HP damage per 120 seconds to a wall with only 127 HP
 * 			this means a GM carpenter can take down a rather expensive wall in ~3 minutes
 * 			Secondly, since we do a skill check, we're awarding carpentry skill points for free (while we grief the township owner). This 
 * 			seems unbalanced
 * 			HACK: reduce damage to average 1 HP per 120 seconds AND require a special tool (not bare hands, and not a newbied ax)
 * 11/30/08, Pix
 *		Added Alive checks.
 * 11/16/08, Pix
 *		Refactored, rebalanced, and fixed stuff
 * 10/19/08, Pix
 *		Spelling fix.
 * 10/17/08, Pix
 *		Reduced the skill requirement to repair the wall.
 * 10/17/08, Pix
 *		Fixed the timer for repair/damage to stop if they moved.
 * 10/15/08, Pix
 *		Changed that you need to be within 2 tiles of the wall to damage/repair it.
 * 10/15/08, Pix
 *		Added graphics.
 *		Added delays to repair/damage.
 * 10/10/08, Pix
 *		Initial.
*/

using Server.ContextMenus;
using Server.Items;
using Server.Regions;
using System;
using System.Collections.Generic;

namespace Server.Township
{
    public interface ITownshipItem
    {
        TownshipStone Stone
        {
            get;
            set;
        }
    }

    #region Base Wall Class

    public class BaseFortificationWall : Item, ITownshipItem
    {
        #region Member Variables
        private TownshipStone m_TownshipStone = null;
        private DateTime m_PlacementDate = DateTime.MinValue;
        private Mobile m_Placer = null;
        private int m_OriginalMaxHits = 100;
        private int m_MaxHits = 100;
        private int m_Hits = 100;
        private SkillName m_RepairSkill = SkillName.Tinkering;
        DateTime m_LastRepair = DateTime.UtcNow;
        DateTime m_LastDamage = DateTime.UtcNow;

        private Mobile m_RepairWorker = null; //Note that this should not be serialized
        private Mobile m_DamageWorker = null; //Note that this should not be serialized

        #endregion

        #region static ALLWalls functionality

        public static List<BaseFortificationWall> TownshipWallList = new List<BaseFortificationWall>();

        public static int DecayAllTownshipWalls()
        {
            int deleted = 0;
            try
            {
                List<BaseFortificationWall> listcopy = new List<BaseFortificationWall>();
                foreach (BaseFortificationWall wall in TownshipWallList)
                {
                    listcopy.Add(wall);
                }

                foreach (BaseFortificationWall wall in listcopy)
                {
                    wall.Hits -= TownshipSettings.WallHitsDecay;

                    TownshipRegion tr = TownshipRegion.GetTownshipAt(wall.Location, wall.Map);

                    if (tr == null || wall.Hits < 0 || OrphanWall(wall))
                    {
                        deleted++;
                        wall.Delete();
                    }
                }
            }
            catch (Exception e)
            {
                Server.Commands.LogHelper.LogException(e);
            }
            return deleted;
        }
        private static bool OrphanWall(BaseFortificationWall wall)
        {
            if (wall is ITownshipItem ts)
            {
                if (ts.Stone == null || ts.Stone.Deleted)
                    return true;
            }
            else
                return true;

            return false;
        }

        #endregion

        #region Constructors

        public BaseFortificationWall(TownshipStone stone)
            : base(0x27C)
        {
            Movable = false;
            Weight = 150;
            TownshipWallList.Add(this);
            Stone = stone;
        }

        public BaseFortificationWall(TownshipStone stone, int itemID)
            : base(itemID)
        {
            Movable = false;
            TownshipWallList.Add(this);
            Stone = stone;
        }

        public BaseFortificationWall(Serial serial)
            : base(serial)
        {
            TownshipWallList.Add(this);
        }

        #endregion

        public override void Delete()
        {
            if (TownshipWallList.Contains(this)) TownshipWallList.Remove(this);
            base.Delete();
        }

        #region Context Menu Entries

        public override void GetContextMenuEntries(Mobile from, System.Collections.ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (this.Parent == null)
            {
                list.Add(new InspectWallEntry(from, this));
                if (this.Placer == from)
                {
                    list.Add(new WallWorkEntry(from, this, false, true)); //destroy
                }
                list.Add(new WallWorkEntry(from, this, true)); //normal repair
                list.Add(new WallWorkEntry(from, this, false)); //normal damage
            }
        }

        #endregion

        #region Properties
        [CommandProperty(AccessLevel.Counselor, AccessLevel.Owner)]
        public TownshipStone Stone
        {
            get { return m_TownshipStone; }
            set { m_TownshipStone = value; }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public Mobile Placer
        {
            get { return m_Placer; }
            set { m_Placer = value; }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public DateTime PlacementDate
        {
            get { return m_PlacementDate; }
            set { m_PlacementDate = value; }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int Hits
        {
            get { return m_Hits; }
            set { m_Hits = value; }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int MaxHits
        {
            get { return m_MaxHits; }
            set { m_MaxHits = value; if (m_Hits > m_MaxHits) m_Hits = m_MaxHits; }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public int OriginalHits
        {
            get { return m_OriginalMaxHits; }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public DateTime LastRepair
        {
            get { return m_LastRepair; }
            set { m_LastRepair = value; }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public DateTime LastDamage
        {
            get { return m_LastDamage; }
            set { m_LastDamage = value; }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public SkillName RepairSkill
        {
            get { return m_RepairSkill; }
            set { m_RepairSkill = value; }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public Mobile CurrentRepairWorker
        {
            get { return m_RepairWorker; }
            set { m_RepairWorker = value; }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public Mobile CurrentDamageWorker
        {
            get { return m_DamageWorker; }
            set { m_DamageWorker = value; }
        }

        #endregion

        #region Repair virtual functions

        public virtual int GetRepairAmount(int damagetorepair)
        {
            return damagetorepair;
        }
        public virtual Type GetRepairType()
        {
            return typeof(Board);
        }
        public virtual string GetRepairTypeDesc()
        {
            return "boards";
        }

        #endregion

        #region Placement

        public void Place(Mobile m, Point3D loc)
        {
            m.SendMessage("You begin constructing the wall.");
            new InternalPlacementTimer(m, loc, this).Start();
        }

        #endregion

        #region Change/CanChange
        public virtual void Change(Mobile from)
        {
            from.SendMessage("Change not implemented.");
        }

        public bool CanChange(Mobile from)
        {
            bool bReturn = false;

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                return true;
            }

            CustomRegion cr = CustomRegion.FindDRDTRegion(from.Map, this.Location);
            if (cr is TownshipRegion)
            {
                TownshipRegion tsr = cr as TownshipRegion;
                if (tsr != null && tsr.TStone != null && tsr.TStone.Guild != null &&
                    tsr.TStone.Guild == from.Guild)
                {
                    bReturn = true;
                }
                else
                {
                    from.SendMessage("You must be a member of this township to modify this wall.");
                    bReturn = false;
                }
            }
            else
            {
                from.SendMessage("You must be within the township to modify this wall.");
                bReturn = false;
            }

            return bReturn;
        }
        #endregion

        #region Hits/Repair methods

        public virtual int GetBaseInitialHits()
        {
            if (m_Placer == null)
            {
                return 1;
            }

            int carp = (int)m_Placer.Skills[SkillName.Carpentry].Value;
            int tink = (int)m_Placer.Skills[SkillName.Tinkering].Value;
            int mine = (int)m_Placer.Skills[SkillName.Mining].Value;
            int jack = (int)m_Placer.Skills[SkillName.Lumberjacking].Value;

            int smit = (int)m_Placer.Skills[SkillName.Blacksmith].Value;
            int alch = (int)m_Placer.Skills[SkillName.Alchemy].Value;
            int item = (int)m_Placer.Skills[SkillName.ItemID].Value;
            int mace = (int)m_Placer.Skills[SkillName.Macing].Value;
            int scrb = (int)m_Placer.Skills[SkillName.Inscribe].Value;
            int dtct = (int)m_Placer.Skills[SkillName.DetectHidden].Value;
            int cart = (int)m_Placer.Skills[SkillName.Cartography].Value;

            int baseHits = 100;
            //"main" skills add the most
            baseHits += carp / 4; //+25 @ GM
            baseHits += tink / 4; //+25 @ GM
            baseHits += mine / 4; //+25 @ GM
            baseHits += jack / 4; //+25 @ GM
                                  //"support" skills add some more
            baseHits += smit / 10;//+10 @ GM
            baseHits += alch / 10;//+10 @ GM
            baseHits += item / 10;//+10 @ GM
            baseHits += mace / 10;//+10 @ GM
            baseHits += scrb / 10;//+10 @ GM
            baseHits += dtct / 10;//+10 @ GM
            baseHits += cart / 10;//+10 @ GM

            return baseHits;
        }

        public void SetInitialHits()
        {
            int baseHits = 0;

            baseHits = GetBaseInitialHits();
            //Special bonuses for different (derived) walls
            baseHits += GetSpecialBuildBonus();

            m_OriginalMaxHits = baseHits;
            m_MaxHits = m_OriginalMaxHits;
            m_Hits = m_MaxHits;
            m_LastRepair = DateTime.UtcNow;
            m_LastDamage = DateTime.UtcNow;
        }

        public void DamageWeapon(BaseWeapon w)
        {
            // taken from Baseweapon OnHit
            if (w != null && w.MaxHitPoints > 0 && Utility.Random(25) == 0)
            {
                if (w.HitPoints > 1)
                    --w.HitPoints;
                else
                    w.Delete();
            }
        }

        public bool CanDamageWall(Mobile m, BaseFortificationWall wall)
        {
            if (m == null || m.Backpack == null)
                return false;

            if (wall is StoneFortificationWall)
            {
                if (m.Weapon is Sledgehammer)
                {
                    return true;
                }
                else
                {
                    m.SendMessage("You'll need to equip a sledgehammer to damage that wall.");
                    return false;
                }
            }
            if (wall is SpearFortificationWall)
            {
                if (m.Weapon is DemolitionAx)
                {
                    return true;
                }
                else
                {
                    m.SendMessage("You'll need to equip a demolition ax to damage that wall.");
                    return false;
                }
            }

            // don't know this wall type
            m.SendMessage("You're unable to damage this wall.");
            return false;
        }

        #region BeginWork

        /*
         * m = mobile doing work
         * repair = true if we're repairing, false if we're destroying
         * full = only applies to damaging - full=true means tear down wall (only if m=placer)
         */
        public void BeginWork(Mobile m, bool repair, bool full)
        {
            double daysPerWork = 0.21;
            if (Misc.TestCenter.Enabled)
            {
                daysPerWork = 1 / (24 * 60); //once per minute on TC
            }

            if (repair)
            {
                if (DateTime.UtcNow < m_LastRepair.AddDays(daysPerWork))
                {
                    m.SendMessage("The wall has been rapaired recently, this wall cannot be worked on yet.");
                    return;
                }
                if (this.CurrentRepairWorker != null)
                {
                    m.SendMessage("Someone else is repairing this wall.");
                    return;
                }

                this.CurrentRepairWorker = m;

                int amount = GetRepairAmount(m_MaxHits - m_Hits);
                if (m.Backpack.ConsumeTotal(GetRepairType(), amount) == false)
                {
                    m.SendMessage("You need " + amount + " " + GetRepairTypeDesc() + " to repair this wall.");
                    this.CurrentRepairWorker = null;
                    return;
                }

                m.SendMessage("You begin repairing the wall.");
            }
            else
            {
                if (!full && DateTime.UtcNow < m_LastDamage.AddDays(daysPerWork))
                {
                    m.SendMessage("The wall has been damaged recently, this wall cannot be worked on yet.");
                    return;
                }
                if (this.CurrentDamageWorker != null)
                {
                    m.SendMessage("Someone else is damaging this wall.");
                    return;
                }

                // make sure the player has the right tool to damage the wall
                if (CanDamageWall(m, this) == false)
                    return;
                this.CurrentDamageWorker = m;

                m.SendMessage("You begin damaging the wall.");
            }
            new InternalWorkTimer(m, this, repair, full).Start();

            #region OLD CODE
            /*
            double daysPerRepair = 0.21;
            if (Misc.TestCenter.Enabled)
            {
                daysPerRepair = 1 / (24 * 60); //once per minute on TC
            }

            if ((repair && DateTime.UtcNow < m_LastRepair.AddDays(daysPerRepair)) ||
                 (!repair && !full && DateTime.UtcNow < m_LastDamage.AddDays(daysPerRepair)))
            {
                m.SendMessage("The wall has been worked on recently, this wall cannot be worked on yet.");
                return;
            }

            if (repair && this.CurrentRepairWorker != null)
            {
                m.SendMessage("Someone else is repairing this wall.");
                return;
            }
            else if (!repair && this.CurrentDamageWorker != null)
            {
                m.SendMessage("Someone else is damaging this wall.");
                return;
            }

            if (repair)
            {
                this.CurrentRepairWorker = m;
            }
            else
            {
                // make sure the player has the right tool to damage the wall
                if (CanDamageWall(m, this) == false)
                    return;
                this.CurrentDamageWorker = m;
            }

            if (repair)
            {
                int amount = GetRepairAmount() * (m_MaxHits - m_Hits);
                if (m.Backpack.ConsumeTotal(GetRepairType(), amount) == false)
                {
                    m.SendMessage("You need " + amount + " " + GetRepairTypeDesc() + " to repair this wall.");
                    return;
                }
            }

            if (repair)
            {
                m.SendMessage("You begin repairing the wall.");
            }
            else
            {
                m.SendMessage("You begin damaging the wall.");
            }
            new InternalWorkTimer(m, this, repair, full).Start();
 */
            #endregion
        }
        #endregion

        #region EndWork function

        public void EndWork(Mobile m, bool repair, bool full)
        {
            int hitsDiff = 0;

            //2010.05.24-Pix - attempting to simplify code - one repair and one damage block
            if (repair)
            {
                m_LastRepair = DateTime.UtcNow;
                m_RepairWorker = null; //safety!

                hitsDiff = m_MaxHits - m_Hits;
                hitsDiff -= GetSpecialRepairBonus(m);

                if (hitsDiff <= 0)
                {
                    hitsDiff = 1;
                }

                //if (m.CheckSkill(m_RepairSkill, 0, 75))
                //{
                m.SendMessage("You repair the wall.");
                m_MaxHits -= (hitsDiff / 2);
                //repair repairs to full, hence the cost based on the difference
                m_Hits = m_MaxHits;
                //}
                //else
                //{
                //    m.SendMessage("You seem to harm the wall.");
                //    m_MaxHits -= hitsDiff;
                //    m_Hits -= hitsDiff;
                //}
            }
            else //damaging
            {
                #region Adam's Hack - removed
                // adam hack until pixie reviews
                // The problem is twofold. Firstly a GM carpenter can do 20+25=42 HP damage per 120 seconds to a wall with only 127 HP
                //	this means a GM carpenter can take down a rather expensive wall in ~3 minutes
                // Secondly, since we do a skill check, we're awarding carpentry skill points for free (while we grief the township owner). This 
                //	seems unbalanced
                //	HACK: reduce damage to average 1 HP per 120 seconds AND require a special tool (not bare hands, and not a newbied ax)
                //                m_LastDamage = DateTime.UtcNow;
                //                m_DamageWorker = null; //safety!
                //
                //                hitsDiff = Utility.RandomBool() ? 0 : 2;	// average 1 hp damage
                //
                //                if (hitsDiff > 0)
                //                {
                //                    m.SendMessage("You harm the wall.");
                //                    m_Hits -= hitsDiff;
                //                }
                //                else
                //                {
                //                    m.SendMessage("You don't seem to have an effect.");
                //                }
                //
                //                if (m_Hits < 0)
                //                {
                //                    m.SendMessage("The wall crumbles down.");
                //                    this.Delete();
                //                }
                //
                //                return;
                #endregion

                m_LastDamage = DateTime.UtcNow;
                m_DamageWorker = null; //safety!

                double skillLevel = m.Skills[m_RepairSkill].Base;
                hitsDiff = 20;

                if (skillLevel >= 90.0) hitsDiff += 25;
                else if (skillLevel >= 70.0) hitsDiff += 15;
                else hitsDiff += 10;


                if (hitsDiff <= 0)
                {
                    hitsDiff = 1;
                }

                if (full)
                {
                    if (m == this.Placer)
                    {
                        m.SendMessage("You destroy the wall.");
                        m_Hits = -1;
                    }
                }
                else
                {
                    //if (m.CheckSkill(m_RepairSkill, 0, 75))
                    //{
                    m.SendMessage("You harm the wall.");
                    m_Hits -= hitsDiff;
                    //}
                    //else
                    //{
                    //    m.SendMessage("You don't seem to have an effect.");
                    //}
                }

            }

            if (m_Hits < 0)
            {
                m.SendMessage("The wall crumbles down.");
                this.Delete();
            }

            #region OLD CODE
            /*
            int hitsDiff = 0;

			#region ADAM HACK
			// adam hack until pixie reviews
			// The problem is twofold. Firstly a GM carpenter can do 20+25=42 HP damage per 120 seconds to a wall with only 127 HP
			//	this means a GM carpenter can take down a rather expensive wall in ~3 minutes
			// Secondly, since we do a skill check, we're awarding carpentry skill points for free (while we grief the township owner). This 
			//	seems unbalanced
			//	HACK: reduce damage to average 1 HP per 120 seconds AND require a special tool (not bare hands, and not a newbied ax)
			if (repair == false)
			{	// we're damaging the wall
				m_LastDamage = DateTime.UtcNow;
				m_DamageWorker = null; //safety!

				hitsDiff = Utility.RandomBool() ? 0 : 2;	// average 1 hp damage

				if (hitsDiff > 0)
				{
					m.SendMessage("You harm the wall.");
					m_Hits -= hitsDiff;
				}
				else
				{
					m.SendMessage("You don't seem to have an effect.");
				}

				if (m_Hits < 0)
				{
					m.SendMessage("The wall crumbles down.");
					this.Delete();
				}

				return;
			}
			#endregion ADAM HACK

			if (repair)
			{
				m_LastRepair = DateTime.UtcNow;
				m_RepairWorker = null; //safety!
			}
			else
			{
				m_LastDamage = DateTime.UtcNow;
				m_DamageWorker = null; //safety!
			}

			double skillLevel = m.Skills[m_RepairSkill].Base;
			hitsDiff = 20;

			if (repair == false)
			{
				if (skillLevel >= 90.0)
					hitsDiff += 25;
				else if (skillLevel >= 70.0)
					hitsDiff += 15;
				else
					hitsDiff += 10;
			}
			else
			{
				hitsDiff -= GetSpecialRepairBonus(m);
			}

			if (hitsDiff <= 0)
			{
				hitsDiff = 1;
			}

			if (repair)
			{
				if (m.CheckSkill(m_RepairSkill, 0, 75))
				{
					m.SendMessage("You repair the wall.");
					m_MaxHits -= (hitsDiff / 2);
					//repair repairs to full, hence the cost based on the difference
					m_Hits = m_MaxHits;
				}
				else
				{
					m.SendMessage("You seem to harm the wall.");
					m_MaxHits -= hitsDiff;
					m_Hits -= hitsDiff;
				}
			}
			else
			{
				if (full)
				{
					if (m == this.Placer)
					{
						m.SendMessage("You destroy the wall.");
						m_Hits = -1;
					}
				}
				else
				{
					if (m.CheckSkill(m_RepairSkill, 0, 75))
					{
						m.SendMessage("You harm the wall.");
						m_Hits -= hitsDiff;
					}
					else
					{
						m.SendMessage("You don't seem to have an effect.");
					}
				}
			}

			if (m_Hits < 0)
			{
				m.SendMessage("The wall crumbles down.");
				this.Delete();
            }
 */
            #endregion
        }
        #endregion

        #region Get Special Build/Repair Bonus virtual functions
        public virtual int GetSpecialBuildBonus()
        {
            return 0;
        }

        public virtual int GetSpecialRepairBonus(Mobile m)
        {
            if (m == this.m_Placer) //if the repairer is the placer, give them a bonus to repair
            {
                return 6;
            }
            else
            {
                return 0;
            }
        }
        #endregion

        #endregion

        #region Serialize/Deserialize

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(2); //version

            // version 2
            writer.Write(this.m_TownshipStone);

            // version 1
            writer.Write(this.m_LastDamage);
            //Version 0 below :)
            writer.Write((int)this.m_RepairSkill);
            writer.Write(this.m_Placer);
            writer.Write(this.m_PlacementDate);
            writer.Write(this.m_OriginalMaxHits);
            writer.Write(this.m_MaxHits);
            writer.Write(this.m_LastRepair);
            writer.Write(this.m_Hits);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    m_TownshipStone = (TownshipStone)reader.ReadItem();
                    goto case 1;
                case 1:
                    m_LastDamage = reader.ReadDateTime();
                    goto case 0;
                case 0:
                    m_RepairSkill = (SkillName)reader.ReadInt();
                    m_Placer = reader.ReadMobile();
                    m_PlacementDate = reader.ReadDateTime();
                    m_OriginalMaxHits = reader.ReadInt();
                    m_MaxHits = reader.ReadInt();
                    m_LastRepair = reader.ReadDateTime();
                    m_Hits = reader.ReadInt();
                    break;
            }
        }

        #endregion

        #region Notify
        public void NotifyOfDamager(Mobile damager)
        {
            if (damager == null) return; //safety

            Point3D targetPoint = this.Location;
            CustomRegion cr = CustomRegion.FindDRDTRegion(damager.Map, targetPoint);
            if (cr is TownshipRegion)
            {
                TownshipRegion tsr = cr as TownshipRegion;
                if (tsr != null &&
                    tsr.TStone != null &&
                    tsr.TStone.Guild != null &&
                    tsr.TStone.Guild != damager.Guild)
                {
                    // every 10 minutes we will get a message unless the damager changes.
                    //	also, township members can type .status from within the township to dump the spam queue
                    string text = string.Format("{0} at {1} is damaging your township's wall.", damager.Name, damager.Location.ToString());
                    if (tsr.IsSpam(damager, text) == false)
                    {
                        tsr.QueueSpam(damager, text);
                        tsr.TStone.Guild.GuildMessage(text);
                        try
                        {
                            string allytext = "[" + TownshipStone.GetTownshipSizeDesc(tsr.TStone.ActivityLevel) + " of the " + tsr.TStone.GuildName + "]: " + text;
                            foreach (Server.Guilds.Guild g in tsr.TStone.Guild.Allies)
                            {
                                g.GuildMessage(allytext);
                            }
                        }
                        catch (Exception exc)
                        {
                            Server.Commands.LogHelper.LogException(exc);
                        }
                    }
                    else
                        tsr.QueueSpam(damager, text);
                }
            }
        }
        #endregion

        #region Internal Placement Timer Class

        private class InternalPlacementTimer : Timer
        {
            private const int SECONDS_UNTIL_DONE = 120;
            private Mobile m_Mobile;
            private Point3D m_Location;
            private Point3D m_MobLoc;
            private BaseFortificationWall m_Wall;
            private Map m_Map;
            private int m_Count = 0;

            public InternalPlacementTimer(Mobile m, Point3D loc, BaseFortificationWall wall)
                : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            {
                m_Mobile = m;
                m_Map = m.Map;
                m_Location = loc;
                m_Wall = wall;
                m_MobLoc = new Point3D(m.Location);

                m_Mobile.Animate(11, 5, 1, true, false, 0);

                Priority = TimerPriority.OneSecond;
            }

            protected override void OnTick()
            {
                m_Count++;

                if (!m_Mobile.Alive)
                {
                    this.Stop();
                    m_Wall.Delete();
                }
                else
                {
                    if (m_Mobile.Hidden)
                    {
                        m_Mobile.Hidden = false;
                    }

                    bool bHasMoved = false;
                    if (m_Mobile.Location != m_MobLoc)
                    {
                        bHasMoved = true;
                    }

                    if (bHasMoved || m_Mobile.Map == Map.Internal)
                    {
                        this.Stop();
                        m_Mobile.SendMessage("You stop building the wall.");
                        m_Wall.Delete();
                    }
                    else
                    {
                        if (m_Count > SECONDS_UNTIL_DONE || (Server.Misc.TestCenter.Enabled && m_Count > 10)) //1 minute to build the wall
                        {
                            this.Stop();
                            m_Mobile.SendMessage("You build the wall.");

                            m_Wall.PlacementDate = DateTime.UtcNow;
                            m_Wall.Placer = m_Mobile;
                            m_Wall.SetInitialHits();
                            m_Wall.MoveToWorld(m_Location, m_Map);
                        }
                        else if (m_Count % 5 == 0)
                        {
                            m_Mobile.Emote("*builds a wall*");
                            m_Mobile.Animate(11, 5, 1, true, false, 0);
                        }
                    }
                }
            }
        }

        #endregion

        #region Internal Work Timer Clase
        private class InternalWorkTimer : Timer
        {
            private const int SECONDS_UNTIL_DONE = 120;
            private Mobile m_Mobile;
            private Point3D m_MobLoc;
            private BaseFortificationWall m_Wall;
            private bool m_bRepair = true;
            private bool m_bFull = false;
            private int m_Count = 0;

            public InternalWorkTimer(Mobile m, BaseFortificationWall wall, bool bRepair, bool bFull)
                : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            {
                m_Mobile = m;
                m_Wall = wall;
                m_bRepair = bRepair;
                m_bFull = bFull;
                m_MobLoc = new Point3D(m.Location);

                m_Mobile.Animate(11, 5, 1, true, false, 0);

                Priority = TimerPriority.OneSecond;
            }

            protected override void OnTick()
            {
                m_Count++;

                if (!m_Mobile.Alive)
                {
                    this.Stop();
                    if (m_bRepair) m_Wall.CurrentRepairWorker = null;
                    else m_Wall.CurrentDamageWorker = null;
                }
                // make sure the player has the right tool to damage the wall
                else if (m_bRepair == false && m_Wall.CanDamageWall(m_Mobile, m_Wall) == false)
                {   // CanDamageWall already sent the player a message
                    this.Stop();
                    if (m_bRepair) m_Wall.CurrentRepairWorker = null;
                    else m_Wall.CurrentDamageWorker = null;
                }
                else
                {
                    if (m_Mobile.Hidden)
                    {
                        m_Mobile.Hidden = false;
                    }

                    bool bHasMoved = false;
                    if (m_Mobile.Location != m_MobLoc)
                    {
                        bHasMoved = true;
                    }

                    if (bHasMoved || m_Mobile.Map == Map.Internal)
                    {
                        if (m_bRepair)
                        {
                            m_Mobile.SendMessage("You move and stop repairing the wall.");
                        }
                        else
                        {
                            m_Mobile.SendMessage("You move and stop damaging the wall.");
                        }
                        this.Stop();
                        if (m_bRepair) m_Wall.CurrentRepairWorker = null;
                        else m_Wall.CurrentDamageWorker = null;
                    }
                    else
                    {
                        if (m_Count > SECONDS_UNTIL_DONE || (Server.Misc.TestCenter.Enabled && m_Count > 10)) //1 minute to repair the wall
                        {
                            this.Stop();
                            if (m_bRepair) m_Wall.CurrentRepairWorker = null;
                            else m_Wall.CurrentDamageWorker = null;
                            m_Wall.EndWork(m_Mobile, m_bRepair, m_bFull);
                        }
                        else if (m_Count % 5 == 0)
                        {
                            if (m_bRepair)
                            {
                                m_Mobile.Emote("*repairs the wall*");
                            }
                            else
                            {   // damage the Damagers waepon
                                m_Wall.DamageWeapon(m_Mobile.Weapon as BaseWeapon);
                                m_Mobile.Emote("*damages the wall*");
                                m_Mobile.CriminalAction(m_Mobile.Criminal == false);
                                m_Wall.NotifyOfDamager(m_Mobile);
                            }
                            m_Mobile.Animate(11, 5, 1, true, false, 0);
                        }
                    }
                }
            }
        }
        #endregion

    }

    #endregion

    #region Context Menus

    #region Repair Wall Context Menu Entry

    public class WallWorkEntry : ContextMenuEntry
    {
        private BaseFortificationWall m_Wall;
        private Mobile m_From;
        private bool m_Repair = true;
        private bool m_Full = false;

        public WallWorkEntry(Mobile from, BaseFortificationWall wall, bool repair)
            : this(from, wall, repair, false)
        {
        }

        public WallWorkEntry(Mobile from, BaseFortificationWall wall, bool repair, bool full)
            : base(5121, 2)
        {
            m_Wall = wall;
            m_From = from;
            m_Repair = repair;
            m_Full = full;

            if (repair == false)
            {
                this.Number = 5009; //Smite

                if (full == true)
                {
                    this.Number = 6275; //Demolish -- //5011; //Delete
                }
            }
            else
            {
                this.Number = 5121; //Refresh

                if (full == true)
                {
                    this.Number = 5006; //Resurrect
                }
            }

            Enabled = true;
        }
        public override void OnClick()
        {
            m_Wall.BeginWork(m_From, m_Repair, m_Full);
        }
    }

    #endregion

    #region Inspect Wall Context Menu Entry

    public class InspectWallEntry : ContextMenuEntry
    {
        private BaseFortificationWall m_Wall;
        private Mobile m_From;

        public InspectWallEntry(Mobile from, BaseFortificationWall wall)
            : base(6121, 6)
        {
            m_Wall = wall;
            m_From = from;
        }
        public override void OnClick()
        {
            if (m_From.AccessLevel > AccessLevel.Player)
            {
                m_From.SendMessage("StaffOnly: The wall has {0} of {1} hitpoints.", m_Wall.Hits, m_Wall.MaxHits);
            }

            double percentage = 0;
            if (m_Wall.MaxHits > 0) //protect divide by zero
            {
                percentage = ((double)m_Wall.Hits / (double)m_Wall.MaxHits) * 100.0;
            }

            string message = "You are unable to tell what shape the wall is in.";

            if (percentage == 100)
            {
                message = "The wall is in perfect condition.";
            }
            else if (percentage >= 75)
            {
                message = "The wall is in great condition.";
            }
            else if (percentage >= 50)
            {
                message = "The wall is in good condition.";
            }
            else if (percentage >= 25)
            {
                message = "The wall has taken quite a bit of damage.";
            }
            else
            {
                message = "The wall is close to collapsing.";
            }

            m_From.SendMessage(message);
        }
    }

    #endregion

    #endregion
}
