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

/*
 * Scripts/Mobiles/Animals/Mounts/BaseMount.cs
 * ChangeLog:
 *  09/25/08, Adam
 *		TURN OFF MOUNTS 
 *		Seems there is at least one client out there that moves mounted players faster regardless of the server settings.
 *		We will need to disable this until this is better understood.
 *  09/23/08, Adam
 *		TURN ON MOUNTS
 *		Comment out the code in OnDoubleClick() that was preventing players from riding a mountable creature. 
 *		Please see ComputeMovementSpeed() in PlayerMobile.cs for changes to Mount Speed.
 *	6/20/2004 - Pixie
 *		Fixed problem with old mountables disappearing from the stables when the server restarts.
 *		Was a problem with the MountItem constructor - if m_Mount was set to null, the next
 *		deserialize would delete the MountItem, and if the MountItem in BaseMount was null, the
 *		BaseMount would delete itself.
 * 3/18/04 code changes by smerX:
 *	"OnDoubleClick( Mobile from )" directed straight to "OnDisallowedRider( Mobile m )"
 * - Mounts are no longer mountable.
*/

namespace Server.Mobiles
{
    public abstract class BaseMount : BaseCreature, IMount
    {
        private Mobile m_Rider;
        private Item m_InternalItem;

        public virtual bool AllowMaleRider { get { return true; } }
        public virtual bool AllowFemaleRider { get { return true; } }

        public BaseMount(string name, int bodyID, int itemID, AIType aiType, FightMode fightMode, int rangePerception, int rangeFight, double activeSpeed, double passiveSpeed)
            : base(aiType, fightMode, rangePerception, rangeFight, activeSpeed, passiveSpeed)
        {
            Name = name;
            Body = bodyID;

            m_InternalItem = new MountItem(this, itemID);
        }

        public BaseMount(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_Rider);
            writer.Write(m_InternalItem);
        }

        #region Fatigue/Stamina Loss
        private double m_riderStamAccumulator;
        public double RiderStamAccumulator { get { return m_riderStamAccumulator; } set { m_riderStamAccumulator = value; } }

        private Memory StamRefresh = new Memory();
        private int LastStepTaken = 0;

        public override void OnControlOrder(OrderType order)
        {   // do the old-school stam refresh on "all follow"
            if (!Core.UOAI && !Core.UOREN && !Core.UOMO && order == OrderType.Follow)
            {
                // as usual, we we don't know the actual formula, make something up, but make it reasonable

                if (ControlMaster is PlayerMobile == false)     // null check + sanity
                    return;

                if (StamRefresh.Recall(ControlMaster) == true)  // too soon to refresh?
                    return;

                StamRefresh.Remember(ControlMaster, .250);      // 1/4 second delay between refreshes

                PlayerMobile pm = ControlMaster as PlayerMobile;

                if (pm.StepsTaken == LastStepTaken)             // you need to move (like you walk while the creature follows)
                    return;

                LastStepTaken = pm.StepsTaken;

                if (Stam >= StamMax)
                    return;

                /*
				 * Content = 60,
				 * Happy = 70,
				 * RatherHappy = 80,
				 * VeryHappy = 90,
				 * ExtremelyHappy = 100,
				 * WonderfullyHappy = 110
				 */
                if (Loyalty < PetLoyalty.Content)
                    return;

                // bump from 12 - 22 stam points
                Stam += (int)Loyalty / 5;
            }
        }

        public override void OnDismount(Mobile mount)
        {
            m_riderStamAccumulator = 0;
        }

        public override void OnMount(Mobile mount)
        {
            m_riderStamAccumulator = 0;
        }
        #endregion

        [Hue, CommandProperty(AccessLevel.GameMaster)]
        public override int Hue
        {
            get
            {
                return base.Hue;
            }
            set
            {
                base.Hue = value;

                if (m_InternalItem != null)
                    m_InternalItem.Hue = value;
            }
        }

        public override bool OnBeforeDeath()
        {
            Rider = null;

            return base.OnBeforeDeath();
        }

        public override void OnAfterDelete()
        {
            if (m_InternalItem != null)
                m_InternalItem.Delete();

            m_InternalItem = null;

            base.OnAfterDelete();
        }

        public override void OnDelete()
        {
            Rider = null;

            base.OnDelete();
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Rider = reader.ReadMobile();
                        m_InternalItem = reader.ReadItem();

                        if (m_InternalItem == null)
                            Delete();

                        break;
                    }
            }
        }

        public virtual void OnDisallowedRider(Mobile m)
        {
            if (Core.UOAI || Core.UOREN || Core.UOMO)    // when no mounts use this message
                m.SendMessage("That beast will not allow you to mount it.");
            else
                m.SendMessage("You may not ride this creature.");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Core.UOAI || Core.UOREN || Core.UOMO) // "turn off" mounts for AI an MO
            {
                if (from.AccessLevel == AccessLevel.Player)
                {
                    OnDisallowedRider(from);
                    return;
                }
            }

            if (IsDeadPet)
                return; // TODO: Should there be a message here?

            if (from.IsBodyMod && !from.Body.IsHuman)
            {
                if (Core.AOS) // You cannot ride a mount in your current form.
                    PrivateOverheadMessage(Network.MessageType.Regular, 0x3B2, 1062061, from.NetState);
                else
                    from.SendLocalizedMessage(1061628); // You can't do that while polymorphed.

                return;
            }

            if (!from.CanBeginAction(typeof(BaseMount)))
            {
                from.SendLocalizedMessage(1040024); // You are still too dazed from being knocked off your mount to ride!
                return;
            }

            if (from.Mounted)
            {
                from.SendLocalizedMessage(1005583); // Please dismount first.
                return;
            }

            if (from.Female ? !AllowFemaleRider : !AllowMaleRider)
            {
                OnDisallowedRider(from);
                return;
            }

            if (!Multis.DesignContext.Check(from))
                return;

            if (from.InRange(this, 1))
            {
                if ((Controlled && ControlMaster == from) || (Summoned && SummonMaster == from) || from.AccessLevel >= AccessLevel.GameMaster)
                {
                    Rider = from;
                    from.OnMount(this);
                }
                else if (!Controlled && !Summoned)
                {
                    from.SendLocalizedMessage(501263); // That mount does not look broken! You would have to tame it to ride it.
                }
                else
                {
                    from.SendLocalizedMessage(501264); // This isn't your mount; it refuses to let you ride.
                }
            }
            else
            {
                from.SendLocalizedMessage(500206); // That is too far away to ride.
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ItemID
        {
            get
            {
                if (m_InternalItem != null)
                    return m_InternalItem.ItemID;
                else
                    return 0;
            }
            set
            {
                if (m_InternalItem != null)
                    m_InternalItem.ItemID = value;
            }
        }

        public static void Dismount(Mobile m)
        {
            IMount mount = m.Mount;

            if (mount != null)
                mount.Rider = null;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Rider
        {
            get
            {
                return m_Rider;
            }
            set
            {
                if (m_Rider != value)
                {
                    if (value == null)
                    {
                        Point3D loc = m_Rider.Location;
                        Map map = m_Rider.Map;

                        if (map == null || map == Map.Internal)
                        {
                            loc = m_Rider.LogoutLocation;
                            map = m_Rider.LogoutMap;
                        }

                        Direction = m_Rider.Direction;
                        Location = loc;
                        Map = map;

                        if (m_InternalItem != null)
                            m_InternalItem.Internalize();
                    }
                    else
                    {
                        if (m_Rider != null)
                            Dismount(m_Rider);

                        Dismount(value);

                        if (m_InternalItem != null)
                            value.AddItem(m_InternalItem);

                        value.Direction = this.Direction;

                        Internalize();
                    }

                    Mobile oldRider = m_Rider;

                    m_Rider = value;

                    if (oldRider != null)
                        oldRider.OnDismount(this);
                }
            }
        }
    }

    public class MountItem : Item, IMountItem
    {
        private BaseMount m_Mount;

        public MountItem(BaseMount mount, int itemID)
            : base(itemID)
        {
            Layer = Layer.Mount;
            Movable = false;

            m_Mount = mount;
        }

        public MountItem(Serial serial)
            : base(serial)
        {
        }

        public override void OnAfterDelete()
        {
            if (m_Mount != null)
                m_Mount.Delete();

            m_Mount = null;

            base.OnAfterDelete();
        }

        public override DeathMoveResult OnParentDeath(Mobile parent)
        {
            if (m_Mount != null)
                m_Mount.Rider = null;

            return DeathMoveResult.RemainEquiped;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_Mount);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Mount = reader.ReadMobile() as BaseMount;

                        if (m_Mount == null)
                            Delete();

                        break;
                    }
            }
        }

        public IMount Mount
        {
            get
            {
                return m_Mount;
            }
        }
    }
}
