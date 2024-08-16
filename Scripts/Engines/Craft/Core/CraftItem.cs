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

/* Scripts/Engines/Craft/Core/CraftItem.cs
 * ChangeLog:
 *	3/16/16, Adam
 *		Do not show the old-school makers mark gump if core is UOAI
 *	04/06/09, plasma
 *		Removed ability to gain from mixing & lightening/darkening dyes completely
 *  01/04/07, Plasma
 *      Fixed special dye tub skill gain exploit      
 *  7/20/06, Rhiannon
 *		Fixed order of precedence bug when setting hitpoints of exceptional and low quality clothing.
 *  7/18/06, Rhiannon
 *		Added default hue of 1001 for undyed cloth gloves.
 *	11/10/05, erlein
 *		Removed assignment of SaveHue property (made obsolete by PlayerCrafted).
 *	10/17/05, erlein
 *		Fixed darkening/lightening and targetting bugs.
 *	10/16/05, erlein
 *		Altered use of special dye tub's "Prepped" property to ascertain whether more dye can be added.
 *		Changed consumption of resources so darken/lighten require 2 per 1-5, 4 per 6-10, 5 per 11-15 etc.
 *	10/15/05, erlein
 *		Added amount limit for special dye tub (on Uses property).
 *		Altered resource consumption to occur after craft in order to control it better.
 *		Created SpecialDyeTubTarget, instanced whenever special dye is crafted and
 *		mutltiple dye tubs exist.
 *	10/15/05, erlein
 *		Re-worked special dye handling to accommodate new dye tub based craft model.
 *	10/15/05, erlein
 *		Added conditions to handle the craft of special dyes.
 *	9/7/05, Adam
 *		In order to keep players from farming new characters for newbie clothes
 *		we are moving this valuable resource into the hands of crafters.
 *		Exceptionally crafted clothes are now newbied. They do however wear out.
 *	02/10/05, erlein
 *		Added initial hits assignment for BaseClothing types.
 *  09/12/05 TK
 *		Added Bolas to Markable Types array, so that they carry crafter's mark.
 *  08/18/05 TK
 *		Made bolas carry over quality.
 *	08/18/05, erlein
 *		Added jewellery to markable types array.
 *		Added extra commands to pass crafter and quality down to BaseJewel.
 *	08/01/05, erlein
 *		Added runebooks and instruments to markable types array.
 *		Added extra commands to pass crafter and quality down to runebook.
 *	07/30/05, erlein
 *		Removed the filling of bookcases (so books are just consumed) and added checks for other
 *		fullbookcase types (randomized when craft lists are established ;)
 *	07/27/05, erlein
 *		Added special messages, filling for bookcases and condition for scribes pen during carpentry craft.
 *	02/25/05, Adam
 *		Make the item and mark it as PlayerCrafted
 *		See:	Item item = Activator.CreateInstance( ItemType ) as Item;
 *				item.PlayerCrafted = true;
 *	1/8/05, mith
 *		CompleteCraft(): Modified Failure consumption to not use all of a resource on failure (i.e. cooking fish and failing doesn't lose all raw fish)
 *		CompleteCraft(): put call to PlayEndingEffect() so that alchemy doesn't consume bottles on failures
 *	1/4/05 smerX
 *		Fixxed issue with crafting exceptional items.
 *	12/30/04, smerX
 *		Players now lose 1/2 to 1/3 of the required resources when failing a craft skill
 * 	10/30/04, Darva
 *		Changed results of player created lockedcontainers to have their
 *		difficulty in line with treasure chests.
 *	9/1/04, Pixie
 *		Now when a tinker traps a container, we make sure that the container
 *		has the TinkerTrapable attribute (vs only checking that it is a TrapableContainer)
 *	8/12/04, mith
 *		ConsumeType(): Added option for ConsumeType.One, used when crafts that set UseAllRes fail (rather than consuming half of the stack of whatever is being crafter)
 *		CompleteCraft(): Added functionality so that items that should only consume one reasource on failure do.
 *		CheckSkills(): Fixed a bug where an exceptional item would be crafted, but the skill check to actually create the item could fail.
 *		CompleteCraft(): Removed a second CheckSkills call that was failing if players had PromptForMark set, so that the item could fail to be created after they were prompted.
 *	6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/22/2004, pixie
 *		Changed so tinkers couldn't trap an already trapped container.
 *	5/18/2004
 *		Added handling for the crafting of tinker traps
 */

using Server.Factions;
using Server.Items;
using Server.Targeting;
using System;
using System.Collections.Generic;

namespace Server.Engines.Craft
{
    public enum ConsumeType
    {
        All, Half, Fail, One, None
    }

    public interface ICraftable
    {
        int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue);
    }

    public class CraftItem
    {
        private CraftResCol m_arCraftRes;
        private CraftSkillCol m_arCraftSkill;
        private Type m_Type;

        private string m_GroupNameString;
        private int m_GroupNameNumber;

        private string m_NameString;
        private int m_NameNumber;

        private int m_Mana;
        private int m_Hits;
        private int m_Stam;

        private bool m_UseAllRes;

        private bool m_NeedHeat;
        private bool m_NeedOven;
        private bool m_NeedWater;   // erl: added for special dyes

        private bool m_UseSubRes2;

        private bool m_ForceNonExceptional;

        public bool ForceNonExceptional
        {
            get { return m_ForceNonExceptional; }
            set { m_ForceNonExceptional = value; }
        }

        private static Dictionary<Type, int> _itemIds = new Dictionary<Type, int>();

        public static int ItemIDOf(Type type)
        {
            int itemId;

            if (!_itemIds.TryGetValue(type, out itemId))
            {
                if (type == typeof(FactionExplosionTrap))
                {
                    itemId = 14034;
                }
                else if (type == typeof(FactionGasTrap))
                {
                    itemId = 4523;
                }
                else if (type == typeof(FactionSawTrap))
                {
                    itemId = 4359;
                }
                else if (type == typeof(FactionSpikeTrap))
                {
                    itemId = 4517;
                }

                if (itemId == 0)
                {
                    object[] attrs = type.GetCustomAttributes(typeof(CraftItemIDAttribute), false);

                    if (attrs.Length > 0)
                    {
                        CraftItemIDAttribute craftItemID = (CraftItemIDAttribute)attrs[0];
                        itemId = craftItemID.ItemID;
                    }
                }

                if (itemId == 0)
                {
                    Item item = null;

                    try { item = Activator.CreateInstance(type) as Item; }
                    catch { }

                    if (item != null)
                    {
                        itemId = item.ItemID;
                        item.Delete();
                    }
                }

                _itemIds[type] = itemId;
            }

            return itemId;
        }

        public CraftItem(Type type, string groupName, string name)
        {
            m_arCraftRes = new CraftResCol();
            m_arCraftSkill = new CraftSkillCol();

            m_Type = type;
            m_GroupNameString = groupName;
            m_NameString = name;
        }

        public CraftItem(Type type, int groupName, int name)
        {
            m_arCraftRes = new CraftResCol();
            m_arCraftSkill = new CraftSkillCol();

            m_Type = type;
            m_GroupNameNumber = groupName;
            m_NameNumber = name;
        }

        public CraftItem(Type type, int groupName, string name)
        {
            m_arCraftRes = new CraftResCol();
            m_arCraftSkill = new CraftSkillCol();

            m_Type = type;
            m_GroupNameNumber = groupName;
            m_NameString = name;
        }

        public void AddRes(Type type, int name, int amount)
        {
            AddRes(type, name, amount, "");
        }

        public void AddRes(Type type, int name, int amount, int localizedMessage)
        {
            CraftRes craftRes = new CraftRes(type, name, amount, localizedMessage);
            m_arCraftRes.Add(craftRes);
        }

        public void AddRes(Type type, int name, int amount, string strMessage)
        {
            CraftRes craftRes = new CraftRes(type, name, amount, strMessage);
            m_arCraftRes.Add(craftRes);
        }

        public void AddRes(Type type, string name, int amount)
        {
            AddRes(type, name, amount, "");
        }

        public void AddRes(Type type, string name, int amount, int localizedMessage)
        {
            CraftRes craftRes = new CraftRes(type, name, amount, localizedMessage);
            m_arCraftRes.Add(craftRes);
        }

        public void AddRes(Type type, string name, int amount, string strMessage)
        {
            CraftRes craftRes = new CraftRes(type, name, amount, strMessage);
            m_arCraftRes.Add(craftRes);
        }

        public void AddSkill(SkillName skillToMake, double minSkill, double maxSkill)
        {
            CraftSkill craftSkill = new CraftSkill(skillToMake, minSkill, maxSkill);
            m_arCraftSkill.Add(craftSkill);
        }

        public int Mana
        {
            get { return m_Mana; }
            set { m_Mana = value; }
        }

        public int Hits
        {
            get { return m_Hits; }
            set { m_Hits = value; }
        }

        public int Stam
        {
            get { return m_Stam; }
            set { m_Stam = value; }
        }

        public bool UseSubRes2
        {
            get { return m_UseSubRes2; }
            set { m_UseSubRes2 = value; }
        }

        public bool UseAllRes
        {
            get { return m_UseAllRes; }
            set { m_UseAllRes = value; }
        }

        public bool NeedHeat
        {
            get { return m_NeedHeat; }
            set { m_NeedHeat = value; }
        }

        public bool NeedOven
        {
            get { return m_NeedOven; }
            set { m_NeedOven = value; }
        }

        public bool NeedWater
        {
            get { return m_NeedWater; }
            set { m_NeedWater = value; }
        }

        public Type ItemType
        {
            get { return m_Type; }
        }

        public string GroupNameString
        {
            get { return m_GroupNameString; }
        }

        public int GroupNameNumber
        {
            get { return m_GroupNameNumber; }
        }

        public string NameString
        {
            get { return m_NameString; }
        }

        public int NameNumber
        {
            get { return m_NameNumber; }
        }

        public CraftResCol Ressources
        {
            get { return m_arCraftRes; }
        }

        public CraftSkillCol Skills
        {
            get { return m_arCraftSkill; }
        }

        public bool ConsumeAttributes(Mobile from, ref object message, bool consume)
        {
            bool consumMana = false;
            bool consumHits = false;
            bool consumStam = false;

            if (Hits > 0 && from.Hits < Hits)
            {
                message = "You lack the required hit points to make that.";
                return false;
            }
            else
            {
                consumHits = consume;
            }

            if (Mana > 0 && from.Mana < Mana)
            {
                message = "You lack the required mana to make that.";
                return false;
            }
            else
            {
                consumMana = consume;
            }

            if (Stam > 0 && from.Stam < Stam)
            {
                message = "You lack the required stamina to make that.";
                return false;
            }
            else
            {
                consumStam = consume;
            }

            if (consumMana)
                from.Mana -= Mana;

            if (consumHits)
                from.Hits -= Hits;

            if (consumStam)
                from.Stam -= Stam;

            return true;
        }

        #region TABLES
        private static int[] m_HeatSources = new int[]
            {
                0x461, 0x48E, // Sandstone oven/fireplace
				0x92B, 0x96C, // Stone oven/fireplace
				0xDE3, 0xDE9, // Campfire
				0xFAC, 0xFAC, // Firepit
				0x184A, 0x184C, // Heating stand (left)
				0x184E, 0x1850, // Heating stand (right)
				0x398C, 0x399F  // Fire field
			};

        private static int[] m_Ovens = new int[]
            {
                0x461, 0x46F, // Sandstone oven
				0x92B, 0x93F  // Stone oven
			};

        private static int[] m_WaterTroughs = new int[]
            {
                0xB41, 0xB42, // Watertrough east
				0xB43, 0xB44  // Watertrough south
			};

        private static Type[] m_MarkableTable = new Type[]
                {
                    typeof( BaseArmor ),
                    typeof( BaseWeapon ),
                    typeof( BaseClothing ),
                    typeof( BaseInstrument ),
                    typeof( DragonBardingDeed ),
                    typeof( BaseTool ),
                    typeof( BaseHarvestTool ),
					/*typeof( FukiyaDarts ), typeof( Shuriken ),*/
					typeof( Spellbook ), typeof( Runebook )
                };

        private static Type[][] m_TypesTable = new Type[][]
            {
                new Type[]{ typeof( Log ), typeof( Board ) },
                new Type[]{ typeof( Leather ), typeof( Hides ) },
                new Type[]{ typeof( SpinedLeather ), typeof( SpinedHides ) },
                new Type[]{ typeof( HornedLeather ), typeof( HornedHides ) },
                new Type[]{ typeof( BarbedLeather ), typeof( BarbedHides ) },
                new Type[]{ typeof( BlankMap ), typeof( BlankScroll ) },
                new Type[]{ typeof( Cloth ), typeof( UncutCloth ) },
                new Type[]{ typeof( CheeseWheel ), typeof( CheeseWedge ) }
            };

        private static Type[] m_ColoredItemTable = new Type[]
            {
                typeof( BaseWeapon ), typeof( BaseArmor ), typeof( BaseClothing ),
                typeof( BaseJewel ), typeof( DragonBardingDeed )
            };

        private static Type[] m_ColoredResourceTable = new Type[]
            {
                typeof( BaseIngot ), typeof( BaseOre ),
                typeof( BaseLeather ), typeof( BaseHides ),
                typeof( UncutCloth ), typeof( Cloth ),
                typeof( BaseGranite ), typeof( BaseScales )
            };
        #endregion

        public bool IsMarkable(Type type)
        {
            if (m_ForceNonExceptional)  //Don't even display the stuff for marking if it can't ever be exceptional.
                return false;

            for (int i = 0; i < m_MarkableTable.Length; ++i)
            {
                if (type == m_MarkableTable[i] || type.IsSubclassOf(m_MarkableTable[i]))
                    return true;
            }

            return false;
        }

        public bool RetainsColorFrom(CraftSystem system, Type type)
        {
            if (system.RetainsColorFrom(this, type))
                return true;

            bool inItemTable = false, inResourceTable = false;

            for (int i = 0; !inItemTable && i < m_ColoredItemTable.Length; ++i)
                inItemTable = (m_Type == m_ColoredItemTable[i] || m_Type.IsSubclassOf(m_ColoredItemTable[i]));

            for (int i = 0; inItemTable && !inResourceTable && i < m_ColoredResourceTable.Length; ++i)
                inResourceTable = (type == m_ColoredResourceTable[i] || type.IsSubclassOf(m_ColoredResourceTable[i]));

            return (inItemTable && inResourceTable);
        }

        public bool Find(Mobile from, int[] itemIDs)
        {
            Map map = from.Map;

            if (map == null)
                return false;

            IPooledEnumerable eable = map.GetItemsInRange(from.Location, 2);

            foreach (Item item in eable)
            {
                if ((item.Z + 16) > from.Z && (from.Z + 16) > item.Z && Find(item.ItemID, itemIDs))
                {
                    eable.Free();
                    return true;
                }
            }

            eable.Free();

            for (int x = -2; x <= 2; ++x)
            {
                for (int y = -2; y <= 2; ++y)
                {
                    int vx = from.X + x;
                    int vy = from.Y + y;

                    Tile[] tiles = map.Tiles.GetStaticTiles(vx, vy, true);

                    for (int i = 0; i < tiles.Length; ++i)
                    {
                        int z = tiles[i].Z;
                        int id = tiles[i].ID & 0x3FFF;

                        if ((z + 16) > from.Z && (from.Z + 16) > z && Find(id, itemIDs))
                            return true;
                    }
                }
            }

            return false;
        }

        public bool Find(int itemID, int[] itemIDs)
        {
            bool contains = false;

            for (int i = 0; !contains && i < itemIDs.Length; i += 2)
                contains = (itemID >= itemIDs[i] && itemID <= itemIDs[i + 1]);

            return contains;
        }

        public bool IsQuantityType(Type[][] types)
        {
            for (int i = 0; i < types.Length; ++i)
            {
                Type[] check = types[i];

                for (int j = 0; j < check.Length; ++j)
                {
                    if (typeof(IHasQuantity).IsAssignableFrom(check[j]))
                        return true;
                }
            }

            return false;
        }

        public int ConsumeQuantity(Container cont, Type[][] types, int[] amounts)
        {
            if (types.Length != amounts.Length)
                throw new ArgumentException();

            Item[][] items = new Item[types.Length][];
            int[] totals = new int[types.Length];

            for (int i = 0; i < types.Length; ++i)
            {
                items[i] = cont.FindItemsByType(types[i], true);

                for (int j = 0; j < items[i].Length; ++j)
                {
                    IHasQuantity hq = items[i][j] as IHasQuantity;

                    if (hq == null)
                    {
                        totals[i] += items[i][j].Amount;
                    }
                    else
                    {
                        if (hq is BaseBeverage && ((BaseBeverage)hq).Content != BeverageType.Water)
                            continue;

                        totals[i] += hq.Quantity;
                    }
                }

                if (totals[i] < amounts[i])
                    return i;
            }

            for (int i = 0; i < types.Length; ++i)
            {
                int need = amounts[i];

                for (int j = 0; j < items[i].Length; ++j)
                {
                    Item item = items[i][j];
                    IHasQuantity hq = item as IHasQuantity;

                    if (hq == null)
                    {
                        int theirAmount = item.Amount;

                        if (theirAmount < need)
                        {
                            need -= theirAmount;
                            item.Delete();
                        }
                        else
                        {
                            item.Consume(need);
                            break;
                        }
                    }
                    else
                    {
                        if (hq is BaseBeverage && ((BaseBeverage)hq).Content != BeverageType.Water)
                            continue;

                        int theirAmount = hq.Quantity;

                        if (theirAmount < need)
                        {
                            hq.Quantity -= theirAmount;
                            need -= theirAmount;
                        }
                        else
                        {
                            hq.Quantity -= need;
                            break;
                        }
                    }
                }
            }

            return -1;
        }

        public int GetQuantity(Container cont, Type[] types)
        {
            Item[] items = cont.FindItemsByType(types, true);

            int amount = 0;

            for (int i = 0; i < items.Length; ++i)
            {
                IHasQuantity hq = items[i] as IHasQuantity;

                if (hq == null)
                {
                    amount += items[i].Amount;
                }
                else
                {

                    if (hq is BaseBeverage && ((BaseBeverage)hq).Content != BeverageType.Water)
                        continue;

                    amount += hq.Quantity;
                }
            }

            return amount;
        }

        public bool ConsumeRes(Mobile from, Type typeRes, CraftSystem craftSystem, ref int resHue, ref int maxAmount, ConsumeType consumeType, ref object message)
        {
            return ConsumeRes(from, typeRes, craftSystem, ref resHue, ref maxAmount, consumeType, ref message, false);
        }

        public bool ConsumeRes(Mobile from, Type typeRes, CraftSystem craftSystem, ref int resHue, ref int maxAmount, ConsumeType consumeType, ref object message, bool isFailure)
        {
            Container ourPack = from.Backpack;

            if (ourPack == null)
                return false;

            if (m_NeedHeat && !Find(from, m_HeatSources))
            {
                message = 1044487; // You must be near a fire source to cook.
                return false;
            }

            if (m_NeedOven && !Find(from, m_Ovens))
            {
                message = 1044493; // You must be near an oven to bake that.
                return false;
            }

            if (m_NeedWater && !Find(from, m_WaterTroughs))
            {
                from.SendMessage("You must be near a water trough to mix dyes.");
                return false;
            }

            Type[][] types = new Type[m_arCraftRes.Count][];
            int[] amounts = new int[m_arCraftRes.Count];

            maxAmount = int.MaxValue;

            CraftSubResCol resCol = (m_UseSubRes2 ? craftSystem.CraftSubRes2 : craftSystem.CraftSubRes);

            for (int i = 0; i < types.Length; ++i)
            {
                CraftRes craftRes = m_arCraftRes.GetAt(i);
                Type baseType = craftRes.ItemType;

                // Resource Mutation
                if ((baseType == resCol.ResType) && (typeRes != null))
                {
                    baseType = typeRes;

                    CraftSubRes subResource = resCol.SearchFor(baseType);

                    if (subResource != null && from.Skills[craftSystem.MainSkill].Base < subResource.RequiredSkill)
                    {
                        message = subResource.Message;
                        return false;
                    }
                }
                // ******************

                for (int j = 0; types[i] == null && j < m_TypesTable.Length; ++j)
                {
                    if (m_TypesTable[j][0] == baseType)
                        types[i] = m_TypesTable[j];
                }

                if (types[i] == null)
                    types[i] = new Type[] { baseType };

                amounts[i] = craftRes.Amount;

                // For stackable items that can be crafted more than one at a time
                if (UseAllRes)
                {
                    int tempAmount = ourPack.GetAmount(types[i]);
                    tempAmount /= amounts[i];
                    if (tempAmount < maxAmount)
                    {
                        maxAmount = tempAmount;

                        if (maxAmount == 0)
                        {
                            CraftRes res = m_arCraftRes.GetAt(i);

                            if (res.MessageNumber > 0)
                                message = res.MessageNumber;
                            else if (!String.IsNullOrEmpty(res.MessageString))
                                message = res.NameString;
                            else
                                message = 502925; // You don't have the resources required to make that item.

                            return false;
                        }
                    }
                }
                // ****************************

                if (isFailure && !craftSystem.ConsumeResOnFailure(from, types[i][0], this))
                    amounts[i] = 0;
            }

            // We adjust the amount of each resource to consume the max posible
            if (UseAllRes)
            {
                for (int i = 0; i < amounts.Length; ++i)
                    amounts[i] *= maxAmount;
            }
            else
                maxAmount = -1;

            Item consumeExtra = null;

            if (m_NameNumber == 1041267)
            {
                // Runebooks are a special case, they need a blank recall rune

                Item[] runes = ourPack.FindItemsByType(typeof(RecallRune));

                for (int i = 0; i < runes.Length; ++i)
                {
                    RecallRune rune = runes[i] as RecallRune;

                    if (rune != null && !rune.Marked)
                    {
                        consumeExtra = rune;
                        break;
                    }
                }

                if (consumeExtra == null)
                {
                    message = 1044253; // You don't have the components needed to make that.
                    return false;
                }
            }

            int index = 0;

            // Consume ALL
            if (consumeType == ConsumeType.All)
            {
                m_ResHue = 0; m_ResAmount = 0; m_System = craftSystem;

                if (IsQuantityType(types))
                    index = ConsumeQuantity(ourPack, types, amounts);
                else
                    index = ourPack.ConsumeTotalGrouped(types, amounts, true, new OnItemConsumed(OnResourceConsumed), new CheckItemGroup(CheckHueGrouping));

                resHue = m_ResHue;
            }

            // Consume Half ( for use all resource craft type )
            else if (consumeType == ConsumeType.Half)
            {
                for (int i = 0; i < amounts.Length; i++)
                {
                    amounts[i] /= 2;

                    if (amounts[i] < 1)
                        amounts[i] = 1;
                }

                m_ResHue = 0; m_ResAmount = 0; m_System = craftSystem;

                if (IsQuantityType(types))
                    index = ConsumeQuantity(ourPack, types, amounts);
                else
                    index = ourPack.ConsumeTotalGrouped(types, amounts, true, new OnItemConsumed(OnResourceConsumed), new CheckItemGroup(CheckHueGrouping));

                resHue = m_ResHue;
            }
            // Consume 1/2 to 1/3 of required - skill check failed
            else if (consumeType == ConsumeType.Fail)
            {
                m_ResHue = 0; m_ResAmount = 0; m_System = craftSystem;

                for (int i = 0; i < amounts.Length; i++)
                {
                    amounts[i] /= Utility.Random(2, 3);

                    if (amounts[i] < 1)
                        amounts[i] = 1;
                }


                if (IsQuantityType(types))
                    index = ConsumeQuantity(ourPack, types, amounts);
                else
                    index = ourPack.ConsumeTotalGrouped(types, amounts, true, new OnItemConsumed(OnResourceConsumed), new CheckItemGroup(CheckHueGrouping));

                resHue = m_ResHue;

            }
            else if (consumeType == ConsumeType.One)
            {
                for (int i = 0; i < amounts.Length; i++)
                    amounts[i] = 1;

                m_ResHue = 0; m_ResAmount = 0; m_System = craftSystem;

                if (IsQuantityType(types))
                    index = ConsumeQuantity(ourPack, types, amounts);
                else
                    index = ourPack.ConsumeTotalGrouped(types, amounts, true, new OnItemConsumed(OnResourceConsumed), new CheckItemGroup(CheckHueGrouping));

                resHue = m_ResHue;
            }


            else // ConstumeType.None ( it's basicaly used to know if the crafter has enough resource before starting the process )
            {
                index = -1;

                if (IsQuantityType(types))
                {
                    for (int i = 0; i < types.Length; i++)
                    {
                        if (GetQuantity(ourPack, types[i]) < amounts[i])
                        {
                            index = i;
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < types.Length; i++)
                    {
                        if (ourPack.GetBestGroupAmount(types[i], true, new CheckItemGroup(CheckHueGrouping)) < amounts[i])
                        {
                            index = i;
                            break;
                        }
                    }
                }
            }

            if (index == -1)
            {
                if (consumeType != ConsumeType.None)
                    if (consumeExtra != null)
                        consumeExtra.Delete();

                return true;
            }
            else
            {
                CraftRes res = m_arCraftRes.GetAt(index);

                if (res.MessageNumber > 0)
                    message = res.MessageNumber;
                else if (res.MessageString != null && res.MessageString != String.Empty)
                    message = res.NameString;
                else
                    message = 502925; // You don't have the resources required to make that item.

                return false;
            }
        }

        private int m_ResHue;
        private int m_ResAmount;
        private CraftSystem m_System;

        private void OnResourceConsumed(Item item, int amount)
        {
            if (!RetainsColorFrom(m_System, item.GetType()))
                return;

            if (amount >= m_ResAmount)
            {
                m_ResHue = item.Hue;
                m_ResAmount = amount;
            }
        }

        private int CheckHueGrouping(Item a, Item b)
        {
            return b.Hue.CompareTo(a.Hue);
        }

        public double GetExceptionalChance(CraftSystem system, double chance, Mobile from)
        {
            if (m_ForceNonExceptional)
                return 0.0;

            switch (system.ECA)
            {
                default:
                case CraftECA.ChanceMinusSixty: return chance - 0.6;
                case CraftECA.FiftyPercentChanceMinusTenPercent: return (chance * 0.5) - 0.1;
                case CraftECA.ChanceMinusSixtyToFourtyFive:
                    {
                        double offset = 0.60 - ((from.Skills[system.MainSkill].Value - 95.0) * 0.03);

                        if (offset < 0.45)
                            offset = 0.45;
                        else if (offset > 0.60)
                            offset = 0.60;

                        return chance - offset;
                    }
            }
        }

        public bool CheckSkills(Mobile from, Type typeRes, CraftSystem craftSystem, ref int quality, ref bool allRequiredSkills)
        {
            return CheckSkills(from, typeRes, craftSystem, ref quality, ref allRequiredSkills, true);
        }

        public bool CheckSkills(Mobile from, Type typeRes, CraftSystem craftSystem, ref int quality, ref bool allRequiredSkills, bool gainSkills)
        {
            bool success = true;
            double rand = Utility.RandomDouble(); // *

            double chance = GetSuccessChance(from, typeRes, craftSystem, gainSkills, ref allRequiredSkills);

            if (GetExceptionalChance(craftSystem, chance, from) >= rand) // ** exceptional
                quality = 2;
            else if (chance >= rand) // ** average
                quality = 1;
            else // failure
                success = false;

            return success;
        }

        public double GetSuccessChance(Mobile from, Type typeRes, CraftSystem craftSystem, bool gainSkills, ref bool allRequiredSkills)
        {
            double minMainSkill = 0.0;
            double maxMainSkill = 0.0;
            double valMainSkill = 0.0;

            allRequiredSkills = true;

            for (int i = 0; i < m_arCraftSkill.Count; i++)
            {
                CraftSkill craftSkill = m_arCraftSkill.GetAt(i);

                double minSkill = craftSkill.MinSkill;
                double maxSkill = craftSkill.MaxSkill;
                double valSkill = from.Skills[craftSkill.SkillToMake].Value;

                if (valSkill < minSkill)
                    allRequiredSkills = false;

                if (craftSkill.SkillToMake == craftSystem.MainSkill)
                {
                    minMainSkill = minSkill;
                    maxMainSkill = maxSkill;
                    valMainSkill = valSkill;
                }

                if (gainSkills) // This is a passive check. Success chance is entirely dependant on the main skill
                    from.CheckSkill(craftSkill.SkillToMake, minSkill, maxSkill);
            }

            double chance;

            if (allRequiredSkills)
                chance = craftSystem.GetChanceAtMin(this) + ((valMainSkill - minMainSkill) / (maxMainSkill - minMainSkill) * (1.0 - craftSystem.GetChanceAtMin(this)));
            else
                chance = 0.0;

            if (allRequiredSkills && valMainSkill == maxMainSkill)
                chance = 1.0;

            return chance;
        }

        public void Craft(Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool)
        {
            if (from.BeginAction(typeof(CraftSystem)))
            {
                bool allRequiredSkills = true;
                double chance = GetSuccessChance(from, typeRes, craftSystem, false, ref allRequiredSkills);

                if (allRequiredSkills && chance >= 0.0)
                {
                    int badCraft = craftSystem.CanCraft(from, tool, m_Type);

                    if (badCraft <= 0)
                    {
                        int resHue = 0;
                        int maxAmount = 0;
                        object message = null;

                        // just check and see if they have thge resources
                        if (ConsumeRes(from, typeRes, craftSystem, ref resHue, ref maxAmount, ConsumeType.None, ref message))
                        {
                            message = null;

                            // just check and see if they have the attributes
                            if (ConsumeAttributes(from, ref message, false))
                            {
                                CraftContext context = craftSystem.GetContext(from);

                                if (context != null)
                                    context.OnMade(this);

                                int iMin = craftSystem.MinCraftEffect;
                                int iMax = (craftSystem.MaxCraftEffect - iMin) + 1;
                                int iRandom = Utility.Random(iMax);
                                iRandom += iMin + 1;
                                new InternalTimer(from, craftSystem, this, typeRes, tool, iRandom).Start();
                            }
                            else
                            {
                                from.EndAction(typeof(CraftSystem));
                                if (!craftSystem.OldSchool)
                                    from.SendGump(new CraftGump(from, craftSystem, tool, message));
                                else
                                    CraftNotice(from, message);
                            }
                        }
                        else
                        {
                            from.EndAction(typeof(CraftSystem));
                            if (!craftSystem.OldSchool)
                                from.SendGump(new CraftGump(from, craftSystem, tool, message));
                            else
                                CraftNotice(from, message);
                        }
                    }
                    else
                    {
                        from.EndAction(typeof(CraftSystem));
                        if (!craftSystem.OldSchool)
                            from.SendGump(new CraftGump(from, craftSystem, tool, badCraft));
                        else
                            CraftNotice(from, badCraft);
                    }
                }
                else
                {
                    from.EndAction(typeof(CraftSystem));
                    if (!craftSystem.OldSchool)
                        from.SendGump(new CraftGump(from, craftSystem, tool, 1044153)); // You don't have the required skills to attempt this item.
                    else
                        CraftNotice(from, 1044153);
                }
            }
            else
            {
                from.SendLocalizedMessage(500119); // You must wait to perform another action
            }
        }

        public void CompleteCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CustomCraft customCraft)
        {
            int badCraft = craftSystem.CanCraft(from, tool, m_Type);

            if (badCraft > 0)
            {
                if (tool != null && !tool.Deleted && tool.UsesRemaining > 0)
                {
                    if (!craftSystem.OldSchool)
                        from.SendGump(new CraftGump(from, craftSystem, tool, badCraft));
                    else
                        CraftNotice(from, badCraft);
                }
                else
                    from.SendLocalizedMessage(badCraft);

                return;
            }

            int checkResHue = 0, checkMaxAmount = 0;
            object checkMessage = null;

            // Do we have enough resource to craft it
            if (!ConsumeRes(from, typeRes, craftSystem, ref checkResHue, ref checkMaxAmount, ConsumeType.None, ref checkMessage, true))
            {
                if (tool != null && !tool.Deleted && tool.UsesRemaining > 0)
                {
                    if (!craftSystem.OldSchool)
                        from.SendGump(new CraftGump(from, craftSystem, tool, checkMessage));
                    else
                        CraftNotice(from, checkMessage);
                }
                else if (checkMessage is int && (int)checkMessage > 0)
                    from.SendLocalizedMessage((int)checkMessage);
                else if (checkMessage is string)
                    from.SendMessage((string)checkMessage);

                return;
            }
            // Do we have enough attributes to craft it
            else if (!ConsumeAttributes(from, ref checkMessage, false))
            {
                if (tool != null && !tool.Deleted && tool.UsesRemaining > 0)
                {
                    if (!craftSystem.OldSchool)
                        from.SendGump(new CraftGump(from, craftSystem, tool, checkMessage));
                    else
                        CraftNotice(from, checkMessage);
                }
                else if (checkMessage is int && (int)checkMessage > 0)
                    from.SendLocalizedMessage((int)checkMessage);
                else if (checkMessage is string)
                    from.SendMessage((string)checkMessage);

                return;
            }

            bool toolBroken = false;
            int ignored = 1;
            int endquality = 1;
            bool allRequiredSkills = true;
#if old

            #region SpecialDyeTub HACK - needs cleanup
			// pla, 01/04/07
			// -----------------------------------------------------------------
			// Additional checks required here to prevent skill gain exploit with special dye tubs.               
			if (craftSystem is DefAlchemy && (ItemType == typeof(SpecialDyeTub) || ItemType == typeof(SpecialDye)))
			{
				// note here that if lighten or darken was chosen, there will always be at least one special tub,
				// as it's set as a resource requirement.
				if (m_NameString == "> Darken the mix" || m_NameString == "> Lighten the mix")
				{
					// darken/lighten requires a special tub
					// Get list of all special tubs
					Item[] sdtubs = ((Container)from.Backpack).FindItemsByType(typeof(SpecialDyeTub), true);
					SpecialDyeTub sdtub;

					if (sdtubs.Length == 0)
					{
						//should be impossible as in reqs
						return;
					}
					else if (sdtubs.Length == 1)
					{
						// in this case we have just one tub.  This means we will leave skill gain and execution to the 
						// standard craft code below.  However, we need to first check if the tub can be lightened/darkened,
						// and if not then return to prevent skill gain with no resource use.
						sdtub = (SpecialDyeTub)sdtubs[0];
						if (sdtub != null)
						{
							if (m_NameString == "> Darken the mix")
							{
								if (!sdtub.CanDarken)
								{
									from.SendMessage("You attempt to darken the mix, but it will go no darker.");
									from.SendGump(new CraftGump(from, craftSystem, tool, 0));
									return;
								}
							}
							else
							{
								if (!sdtub.CanLighten)
								{
									from.SendMessage("You attempt to lighten the mix, but it will go no lighter.");
									from.SendGump(new CraftGump(from, craftSystem, tool, 0));
									return;
								}
							}
						}

					}
					else if (sdtubs.Length > 1)
					{
						// in this case we have more than one possible tub to select so we hand execution over to the target.
						//target also deals with all skill gain and failure/tool use etc.
						int resHue = 0;
						int maxAmount = 0;
						object message = null;
						from.SendMessage("Select the dye tub you wish to use for the process.");
						from.Target = new SpecialDyeTubTarget(this, typeRes, craftSystem, ref resHue, ref maxAmount, ref message, m_NameString, (m_NameString == "> Darken the mix" ? 1 : 2), tool);
						//and now return to prevent the rest of the code executing
						return;
					}
				}
				else
				{
					// Create/append dye.  This can either add to a same coloured special tub, or a fresh non-special tub.
					// So first we find all the valid tubs...
					Item[] dtubs = ((Container)from.Backpack).FindItemsByType(typeof(DyeTub), true);

					int iFound = 0;
					if (dtubs.Length > 0)
					{
						for (int i = 0; i < dtubs.Length; i++)
						{
							if (dtubs[i] is SpecialDyeTub)
							{
								// Is the same color?
								if (((SpecialDyeTub)dtubs[i]).StoredColorName == m_NameString && ((SpecialDyeTub)dtubs[i]).Uses < 100 && !((SpecialDyeTub)dtubs[i]).Prepped)
								{
									iFound++;
								}
							}
							else
							{
								iFound++;
							}
						}
					}

					if (iFound == 0)
					{
						from.SendMessage("You need a fresh dye tub or one that is not yet full and of the same color as the dye you are preparing.");
						from.SendGump(new CraftGump(from, craftSystem, tool, 0));
						return;
					}
					else if (iFound == 1)
					{
						// once again if no target is required then we just let it fall through
					}
					else if (iFound > 1)
					{
						//ok in this case we found more than one valid tub so hand execution over to the target and end
						int resHue = 0;
						int maxAmount = 0;
						object message = null;
						from.SendMessage("Select the dye tub you wish to use for the process.");
						from.Target = new SpecialDyeTubTarget(this, typeRes, craftSystem, ref resHue, ref maxAmount, ref message, m_NameString, 0, tool);
						return;
					}
				}
			} // ----------------------------------------------------------------- /
            #endregion SpecialDyeTub HACK - needs cleanup

            #region MORE SpecialDyeTub HACKS - cleanup
			//This check is where skill is (possibly) gained!  
			//PLA: 04/06/09 - Prevent skillgain from special tubs entirely
			if ((craftSystem is DefAlchemy && (ItemType == typeof(SpecialDyeTub) || ItemType == typeof(SpecialDye))))
			{
				/* no gain */
				;
			}
			else
            #endregion
				CheckSkills(from, typeRes, craftSystem, ref ignored, ref allRequiredSkills);
#endif

            if (CheckSkills(from, typeRes, craftSystem, ref ignored, ref allRequiredSkills))
            {
                // Resource
                int resHue = 0;
                int maxAmount = 0;

                object message = null;

                ConsumeType ct = ConsumeType.All;

                #region SpecialDyeTub
                // erl: if this is special dye, forget about consumption for now
                // ..
                if (craftSystem is DefAlchemy && (ItemType == typeof(SpecialDyeTub) || ItemType == typeof(SpecialDye)))
                    ct = ConsumeType.None;
                // ..
                #endregion SpecialDyeTub

                // Not enough resource to craft it				ha
                if (!ConsumeRes(from, typeRes, craftSystem, ref resHue, ref maxAmount, ct, ref message))
                {
                    if (tool != null && !tool.Deleted && tool.UsesRemaining > 0)
                    {
                        if (!craftSystem.OldSchool)
                            from.SendGump(new CraftGump(from, craftSystem, tool, message));
                        else
                            CraftNotice(from, message);
                    }
                    else if (message is int && (int)message > 0)
                        from.SendLocalizedMessage((int)message);
                    else if (message is string)
                        from.SendMessage((string)message);

                    return;
                }
                else if (!ConsumeAttributes(from, ref message, true))
                {
                    if (tool != null && !tool.Deleted && tool.UsesRemaining > 0)
                    {
                        if (!craftSystem.OldSchool)
                            from.SendGump(new CraftGump(from, craftSystem, tool, message));
                        else
                            CraftNotice(from, message);
                    }
                    else if (message is int && (int)message > 0)
                        from.SendLocalizedMessage((int)message);
                    else if (message is string)
                        from.SendMessage((string)message);

                    return;
                }

                tool.UsesRemaining--;

                if (tool.UsesRemaining < 1)
                    toolBroken = true;

                if (toolBroken)
                    tool.Delete();

                int num = 0;

                Item item;
                if (customCraft != null)
                {
                    item = customCraft.CompleteCraft(out num);
                }
                else if (typeof(MapItem).IsAssignableFrom(ItemType) && from.Map != Map.Trammel && from.Map != Map.Felucca)
                {
                    item = new IndecipherableMap();
                    from.SendLocalizedMessage(1070800); // The map you create becomes mysteriously indecipherable.
                }
                else
                {
                    item = Activator.CreateInstance(ItemType) as Item;
                }

                if (item != null)
                {
                    // Adam: mark it as PlayerCrafted
                    item.PlayerCrafted = true;

                    if (item is ICraftable)
                        endquality = ((ICraftable)item).OnCraft(quality, makersMark, from, craftSystem, typeRes, tool, this, resHue);
                    else if (item.Hue == 0)
                        item.Hue = resHue;

                    if (maxAmount > 0)
                    {
                        if (!item.Stackable && item is IUsesRemaining)
                            ((IUsesRemaining)item).UsesRemaining *= maxAmount;
                        else
                            item.Amount = maxAmount;
                    }

                    #region SpecialDyeTub HACK - needs cleanup
                    if (craftSystem is DefAlchemy && (item is SpecialDye || item is SpecialDyeTub))
                    {
                        // Which special dye tub do we want to use?

                        Item[] sdtubs = ((Container)from.Backpack).FindItemsByType(typeof(SpecialDyeTub), true);
                        SpecialDyeTub sdtub;

                        if (m_NameString == "> Darken the mix" || m_NameString == "> Lighten the mix" && sdtubs.Length > 0)
                        {
                            sdtub = (SpecialDyeTub)sdtubs[0];

                            if (sdtubs.Length > 1)
                            {
                                // Pla, 04/01/07
                                // This code will never be called now but left it in for sanity.
                                // -----------------------------------------------------------------
                                // Let the target take over from here
                                //from.Target = new SpecialDyeTubTarget( this, typeRes, craftSystem, ref resHue, ref maxAmount, ref message, ref item, m_NameString, (m_NameString == "> Darken the mix" ? 1 : 2) );
                                //from.SendMessage("Select the dye tub you wish to use for the process.");
                            }
                            else
                            {
                                int iConsume = 0;

                                if (m_NameString == "> Darken the mix")
                                {
                                    if (sdtub.DarkenMix())
                                    {
                                        from.SendMessage("You darken the mix with black pearl...");
                                        double dCalc = sdtub.Uses;
                                        dCalc /= 5;
                                        iConsume = ((int)dCalc) + 1;
                                    }
                                    else //pla: This will never happen now
                                        from.SendMessage("You attempt to darken the mix, but it will go no darker.");

                                }
                                else
                                {
                                    if (sdtub.LightenMix())
                                    {
                                        from.SendMessage("You lighten the mix with sulfurous ash...");
                                        double dCalc = sdtub.Uses;
                                        dCalc /= 5;
                                        iConsume = ((int)dCalc) + 1;
                                    }
                                    else //pla: This will never happen now
                                        from.SendMessage("You attempt to lighten the mix, but it will go no lighter.");
                                }

                                for (int i = 0; i < iConsume; i++)
                                    ConsumeRes(from, typeRes, craftSystem, ref resHue, ref maxAmount, ConsumeType.All, ref message);

                                from.PlaySound(0x242);
                            }
                        }
                        else
                        {
                            // Make sure they've got a regular dye tub / a special one of the same color

                            Item[] dtub = ((Container)from.Backpack).FindItemsByType(typeof(DyeTub), true);
                            DyeTub dt = null;

                            int iFound = 0;

                            if (dtub.Length > 0)
                            {
                                for (int i = 0; i < dtub.Length; i++)
                                {
                                    if (dtub[i] is SpecialDyeTub)
                                    {
                                        // Is the same color?
                                        if (((SpecialDyeTub)dtub[i]).StoredColorName == m_NameString && ((SpecialDyeTub)dtub[i]).Uses < 100 && !((SpecialDyeTub)dtub[i]).Prepped)
                                        {
                                            dt = (DyeTub)dtub[i];
                                            iFound++;
                                        }

                                        continue;
                                    }
                                    else
                                    {
                                        dt = (DyeTub)dtub[i];
                                        iFound++;
                                    }
                                }
                            }

                            sdtub = (SpecialDyeTub)item;

                            if (dt == null)
                            {
                                from.SendMessage("You need a fresh dye tub or one that is not yet full and of the same color as the dye you are preparing.");
                                sdtub.Delete();
                            }
                            else
                            {
                                if (iFound > 1)
                                {
                                    // Pla, 04/01/07
                                    // This code will never be called now but left it in for sanity
                                    // -----------------------------------------------------------------
                                    // Let the target take over from here
                                    //from.Target = new SpecialDyeTubTarget( this, typeRes, craftSystem, ref resHue, ref maxAmount, ref message, ref item, m_NameString, 0 );
                                    //from.SendMessage("Select the dye tub you wish to use for the process.");
                                }
                                else
                                {

                                    if (dt is SpecialDyeTub)
                                    {
                                        sdtub.Delete();
                                        ((SpecialDyeTub)dt).Uses++;
                                        from.SendMessage("You mix the dye and add it to an existing batch.");
                                    }
                                    else
                                    {
                                        sdtub.StoreColor(m_NameString);
                                        dt.Delete();
                                        from.SendMessage("You successfully mix the dye.");
                                        from.AddToBackpack(item);
                                    }
                                    ConsumeRes(from, typeRes, craftSystem, ref resHue, ref maxAmount, ConsumeType.All, ref message);
                                }
                            }
                        }
                    }
                    #endregion SpecialDyeTub HACK - needs cleanup
#if old
                    #region BaseTinkerTrap

					if (craftSystem is DefTinkering && item is BaseTinkerTrap)
					{
						//Need to send target cursor to target
						//the trappable container
						bTinkerTrap = true;

						int power = (int)from.Skills[SkillName.Tinkering].Value + Utility.Random(0, 15);
						if (power <= 10) power = 10;
						((BaseTinkerTrap)item).Power = power;
						from.SendMessage("Target the container you wish to trap.");
						from.Target = new TrappableContainerTarget(this, item, craftSystem, toolBroken, tool);
					}
                    #endregion BaseTinkerTrap
#endif
                    #region FullBookcase HACK
                    if (item is FullBookcase || item is FullBookcase2 || item is FullBookcase3)
                    {
                        // Does it now become a ruined bookcase? 5% chance.
                        if (Utility.RandomDouble() > 0.95)
                        {
                            from.SendMessage("You craft the bookcase, but it is ruined.");

                            item.Delete();
                            item = new RuinedBookcase();
                            item.Movable = true;
                        }
                        else
                            from.SendMessage("You finish the bookcase and fill it with books.");
                    }
                    #endregion FullBookcase HACK

                    #region SpecialDyeTub HACK - needs cleanup
                    if (item is SpecialDyeTub || item is SpecialDye)
                    {
                        /* do nothing */
                    }
                    #endregion SpecialDyeTub HACK - needs cleanup
                    else
                        from.AddToBackpack(item);

                    //from.PlaySound( 0x57 );
                }

                if (num == 0)
                {
                    num = craftSystem.PlayEndingEffect(from, false, true, toolBroken, endquality, makersMark, this);

                    // not sure what era we want to attach this tow, but I know it's not appropriate for AI
                    // offer the mark option at this point
                    if (!Core.Localized && !Core.UOAI)
                    {   // old-school makers mark gump
                        if (quality == 2 && from.Skills[craftSystem.MainSkill].Base >= 100.0 && this.IsMarkable(item.GetType()))
                            from.SendGump(new OldSchoolMakersMarkGump(from, item));
                    }
                }

                #region Factions
                bool queryFactionImbue = false;
                int availableSilver = 0;
                FactionItemDefinition def = null;
                Faction faction = null;

                if (item is IFactionItem)
                {
                    def = FactionItemDefinition.Identify(item);

                    if (def != null)
                    {
                        faction = Faction.Find(from);

                        if (faction != null)
                        {
                            Town town = Town.FromRegion(from.Region);

                            if (town != null && town.Owner == faction)
                            {
                                Container pack = from.Backpack;

                                if (pack != null)
                                {
                                    availableSilver = pack.GetAmount(typeof(Silver));

                                    if (availableSilver >= def.SilverCost)
                                        queryFactionImbue = Faction.IsNearType(from, def.VendorType, 12);
                                }
                            }
                        }
                    }
                }

                // TODO: Scroll imbuing
                #endregion

                if (queryFactionImbue)
                    from.SendGump(new FactionImbueGump(quality, item, from, craftSystem, tool, num, availableSilver, faction, def));
                else if (tool != null && !tool.Deleted && tool.UsesRemaining > 0)
                {
                    if (!craftSystem.OldSchool)
                        from.SendGump(new CraftGump(from, craftSystem, tool, num));
                    else if (num > 0)
                        from.SendLocalizedMessage(num);
                }
                else if (num > 0)
                    from.SendLocalizedMessage(num);

            }
            else if (!allRequiredSkills)
            {
                if (tool != null && !tool.Deleted && tool.UsesRemaining > 0)
                {
                    if (!craftSystem.OldSchool)
                        from.SendGump(new CraftGump(from, craftSystem, tool, 1044153));
                    else
                        from.SendLocalizedMessage(1044153);
                }
                else
                    from.SendLocalizedMessage(1044153); // You don't have the required skills to attempt this item.
            }
            else
            {
                ConsumeType consumeType = (UseAllRes ? ConsumeType.Fail : ConsumeType.Half);
                int resHue = 0;
                int maxAmount = 0;

                object message = null;

                // Do we have enough resource to craft it? If so, consume them
                if (!ConsumeRes(from, typeRes, craftSystem, ref resHue, ref maxAmount, consumeType, ref message))
                {
                    if (tool != null && !tool.Deleted && tool.UsesRemaining > 0)
                    {
                        if (!craftSystem.OldSchool)
                            from.SendGump(new CraftGump(from, craftSystem, tool, message));
                        else
                            CraftNotice(from, message);
                    }
                    else if (message is int && (int)message > 0)
                        from.SendLocalizedMessage((int)message);
                    else if (message is string)
                        from.SendMessage((string)message);

                    return;
                }
                // adam: added 5/27/11 to allow consumption of attributes like mana on a failure.
                // this is the case for some early shards like publish 5 Siege
                // Currently we're not restoring the bug "don�t have the proper materials" case
                // http://www.uoguide.com/Publish_15
                // BUG No:	"Players will no longer lose mana when attempting to inscribe a scroll they don�t have the proper materials for."
                // BUG Yes:	"Characters will no longer lose mana when they fail to make a scroll."
                else if (!ConsumeAttributes(from, ref message, craftSystem.ConsumeAttributeOnFailure(from)))
                {
                    if (tool != null && !tool.Deleted && tool.UsesRemaining > 0)
                    {
                        if (!craftSystem.OldSchool)
                            from.SendGump(new CraftGump(from, craftSystem, tool, message));
                        else
                            CraftNotice(from, message);
                    }
                    else if (message is int && (int)message > 0)
                        from.SendLocalizedMessage((int)message);
                    else if (message is string)
                        from.SendMessage((string)message);

                    return;
                }

                tool.UsesRemaining--;

                if (tool.UsesRemaining < 1)
                    toolBroken = true;

                if (toolBroken)
                    tool.Delete();

                // SkillCheck failed.
                int num = craftSystem.PlayEndingEffect(from, true, true, toolBroken, endquality, false, this);

                if (tool != null && !tool.Deleted && tool.UsesRemaining > 0)
                {
                    if (!craftSystem.OldSchool)
                        from.SendGump(new CraftGump(from, craftSystem, tool, num));
                    else if (num > 0)
                        from.SendLocalizedMessage(num);
                }
                else if (num > 0)
                    from.SendLocalizedMessage(num);
            }
        }

        // For targetting dye tub to use in dying process (if > 1)
        // pla, 01/04/07
        // modifed this class to control skillgain.
        // this is to prevent a skillgain exploit.
        // pla, 04/06/09
        // Removed ability to gain skill entirely
        private class SpecialDyeTubTarget : Target
        {
            Type m_TypeRes;
            CraftSystem m_CraftSystem;
            CraftItem m_CraftItem;
            BaseTool m_Tool;

            int m_ResHue;
            int m_MaxAmount;
            object m_Message;

            string m_NameString;

            int m_Function;         // 0 = dye, 1 = darken, 2 = lighten

            bool allRequiredSkills = true;

            public SpecialDyeTubTarget(CraftItem citem, Type typeRes, CraftSystem craftSystem, ref int resHue, ref int maxAmount, ref object message, string namestring, int function, BaseTool tool)
                : base(2, false, TargetFlags.None)
            {
                m_TypeRes = typeRes;
                m_CraftSystem = craftSystem;
                m_ResHue = resHue;
                m_MaxAmount = maxAmount;
                m_Message = message;
                m_CraftItem = citem;
                m_NameString = namestring;
                m_Tool = tool;  //pla: added this - removed item that is now redundant
                m_Function = function;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                switch (m_Function)
                {
                    case 0: // mix dye
                        {
                            if (targeted is SpecialDyeTub)
                            {
                                SpecialDyeTub sdt = (SpecialDyeTub)targeted;

                                if (sdt.StoredColorName == m_NameString && !sdt.Prepped)
                                {
                                    //skillgain check. Note we don't care about this, only that it might gain skills.
                                    // pla, 04/06/09
                                    // Removed ability to gain skill entirely
                                    //m_CraftItem.GetSuccessChance(from, m_TypeRes, m_CraftSystem, true, ref allRequiredSkills);
                                    sdt.Uses++;
                                    //		m_sdtub.Delete();
                                    from.SendMessage("You mix the dye and add it to an existing batch.");
                                    m_CraftItem.ConsumeRes(from, m_TypeRes, m_CraftSystem, ref m_ResHue, ref m_MaxAmount, ConsumeType.All, ref m_Message);
                                    EndTask(from, true);
                                }
                                else
                                {
                                    from.SendMessage("You can only use a special dye tub with the same base color in it.");
                                    EndTask(from, false);
                                }
                            }
                            else if (targeted is DyeTub)
                            {
                                SpecialDyeTub sdtub = (SpecialDyeTub)Activator.CreateInstance(typeof(SpecialDyeTub));
                                if (sdtub != null)
                                {
                                    //skillgain check. Note we don't care about this, only that it might gain skills.
                                    // pla, 04/06/09
                                    // Removed ability to gain skill entirely
                                    //m_CraftItem.GetSuccessChance(from, m_TypeRes, m_CraftSystem, true, ref allRequiredSkills);
                                    ((Item)targeted).Delete();
                                    sdtub.StoreColor(m_NameString);
                                    from.SendMessage("You successfully mix the dye.");
                                    from.AddToBackpack(sdtub);
                                    m_CraftItem.ConsumeRes(from, m_TypeRes, m_CraftSystem, ref m_ResHue, ref m_MaxAmount, ConsumeType.All, ref m_Message);
                                    EndTask(from, true);
                                }
                            }
                            else
                            {
                                from.SendMessage("You can only use a special tub with the same color dye in it or normal dye tub.");
                                EndTask(from, false);
                                //	m_sdtub.Delete();
                            }

                            break;
                        }
                    case 1:         // darken
                        {
                            if (targeted is SpecialDyeTub)
                            {
                                SpecialDyeTub sdt = (SpecialDyeTub)targeted;
                                if (sdt.DarkenMix())
                                {
                                    // pla, 04/06/09
                                    // Removed ability to gain skill entirely
                                    //m_CraftItem.GetSuccessChance(from, m_TypeRes, m_CraftSystem, true, ref allRequiredSkills);
                                    from.SendMessage("You darken the mix with black pearl...");
                                    from.PlaySound(0x242);

                                    double dCalc = sdt.Uses;
                                    dCalc /= 5;
                                    int iConsume = ((int)dCalc) + 1;

                                    for (int i = 0; i < iConsume; i++)
                                        m_CraftItem.ConsumeRes(from, m_TypeRes, m_CraftSystem, ref m_ResHue, ref m_MaxAmount, ConsumeType.All, ref m_Message);

                                    EndTask(from, true);
                                }
                                else
                                {
                                    from.SendMessage("You attempt to darken the mix, but it will go no darker.");
                                    EndTask(from, false);
                                }
                            }

                            break;
                        }
                    case 2:     // lighten
                        {
                            if (targeted is SpecialDyeTub)
                            {
                                SpecialDyeTub sdt = (SpecialDyeTub)targeted;
                                if (sdt.LightenMix())
                                {
                                    // pla, 04/06/09
                                    // Removed ability to gain skill entirely
                                    //m_CraftItem.GetSuccessChance(from, m_TypeRes, m_CraftSystem, true, ref allRequiredSkills);
                                    from.SendMessage("You lighten the mix with sulfurous ash...");
                                    from.PlaySound(0x242);

                                    double dCalc = sdt.Uses;
                                    dCalc /= 5;
                                    int iConsume = ((int)dCalc) + 1;

                                    for (int i = 0; i < iConsume; i++)
                                        m_CraftItem.ConsumeRes(from, m_TypeRes, m_CraftSystem, ref m_ResHue, ref m_MaxAmount, ConsumeType.All, ref m_Message);

                                    EndTask(from, true);
                                }
                                else
                                {
                                    from.SendMessage("You attempt to lighten the mix, but it will go no lighter.");
                                    EndTask(from, false);
                                }
                            }

                            break;
                        }

                }
            }

            private void EndTask(Mobile from, bool bTool)
            {
                //pla, 01/04/07
                //this proc decreases the tool by 1 and re-shows the gump
                // -----------------------------------------------------------------
                bool bToolBroken = false;

                if (bTool)
                {
                    m_Tool.UsesRemaining--;

                    if (m_Tool.UsesRemaining < 1)
                        bToolBroken = true;

                    if (bToolBroken)
                        m_Tool.Delete();
                }

                int num = m_CraftSystem.PlayEndingEffect(from, false, true, bToolBroken, 1, false, m_CraftItem);

                if (m_Tool != null && !m_Tool.Deleted && m_Tool.UsesRemaining > 0)
                {
                    if (!m_CraftSystem.OldSchool)
                        from.SendGump(new CraftGump(from, m_CraftSystem, m_Tool, num));
                    else if (num > 0)
                        from.SendLocalizedMessage(num);
                }
                else if (num > 0)
                    from.SendLocalizedMessage(num);
            }
        }

#if old
		private class TrappableContainerTarget : Target
		{
			Item m_item;
			CraftSystem m_craftSystem;
			BaseTool m_tool;
			bool m_toolBroken;
			CraftItem m_craftItem;

			public TrappableContainerTarget(CraftItem craftItem, Item item, CraftSystem craftSystem, bool toolBroken, BaseTool tool)
				: base(2, false, TargetFlags.None)
			{
				m_item = item;
				m_craftSystem = craftSystem;
				m_tool = tool;
				m_toolBroken = toolBroken;
				m_craftItem = craftItem;
			}

			protected override void OnTarget(Mobile from, object targeted)
			{
				if (targeted is TrapableContainer
					&& (targeted.GetType().GetCustomAttributes(typeof(TinkerTrapableAttribute), false).Length > 0)
					)
				{
					TrapableContainer tc = (TrapableContainer)targeted;

					if (tc.TrapType == TrapType.None)
					{
						int num = m_craftSystem.PlayEndingEffect(from, false, true, m_toolBroken, 1, false, m_craftItem);

						object notice = string.Format("You carefully trap the container.");

						//TRAP THE TARGET HERE!
						BaseTinkerTrap btt = (BaseTinkerTrap)m_item;

						// tc.TrapPower = btt.Power;

						// adam: use runuo 2.0 power/level logic
						int trapLevel = (int)(from.Skills.Tinkering.Value / 10);
						tc.TrapPower = trapLevel * 9;
						tc.TrapLevel = trapLevel;

						if (btt is PoisonTinkerTrap)
						{
							tc.TrapType = TrapType.PoisonTrap;
						}
						else if (btt is ExplosionTinkerTrap)
						{
							tc.TrapType = TrapType.ExplosionTrap;
						}
						else if (btt is DartTinkerTrap)
						{
							tc.TrapType = TrapType.DartTrap;
						}

						tc.TrapEnabled = true;
						tc.TinkerMadeTrap = true;
						if (tc is LockableContainer)
						{
							LockableContainer lc = (LockableContainer)tc;
							if (lc.Locked == false)
							{
								lc.TrapEnabled = false;
								//NOTE: need to find the localized message for this...
								//from.SendMessage("The trap is disabled until you lock the container.");
								from.SendLocalizedMessage(1005639); // Trap is disabled until you lock the chest.
							}
						}

						if (m_tool != null && !m_tool.Deleted && m_tool.UsesRemaining > 0)
						{
							from.SendGump(new CraftGump(from, m_craftSystem, m_tool, notice));
						}
						else
						{
							from.SendMessage((string)notice);
						}
					}
					else
					{
						from.SendMessage("That container is already trapped!");
						from.SendMessage("Target the container you wish to trap.");
						from.Target = new TrappableContainerTarget(m_craftItem, m_item, m_craftSystem, m_toolBroken, m_tool);
					}
				}
				else
				{
					from.SendMessage("That is not a valid container to be trapped!");
				}
			}

		}
#endif

        private class InternalTimer : Timer
        {
            private Mobile m_From;
            private int m_iCount;
            private int m_iCountMax;
            private CraftItem m_CraftItem;
            private CraftSystem m_CraftSystem;
            private Type m_TypeRes;
            private BaseTool m_Tool;

            public InternalTimer(Mobile from, CraftSystem craftSystem, CraftItem craftItem, Type typeRes, BaseTool tool, int iCountMax)
                : base(TimeSpan.Zero, TimeSpan.FromSeconds(craftSystem.Delay), iCountMax)
            {
                m_From = from;
                m_CraftItem = craftItem;
                m_iCount = 0;
                m_iCountMax = iCountMax;
                m_CraftSystem = craftSystem;
                m_TypeRes = typeRes;
                m_Tool = tool;
            }

            protected override void OnTick()
            {
                m_iCount++;

                m_From.DisruptiveAction();

                if (m_iCount < m_iCountMax)
                {
                    // erl: don't play default effect for special dyes
                    if (!(m_CraftSystem is DefAlchemy && (m_CraftItem.ItemType == typeof(SpecialDyeTub) || m_CraftItem.ItemType == typeof(SpecialDye))))
                        m_CraftSystem.PlayCraftEffect(m_From);
                }
                else
                {
                    m_From.EndAction(typeof(CraftSystem));

                    int badCraft = m_CraftSystem.CanCraft(m_From, m_Tool, m_CraftItem.m_Type);

                    if (badCraft > 0)
                    {
                        if (m_Tool != null && !m_Tool.Deleted && m_Tool.UsesRemaining > 0)
                        {
                            if (!m_CraftSystem.OldSchool)
                                m_From.SendGump(new CraftGump(m_From, m_CraftSystem, m_Tool, badCraft));
                            else
                                m_From.SendLocalizedMessage(badCraft);
                        }
                        else
                            m_From.SendLocalizedMessage(badCraft);

                        return;
                    }

                    int quality = 1;
                    bool allRequiredSkills = true;

                    m_CraftItem.CheckSkills(m_From, m_TypeRes, m_CraftSystem, ref quality, ref allRequiredSkills, false);

                    CraftContext context = m_CraftSystem.GetContext(m_From);

                    if (context == null)
                        return;

                    if (typeof(CustomCraft).IsAssignableFrom(m_CraftItem.ItemType))
                    {
                        CustomCraft cc = null;

                        try { cc = Activator.CreateInstance(m_CraftItem.ItemType, new object[] { m_From, m_CraftItem, m_CraftSystem, m_TypeRes, m_Tool, quality }) as CustomCraft; }
                        catch { }

                        if (cc != null)
                            cc.EndCraftAction();

                        return;
                    }

                    bool makersMark = false;

                    if (quality == 2 && m_From.Skills[m_CraftSystem.MainSkill].Base >= 100.0)
                    {
                        for (int i = 0; !makersMark && i < m_MarkableTable.Length; ++i)
                        {
                            Type t = m_MarkableTable[i];
                            makersMark = (m_MarkableTable[i].IsAssignableFrom(m_CraftItem.ItemType));
                        }
                    }

                    if (makersMark && context.MarkOption == CraftMarkOption.PromptForMark)
                    {
                        m_From.SendGump(new QueryMakersMarkGump(quality, m_From, m_CraftItem, m_CraftSystem, m_TypeRes, m_Tool));
                    }
                    else
                    {
                        if (context.MarkOption == CraftMarkOption.DoNotMark)
                            makersMark = false;

                        m_CraftItem.CompleteCraft(quality, makersMark, m_From, m_CraftSystem, m_TypeRes, m_Tool, null);
                    }
                }
            }
        }

        #region old school crafting support
        // used for old school item list crafting
        public void CraftNotice(Mobile from, object notice)
        {
            if (notice is int && (int)notice > 0)
                from.SendLocalizedMessage((int)notice);
            else if (notice is string)
                from.SendMessage(notice as string);
        }
        #endregion
    }
}
