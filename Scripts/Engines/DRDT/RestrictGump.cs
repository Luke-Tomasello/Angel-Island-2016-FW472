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

/* Scripts/Engines/DRDT/RestrictGump.cs
 *	6/30/08, Adam
 *		- Use math.min to create a loop counter of the smaller of the BitArray or Table for which the BitArray was created 
 *			This came about because the RegionController saves two a bitarrays; one for the size of the Spell Table and the other for the size of the
 *			Skill Table. The problem is that when we merge in code that includes new Spells or Skills, this logic breaks. 
 *			(This happened when we recently merged in new networking code and also merged in SpellWeaving, the 55th skill.
 *		- Add exception logging via assert
 *	6/30/08, weaver
 *		Added try/catch to cater for cases where the registered spell count differs from the restricted spell count
 *		(in order to prevent shard crash when editing restricted spells while this is the case).
 *		Added changelog + copyright header.
 */


using Server.Network;
using Server.Spells;
using System;
using System.Collections;

public enum RestrictType
{
    Spells,
    Skills
}

namespace Server.Gumps
{
    public abstract class RestrictGump : Gump
    {
        BitArray m_Restricted;

        RestrictType m_type;

        public RestrictGump(BitArray ba, RestrictType t)
            : base(50, 50)
        {
            m_Restricted = ba;
            m_type = t;

            Closable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);

            AddBackground(10, 10, 225, 425, 9380);
            AddLabel(73, 15, 1152, (t == RestrictType.Spells) ? "Restrict Spells" : "Restrict Skills");
            AddButton(91, 411, 247, 248, 1, GumpButtonType.Reply, 0);
            //Okay Button ->  # 1

            int itemsThisPage = 0;
            int nextPageNumber = 1;

            Object[] ary;// = (t == RestrictType.Skills) ? SkillInfo.Table : SpellRegistry.Types;

            if (t == RestrictType.Skills)
                ary = SkillInfo.Table;
            else
                ary = SpellRegistry.Types;

            // in the case where the static Spell or Skill table changed, only loop over the smaller set
            int MaxIterations = Math.Min(ary.Length, ba.Count);

            // report this so that we can recreate these controllers with an updated set of spells/skills
            Misc.Diagnostics.Assert(ary.Length == ba.Count, "Bit array size does not match Spell/Skill array size.");

            try
            {
                for (int i = 0; i < MaxIterations; i++)
                {
                    if (ary[i] != null)
                    {
                        if (itemsThisPage >= 8 || itemsThisPage == 0)
                        {
                            itemsThisPage = 0;

                            if (nextPageNumber != 1)
                            {
                                AddButton(190, 412, 4005, 4007, 2, GumpButtonType.Page, nextPageNumber);
                                //Forward button -> #2
                            }

                            AddPage(nextPageNumber++);

                            if (nextPageNumber != 2)
                            {
                                AddButton(29, 412, 4014, 4016, 3, GumpButtonType.Page, nextPageNumber - 2);
                                //Back Button -> #3
                            }
                        }

                        AddCheck(40, 55 + (45 * itemsThisPage), 210, 211, ba[i], i + ((t == RestrictType.Spells) ? 100 : 500));
                        AddLabel(70, 55 + (45 * itemsThisPage), 0, ((t == RestrictType.Spells) ? ((Type)(ary[i])).Name : ((SkillInfo)(ary[i])).Name));

                        itemsThisPage++;
                    }
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1)
            {
                for (int i = 0; i < m_Restricted.Length; i++)
                {
                    m_Restricted[i] = info.IsSwitched(i + ((m_type == RestrictType.Spells) ? 100 : 500));
                    //This way is faster after looking at decompiled BitArray.SetAll( bool )
                }
            }
        }
    }

    public class SpellRestrictGump : RestrictGump
    {
        public SpellRestrictGump(BitArray ba)
            : base(ba, RestrictType.Spells)
        {

        }
    }

    public class SkillRestrictGump : RestrictGump
    {
        public SkillRestrictGump(BitArray ba)
            : base(ba, RestrictType.Skills)
        {

        }
    }
}
