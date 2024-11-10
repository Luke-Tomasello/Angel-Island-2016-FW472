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

/* Scripts/Engines/Township/TownshipNPCDeed.cs
 * CHANGELOG
 *	10/19/08, Pix
 *		Added additional checks and messages to TSEvocatorDeed and TSEmissaryDeed
 *	8/3/08, Pix
 *		Change for CanExtend() call - now returns a reason.
 *	5/11/08, Adam
 *		Performance Conversion: Regions now use HashTables instead of ArrayLists
 *	4/13/07 Pix
 *		Now correctly sets lookouts to non-walking.
 */

using Server.Diagnostics;
using Server.Mobiles;
using Server.Multis;
using Server.Regions;
using System;

namespace Server.Items
{
    public abstract class TownshipNPCDeed : Item
    {
        protected string m_GuildAbbr = "NO GUILD";
        protected Server.Guilds.Guild m_Guild = null;

        public TownshipNPCDeed(Server.Guilds.Guild guild)
            : base(0x14F0)
        {
            if (guild != null)
            {
                m_GuildAbbr = guild.Abbreviation;
                m_Guild = guild;
            }

            Weight = 1.0;
            SetName("a township NPC");
            LootType = LootType.Blessed;
            this.Hue = Township.TownshipSettings.Hue;
        }

        public TownshipNPCDeed(Serial s)
            : base(s)
        {
        }

        protected void SetName(string title)
        {
            Name = string.Format("{0} deed [{1}]", title, m_GuildAbbr);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); //version

            writer.Write(this.m_GuildAbbr);
            writer.Write(this.m_Guild);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            this.m_GuildAbbr = reader.ReadString();
            this.m_Guild = (Server.Guilds.Guild)reader.ReadGuild();
        }

        public override void OnDoubleClick(Mobile from)
        {
            BaseHouse house = BaseHouse.FindHouseAt(from);

            TownshipRegion tr = TownshipRegion.GetTownshipAt(from);

            if (house == null)
            {
                from.SendMessage("You must be in a house to place this vendor.");
            }
            else if (tr == null)
            {
                from.SendMessage("You must be in a township to place this vendor.");
            }
            else if (!house.IsCoOwner(from))
            {
                from.SendMessage("You must be a coowner of the house to place this vendor.");
            }
            else if (!tr.CanBuildHouseInTownship(from))
            {
                from.SendMessage("You must be a guildmate to place this vendor.");
            }
            else if (!tr.CanBuildHouseInTownship(house.Owner))
            {
                from.SendMessage("The house must be owned by a guildmember to place this vendor.");
            }
            else if (!house.Public)
            {
                from.SendMessage("This vendor must be placed in a public house.");
            }
            else if (tr.TStone.Guild.Abbreviation != this.m_GuildAbbr)
            {
                from.SendMessage("This vendor must be placed in your guild's town.");
            }
            else
            {
                bool bCanPlace = true;

                int playervendorCount = 0;
                int tsnpcCount = 0;

                foreach (Mobile mx in house.Region.Mobiles.Values)
                {
                    if (mx is PlayerVendor)
                        playervendorCount++;

                    Type type = mx.GetType();
                    TownshipNPCAttribute[] attributearray = (TownshipNPCAttribute[])type.GetCustomAttributes(typeof(TownshipNPCAttribute), false);
                    if (attributearray.Length > 0)
                    {
                        tsnpcCount++;

                        //it's a townshipNPC
                        if (TownshipHelper.IsRestrictedTownshipNPC(mx))
                        {
                            bCanPlace = false;
                        }
                    }
                }

                if (
                    (playervendorCount > 0 || tsnpcCount > 0)
                    &&
                    (TownshipHelper.IsRestrictedTownshipNPCDeed(this))
                   )
                {
                    bCanPlace = false;
                }

                if (!bCanPlace)
                {
                    from.SendMessage("You cannot place this vendor in a house with the other vendors that are present.");
                }
                else
                {
                    //now check that the total TownshipNPC count is under what the house can hold.
                    if (house.MaxSecures <= tsnpcCount)
                    {
                        from.SendMessage("You can't have any more Township NPCs in this house");
                    }
                    else
                    {
                        if (this.Place(from))
                        {
                            this.Delete();
                        }
                        else
                        {
                            from.SendMessage("Placement failed");
                        }
                    }
                }
            }
        }

        public virtual bool Place(Mobile from)
        {
            return false;
        }

        public virtual void SetupTownshipNPC(Mobile m, Mobile placer) //BaseCreature m)
        {
            Point3D location = placer.Location;
            Map map = placer.Map;

            m.MoveToWorld(location, map);

            m.Guild = this.m_Guild;
            m.DisplayGuildTitle = true;
            m.CantWalk = true;

            BaseCreature bc = m as BaseCreature;
            if (bc != null) //that shouldn't be possible, but good to check anyways :-P
            {
                bc.Home = location;
                bc.RangeHome = 3;
                bc.CantWalk = false; //set this back to false
            }
        }
    }

    public class TSBankerDeed : TownshipNPCDeed
    {
        public TSBankerDeed(Guilds.Guild guild)
            : base(guild)
        {
            SetName("a banker");
        }

        public TSBankerDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); //version
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override bool Place(Mobile from)
        {
            try
            {
                if (((TownshipStone)m_Guild.TownshipStone).ActivityLevel >= Township.ActivityLevel.HIGH)
                {
                    TSBanker tsm = new TSBanker();
                    SetupTownshipNPC(tsm, from);
                    return true;
                }
                else
                {
                    from.SendMessage("Your township must have a higher activity level to place this NPC.");
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }
            return false;
        }
    }

    public class TSAnimalTrainerDeed : TownshipNPCDeed
    {
        public TSAnimalTrainerDeed(Guilds.Guild guild)
            : base(guild)
        {
            SetName("an animal trainer");
        }

        public TSAnimalTrainerDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); //version
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override bool Place(Mobile from)
        {
            try
            {
                if (((TownshipStone)m_Guild.TownshipStone).ActivityLevel >= Township.ActivityLevel.HIGH)
                {
                    TSAnimalTrainer tsm = new TSAnimalTrainer();
                    SetupTownshipNPC(tsm, from);
                    return true;
                }
                else
                {
                    from.SendMessage("Your township must have a higher activity level to place this NPC.");
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }
            return false;
        }
    }


    public class TSMageDeed : TownshipNPCDeed
    {
        public TSMageDeed(Guilds.Guild guild)
            : base(guild)
        {
            SetName("a mage");
        }

        public TSMageDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); //version
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override bool Place(Mobile from)
        {
            try
            {
                if (((TownshipStone)m_Guild.TownshipStone).ActivityLevel >= Township.ActivityLevel.MEDIUM)
                {
                    TSMage tsm = new TSMage();
                    SetupTownshipNPC(tsm, from);
                    return true;
                }
                else
                {
                    from.SendMessage("Your township must have a higher activity level to place this NPC.");
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }
            return false;
        }
    }


    public class TSAlchemistDeed : TownshipNPCDeed
    {
        public TSAlchemistDeed(Guilds.Guild guild)
            : base(guild)
        {
            SetName("an alchemist");
        }

        public TSAlchemistDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); //version
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override bool Place(Mobile from)
        {
            try
            {
                if (((TownshipStone)m_Guild.TownshipStone).ActivityLevel >= Township.ActivityLevel.MEDIUM)
                {
                    TSAlchemist tsm = new TSAlchemist();
                    SetupTownshipNPC(tsm, from);
                    return true;
                }
                else
                {
                    from.SendMessage("Your township must have a higher activity level to place this NPC.");
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }
            return false;
        }
    }


    public class TSProvisionerDeed : TownshipNPCDeed
    {
        public TSProvisionerDeed(Guilds.Guild guild)
            : base(guild)
        {
            SetName("a provisioner");
        }

        public TSProvisionerDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); //version
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override bool Place(Mobile from)
        {
            TSProvisioner tsm = new TSProvisioner();
            SetupTownshipNPC(tsm, from);
            return true;
        }
    }


    public class TSArmsTrainerDeed : TownshipNPCDeed
    {
        public TSArmsTrainerDeed(Guilds.Guild guild)
            : base(guild)
        {
            SetName("an arms trainer");
        }

        public TSArmsTrainerDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); //version
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override bool Place(Mobile from)
        {
            TSArmsTrainer tsm = new TSArmsTrainer();
            SetupTownshipNPC(tsm, from);
            return true;
        }
    }

    public class TSMageTrainerDeed : TownshipNPCDeed
    {
        public TSMageTrainerDeed(Guilds.Guild guild)
            : base(guild)
        {
            SetName("a mage trainer");
        }

        public TSMageTrainerDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); //version
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override bool Place(Mobile from)
        {
            TSMageTrainer tsm = new TSMageTrainer();
            SetupTownshipNPC(tsm, from);
            return true;
        }
    }

    public class TSRogueTrainerDeed : TownshipNPCDeed
    {
        public TSRogueTrainerDeed(Guilds.Guild guild)
            : base(guild)
        {
            SetName("a rogue trainer");
        }

        public TSRogueTrainerDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); //version
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override bool Place(Mobile from)
        {
            TSRogueTrainer tsm = new TSRogueTrainer();
            SetupTownshipNPC(tsm, from);
            return true;
        }
    }


    public class TSEmissaryDeed : TownshipNPCDeed
    {
        public TSEmissaryDeed(Guilds.Guild guild)
            : base(guild)
        {
            SetName("an emissary");
        }

        public TSEmissaryDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); //version
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override bool Place(Mobile from)
        {
            try
            {
                if (m_Guild != null)
                {
                    if (m_Guild.TownshipStone != null)
                    {
                        if (((TownshipStone)m_Guild.TownshipStone).ActivityLevel >= Township.ActivityLevel.MEDIUM)
                        {
                            TSEmissary tsm = new TSEmissary();
                            SetupTownshipNPC(tsm, from);
                            return true;
                        }
                        else
                        {
                            from.SendMessage("Your township must have a higher activity level to place this NPC.");
                        }
                    }
                    else
                    {
                        from.SendMessage("The guild assigned to this deed ({0}) has no township.", m_Guild.Name);
                    }
                }
                else
                {
                    from.SendMessage("There is no guild assigned to this deed.");
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }
            return false;
        }
    }

    public class TSEvocatorDeed : TownshipNPCDeed
    {
        public TSEvocatorDeed(Guilds.Guild guild)
            : base(guild)
        {
            SetName("an evocator");
        }

        public TSEvocatorDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); //version
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override bool Place(Mobile from)
        {
            try
            {
                if (m_Guild != null)
                {
                    if (m_Guild.TownshipStone != null)
                    {
                        if (((TownshipStone)m_Guild.TownshipStone).ActivityLevel >= Township.ActivityLevel.MEDIUM)
                        {
                            TSEvocator tsm = new TSEvocator();
                            SetupTownshipNPC(tsm, from);
                            return true;
                        }
                        else
                        {
                            from.SendMessage("Your township must have a higher activity level to place this NPC.");
                        }
                    }
                    else
                    {
                        from.SendMessage("The guild assigned to this deed ({0}) has no township.", m_Guild.Name);
                    }
                }
                else
                {
                    from.SendMessage("There is no guild assigned to this deed.");
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }
            return false;
        }
    }

    public class TSInnkeeperDeed : TownshipNPCDeed
    {
        public TSInnkeeperDeed(Guilds.Guild guild)
            : base(guild)
        {
            SetName("an innkeeper");
        }

        public TSInnkeeperDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); //version
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override bool Place(Mobile from)
        {
            TSInnKeeper tsm = new TSInnKeeper();
            SetupTownshipNPC(tsm, from);
            return true;
        }
    }


    public class TSTownCrierDeed : TownshipNPCDeed
    {
        public TSTownCrierDeed(Guilds.Guild guild)
            : base(guild)
        {
            SetName("the town crier");
        }

        public TSTownCrierDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); //version
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override bool Place(Mobile from)
        {
            try
            {
                if (((TownshipStone)m_Guild.TownshipStone).ActivityLevel >= Township.ActivityLevel.MEDIUM)
                {
                    TownshipRegion tr = TownshipRegion.FindDRDTRegion(from.Map, from.Location) as TownshipRegion;
                    if (tr != null)
                    {
                        if (tr.TStone != null)
                        {
                            CanExtendResult cer = tr.TStone.CanExtendRegion(from);
                            if (cer == CanExtendResult.CanExtend)
                            {
                                tr.TStone.ExtendRegion();
                                TSTownCrier tsm = new TSTownCrier();
                                SetupTownshipNPC(tsm, from);
                                return true;
                            }
                            else
                            {
                                from.SendMessage("Can't extend Region.");
                                switch (cer)
                                {
                                    case CanExtendResult.ConflictingRegion:
                                        from.SendMessage("Extended area would conflict with another special area.");
                                        break;
                                    case CanExtendResult.HousingPercentage:
                                        from.SendMessage("Guild doens't own enough of the housing in the extended area.");
                                        break;
                                    default:
                                        from.SendMessage("Unknown reason - inform an administrator.");
                                        break;
                                }
                            }
                        }
                        else
                        {
                            from.SendMessage("Can't find Township Stone.");
                        }
                    }
                    else
                    {
                        from.SendMessage("Can't find Township.");
                    }
                }
                else
                {
                    from.SendMessage("Your township must have a higher activity level to place this NPC.");
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }
            return false;

        }
    }

    public class TSLookoutDeed : TownshipNPCDeed
    {
        public TSLookoutDeed(Guilds.Guild guild)
            : base(guild)
        {
            SetName("a lookout");
        }

        public TSLookoutDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); //version
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override bool Place(Mobile from)
        {
            TSLookout tsm = new TSLookout();
            SetupTownshipNPC(tsm, from);
            tsm.CantWalk = true;
            return true;
        }
    }

    public class TSFightbrokerDeed : TownshipNPCDeed
    {
        public TSFightbrokerDeed(Guilds.Guild guild)
            : base(guild)
        {
            SetName("a fight broker");
        }

        public TSFightbrokerDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); //version
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override bool Place(Mobile from)
        {
            TSFightBroker tsm = new TSFightBroker();
            SetupTownshipNPC(tsm, from);
            tsm.CantWalk = true;
            return true;
        }
    }


    public class TSAuctioneerDeed : TownshipNPCDeed
    {
        public TSAuctioneerDeed(Guilds.Guild guild)
            : base(guild)
        {
            SetName("an auctioneer");
        }

        public TSAuctioneerDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); //version
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override bool Place(Mobile from)
        {
            TSAuctioneer tsm = new TSAuctioneer();
            SetupTownshipNPC(tsm, from);
            tsm.CantWalk = true;
            return true;
        }
    }

}
