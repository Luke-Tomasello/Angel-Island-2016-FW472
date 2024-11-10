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

/* Scripts/Skills/Stealing.cs
 * ChangeLog:
 *	5/27/10, Adam
 *		special case: disallow stealing from someones stonebag
 *		We could generalize thos say you cannot steal from a blessed container if need be
 *	06/01/09, plasma
 *		Implement reverse pickpocket method
 *	4/9/09, Adam
 *		Add CheckStealing() to check the region rules re looting. Stealing off a corpse is considered looting
 *	1/7/09, Adam
 *		Add "You can't steal from them." message when CanBeHarmful == false
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *	11/17/07, Pix.
 *		Removed code bleed from who knows what I was trying to do!
 *	6/7/07, Pix.
 *		Removed check from stealing from targets in the thieves guild, which allowed
 *		thieves to steal from any other thief without going crim or permagrey.
 *  11/23/06, plasma
 *      Modified to only apply skill delay OnTraget()
 *  03/01/06, Kit
 *		Added check to close bankbox on item steal if open.
 *  02/28/06, weaver
 *		Added reset of next skill use time on steal target.
 *  01/02/05, Pix
 *		Now thief must be in the thieves guild to steal from players.
 *  12/13/04, Pigpen
 *		System message will now be played upon failure of item theft.
 *  10/16/04, Darva
 *		Added logic to update the players LastStoleAt value.
 *	7/31/04, mith
 *		OnTarget() final IF statement, added check to see if the item was successfully stolen before making thief perma-grey to victim.
 *  6/12/04, Old Salty
 * 		OrcishKinMasks now explode on stealing from orcs
 *	6/10/04, mith
 *		modified minimum amount stealable when stealing stacks of items
 *	4/12/04, changes by mith
 *		Set ClassicMode = true to re-enable perma-grey.
 */

using Server.Diagnostics;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Regions;
using Server.Targeting;
using System;

namespace Server.SkillHandlers
{
    public class Stealing
    {

        public static void Initialize()
        {
            SkillInfo.Table[33].Callback = new SkillUseCallback(OnUse);
        }

        public static readonly bool ClassicMode = true;
        public static readonly bool SuspendOnMurder = false;

        public static bool IsInGuild(Mobile m)
        {
            return (m is PlayerMobile && ((PlayerMobile)m).NpcGuild == NpcGuild.ThievesGuild);
        }

        public static bool IsInnocentTo(Mobile from, Mobile to)
        {
            return (Notoriety.Compute(from, (Mobile)to) == Notoriety.Innocent);
        }

        private class StealingTarget : Target
        {
            private Mobile m_Thief;

            public StealingTarget(Mobile thief)
                : base(1, false, TargetFlags.None)
            {
                m_Thief = thief;

                AllowNonlocal = true;
            }

            public bool CheckStealing(Corpse corpse)
            {
                if (!corpse.IsCriminalAction(m_Thief))
                    return true;

                try
                {
                    if (m_Thief.AccessLevel == AccessLevel.Player)
                    {
                        RegionControl regstone = null;
                        CustomRegion reg = null;
                        if (corpse.Deleted == false)
                            reg = CustomRegion.FindDRDTRegion(corpse);
                        if (reg != null)
                            regstone = reg.GetRegionControler();

                        // if this region does not allow looting, fail
                        if (regstone != null && regstone.BlockLooting == true)
                            return false;
                    }

                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }

                return true;
            }
            private Item TryStealItem(Item toSteal, ref bool caught)
            {
                //Zen make bankbox close when stealing!
                BankBox box = m_Thief.FindBankNoCreate();

                if (box != null && box.Opened)
                {
                    box.Close();
                    m_Thief.Send(new MobileUpdate(m_Thief));
                }

                Item stolen = null;

                object root = toSteal.RootParent;

                if (!IsEmptyHanded(m_Thief))
                {
                    m_Thief.SendLocalizedMessage(1005584); // Both hands must be free to steal.
                }
                // stealing off a corpse is looting
                else if (root is Corpse && CheckStealing(root as Corpse) == false)
                {
                    m_Thief.SendMessage("You can't steal that!"); // You can't steal that!
                }
                else if (root is Mobile
                    && ((Mobile)root).Player
                    /*&& IsInnocentTo( m_Thief, (Mobile)root )*/
                    && !IsInGuild(m_Thief))
                {
                    m_Thief.SendLocalizedMessage(1005596); // You must be in the thieves guild to steal from other players.
                }
                else if (SuspendOnMurder && root is Mobile && ((Mobile)root).Player && IsInGuild(m_Thief) && m_Thief.LongTermMurders > 0)
                {
                    m_Thief.SendLocalizedMessage(502706); // You are currently suspended from the thieves guild.
                }
                else if (root is BaseVendor && ((BaseVendor)root).IsInvulnerable)
                {
                    m_Thief.SendLocalizedMessage(1005598); // You can't steal from shopkeepers.
                }
                else if (root is PlayerVendor)
                {
                    m_Thief.SendLocalizedMessage(502709); // You can't steal from vendors.
                }
                else if (!m_Thief.CanSee(toSteal))
                {
                    m_Thief.SendLocalizedMessage(500237); // Target can not be seen.
                }
                else if (toSteal.GetItemBool(Item.ItemBoolTable.DeleteOnLift))
                {   // special case for DeleteOnLift items
                    m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                }
                else if (toSteal.Parent == null || !toSteal.Movable || toSteal.LootType == LootType.Newbied || toSteal.CheckBlessed(root))
                {
                    m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                }
                else if (toSteal.Parent is Stonebag && m_Thief.AccessLevel == AccessLevel.Player)
                {   // disallow stealing from someones stonebag
                    m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                }
                else if (!m_Thief.InRange(toSteal.GetWorldLocation(), 1))
                {
                    m_Thief.SendLocalizedMessage(502703); // You must be standing next to an item to steal it.
                }
                else if (toSteal.Parent is Mobile)
                {
                    m_Thief.SendLocalizedMessage(1005585); // You cannot steal items which are equiped.
                }
                else if (root == m_Thief)
                {
                    m_Thief.SendLocalizedMessage(502704); // You catch yourself red-handed.
                }
                else if (root is Mobile && ((Mobile)root).AccessLevel > AccessLevel.Player)
                {
                    m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                }
                else if (root is Mobile && !m_Thief.CanBeHarmful((Mobile)root))
                {
                    m_Thief.SendMessage("You can't steal from them.");
                }
                else
                {
                    double w = toSteal.Weight + toSteal.TotalWeight;

                    if (w > 10)
                    {
                        m_Thief.SendMessage("That is too heavy to steal.");
                    }
                    else
                    {
                        if (toSteal.Stackable && toSteal.Amount > 1)
                        {
                            int minAmount = (int)((m_Thief.Skills[SkillName.Stealing].Value / 25.0) / toSteal.Weight);
                            int maxAmount = (int)((m_Thief.Skills[SkillName.Stealing].Value / 10.0) / toSteal.Weight);

                            if (minAmount < 1)
                                minAmount = 1;

                            if (maxAmount < 1)
                                maxAmount = 1;
                            else if (maxAmount > toSteal.Amount)
                                maxAmount = toSteal.Amount;

                            int amount = Utility.RandomMinMax(minAmount, maxAmount);

                            if (amount >= toSteal.Amount)
                            {
                                int pileWeight = (int)Math.Ceiling(toSteal.Weight * toSteal.Amount);
                                pileWeight *= 10;

                                if (m_Thief.CheckTargetSkill(SkillName.Stealing, toSteal, pileWeight - 22.5, pileWeight + 27.5))
                                    stolen = toSteal;
                            }
                            else
                            {
                                int pileWeight = (int)Math.Ceiling(toSteal.Weight * amount);
                                pileWeight *= 10;

                                if (m_Thief.CheckTargetSkill(SkillName.Stealing, toSteal, pileWeight - 22.5, pileWeight + 27.5))
                                {
                                    stolen = toSteal.Dupe(amount);
                                    toSteal.Amount -= amount;
                                }
                            }
                        }
                        else
                        {
                            int iw = (int)Math.Ceiling(w);
                            iw *= 10;

                            if (m_Thief.CheckTargetSkill(SkillName.Stealing, toSteal, iw - 22.5, iw + 27.5))
                                stolen = toSteal;
                        }

                        if (stolen != null)
                            m_Thief.SendLocalizedMessage(502724); // You succesfully steal the item.
                        if (m_Thief.Player == true)
                        {
                            ((PlayerMobile)m_Thief).LastStoleAt = DateTime.UtcNow;
                        }
                        if (stolen == null) //change from else to if - Pigpen						
                            m_Thief.SendLocalizedMessage(502723); // You fail to steal the item.

                        caught = (m_Thief.Skills[SkillName.Stealing].Value < Utility.Random(150));

                        // Monster ignore	Velgo K'balc
                        // This ability makes even aggressive monsters ignore the evil player for a time, unless they were already engaged in combat with them. 
                        // Attacking or stealing from a monster will shatter the spell.
                        if ((m_Thief is PlayerMobile) && root is Mobile && m_Thief.Evil && (m_Thief as PlayerMobile).CheckState(Mobile.ExpirationFlagID.MonsterIgnore))
                            (m_Thief as PlayerMobile).RemoveState(Mobile.ExpirationFlagID.MonsterIgnore);
                    }
                }

                // wea: reset next skill time
                m_Thief.NextSkillTime = DateTime.UtcNow + TimeSpan.FromSeconds(10.0);

                // adam: reset the LootType.Special to LootType.Regular 
                if (stolen != null && stolen.LootType == LootType.Special)
                    stolen.LootType = LootType.Regular;

                return stolen;
            }

            protected override void OnTarget(Mobile from, object target)
            {
                from.RevealingAction();

                Item stolen = null;
                object root = null;
                bool caught = false;

                if (target is Item)
                {
                    root = ((Item)target).RootParent;
                    stolen = TryStealItem((Item)target, ref caught);
                }
                else if (target is Mobile)
                {
                    Container pack = ((Mobile)target).Backpack;

                    Item hat = from.FindItemOnLayer(Layer.Helm);      // Added by OldSalty 6/12/04 from here...
                    if (hat is OrcishKinMask && (target is Orc || target is OrcBomber || target is OrcBrute || target is OrcCaptain || target is OrcishLord || target is OrcishMage))
                    {
                        AOS.Damage(from, 50, 0, 100, 0, 0, 0, this);
                        hat.Delete();
                        from.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);
                        from.PlaySound(0x307);
                    }                                                   // . . . to here

                    if (pack != null && pack.Items.Count > 0)
                    {
                        int randomIndex = Utility.Random(pack.Items.Count);

                        root = target;
                        stolen = TryStealItem((Item)pack.Items[randomIndex], ref caught);
                    }
                }
                else
                {
                    m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                }

                if (stolen != null)
                    from.AddToBackpack(stolen);

                if (caught)
                {
                    if (root == null)
                    {
                        m_Thief.CriminalAction(false);
                    }
                    else if (root is Corpse && ((Corpse)root).IsCriminalAction(m_Thief))
                    {
                        m_Thief.CriminalAction(false);
                    }
                    else if (root is Mobile)
                    {
                        Mobile mobRoot = (Mobile)root;

                        if ( //!IsInGuild(mobRoot) && //Pix: we don't care if the target's also in the guild...
                             IsInnocentTo(m_Thief, mobRoot))
                        {
                            m_Thief.CriminalAction(false);
                        }

                        string message = String.Format("You notice {0} trying to steal from {1}.", m_Thief.Name, mobRoot.Name);

                        IPooledEnumerable eable = m_Thief.GetClientsInRange(8);
                        foreach (NetState ns in eable)
                        {
                            if (ns != m_Thief.NetState)
                                ns.Mobile.SendMessage(message);
                        }
                        eable.Free();
                    }
                }
                else if (root is Corpse && ((Corpse)root).IsCriminalAction(m_Thief))
                {
                    m_Thief.CriminalAction(false);
                }

                if (root is Mobile &&
                    ((Mobile)root).Player &&
                    m_Thief is PlayerMobile &&
                    IsInnocentTo(m_Thief, (Mobile)root) &&
                    //!IsInGuild( (Mobile)root ) && //Pix: we don't care if the target's also in the guild...
                    stolen != null)
                {
                    PlayerMobile pm = (PlayerMobile)m_Thief;

                    pm.PermaFlags.Add((Mobile)root);
                    pm.Delta(MobileDelta.Noto);
                }

                //PIX: 11/17/07 - WTF is this?  Why did I have this in my code?
                //if (stolen != null)
                //{
                //	if (root is Mobile)
                //	{
                //		((Mobile)root).AggressiveAction(m_Thief, false);
                //	}
                //}
            }
        }

        public static bool IsEmptyHanded(Mobile from)
        {
            if (from.FindItemOnLayer(Layer.OneHanded) != null)
                return false;

            if (from.FindItemOnLayer(Layer.TwoHanded) != null)
                return false;

            return true;
        }

        public static TimeSpan OnUse(Mobile m)
        {


            if (!IsEmptyHanded(m))
            {
                m.SendLocalizedMessage(1005584); // Both hands must be free to steal.
            }
            else
            {
                m.Target = new Stealing.StealingTarget(m);
                m.RevealingAction();

                m.SendLocalizedMessage(502698); // Which item do you want to steal?
            }

            //pla : changed this to 1 second by default. 
            //The 10 second delay is only after a steal attempt
            return TimeSpan.FromSeconds(1.0);
        }

        public static bool CheckReversePickpocket(Mobile from, Item item, Item target)
        {
            if (from == null || from.Deleted ||
                    item == null || !(item is BaseBook) || item.Deleted ||
                    target == null || target.Deleted)
                return false;

            if (!(from is PlayerMobile) || !(target is Container))
                return false;

            Container c = ((Container)target);
            PlayerMobile mark = null;

            if (!(c.Parent is PlayerMobile))
                return false;

            mark = ((PlayerMobile)c.Parent);

            if (mark.Deleted)
                return false;

            PlayerMobile thief = (PlayerMobile)from;

            //Must be close
            if (thief.GetDistanceToSqrt(mark) > 1) return false;

            //Need 100 steal and snoop to attempt
            if (thief.Skills.Stealing.Base < 100.0 || thief.Skills.Snooping.Base < 100.0)
                return false;

            //Check perma flags
            if (!thief.PermaFlags.Contains(mark))
            {
                thief.SendMessage("You many only attempt to plant books on marks who you are already criminal to.");
                return false;
            }

            //Weight..
            if (!c.CheckHold(mark, item, false))
            {
                thief.SendMessage("That person cannot hold the weight!");
                return false;
            }

            //All we need to do here is the criminal action, since they will already be perma
            if (IsInnocentTo(thief, mark))
            {
                thief.CriminalAction(false);
            }

            //Normal check, 75% chance to plant book successfully
            if (thief.CheckSkill(SkillName.Stealing, 0.75))
            {
                thief.SendMessage("You sucessfully slip them the book.");
                mark.SendMessage(string.Format("You have been slipped {0} !", ((BaseBook)item).Title));
                IPooledEnumerable eable = thief.GetClientsInRange(6);
                if (eable != null)
                {
                    foreach (NetState ns in eable)
                    {
                        if (ns != thief.NetState && ns != mark.NetState)
                            ns.Mobile.SendMessage("You notice {0} slipping {1} to {2}", thief.Name, ((BaseBook)item).Title, mark.Name);
                    }
                    eable.Free(); eable = null;
                }
                return true;
            }
            else
            {
                thief.SendMessage("You fail to slip them the book.");
                IPooledEnumerable eable = thief.GetClientsInRange(6);
                if (eable != null)
                {
                    foreach (NetState ns in eable)
                    {
                        if (ns != thief.NetState && ns != mark.NetState)
                            ns.Mobile.SendMessage("You notice {0} attempting to slip {1} a book", thief.Name, mark.Name);
                    }
                    eable.Free(); eable = null;
                }

            }
            return false;
        }
    }
}
