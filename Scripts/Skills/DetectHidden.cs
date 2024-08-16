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

/* Scripts\Skills\DetectHidden.cs
 * Changelog
 * 5/11/08, Adam
 *		Upgrade code to use new region hashtables instead of ArrayLists
 * 12/14/07, plasma
 *  Changed in-house detection to use the housing region instead of a crazy 
 *  sized detection range.  This also fixed a bug that made it possible to
 *  have a full power DH check for 22 tiles.
 * 6/03/06, Kit
 *	Changed internal target to public for AI use.
 * 8/15/04, Old Salty
 * 	Added functionality for assessing whether a container is trapped, 
 *  and how likely the player will be to disarm it.
 */

using Server.Items;
using Server.Multis;
using Server.Targeting;
using System;

namespace Server.SkillHandlers
{
    public class DetectHidden
    {
        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.DetectHidden].Callback = new SkillUseCallback(OnUse);
        }

        public static TimeSpan OnUse(Mobile src)
        {
            src.SendLocalizedMessage(500819);//Where will you search?
            src.Target = new InternalTarget();

            return TimeSpan.FromSeconds(1.0);
        }

        public class InternalTarget : Target
        {
            public InternalTarget()
                : base(12, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile src, object targ)
            {
                bool foundAnyone = true;

                Point3D p;
                if (targ is Mobile)
                    p = ((Mobile)targ).Location;
                else if (targ is Item)
                    p = ((Item)targ).Location;
                else if (targ is IPoint3D)
                    p = new Point3D((IPoint3D)targ);
                else
                    p = src.Location;

                //Added functionality for assessing whether a container is trapped,
                //and how likely the player will be to disarm it. (Old Salty, 8/15/04)
                if (targ is TrapableContainer)
                {
                    TrapableContainer trap = (TrapableContainer)targ;
                    if (trap.TrapType == TrapType.None)
                    {
                        src.SendLocalizedMessage(502373); // That doesn't appear to be trapped
                        return;
                    }
                    else if (trap.TrapType != TrapType.None && src.CheckSkill(SkillName.DetectHidden, 0.0, 100.0))
                    {
                        string level = "unknown";
                        switch (trap.TrapPower / 25)
                        {
                            case 1:
                                level = "It appears to be protected by a simple and obvious trap."; break;
                            case 2:
                                level = "It seems to be guarded by a somewhat clever trap."; break;
                            case 3:
                                level = "It is protected by a respectably complex trap."; break;
                            case 4:
                                level = "It appears to be guarded by an impressively devious trap."; break;
                            case 5:
                                level = "It is defended by a dauntingly intricate and dangerous trap."; break;
                        }
                        src.LocalOverheadMessage(0, 33, false, level);

                        double minskill = trap.TrapPower;
                        //modify minskill with detect hidden skill
                        if (src.Skills[SkillName.DetectHidden].Base > 50.0)
                        {
                            minskill -= (src.Skills[SkillName.DetectHidden].Base - 50.0);
                        }
                        double maxskill = minskill + 50;
                        double RTskill = src.Skills[SkillName.RemoveTrap].Value;

                        if (RTskill <= minskill)
                            src.SendMessage("You are baffled by the complexity of this trap.");
                        else if (RTskill > minskill && RTskill < maxskill)
                        {
                            if (RTskill - minskill <= maxskill - RTskill)
                                src.SendMessage("You have a fair chance at disarming this trap.");
                            else if (RTskill - minskill > maxskill - RTskill)
                                src.SendMessage("You have a very good chance at disarming this trap.");
                        }
                        else
                            src.SendMessage("You could disable this trap with ease.");
                    }

                    return;
                }
                //end addition by OldSalty

                double srcSkill = src.Skills[SkillName.DetectHidden].Value;
                int range = (int)(srcSkill / 10.0);

                if (!src.CheckSkill(SkillName.DetectHidden, 0.0, 100.0))
                    range /= 2;

                //pla: remove house stuff from here as it's now implelemted additonally after the standard distance DH check


                //BaseHouse house = BaseHouse.FindHouseAt( p, src.Map, 16 );        
                //bool inHouse = ( house != null && house.IsFriend( src ) );

                //if ( inHouse )
                //	range = 22;

                if (range > 0)
                {
                    IPooledEnumerable inRange = src.Map.GetMobilesInRange(p, range);

                    foreach (Mobile trg in inRange)
                    {
                        if (trg.Hidden && src != trg)
                        {
                            double ss = srcSkill + Utility.Random(21) - 10;
                            double ts = trg.Skills[SkillName.Hiding].Value + Utility.Random(21) - 10;

                            //pla: removed house bits from this check too
                            if (src.AccessLevel >= trg.AccessLevel && (ss >= ts))
                            {
                                trg.RevealingAction();
                                trg.SendLocalizedMessage(500814); // You have been revealed!
                                foundAnyone = true;
                            }
                        }
                    }
                    inRange.Free();
                }

                //pla: if the mobile is in a friended house and targets a spot within the same house
                //then reveal all mobiles within the housing region regardless of skill checks
                BaseHouse house = BaseHouse.FindHouseAt(p, src.Map, 16);
                if (house != null && house.IsInside(src) && house.IsFriend(src))
                    foreach (Mobile m in house.Region.Mobiles.Values)
                        if (m.Hidden && m != src)
                        {
                            m.RevealingAction();
                            m.SendLocalizedMessage(500814); // You have been revealed!
                            foundAnyone = true;
                        }

                if (!foundAnyone)
                {
                    src.SendLocalizedMessage(500817); // You can see nothing hidden there.
                }
            }
        }
    }
}
