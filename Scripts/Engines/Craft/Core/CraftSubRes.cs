using System;
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

namespace Server.Engines.Craft
{
    public class CraftSubRes
    {
        private Type m_Type;
        private double m_ReqSkill;
        private string m_NameString;
        private int m_NameNumber;
        private int m_GenericNameNumber;
        private object m_Message;

        public CraftSubRes(Type type, string name, double reqSkill, object message)
        {
            m_Type = type;
            m_NameString = name;
            m_ReqSkill = reqSkill;
            m_Message = message;
        }

        public CraftSubRes(Type type, int name, double reqSkill, object message)
        {
            m_Type = type;
            m_NameNumber = name;
            m_ReqSkill = reqSkill;
            m_Message = message;
        }

        public CraftSubRes(Type type, int name, double reqSkill, int genericNameNumber, object message)
        {
            m_Type = type;
            m_NameNumber = name;
            m_ReqSkill = reqSkill;
            m_GenericNameNumber = genericNameNumber;
            m_Message = message;
        }

        public Type ItemType
        {
            get { return m_Type; }
        }

        public string NameString
        {
            get { return m_NameString; }
        }

        public int NameNumber
        {
            get { return m_NameNumber; }
        }

        public int GenericNameNumber
        {
            get { return m_GenericNameNumber; }
        }

        public object Message
        {
            get { return m_Message; }
        }

        public double RequiredSkill
        {
            get { return m_ReqSkill; }
        }
    }
}
