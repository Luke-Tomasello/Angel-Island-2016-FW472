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

/* Items/Misc/Teleporter.cs
 * CHANGELOG:
 *	4/15/08, Adam
 *		Add an AccessLevel property so we can have restricted access teleporters to areas under construction or that are only 
 *			available during certain times of the year. 
 *			We'll probably use this at least during construction and test of the Summer Champ in Ishlenar. We may also set it to
 *			no access in non summer months
 *  06/26/06, Kit
 *		Added Bool TeleportPets, added new string for msg that pets cant use.
 *  06/03/06, Kit
 *		Added additional destination point3d, now choose random one if multiple are filled in.
 *		added ability to define rect and teleport to random point in rect that can spawn mobile.
 *  04/13/06, Kit
 *		Added bool Criminal for checking to only transport non criminals on normal teleporters.
 *	6/1/05, erlein
 *		- Added SparkleEffect property which, if enabled, will send sparklies
 *		when player steps on + keep resending every second leading to end
 *		of teleporter delay period.
 *		- Added string property which passes message to player when they step
 *		on the teleporter.
 *	5/8/05, Pix
 *		Made teleporters with delays work.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Network;
using System;
using System.Collections;

namespace Server.Items
{
    public class Teleporter : Item
    {
        private bool m_Active, m_Creatures;
        private Point3D m_PointDest;
        private Point3D m_PointDest2;
        private Point3D m_PointDest3;
        private Point3D m_PointDest4;
        private Point3D m_PointDest5;
        private Point2D m_RectStart;
        private Point2D m_RectEnd;

        private Map m_MapDest;
        private bool m_SourceEffect;
        private bool m_DestEffect;
        private bool m_SparkleEffect;
        private string m_DelayMessage;
        private int m_SoundID;
        private TimeSpan m_Delay;
        private bool m_Criminal;
        private bool m_TransportPets;
        private string m_PetMessage;
        private AccessLevel m_AccessLevel;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool SourceEffect
        {
            get { return m_SourceEffect; }
            set { m_SourceEffect = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool TelePortCriminals
        {
            get { return m_Criminal; }
            set { m_Criminal = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public AccessLevel AccessLevel
        {
            get { return m_AccessLevel; }
            set { m_AccessLevel = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool TeleportPets
        {
            get { return m_TransportPets; }
            set { m_TransportPets = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string NoTeleportPetMessage
        {
            get { return m_PetMessage; }
            set { m_PetMessage = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool DestEffect
        {
            get { return m_DestEffect; }
            set { m_DestEffect = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool SparkleEffect
        {
            get { return m_SparkleEffect; }
            set { m_SparkleEffect = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string DelayMessage
        {
            get { return m_DelayMessage; }
            set { m_DelayMessage = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SoundID
        {
            get { return m_SoundID; }
            set { m_SoundID = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Delay
        {
            get { return m_Delay; }
            set { m_Delay = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get { return m_Active; }
            set { m_Active = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D PointDest
        {
            get { return m_PointDest; }
            set { m_PointDest = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D PointDest2
        {
            get { return m_PointDest2; }
            set { m_PointDest2 = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D PointDest3
        {
            get { return m_PointDest3; }
            set { m_PointDest3 = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D PointDest4
        {
            get { return m_PointDest4; }
            set { m_PointDest4 = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D PointDest5
        {
            get { return m_PointDest5; }
            set { m_PointDest5 = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point2D RectStartXY
        {
            get { return m_RectStart; }
            set { m_RectStart = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point2D RectEndXY
        {
            get { return m_RectEnd; }
            set { m_RectEnd = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map MapDest
        {
            get { return m_MapDest; }
            set { m_MapDest = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Creatures
        {
            get { return m_Creatures; }
            set { m_Creatures = value; }
        }

        public override int LabelNumber { get { return 1026095; } } // teleporter

        [Constructable]
        public Teleporter()
            : this(new Point3D(0, 0, 0), null, false)
        {
        }

        [Constructable]
        public Teleporter(Point3D pointDest, Map mapDest)
            : this(pointDest, mapDest, false)
        {
        }

        [Constructable]
        public Teleporter(Point3D pointDest, Map mapDest, bool creatures)
            : base(0x1BC3)
        {
            Movable = false;
            Visible = false;

            m_Active = true;
            m_PointDest = pointDest;
            m_MapDest = mapDest;
            m_Creatures = creatures;
            m_SparkleEffect = false;
            m_DelayMessage = "";
            m_Criminal = true;
            m_TransportPets = true;
            m_PetMessage = null;
            m_AccessLevel = Server.AccessLevel.Player;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Active)
                list.Add(1060742); // active
            else
                list.Add(1060743); // inactive

            if (m_MapDest != null)
                list.Add(1060658, "Map\t{0}", m_MapDest);

            if (m_PointDest != Point3D.Zero)
                list.Add(1060659, "Coords\t{0}", m_PointDest);

            list.Add(1060660, "Creatures\t{0}", m_Creatures ? "Yes" : "No");
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Active)
            {
                Rectangle2D rect = new Rectangle2D(m_RectStart.X, m_RectStart.Y, m_RectEnd.X, m_RectEnd.Y);
                if (m_MapDest != null && m_PointDest != Point3D.Zero)
                    LabelTo(from, "{0} [{1}]", m_PointDest, m_MapDest);
                else if (m_MapDest != null)
                    LabelTo(from, "[{0}]", m_MapDest);
                else if (m_PointDest != Point3D.Zero)
                    LabelTo(from, m_PointDest.ToString());
                else if (m_PointDest2 != Point3D.Zero)
                    LabelTo(from, m_PointDest2.ToString());
                else if (m_PointDest3 != Point3D.Zero)
                    LabelTo(from, m_PointDest3.ToString());
                else if (m_PointDest4 != Point3D.Zero)
                    LabelTo(from, m_PointDest4.ToString());
                else if (m_PointDest5 != Point3D.Zero)
                    LabelTo(from, m_PointDest5.ToString());
                else if (m_RectStart != Point2D.Zero && m_RectEnd != Point2D.Zero)
                    LabelTo(from, rect.ToString());
            }
            else
            {
                LabelTo(from, "(inactive)");
            }
        }

        public virtual void StartTeleport(Mobile m)
        {
            if (m_Delay == TimeSpan.Zero)
                DoTeleport(m);
            else
            {
                Timer.DelayCall(m_Delay, new TimerStateCallback(DoTeleport_Callback), m);
                if (m_DelayMessage != "")
                    m.SendMessage(m_DelayMessage);
            }
        }

        private void DoTeleport_Callback(object state)
        {
            DoTeleport((Mobile)state);
        }

        public virtual void DoTeleport(Mobile m)
        {
            if (this.Delay != TimeSpan.Zero
                && m.Location != this.Location)
            {
                //If we're delayed and we're not on the teleporter,
                // ignore the teleport
                return;
            }

            Map map = m_MapDest;
            ArrayList temp = new ArrayList();

            if (map == null || map == Map.Internal)
                map = m.Map;

            Point3D p;

            if (m_PointDest != Point3D.Zero)
                temp.Add(m_PointDest);
            if (m_PointDest2 != Point3D.Zero)
                temp.Add(m_PointDest2);
            if (m_PointDest3 != Point3D.Zero)
                temp.Add(m_PointDest3);
            if (m_PointDest4 != Point3D.Zero)
                temp.Add(m_PointDest4);
            if (m_PointDest5 != Point3D.Zero)
                temp.Add(m_PointDest5);

            if (temp.Count == 0)
                p = m.Location;
            else
                p = (Point3D)temp[Utility.Random(temp.Count)];

            if (m_RectStart != Point2D.Zero && m_RectEnd != Point2D.Zero)
            {
                for (int i = 0; i < 20; ++i)
                {
                    int x = Utility.RandomMinMax(m_RectStart.X, m_RectEnd.X);
                    int y = Utility.RandomMinMax(m_RectStart.Y, m_RectEnd.Y);
                    if (map.CanSpawnMobile(x, y, 0))
                    {
                        p = new Point3D(x, y, 0);
                        continue;
                    }
                    else
                    {
                        int z = map.GetAverageZ(x, y);

                        if (map.CanSpawnMobile(x, y, z))
                        {
                            p = new Point3D(x, y, z);
                            continue;
                        }
                    }
                }
            }

            if (TeleportPets)
                Server.Mobiles.BaseCreature.TeleportPets(m, p, map);
            else
            {
                if (NoTeleportPetMessage != null)
                    m.SendMessage(NoTeleportPetMessage);
                else
                    m.SendMessage("Your companion is unable to accompany you and remains behind.");
            }

            if (m_SourceEffect)
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);

            m.MoveToWorld(p, map);

            if (m_DestEffect)
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);

            if (m_SoundID > 0)
                Effects.PlaySound(m.Location, m.Map, m_SoundID);
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m_Active)
            {
                if (!m_Creatures && !m.Player)
                    return true;

                if (!m_Criminal && m.Criminal == true)
                {
                    m.SendMessage("You are a criminal and may not use this.");
                    return true;
                }

                if (m.AccessLevel < m_AccessLevel)
                {
                    m.SendMessage("You shall not pass!");
                    return true;
                }

                StartTeleport(m);
                if (m_SparkleEffect)
                    Timer.DelayCall(TimeSpan.FromSeconds(0.5), new TimerStateCallback(SendSparkles_Callback), m);

                if (Delay == TimeSpan.Zero)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            return true;
        }

        private void SendSparkles_Callback(object state)
        {
            Mobile from = (Mobile)state;

            if (from.Location != this.Location)
                return;

            Effects.SendLocationParticles(EffectItem.Create(
                this.Location, this.Map, TimeSpan.FromSeconds(1.0)
                ), 0x376A, 9, 32, 5020);

            Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerStateCallback(SendSparkles_Callback), from);
        }

        public Teleporter(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)7); // version

            // version 7
            writer.Write((int)m_AccessLevel);

            // version 6 (I guess)
            writer.Write((bool)m_TransportPets);
            writer.Write(m_PetMessage);
            writer.Write(m_PointDest2);
            writer.Write(m_PointDest3);
            writer.Write(m_PointDest4);
            writer.Write(m_PointDest5);
            writer.Write(m_RectStart);
            writer.Write(m_RectEnd);
            writer.Write((bool)m_Criminal);
            writer.Write(m_DelayMessage);
            writer.Write((bool)m_SparkleEffect);
            writer.Write((bool)m_SourceEffect);
            writer.Write((bool)m_DestEffect);
            writer.Write((TimeSpan)m_Delay);
            writer.WriteEncodedInt((int)m_SoundID);

            writer.Write(m_Creatures);

            writer.Write(m_Active);
            writer.Write(m_PointDest);
            writer.Write(m_MapDest);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 7:
                    {
                        m_AccessLevel = (AccessLevel)reader.ReadInt();
                        goto case 6;
                    }
                case 6:
                    {
                        m_TransportPets = reader.ReadBool();
                        m_PetMessage = reader.ReadString();
                        goto case 5;
                    }

                case 5:
                    {
                        m_PointDest2 = reader.ReadPoint3D();
                        m_PointDest3 = reader.ReadPoint3D();
                        m_PointDest4 = reader.ReadPoint3D();
                        m_PointDest5 = reader.ReadPoint3D();
                        m_RectStart = reader.ReadPoint2D();
                        m_RectEnd = reader.ReadPoint2D();

                        goto case 4;
                    }

                case 4:
                    {
                        m_Criminal = reader.ReadBool();

                        goto case 3;
                    }

                case 3:
                    {
                        m_DelayMessage = reader.ReadString();
                        m_SparkleEffect = reader.ReadBool();

                        goto case 2;
                    }

                case 2:
                    {
                        m_SourceEffect = reader.ReadBool();
                        m_DestEffect = reader.ReadBool();
                        m_Delay = reader.ReadTimeSpan();
                        m_SoundID = reader.ReadEncodedInt();

                        goto case 1;
                    }
                case 1:
                    {
                        m_Creatures = reader.ReadBool();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Active = reader.ReadBool();
                        m_PointDest = reader.ReadPoint3D();
                        m_MapDest = reader.ReadMap();

                        break;
                    }
            }

            if (version < 7)
            {
                m_AccessLevel = AccessLevel.Player;
            }

            if (version < 6)
            {
                m_TransportPets = true;
                m_PetMessage = null;

            }

        }
    }

    public class SkillTeleporter : Teleporter
    {
        private SkillName m_Skill;
        private double m_Required;
        private string m_MessageString;
        private int m_MessageNumber;

        [CommandProperty(AccessLevel.GameMaster)]
        public SkillName Skill
        {
            get { return m_Skill; }
            set { m_Skill = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double Required
        {
            get { return m_Required; }
            set { m_Required = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string MessageString
        {
            get { return m_MessageString; }
            set { m_MessageString = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MessageNumber
        {
            get { return m_MessageNumber; }
            set { m_MessageNumber = value; InvalidateProperties(); }
        }

        private void EndMessageLock(object state)
        {
            ((Mobile)state).EndAction(this);
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (Active)
            {
                if (!Creatures && !m.Player)
                    return true;

                Skill sk = m.Skills[m_Skill];

                if (sk == null || sk.Base < m_Required)
                {
                    if (m.BeginAction(this))
                    {
                        if (m_MessageString != null)
                            m.Send(new UnicodeMessage(Serial, ItemID, MessageType.Regular, 0x3B2, 3, "ENU", null, m_MessageString));
                        else if (m_MessageNumber != 0)
                            m.Send(new MessageLocalized(Serial, ItemID, MessageType.Regular, 0x3B2, 3, m_MessageNumber, null, ""));

                        Timer.DelayCall(TimeSpan.FromSeconds(5.0), new TimerStateCallback(EndMessageLock), m);
                    }

                    return false;
                }

                StartTeleport(m);
                return false;
            }

            return true;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            int skillIndex = (int)m_Skill;
            string skillName;

            if (skillIndex >= 0 && skillIndex < SkillInfo.Table.Length)
                skillName = SkillInfo.Table[skillIndex].Name;
            else
                skillName = "(Invalid)";

            list.Add(1060661, "{0}\t{1:F1}", skillName, m_Required);

            if (m_MessageString != null)
                list.Add(1060662, "Message\t{0}", m_MessageString);
            else if (m_MessageNumber != 0)
                list.Add(1060662, "Message\t#{0}", m_MessageNumber);
        }

        [Constructable]
        public SkillTeleporter()
        {
        }

        public SkillTeleporter(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_Skill);
            writer.Write((double)m_Required);
            writer.Write((string)m_MessageString);
            writer.Write((int)m_MessageNumber);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Skill = (SkillName)reader.ReadInt();
                        m_Required = reader.ReadDouble();
                        m_MessageString = reader.ReadString();
                        m_MessageNumber = reader.ReadInt();

                        break;
                    }
            }
        }
    }

    public class KeywordTeleporter : Teleporter
    {
        private string m_Substring;
        private int m_Keyword;
        private int m_Range;

        [CommandProperty(AccessLevel.GameMaster)]
        public string Substring
        {
            get { return m_Substring; }
            set { m_Substring = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Keyword
        {
            get { return m_Keyword; }
            set { m_Keyword = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Range
        {
            get { return m_Range; }
            set { m_Range = value; InvalidateProperties(); }
        }

        public override bool HandlesOnSpeech { get { return true; } }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (!e.Handled && Active)
            {
                Mobile m = e.Mobile;

                if (!Creatures && !m.Player)
                    return;

                if (!m.InRange(GetWorldLocation(), m_Range))
                    return;

                bool isMatch = false;

                if (m_Keyword >= 0 && e.HasKeyword(m_Keyword))
                    isMatch = true;
                else if (m_Substring != null && e.Speech.ToLower().IndexOf(m_Substring.ToLower()) >= 0)
                    isMatch = true;

                if (!isMatch)
                    return;

                e.Handled = true;
                StartTeleport(m);
            }
        }

        public override bool OnMoveOver(Mobile m)
        {
            return true;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1060661, "Range\t{0}", m_Range);

            if (m_Keyword >= 0)
                list.Add(1060662, "Keyword\t{0}", m_Keyword);

            if (m_Substring != null)
                list.Add(1060663, "Substring\t{0}", m_Substring);
        }

        [Constructable]
        public KeywordTeleporter()
        {
            m_Keyword = -1;
            m_Substring = null;
        }

        public KeywordTeleporter(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_Substring);
            writer.Write(m_Keyword);
            writer.Write(m_Range);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Substring = reader.ReadString();
                        m_Keyword = reader.ReadInt();
                        m_Range = reader.ReadInt();

                        break;
                    }
            }
        }
    }
}
