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

/* Items/Misc/ClientVersionTeleporter.cs
 * CHANGELOG:
 *	3/18/2008 - Pix
 *		Initial Version.
 */

namespace Server.Items
{
    class ClientVersionTeleporter : Teleporter
    {
        private ClientVersion m_MinVersion = new ClientVersion(0, 0, 0, 0);
        private ClientVersion m_MaxVersion = new ClientVersion(99, 0, 0, 0);

        [Constructable]
        public ClientVersionTeleporter()
            : base()
        {
            Active = false; //initially set to false :O
            Name = "client version teleporter";
        }

        public ClientVersionTeleporter(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string MinVersion
        {
            get { return m_MinVersion.ToString(); }
            set { m_MinVersion = new ClientVersion(value); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string MaxVersion
        {
            get { return m_MaxVersion.ToString(); }
            set { m_MaxVersion = new ClientVersion(value); }
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m.NetState != null &&
                m.NetState.Version >= m_MinVersion &&
                m.NetState.Version <= m_MaxVersion)
            {
                return base.OnMoveOver(m);
            }

            return true;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);
            LabelTo(from, "Min: " + this.m_MinVersion.ToString() + " - Max: " + this.m_MaxVersion.ToString());
        }


        #region Serialize/Deserialize
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            if (m_MinVersion == null)
            {
                writer.Write((int)0);//major
                writer.Write((int)0);//minor
                writer.Write((int)0);//revision
                writer.Write((int)0);//patch
            }
            else
            {
                writer.Write(m_MinVersion.Major);//major
                writer.Write(m_MinVersion.Minor);//minor
                writer.Write(m_MinVersion.Revision);//revision
                writer.Write(m_MinVersion.Patch);//patch
            }

            if (m_MaxVersion == null)
            {
                writer.Write((int)0);//major
                writer.Write((int)0);//minor
                writer.Write((int)0);//revision
                writer.Write((int)0);//patch
            }
            else
            {
                writer.Write(m_MaxVersion.Major);//major
                writer.Write(m_MaxVersion.Minor);//minor
                writer.Write(m_MaxVersion.Revision);//revision
                writer.Write(m_MaxVersion.Patch);//patch
            }

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    int major = reader.ReadInt();
                    int minor = reader.ReadInt();
                    int revision = reader.ReadInt();
                    int patch = reader.ReadInt();
                    m_MinVersion = new ClientVersion(major, minor, revision, patch);

                    major = reader.ReadInt();
                    minor = reader.ReadInt();
                    revision = reader.ReadInt();
                    patch = reader.ReadInt();
                    m_MaxVersion = new ClientVersion(major, minor, revision, patch);
                    break;
            }
        }
        #endregion

    }
}
