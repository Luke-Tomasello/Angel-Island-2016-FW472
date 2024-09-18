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

/* Scripts/Engines/AI/Creature/BaseCreature.cs
 * ChangeLog
 *	3/4/11, Adam
 *		Add leather as a FoodPreference (lol, no kidding, goats eat leather.)
 *	2/3/11, Adam
 *		Bonding is now based upon publish.
 *			I.e., IsBondable { get { return (BondingEnabled && (PublishInfo.Publish >= 16.0 || Core.RuleSets.AngelIslandRules() || Core.UOAR) &&...
 *		Since our Siege is publish 15, there will be no bonding
 *	2/14/11, Adam
 *		Add Mortalis to the servers where reds receive 1/3 loot off creatures
 *	2/11/11, Adam
 *		Reds get only 1/3 the gold off creatures (Siege)
 *		See comments (publish 13.6 change)
 *	2/9/11, Adam
 *		Make summon bonus based upon UOAI
 *	1/28/11, Adam
 *		Add back OppositionGroup for !Core.AngelIsland
 *	12/26/10, Adam
 *		Add the missing fish steak to the carve system
 *  11/14/10, Adam
 *      Have m_IOBAlignment contingent upon Core.AngelIsland
 *	7/16/10, Adam
 *		o add new OnBeforeDispel() to allow dispelled creatures to do something.
 *		o apply Spirit Speak bonus during spawn
 *	6/30/10, Adam
 *		Add AI_Guard support
 *	6/21/10, Adam
 *		New function bool Remembers(object o)
 *		Vendors and other townspeople now LookAround() as part of their wandering AI. I.e:
 *		remember the mobiles around me so that if they are then hidden and release a spell while hidden, I can reasonably call guards.
 *		If a crime is committed around me, the guards may ask me if i've seen anyone and if I have, then they will go into action.
 *	6/18/10, Adam
 *		Update region logic to reflect shift from static to new dynamic regions
 *	5/12/10, Adam
 *		1. update AttackOrderHack to account for ScaredOfScaryThings
 *		2. Add new PackSlayerWeapon() function. Called from Vampire
 *	5/10/10, Adam
 *		Add a baby rare factory.
 *		Most creatures (in a dungeon with str > 50) now have a small chance to drop something cool!
 *	5/8/10, Adam
 *		Replace the whold notion of GetMaxStabled with GetMaxPremiumStabled and GetMaxEconomyStabled
 *		pets stored > GetMaxEconomyStabled < GetMaxPremiumStabled are charged a premimum
 *		pets stored <= GetMaxEconomyStabled are charged the OSI rate (which OSI doesn't actually charge but we do.)
 *	5/2/10, Adam
 *		Angel Island herding bonus for max stable slots
 *		max += (int)from.Skills[SkillName.Herding].Value / 20;
 *	4/29/10, Adam
 *		Add back the SpeedInfo table to correct inappropriate speeds.
 *		(moved the Nightmare and WhiteWyrm to the Medium table from the Fast table.
 *		these high-level tames are too fast on a shard without mounts .. they now match Dragons)
 *	4/2/10, Adam
 *		Add notion of SuppressNormalLoot so that we can replace normal loot (not add to it) with one of our lootpacks.
 *	3/10/10, Adam
 *		Add m_AI.Activate() to OnDamage to make sure the creature will heal and agress if attacked with delayed damage
 *		as tyhe player exits the sector whereby deactivating the sector.
 *	05/25/09, plasma
 *		- Changed the m_ConstantFocus property to use a member variable rather than relying on inheritance.
 *		- Added new serial version.
 *		Note:  ConstantFocus was not currently being used - only in Revenant.cs
 *	4/17/09, Adam
 *		Add a new 'factory' type of container to the spawner lootpack processing.
 *		This 'factory' type allows us to select one random item from the pack instead of giving the chance for each item.
 *	3/5/09, Adam
 *		Make Paragons short lived so they don't accumulate whereby making an area too difficult
 *	1/13/09, Adam
 *		Remove kooky auto-reset of IOBAlignment. The Pet always reflects the alignment of his master.
 *	1/7/09, Adam
 *		Have PackWeakPotion and PackStrongPotion set LootType.Special - don't drop, but can be stolen
 *	12/28/08, Adam
 *		Add new FightStyle member to tell the AI what fight style to use
 *	12/19/08, Adam
 *		total rewrite of AcquireFocusMob()
 *		See comments at the top of BaseAI.cs
 *	12/16/08, Adam
 *		Add the ForcedAI() function from RunUO 2.0 to support faction guards.
 *	12/07/08, Adam
 *		- Make FightMode a [Flags] Enum so that they can be combined
 *		- Add a new version (29) so we could convert all old FightMode values when version < 29 in Deserialize
 *	12/5/08, Adam
 *		Add calls to the new KinAwards system to calculate the silver dropped on this creature (if appropriate)
 *	11/05/08, Adam
 *		- fix all of Kit's typos surrounding "preferred" targets
 *		- make the properties IsScaryToPets IsScaredOfScaryThings values instead of hard-code
 *	10/16/08, Adam
 *		OnBeforeDeath() now calls SpawnerLoot() to see of a spawner specified loot pack (or item) should be dropped. If so, we will dupe a �template� item and drop that.
 *		Please note; we reset the droprate to 1.0 on the item duped as that will cause the new item�s serialization to assume the default and not write it.
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 5 loops updated.
 *	7/3/08, weaver
 *		Added empty base implementation OnActionChase for code triggered by chasing AI action.
 *	3/15/08, Adam
 *		- convert the code to that makes all clothes and equiped items newbied to a function:
 *			NewbieAllLayers()
 *		- cleanup the DoTransform() logic to ignore the notion of being human. You must now explicitly pass in the correct body type. 
 *	12/8/07, Pix
 *		Moved check up in PackMagicItem() so we don't create the item if we don't need it
 *			(and thus it's not left on the internal map)
 *  12/3/07, Pix
 *      Added IOBAlignement.Healer for kin-healers instead of overloading Outcast
 *	9/1/07, Adam
 *		Enhance Paragons so they have 16 varations:
 *		1. It's a paragon
 *		2. not tamable
 *		3. will be difficult to peace/provoke ~10% chance
 *		4. may like to attack the player and not his pet
 *		5. may get a boost in magical AI
 *		6. may be a creature that can reveal
 *		7. may be a runner
 *	8/28/07, Adam
 *		Add a very simple version of Paragon creatures .. these creatures are chosen by the spawner (10%)
 *		and are immune to barding, yet do not display such a message (to thwart scripters)
 *		These creatures also prefer the weaker targets such as players.
 *		for the added difficulty, paragon creatures deliver a tad more gold .. up to double.
 *		If needed we can add advanced magery, reveal, running, etc.
 *	8/22/07, Adam
 *		override CanTarget to return false if creature is blessed
 *		(the brit zoo has creatures that cannot be targeted by players)
 *	06/18/07 Taran Kain
 *		Added support for DragonAI, abstracted proxy aggression
 *	6/14/07, Pix
 *		Added protection in the Delete() function while we're logging things.
 *  6/2/07, Adam
 *      add a new version of PackItem() that allows you to override the enchanted scroll 
 *      item conversion. i.e., keep the item, don't make a scroll.
 *  04/16/07 Taran Kain
 *      Fixed GenderGene to be consistent.
 *      Dunno what 3-28 change is about.
 *	03/28/07 Taran Kain
 *  3/19/07, Adam
 *      Pacakge up loot generation from BaseChampion.cs and move it here
 *      We want to be able to designate any creature as a champ via our Mobile Factory
 *	3/18/07, weaver
 *		Added new virtual "Rarity" property which calculates a 0-10 value based on
 *		provocation difficulty.
 *  1/26/07 Adam
 *      - new dynamic property system
 *      - Rename all LifeSpan stuffs to be Lifespan
 *      - Add a new minutes seed variable to be serialized which seeds the lifespan
 *  1/08/07 Taran Kain
 *      Changed ControlSlotModifier, MaxLoyalty gene properties
 *      Changed how mutation works in breeding
 *      Added in BaseCreature-specific skillcheck stuff (now inheritance-based!)
 *  12/18/06, Adam
 *      add the bool 'Hibernating' to indicate a creature has not 'thought' in a while indicating
 *      there are no players in the area.
 *      See: engins/heartbeat.MobileLifespanCleanup for usage
 *	12/07/06 Taran Kain
 *		Removed console output when breeding. (Exceptions still show)
 *		Lowered ControlSlotModifier variance. (1.25,1.25 -> 1.1,1.1)
 *  11/28/06 Taran Kain
 *      Made deserialize handle grandfathering old critters into genetics
 *  11/27/06 Taran Kain
 *      Fixed CSM and ControlSlots.
 *      Made Regen Rate genes serialize.
 *	11/27/06, Pix
 *		Reverted the ControlSlots property to ignore ControlSlotModifier.  TK can fix it later.
 *	11/20/06 Taran Kain
 *		Modified Loyalty from a 0-11 scale to 0-110 scale.
 *		Virtualized several properties.
 *		Added ControlSlotModifier, Wisdom, Patience, Temper, MaxLoyalty genes.
 *		Added genetics system.
 *  10/19/06, Kit
 *		Changed spawner template mob deleteion logging to include spawner location.
 *  10/10/06, Kit
 *		Complete revert of b1, may the trammy tamers bring something great to the shard.
 *  10/08/06, Kit
 *		Reverted B1 bonding control slot increase logic, changed para confusion code to aggressive
 *		action vs Damage()
 *	9/12/06, Adam
 *		Add SpawnerTempMob to Delete logging system
 *  8/29/06, Kit
 *		Fixed exceptions being thrown via aura code and CanDoHarmful DRDT check code.
 *  8/24/06, Kit
 *		Added in special case Delete() override for checking and logging propertys/stack trace
 *		of specific serial deleted creature on test center.
 *  8/19/06, Kit
 *		Added in check of NoExternalHarmful DRDT setting to CanDoHarmful()
 *  8/16/06, Rhiannon
 *		Commented out calls to SpeedInfo.GetSpeeds()
 *  7/22/06, Kit
 *		Add CreatureFlags, Move virtual function AI bools to CreatureFlags
 *	7/10/06, Pix
 *		Removed penalty from harming Hires of the same kin.
 *	7/01/06, Pix
 *		Removed reflective damage on like-aligned-kin damaging.  It's not needed with the Outcast alignment.
 *	6/25/06, Pix
 *		Changed IOBAlignment property to return controller's IOBAlignment (if controlled).
 *  6/23/06, Kit
 *		Added Msg to OnThink for confusion level pets to warn master every 5 minutes, made loyalty drop via
 *		confusion code reset normal loyalty drop timer(prevents double hits), made pets now display msg anytime loyalty
 *		drops and loyalty is at unhappy or below. Made bonded pet going wild leave corpse and not just vanish.
 *	6/22/06, Pix
 *		Changed outcast flagging for aggressive actions to ignore combat pets/summons
 *	6/18/06, Pix
 *		Added call to PlayerMobile.OnKinAggression().
 *	6/14/06, Adam
 *		Eliminate Dupicate IsEnemy() function as it was mistanking being called when
 *			the other one was supposed to be called.
 *	6/10/06, Pix
 *		Changed Friendly-IOB Penalty.
 *		Equipped IOB explodes for 50 damage.
 *		AND any damage caused to mob from same-aligned player is reflected to player.
 *	06/07/06, Pix
 *		Fixed non-aligned IOB exploding problem
 *	06/06/06, Pix
 *		Changes for Kin System
 *  06/03/06, Kit
 *		Added CheckSpellImmunity virtual function
 *  06/02/06, Kit
 *		Fixed bug with set control master setting bondedbegin to min value causeing bonding and stable problem.
 *	06/01/06, Adam
 *		Make sure you can not Join a Summoned creature.
 *  05/20/06, Kit
 *		Added additional check to OnBeforeDeath to stop IOB exploit
 *	05/19/06, Adam
 *		- make sure feeding always resets the LoyaltyCheck if we are more than 5 minutes into our Loyalty timer
 *			The 5 minute check supresses the "your pet is happier" message when you hit max loyality to give
 *			visual feedback that you can stop feeding now.
 *		- Add virtual ControlDifficulty(). 
 *			We added this so that creatures like the Basilisk with HIGH min taming skill
 *			would still be highly controllable when the pet is max loyalty
 *  05/17/06, Kit
 *		Removed old loyalty decrease code in global loyalty timer, added in new serialized LoyaltyCheck date time
 *		per creature, Loyalty drop now checked in global loyalty timer vs current time vs LoyaltyCheck date/time.
 *		Feeding pets now sets LoyaltyCheck to current time + 1 hour.
 *  05/16/06, Kit
 *		Removed old pet confusion code, Fixed control chance for tames and loyalty effecting it.
 *		Added in check that if pet is paraed and owner breaks para to half current loyalty rateing and set order to none
 *		Loyalty to not drop below confused level from owner breaking para.
 *		Fixed bug with bonding and stableing.
 *  05/16/06, Kit
 *		Removed debug console output of slots caculation for bonding testing.
 *  05/09/06 Taran Kain
 *		Removed JM test code
 *  5/09/06, Kit
 *		Feeding a pet a kukui nut will unbond it and consume nut.
 *  5/07/06, Kit
 *		Made feeding no longer initiate bonding process, this is now handled via TOK.
 *  5/02/06, Kit
 *		Added in bonded tames +1 control slots logic, made Bonded tag only show to pets owner.
 *  4/22/06, Kit
 *		Changed UsesHumanWeapons property from bool variable to overidable virtual function.
 *	4/08/06, Pix
 *		Commented out unused GetLootingRights() put in for Harrower.
 *  3/26/06, Pix
 *		IOBJoin able to be turned off via CoreAI.
 *	4/6/06/ weaver
 *		Modified incorrect comment text ("against masters" -> "by masters").
 *	4/4/06, weaver
 *		Added logging of aggressive actions committed by masters (initiation only).
 *	3/28/06, weaver
 *		Made sure that pets are made visible in TeleportPets().
 *  3/24/06, Pix
 *		Added some more logging to GetLootingRights
 *	3/19/06, Adam
 *		Add logging to GetLootingRights() so we can figure out why nobody gets harrower loot
 *	3/12/06, Pix
 *		Merged in new GetLootingRights function which takes damageentries and maxhits as parameters
 *		instead of just damageentries.
 *	2/11/06, Adam
 *		convert BardImmune from an override to a property and serialize it :O
 *	1/23/06, Adam
 *		Certain mobs are shorter lived, make RefreshLifespan() virtual
 *	1/20/06, Adam
 *		Redesign DebugSay() - anti spam implementation
 *		Don't print the string unless it changes, or 5 seconds has passed.
 *  01/18/06 Taran Kain
 *		Changed JobManager test job to use a callback
 *	1/13/06, Adam
 *		Change TeleportPets() to check if "BaseOverland.GateTravel==false"
 *		If so, invoke the BaseOverland handler for 'scary' magic
 *	1/4/06, Adam
 *		Condition the OnThink() JobManager testing on if (CoreAI.TempInt == 2)
 *		Use 2 instead of 1 so that it does not collide with the Concussion changes
 *		in BaseWeapon.cs
 *	1/3/06, Adam
 *		Condition the OnThink() JobManager testing on if (CoreAI.TempInt == 1)
 *  12/29/05 Taran Kain
 *		Added simple JobManager task to OnThink for testing
 *  12/28/05, Kit
 *		Added FightMode Player, mobile will always choose a player on screen before anything else,
 *		Added virtual function IsScaryCondition() for specifing conditions for Scarylogic.
 *  12/26/05, Kit
 *		Added msg to fear aura
 *  12/10/05, Kit
 *		Added AuraType Fear that repells pets(vampires), allowed aura's to hit creatures of another team,
 *		and added generic CheckWeaponImmunity() method for altering damage done by a weapon.
 *  12/09/05, Kit
 *		Added Function DoTransform for shifting body/hue and added transform effect class.
 *  11/29/05, Kit
 *		Added NavDestination and NavBeacon propertys, added FSM state NavStar.
 *  11/07/05, Kit
 *		Added CanUseHumanWeapons bool value/UsesHumanWeapons property, for allowing creatures to use weapon damage vs creature set damage.
 *	10/06/05, erlein
 *		Moved the confusion creation check to AggressiveAction().
 *	10/02/05, erlein
 *		Added ConfusionEndTime to handle creature confusion.
 *	9/28/05, Adam
 *		In BreathDealDamage(), and to nerf fire-breathing pets in PvP
 *		We now 'cool' the fire breath over distance. That is, the farther it travels
 *		the less damage it does. This only affects Controlled Pets attacking Players.
 *	9/27/05, Adam
 *		Add new Aura helper AuraTarget()
 *		This function allows you to filter the targets in the area.
 *	9/24/05, Adam
 *		To address the 'Succubus attacks my bonded dead pet' bug:
 *		add IsDeadBondedPet to the test to keep ol' Succy from attacking dead bonded pets
 *	9/19/05, Adam
 *		New backwards compatable versions of IsEnemy() and is IsFriend()
 *		New versions of IsEnemy() and is IsFriend() that distinguish between Opposition and other enemies
 *		New function IsOppossition() so we can handle true enemies differently than say some random Enery Vortex
 *		Add IsOpposedToPlayers() and IsOpposedToPlayersPets() in IsOppossition() 
 *			this allows us to manage if enemy kin will attack the pets and/or player.
 *			This only applies to mobs that are FightMode Aggressor, Evil, or Criminal.
 *			Mobs that are Attack Closest do not need this control.
 *  09/14/05 Taran Kain
 *		Made SetControlMaster() break any barding effects.
 *	9/04/05, erlein
 *		Altered conditions of ChangingCombatant check so commands override re-aggressive acts if the
 *		aggressed creature is being controlled by the aggressor.
 *	9/02/05, erlein
 *		Altered conditions of ChangingCombatant check so is a % chance to take note of re-aggressive acts
 *		after command issue (excepting only attack commands).
 *	9/01/05, erlein
 *		Added extra condition to AggressiveAction() to bypass ChangingCombatant check for summoned
 *		or controlled pets in OrderType.Stop or OrderType.None modes when evaluating whether or not
 *		to fight back.
 *	8/02/05, Pix
 *		Added check in addition to InitialInnocent check to see whether the basecreature is controled
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 22 lines removed.
 *	7/23/05, Adam
 *		Remove all Necromancy, and Chivalry nonsense
 *	7/21/05, erlein
 *		Removed some code referencing resistance variables & redundant resist orientated functions
 *  7/20/05, Adam
 *		Remove resistance variables from memory
 *		Remove resistance variables from serialization
 *	7/13/05, erlein
 *		Added EScroll drop chance via PackItem.
 *	6/04/05, Kit
 *		Added CouncilMemberAI
 *  5/30/05, Kit
 *		Added non serialized variable/access function for LastSoundHeard,added HumanMageAI/GolemControolerAI to AI list.
 *	5/11/05, Kit
 *		Added new virtual function CheckWork() called by heartbeat for any possible work a mob may need to do
 *		while actually not active or thinking in the world.
 *	5/05/05, Kit
 *		Evil mage AI support added
 *	4/27/05, Kit
 *		Added Support for new AI options of preferred Type
 *	4/27/05, erlein
 *		Added read-only Counsleor access to BondingBegin & MinTameSkill
 *	4/26/05, Pix
 *		Now non-contolled summons are friend to nobody and enemy to everyone.
 *	4/21/05, Adam
 *		In AggressiveAction():
 *		only ignite IOB if attacking actual kin (!Tamable && !Summoned)
 *	4/20/05, Kit
 *		Added check in IsEnemy for if guild is null, fixed ev logic and iob exploding problem
 *	4/19/05, Kit
 *		Redesigned inenemy and isfriend to allow summons to attack other summons and controlled pets.
 *	4/13/05, Adam
 *		Add auto-IOB alignment support for Summoned creatures as well as Tamable.
 *		See: OnThink() and SetControlMaster()
 *		Add GetGold() that returns the total amount of gold on mob
 *	4/03/05, Kitaras
 *		Added GenieAI to ChangeAIType()
 *	3/20/05, Pix
 *		Now when a iob follower goes wild, it will do the right thing and not still be
 *		marked as following.
 *	3/2/05, Adam
 *		Replace the old logic in AggressiveAction (If tamable, don't ignite IOB if we are being attacked by our master)
 * 		with: if (Tamable == false)
 *		This change allows us to attack an IOB aligned pet without negative consequences.
 *	3/2/05, Adam
 *		bring back changes of 12/12/04: (In OnThink(): have pet assume owners alignment, if owner has IOB equipped)
 *		This change will work with SetControlMaster() insure the pets alignment tracks the masters.
 *		We need SetControlMaster() to do an immediate reset of the alignment during tame/release cycles.
 *	02/19/05, Pix
 *		Added call to CheckStatTimers() on resurrection of pet.
 *	01/20/05, Adam
 *		Rewrite Fame/Karma distribution to eliminate casts.
 *  01/19/05, Pix
 *		Set lifespan from 8-16 hours.
 *	01/19/05, Darva
 *			Split fame/karma evenly between party members,
 *	01/18/05, Pix
 *		Added Lifespan.
 *	1/14/05, Adam
 *		1. Reverse changes: 01/12/05 - smerX
 *		2. Reverse changes: 12/12/04, Adam
 *		3. Move code that aligns a pet with his master to SetControlMaster()
 *		4. In AggressiveAction(), we check to see if (this.ControlMaster != aggressor)
 *			If are a tamable, don't ignite IOB if we are being attacked by our master
 *  01/12/05 - Taran Kain
 *		Added a CanSee check to OnMovement when deciding whether or not to call guards.
 *	01/12/05 - smerX
 *		Tamables now have their IOBAlignment set to None immediately upon Controled being set to false
 *	01/06/05 - Pix
 *		Removed ability to heal if AI_Suicide.
 *  01/06/05 - Pix
 *		Backpack items w/AI_Suicide no longer newbied.
 *	01/05/05 - Pix
 *		Made all wearables/backpack items newbied on death of AI_Suicide, so they don't
 *		drop anything when dead.
 *	01/05/05 - Pix
 *		Tweaked so suiciding followers will stick around while they're commiting suicide.
 *	01/05/05 - Pix
 *		Made the "suicide" messages nicer.
 *  01/05/05 - Pix
 *		Removed "home" requirement for dismissing IOBFollowers
 *		Now can't join with dismissed IOBFollowers (who have AI_Suicide)
 *		Changed IOB requirement from 36 hours to 10 days
 *		Added time restriction to joining based on when player dismisses follower
 *		and the maxdelay on the spawner the follower came from.
 *	01/03/05 - Pix
 *		Added AI_Suicide
 *		Made IOBFollowers change to AI_Suicide when dismissed.
 *	01/03/05 - Pix
 *		Made sure Tamables couldn't be made IOBFollowers.
 *	12/29/04, Pix
 *		Added AttemptIOBDismiss, m_Spawner, Spawner, SpawnerLocation
 *		for controlling the dismissal of IOBFollowers.
 *	12/28/04, Pix
 *		Re-instated auto-dismiss until a better solution is found.
 *		Added message for successful join.
 *	12/28/04, Pix
 *		Removed auto-dismiss - now when an IOBLeader removes their IOB, their
 *		IOBFollowers will just follow them.
 *	12/26/04, Pix
 *		Made rank1 of IOB take 1.5X controlslots
 *	12/22/04, Pix
 *		Added that AccessLevel>Player gets a tag on following bretheren w/the controller's name
 *		for ease of debugging problems.
 *		Fixed dismissing control-slot issue.
 *	12/21/04, Pix
 *		Auto-dismiss IOBFollower if IOBLeader isn't wearing IOB.
 *	12/21/04, Pix
 *		Commented out Green Acres restriction for IOB joining.
 *	12/20/04, Pix
 *		Serialize/Deserialize IOBLeader/IOBFollower
 *		Fixed Controled = true when joining IOBLeader.
 *	12/20/04, Pix
 *		Added check for IOBEquipped to joining function.
 *	12/20/04, Pix
 *		Changed OppositionGroup to return IOBFactionGroups is IOBAlignment is not none.
 *	12/20/04, Pix
 *		Incorporated IOBRank.
 *	12/15/04, Pix
 *		Made Bretheren never able to Bond.  Basically IsBonded refuses to be set to true
 *		if IOBFollower is true.
 *	12/15/04, Pix
 *		Set up for doubling of Control Slots if not top rank in IOBF.
 *	12/12/04, Adam
 *		In OnThink(): have pet assume owners alignment, if owner has IOB equipped
 *	12/09/04, Pix
 *		First TEST!! code checkin for IOB Factions.
 *	11/07/04, Pigpen
 *		Updated aggressive action loop to set IOBtimer to 36 hours when the item is destroyed.
 *	11/07/04 - Pix.
 *		Fixed bad cast crash in AggressiveAction.
 *		Also changed the checking for all the items for IOB into a loop.
 *	11/05/04, Pigpen
 *		Made Additions to basecreature to facilitate the new IOB changes.
 *		Changes include:
 * 		Addition of IOBAlignment Enum call; All IOB Functions are carried out in BaseCreature. IsEnemy is
 *		no longer needed on each creature. Aggressive action is also no longer needed.
 *	11/2/04, Adam
 *		Remove some bogus checks for PlayerMobile that were causing compiler warnings
 *	10/07/04, Pixie
 *		Added datetime for checking whether bonded dead pet takes statloss on
 *		resurrection.  Also added check for stabled dead pet, so the pet doesn't
 *		think it's abandoned.
 *	9/30/04, Adam
*		Creatures now gain poisoning:
 *			Passively check Poisoning for gain in OnGaveMeleeAttack() for creatures.
 *		Scale a creatures chance to poison based on their Poisoning skill
 *  7/24/04, Adam
 *		Add the new method PackSlayerInstrument() to Pack a random slayer Instrument,
 *			not tied to the current creature.
 *	7/15/04, mith
 *		OnMovement(): Removed InhumanSpeech and Warning sounds if the mobile that's moving is > Player level access.
 *		Creatures capable of speech will also not speak if the player can't be seen (stealthing).
 *	6/25/04, Pix
 *		Removed RunUO's/OSI's lootpack/generateloot() stuff that conflicts
 *		with our loot model.
 *  6/20/04, Old Salty
 * 		Small addendum to OnDeath to make corpses disappear if killed by guards.
 *  6/12/04, Old Salty
 * 		Changes to OnSingleClick to accomodate hirables
 *	6/10/04, mith
 *		Modified for the new guard non-insta-kill guards.
 *  6/8/2004, Pulse
 *		Removed the doubling of resources for Felucca in the OnCarve() method
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	4/29/04, mith
 *		Another modification to PackMagicItems() using Utility.RandomBool() instead of Utility.RandomDouble().
 *	4/24/04, mith
 *		Modified PackMagicItems() to randomize the choice between Armor and Weapons a little better.
 *	4/13/04, mith
 *		Fixed typo in StablePets that caused player to be resed rather than pets.
 *	4/7/04, changes by mith
 *		Added StablePets() method, which is called by AIEntrance teleporter to stable all of a person's
 *			pets on entrance to Angel Island. If there is not enough room in the stables, the pet is left to go wild.
 *	4/3/04, changes by mith
 *		PackWeapon() and PackArmor() modified with new cap of 3 instead of the previous 5.
 *			Durability/Accuracy/Damage mods work in range of 0-3 (up to and including Force weaps)
 */

using Server.Commands;
using Server.ContextMenus;
using Server.Engines;
using Server.Engines.IOBSystem;
using Server.Engines.PartySystem;
using Server.Engines.Quests;
using Server.Factions;
using Server.Items;
using Server.Misc;
using Server.Network;
using Server.Regions;
using Server.Spells;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Server.Mobiles
{
    /// <summary>
    /// Summary description for MobileAI.
    /// </summary>
    ///
    [Flags]
    public enum FightMode
    {
        /* ** Need to add these ** 
		 * FactionOnly,
		 * FactionAndReds,
		 * FactionAndRedsAndCrim,
		 * Everyone, (except own faction)
		 * RedsAndCrim, 
		 * Crim (Done)
		 */
        None = 0x00,        // Never focus on others

        //
        // 0x01 - 0x8000 reserved for FOCUS MOB
        //
        Aggressor = 0x01,       // Only attack Aggressors
        Evil = 0x02,            // Attack negative karma -or- aggressor 
        Criminal = 0x04,        // Attack the criminals -or-  aggressor
        Murderer = 0x08,        // Attack Murderers -or-  aggressor
        All = 0x10,             // Attack all -or-  aggressor
        ConstantFocus = 0x20,   // Attack m_ConstantFocus (currently unused - needs design review)

        //
        // 0x10000 on reserved for SORT ORDER
        //
        Strongest = 0x10000,    // Attack the strongest first 
        Weakest = 0x20000,      // Attack the weakest first 
        Closest = 0x40000,      // Attack the closest first 		

        Int = 0x80000,          // Attack the highest INT first 
        Str = Strongest,        // Attack the highest STR first (NOTE: Same as Strongest above)
        Dex = 0x100000,         // Attack the highest DEX first 

    }

    /// <summary>
    /// Summary description for MobileAI.
    /// </summary>
    ///
    [Flags]
    public enum FightStyle
    {
        Default = 0x00, // default
        Bless = 0x01,   // heal, cure, +stats
        Curse = 0x02,   // poison, -stats
        Melee = 0x04,   // weapons
        Magic = 0x08,   // damage spells
        Smart = 0x10    // smart weapons/damage spells
    }

    public enum OrderType
    {
        None,           //When no order, let's roam
        Come,           //"(All/Name) come"  Summons all or one pet to your location.
        Drop,           //"(Name) drop"  Drops its loot to the ground (if it carries any).
        Follow,         //"(Name) follow"  Follows targeted being.
                        //"(All/Name) follow me"  Makes all or one pet follow you.
        Friend,         //"(Name) friend"  Allows targeted player to confirm resurrection.
        Guard,          //"(Name) guard"  Makes the specified pet guard you. Pets can only guard their owner.
                        //"(All/Name) guard me"  Makes all or one pet guard you.
        Attack,         //"(All/Name) kill",
                        //"(All/Name) attack"  All or the specified pet(s) currently under your control attack the target.
        Patrol,         //"(Name) patrol"  Roves between two or more guarded targets.
        Release,        //"(Name) release"  Releases pet back into the wild (removes "tame" status).
        Stay,           //"(All/Name) stay" All or the specified pet(s) will stop and stay in current spot.
        Stop,           //"(All/Name) stop Cancels any current orders to attack, guard or follow.
        Transfert       //"(Name) transfer" Transfers complete ownership to targeted player.
    }

    public enum AuraType
    {
        None,
        Ice,
        Fire,
        Poison,
        Hate,
        Fear
    }

    [Flags]
    public enum FoodType
    {
        Meat = 0x0001,
        FruitsAndVegies = 0x0002,
        GrainsAndHay = 0x0004,
        Fish = 0x0008,
        Eggs = 0x0010,
        Gold = 0x0020,
        Leather = 0x0040
    }

    [Flags]
    public enum PackInstinct
    {
        None = 0x0000,
        Canine = 0x0001,
        Ostard = 0x0002,
        Feline = 0x0004,
        Arachnid = 0x0008,
        Daemon = 0x0010,
        Bear = 0x0020,
        Equine = 0x0040,
        Bull = 0x0080
    }

    public enum ScaleType
    {
        Red,
        Yellow,
        Black,
        Green,
        White,
        Blue,
        All
    }

    public enum MeatType
    {
        Ribs,
        Bird,
        LambLeg
    }

    public enum HideType
    {
        Regular,
        Spined,
        Horned,
        Barbed
    }

    public enum PetLoyalty
    {
        None = 0,
        Confused = 10,
        ExtremelyUnhappy = 20,
        RatherUnhappy = 30,
        Unhappy = 40,
        SomewhatContent = 50,
        Content = 60,
        Happy = 70,
        RatherHappy = 80,
        VeryHappy = 90,
        ExtremelyHappy = 100,
        WonderfullyHappy = 110
    }

    public class DamageStore : IComparable
    {
        public Mobile m_Mobile;
        public int m_Damage;
        public bool m_HasRight;

        public DamageStore(Mobile m, int damage)
        {
            m_Mobile = m;
            m_Damage = damage;
        }

        public int CompareTo(object obj)
        {
            DamageStore ds = (DamageStore)obj;

            return ds.m_Damage - m_Damage;
        }
    }

    [Flags]
    public enum CreatureFlags
    {
        None = 0x00000000,
        DamageSlows = 0x00000001,
        CanRun = 0x00000002,
        CanReveal = 0x00000004,
        CanHear = 0x00000008,
        UsesRegeants = 0x00000010,
        UsesHumanWeapons = 0x00000020,
        Debug = 0x00000040,
        BreedingParticipant = 0x00000080,
        Paragon = 0x00000100,
        ScaryToPets = 0x00000200,
        ScaredOfScaryThings = 0x00000400,
        UsesBandages = 0x00000800,
        UsesPotions = 0x00001000,
        CrossHeals = 0x00002000,
        DebugSpeed = 0x00004000,
        StableHold = 0x00008000,            // player ran out of gold, the pet will be deleted when the player returns
    }

    public class BaseCreature : Mobile
    {
        public virtual void OnBeforeDispel(Mobile Caster)
        {
        }

        public void SetFlag(CreatureFlags flag, bool value)
        {
            if (value)
                m_Flags |= flag;
            else
                m_Flags &= ~flag;
        }

        public bool GetFlag(CreatureFlags flag)
        {
            return ((m_Flags & flag) != 0);
        }


        private CreatureFlags m_Flags;          // flags.
        private BaseAI m_AI;                    // THE AI

        private AIType m_CurrentAI;         // The current AI
        private AIType m_DefaultAI;         // The default AI

        private Mobile m_FocusMob;              // Use focus mob instead of combatant, maybe we don't whan to fight
        private FightMode m_FightMode;          // The style the mob uses

        private int m_iRangePerception;     // The view area
        private int m_iRangeFight;          // The fight distance

        private int m_iTeam;                // Monster Team

        private double m_dActiveSpeed;          // Timer speed when active
        private double m_dPassiveSpeed;     // Timer speed when not active
        private double m_dCurrentSpeed;     // The current speed, lets say it could be changed by something;

        private Point3D m_pHome;                // The home position of the creature, used by some AI
        private int m_iRangeHome = 10;      // The home range of the creature

        private Spawner m_Spawner;              //The spawner that spawned this mob, if applicable

        ArrayList m_arSpellAttack;      // List of attack spell/power
        ArrayList m_arSpellDefense;     // Liste of defensive spell/power

        private bool m_bControlled;     // Is controled
        private Mobile m_ControlMaster; // My master
        private Mobile m_ControlTarget; // My target mobile
        private Point3D m_ControlDest;      // My target destination (patrol)
        private OrderType m_ControlOrder;       // My order

        private PetLoyalty m_Loyalty;

        private double m_dMinTameSkill;
        private bool m_bTamable;

        private bool m_bSummoned = false;
        private DateTime m_SummonEnd;
        private int m_iControlSlots = 1;

        private bool m_bBardImmune = false;
        private bool m_bBardProvoked = false;
        private bool m_bBardPacified = false;
        private Mobile m_bBardMaster = null;
        private Mobile m_bBardTarget = null;
        private DateTime m_timeBardEnd;
        private WayPoint m_CurrentWayPoint = null;
        private Point2D m_TargetLocation = Point2D.Zero;
        private IOBAlignment m_IOBAlignment; //Pigpen - Addition for IOB Sytem
        private Mobile m_SummonMaster;

        private int m_HitsMax = -1;
        private int m_StamMax = -1;
        private int m_ManaMax = -1;
        private int m_DamageMin = -1;
        private int m_DamageMax = -1;

        private ArrayList m_Owners;

        private bool m_IsStabled;

        private bool m_HasGeneratedLoot; // have we generated our loot yet?

        private Point3D LastSound;

        private Point3D Destination;

        private NavDestinations dest;

        private NavBeacon Nav;

        private DateTime loyaltyfeed;

        private Mobile m_ConstantFocus = null;

        public override bool Remembers(object o)
        {   // does this creature remember the object?
            // (usually a towns person remembering a player)
            if (AIObject == null)
                return false;

            return AIObject.Remembers(o);
        }

        public override bool CanTarget
        {   // Adam: the brit zoo has creatures that cannot be targeted by players
            get { return this.Blessed == false; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public NavDestinations NavDestination
        {
            get { return dest; }
            set { dest = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public NavBeacon Beacon
        {
            get { return Nav; }
            set { Nav = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D NavPoint
        {
            get { return Destination; }
            set { Destination = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D LastSoundHeard
        {
            get { return LastSound; }
            set { LastSound = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LoyaltyCheck
        {
            get { return loyaltyfeed; }
            set { loyaltyfeed = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Paragon
        {
            get { return GetFlag(CreatureFlags.Paragon); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CrossHeals
        {
            get { return GetFlag(CreatureFlags.CrossHeals); }
            set { SetFlag(CreatureFlags.CrossHeals, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool UsesBandages
        {
            get { return GetFlag(CreatureFlags.UsesBandages); }
            set { SetFlag(CreatureFlags.UsesBandages, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool UsesPotions
        {
            get { return GetFlag(CreatureFlags.UsesPotions); }
            set { SetFlag(CreatureFlags.UsesPotions, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool DmgDoesntSlowsMovement
        {
            get { return GetFlag(CreatureFlags.DamageSlows); }
            set { SetFlag(CreatureFlags.DamageSlows, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CanRun
        {
            get { return GetFlag(CreatureFlags.CanRun); }
            set { SetFlag(CreatureFlags.CanRun, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool UsesHumanWeapons
        {
            get { return GetFlag(CreatureFlags.UsesHumanWeapons); }
            set { SetFlag(CreatureFlags.UsesHumanWeapons, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool UsesRegsToCast
        {
            get { return GetFlag(CreatureFlags.UsesRegeants); }
            set { SetFlag(CreatureFlags.UsesRegeants, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CanReveal
        {
            get { return GetFlag(CreatureFlags.CanReveal); }
            set { SetFlag(CreatureFlags.CanReveal, value); }
        }

        public virtual InhumanSpeech SpeechType { get { return null; } }


        public bool IsStabled
        {
            get { return m_IsStabled; }
            set { m_IsStabled = value; }
        }

        public virtual Faction FactionAllegiance { get { return null; } }
        public virtual int FactionSilverWorth { get { return 30; } }

        private DateTime NextBandageTime = DateTime.UtcNow;
        public virtual bool CanBandage { get { return false; } }
        public virtual TimeSpan BandageDelay { get { return TimeSpan.FromSeconds(11.0); } }
        public virtual int BandageMin { get { return 30; } }
        public virtual int BandageMax { get { return 45; } }

        private DateTime NextAuraTime = DateTime.UtcNow;
        public virtual AuraType MyAura { get { return AuraType.None; } }
        public virtual TimeSpan NextAuraDelay { get { return TimeSpan.FromSeconds(2.0); } }
        public virtual int AuraRange { get { return 2; } }
        public virtual int AuraMin { get { return 2; } }
        public virtual int AuraMax { get { return 4; } }
        public virtual bool AuraTarget(Mobile m) { return true; }


        #region Bonding
        public const bool BondingEnabled = true;

        public virtual bool IsBondable { get { return (BondingEnabled && (PublishInfo.Publish >= 16.0 || Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules()) && !Summoned); } }
        public virtual TimeSpan BondingDelay { get { return TimeSpan.FromDays(7.0); } }
        public virtual TimeSpan BondingAbandonDelay { get { return TimeSpan.FromDays(1.0); } }

        public override bool CanRegenHits { get { return !m_IsDeadPet && base.CanRegenHits; } }
        public override bool CanRegenStam { get { return !m_IsDeadPet && base.CanRegenStam; } }
        public override bool CanRegenMana { get { return !m_IsDeadPet && base.CanRegenMana; } }

        public override bool IsDeadBondedPet { get { return m_IsDeadPet; } }

        //Pix: variables for bonded-pet no-statloss
        private DateTime m_StatLossTime;

        //plasma: enables mobs to change the target priorty order
        /*public virtual FightMode[] FightModePriority
		{
			get { return null; }
		}*/

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime BondedDeadPetStatLossTime
        {
            get { return m_StatLossTime; }
            set { m_StatLossTime = value; }
        }

        private bool m_IsBonded;
        private bool m_IsDeadPet;
        private DateTime m_BondingBegin;
        private DateTime m_OwnerAbandonTime;

        [CommandProperty(AccessLevel.GameMaster)] //Pigpen - Addition for IOB Sytem
        public IOBAlignment IOBAlignment
        {
            get
            {   // pets assume the alignment of their master
                if (this.Controlled)
                {
                    if (this.ControlMaster != null)
                    {
                        if (this.ControlMaster is PlayerMobile)
                        {
                            return ((PlayerMobile)this.ControlMaster).IOBAlignment;
                        }
                    }
                }
                return m_IOBAlignment;
            }
            set
            {   // world respawn required to reset all creatures to 'none'
                m_IOBAlignment = Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules() ? value : IOBAlignment.None;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsBonded
        {
            get { return m_IsBonded; }
            set
            {
                if (value == true && this.IOBFollower) //Don't bond if it's a bretheren!
                {
                    m_IsBonded = false;
                    InvalidateProperties();
                    return;
                }

                m_IsBonded = value;
                InvalidateProperties();
            }
        }

        public bool IsDeadPet
        {
            get { return m_IsDeadPet; }
            set { m_IsDeadPet = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public DateTime BondingBegin
        {
            get { return m_BondingBegin; }
            set { m_BondingBegin = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime OwnerAbandonTime
        {
            get { return m_OwnerAbandonTime; }
            set { m_OwnerAbandonTime = value; }
        }
        #endregion

        public virtual double WeaponAbilityChance { get { return 0.4; } }

        public virtual WeaponAbility GetWeaponAbility()
        {
            return null;
        }

        // make this creature anti-scripter.
        // Codename Adam was a script designed to automatically farm creatures for gold.
        //	This 'paragon' creature will be a new breed that will have characteristics that make
        //	scripted farming much more difficult
        public void MakeParagon()
        {
            // only agressive creatures may be paragons
            if ((m_FightMode & FightMode.All) > 0)
            {
                /*
				 * 1. It's a paragon
				 * 2. not tamable
				 * 3. will be difficult to peace/provoke
				 * 4. may like to attack the player and not his pet
				 * 5. may get a boost in magical AI
				 * 6. may be a creature that can reveal
				 * 7. may be a runner
				 */
                SetFlag(CreatureFlags.Paragon, true);
                Tamable = false;

                if (Utility.RandomBool())
                    m_FightMode = FightMode.All | FightMode.Weakest;

                if (Utility.RandomBool() && AIObject is MageAI)
                {
                    m_CurrentAI = AIType.AI_BaseHybrid;
                    m_DefaultAI = AIType.AI_BaseHybrid;
                    ChangeAIType(AIType.AI_BaseHybrid);
                }

                if (Utility.RandomBool())
                    CanReveal = true;

                if (Utility.RandomBool())
                    CanRun = true;

                // short lived so they don't accumulate whereby making an area too difficult for everyone else (10-30 minutes)
                Lifespan = TimeSpan.FromMinutes(Utility.RandomMinMax(10, 30));

                InvalidateProperties();
            }
        }

        int rx_dummy;
        // genes?
        //[CommandProperty( AccessLevel.GameMaster )]
        public int FireResistSeed { get { return 0; } set { rx_dummy = value; } }

        //[CommandProperty( AccessLevel.GameMaster )]
        public int ColdResistSeed { get { return 0; } set { rx_dummy = value; } }

        //[CommandProperty( AccessLevel.GameMaster )]
        public int PoisonResistSeed { get { return 0; } set { rx_dummy = value; } }

        //[CommandProperty( AccessLevel.GameMaster )]
        public int EnergyResistSeed { get { return 0; } set { rx_dummy = value; } }

        //[CommandProperty( AccessLevel.GameMaster )]
        public int PhysicalDamage { get { return 100; } set { rx_dummy = value; } }

        //[CommandProperty( AccessLevel.GameMaster )]
        public int FireDamage { get { return 0; } set { rx_dummy = value; } }

        //[CommandProperty( AccessLevel.GameMaster )]
        public int ColdDamage { get { return 0; } set { rx_dummy = value; } }

        //[CommandProperty( AccessLevel.GameMaster )]
        public int PoisonDamage { get { return 0; } set { rx_dummy = value; } }

        //[CommandProperty( AccessLevel.GameMaster )]
        public int EnergyDamage { get { return 0; } set { rx_dummy = value; } }

        public virtual FoodType FavoriteFood { get { return FoodType.Meat; } }
        public virtual PackInstinct PackInstinct { get { return PackInstinct.None; } }

        public ArrayList Owners { get { return m_Owners; } }

        public virtual bool AllowMaleTamer { get { return true; } }
        public virtual bool AllowFemaleTamer { get { return true; } }
        public virtual bool SubdueBeforeTame { get { return false; } }

        public virtual bool Commandable { get { return true; } }

        public virtual bool CheckWork()
        {
            return false;
        }

        // genes?
        public virtual Poison HitPoison { get { return null; } }
        public virtual Poison PoisonImmune { get { return null; } }
        public virtual double HitPoisonChance
        {
            get
            {
                // Adam: scale a creatures chance to poison based on their Poisoning skill
                if (Skills[SkillName.Poisoning].Base == 100.0)
                    return 0.50;
                if (Skills[SkillName.Poisoning].Base > 90.0)
                    return (0.8 >= Utility.RandomDouble() ? 0.45 : 0.50);
                if (Skills[SkillName.Poisoning].Base > 80.0)
                    return (0.8 >= Utility.RandomDouble() ? 0.40 : 0.45);
                if (Skills[SkillName.Poisoning].Base > 70.0)
                    return (0.8 >= Utility.RandomDouble() ? 0.35 : 0.40);
                if (Skills[SkillName.Poisoning].Base > 60.0)
                    return (0.8 >= Utility.RandomDouble() ? 0.30 : 0.35);
                if (Skills[SkillName.Poisoning].Base > 50.0)
                    return (0.8 >= Utility.RandomDouble() ? 0.25 : 0.30);
                return (0.8 >= Utility.RandomDouble() ? 0.20 : 0.25);
            }
        }

        // you no longer override this function, and instead set the value.
        //	example: BardImmune = true;
        [CommandProperty(AccessLevel.GameMaster)]
        public bool BardImmune
        {
            get { return m_bBardImmune; }

            set { m_bBardImmune = value; }
        }

        public virtual bool Unprovokable { get { return BardImmune || m_IsDeadPet; } }
        public virtual bool Uncalmable
        {
            get { return BardImmune || m_IsDeadPet; }
        }

        // Hey, someone's trying to peace .. speak out if you have something to say!
        public virtual void OnPeace()
        {
            foreach (Item ix in Items)
            {
                if (ix is OnPeace)
                {   // say one thing and break
                    (ix as OnPeace).Say(this, true);
                    break;
                }
            }
        }

        public virtual double DispelDifficulty { get { return 0.0; } } // at this skill level we dispel 50% chance
        public virtual double DispelFocus { get { return 20.0; } } // at difficulty - focus we have 0%, at difficulty + focus we have 100%

        #region Transformations/body/hue morphing
        //public virtual bool CanTransform() { return true; }
        //public virtual void	 LastTransform() { return ; }
        public virtual void DoTransform(Mobile m, int body, TransformEffect effect)
        {
            if (m != null)
                DoTransform(m, body, m.Hue, effect);
        }
        public void DoTransform(Mobile m, int body, int hue, TransformEffect effect)
        {
            //LastTransform();
            TransformEffect temp = effect;
            temp.Transform(m);
            m.Body = body;
            m.Hue = hue;
        }
        public class TransformEffect
        {
            public virtual void Transform(Mobile m)
            {
            }
        }
        #endregion
        #region Breath ability, like dragon fire breath
        private DateTime m_NextBreathTime;

        // Must be overriden in subclass to enable
        public virtual bool HasBreath { get { return false; } }

        // Base damage given is: CurrentHitPoints * BreathDamageScalar
        public virtual double BreathDamageScalar { get { return (Core.RuleSets.AOSRules() ? 0.16 : 0.05); } }

        // Min/max seconds until next breath
        // genes?
        public virtual double BreathMinDelay { get { return 10.0; } }
        public virtual double BreathMaxDelay { get { return 15.0; } }

        // Creature stops moving for 1.0 seconds while breathing
        public virtual double BreathStallTime { get { return 1.0; } }

        // Effect is sent 1.3 seconds after BreathAngerSound and BreathAngerAnimation is played
        public virtual double BreathEffectDelay { get { return 1.3; } }

        // Damage is given 1.0 seconds after effect is sent
        public virtual double BreathDamageDelay { get { return 1.0; } }

        public virtual int BreathRange { get { return RangePerception; } }

        // Damage types
        public virtual int BreathPhysicalDamage { get { return 0; } }
        public virtual int BreathFireDamage { get { return 100; } }
        public virtual int BreathColdDamage { get { return 0; } }
        public virtual int BreathPoisonDamage { get { return 0; } }
        public virtual int BreathEnergyDamage { get { return 0; } }

        // Effect details and sound
        public virtual int BreathEffectItemID { get { return 0x36D4; } }
        public virtual int BreathEffectSpeed { get { return 5; } }
        public virtual int BreathEffectDuration { get { return 0; } }
        public virtual bool BreathEffectExplodes { get { return false; } }
        public virtual bool BreathEffectFixedDir { get { return false; } }
        public virtual int BreathEffectHue { get { return 0; } }
        public virtual int BreathEffectRenderMode { get { return 0; } }

        public virtual int BreathEffectSound { get { return 0x227; } }

        // Anger sound/animations
        public virtual int BreathAngerSound { get { return GetAngerSound(); } }
        public virtual int BreathAngerAnimation { get { return 12; } }

        public virtual void BreathStart(Mobile target)
        {
            BreathStallMovement();
            BreathPlayAngerSound();
            BreathPlayAngerAnimation();

            this.Direction = this.GetDirectionTo(target);

            Timer.DelayCall(TimeSpan.FromSeconds(BreathEffectDelay), new TimerStateCallback(BreathEffect_Callback), target);
        }

        public virtual void BreathStallMovement()
        {
            if (m_AI != null)
                m_AI.NextMove = DateTime.UtcNow + TimeSpan.FromSeconds(BreathStallTime);
        }

        public virtual void BreathPlayAngerSound()
        {
            PlaySound(BreathAngerSound);
        }

        public virtual void BreathPlayAngerAnimation()
        {
            Animate(BreathAngerAnimation, 5, 1, true, false, 0);
        }

        public virtual void BreathEffect_Callback(object state)
        {
            Mobile target = (Mobile)state;

            if (!target.Alive || !CanBeHarmful(target))
                return;

            BreathPlayEffectSound();
            BreathPlayEffect(target);

            Timer.DelayCall(TimeSpan.FromSeconds(BreathDamageDelay), new TimerStateCallback(BreathDamage_Callback), target);
        }

        public virtual void BreathPlayEffectSound()
        {
            PlaySound(BreathEffectSound);
        }

        public virtual void BreathPlayEffect(Mobile target)
        {
            Effects.SendMovingEffect(this, target, BreathEffectItemID,
                BreathEffectSpeed, BreathEffectDuration, BreathEffectFixedDir,
                BreathEffectExplodes, BreathEffectHue, BreathEffectRenderMode);
        }

        public virtual void BreathDamage_Callback(object state)
        {
            Mobile target = (Mobile)state;

            if (CanBeHarmful(target))
            {
                DoHarmful(target);
                BreathDealDamage(target);
            }
        }

        public virtual void BreathDealDamage(Mobile target)
        {
            int physDamage = BreathPhysicalDamage;
            int fireDamage = BreathFireDamage;
            int coldDamage = BreathColdDamage;
            int poisDamage = BreathPoisonDamage;
            int nrgyDamage = BreathEnergyDamage;

            if (physDamage == 0 && fireDamage == 0 && coldDamage == 0 && poisDamage == 0 && nrgyDamage == 0)
            {   // Unresistable damage even in AOS
                target.Damage(BreathComputeDamage(), this, source_weapon: this);
            }
            else
            {
                // dragon's HP * BreathDamageScalar (so like 850 * .05 = 42 damage)
                double damage = (double)BreathComputeDamage();
                if (target.Player && this.Controlled)
                {   // Adam: dragon nerf (see Changelog: 9/28/05)
                    double distance = GetDistanceToSqrt(target);
                    damage -= damage * (.1 * distance);
                    if (damage <= 0.0) damage = 1.0;
                }
                AOS.Damage(target, this, (int)damage, physDamage, fireDamage, coldDamage, poisDamage, nrgyDamage, source_weapon: this);
            }
        }

        public virtual int BreathComputeDamage()
        {   // this the dragons HP not the target ;p
            int damage = (int)(Hits * BreathDamageScalar);
            return damage;
        }
        #endregion

        private DateTime m_EndFlee;

        public DateTime EndFleeTime
        {
            get { return m_EndFlee; }
            set { m_EndFlee = value; }
        }

        public virtual void StopFlee()
        {
            m_EndFlee = DateTime.MinValue;
        }

        public virtual bool CheckFlee()
        {
            if (m_EndFlee == DateTime.MinValue)
                return false;

            if (DateTime.UtcNow >= m_EndFlee)
            {
                StopFlee();
                return false;
            }

            return true;
        }

        public virtual void BeginFlee(TimeSpan maxDuration)
        {
            m_EndFlee = DateTime.UtcNow + maxDuration;
        }

        public BaseAI AIObject { get { return m_AI; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public override Mobile Combatant
        {
            get { return base.Combatant; }
            set
            {
                base.Combatant = value;
                if (AIObject != null && value != null)
                    AIObject.ShortTermMemory.Remember(value, new Point3D(value.Location), 10);
            }
        }

        public const int MaxOwners = 5;

        public virtual OppositionGroup OppositionGroup
        {
            get { return null; }
        }

        // Adam: Do we wish Aligned Players to appear as enimies to NPCs of a different alignment?
        public bool IsOpposedToPlayers()
        {
            return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.OpposePlayers);
        }

        public bool IsOpposedToPlayersPets()
        {
            return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.OpposePlayersPets);
        }

        public bool IsOpposition(Mobile m)
        {
            if (m != null)
            {
                // Adam: check for PC opposition
                // some good aligned kin may not want enemy players around
                if (m is PlayerMobile && m.Player && IsOpposedToPlayers())
                    if (IOBSystem.IsEnemy(this, m) == true)
                        return true;

                // Adam: check for PC Pet opposition
                // some good aligned kin may not want enemy pets around
                BaseCreature bc = m as BaseCreature;
                if (IsOpposedToPlayersPets() && bc != null)
                    if (bc.m_bControlled == true && bc.m_bTamable == true)
                        if (bc.m_ControlMaster != null)
                            if (IOBSystem.IsEnemy(this, bc) == true)
                                return true;

                // Adam: check for NPC opposition
                if (bc != null && IOBSystem.IsEnemy(this, m))
                    return true;
            }

            return false;
        }

        //add in test center evil logging of specific baski
        public override void Delete()
        {
            LogHelper Logger = null;
            try
            {
                if (Debug)
                {
                    Logger = new LogHelper("DebugDeleted.log", false, true);

                    PropertyInfo[] props = this.GetType().GetProperties();
                    Logger.Log(LogType.Text, "--------New Debug Deletion Entry---------");
                    for (int i = 0; i < props.Length; i++)
                    {
                        Logger.Log(LogType.Text, string.Format("{0}:{1}", props[i].Name, props[i].GetValue(this, null)));
                    }

                    Logger.Log(LogType.Text, new System.Diagnostics.StackTrace());
                    //Logger.Finish(); //Now occurs in finally block
                }
                else if (this.Loyalty <= PetLoyalty.None && this.IsStabled == false)
                {
                    Logger = new LogHelper("PetDeleted.log", false, true);
                    Logger.Log(LogType.Text, "--------Start Out Of Stable Bonded Loyalty Deleteion Entry---------");
                    Logger.Log(LogType.Text, string.Format("{0}: {1}: Owner:{2}", this.Name, this.Serial, this.ControlMaster));
                    Logger.Log(LogType.Text, new System.Diagnostics.StackTrace());
                    //Logger.Finish(); //Now occurs in finally block
                }
                else if (this.IsStabled == true || (this.Map == Map.Internal && this.Controlled == true || this.SpawnerTempMob == true))
                {
                    Logger = new LogHelper("PetDeleted.log", false, true);
                    Logger.Log(LogType.Text, "--------Start Inside Stable or Interal Map Deleted---------");
                    //Logger.Log(LogType.Text, string.Format("{0}: {1}: Owner:{2} Stabled:{3} Map:{4} Spawner:{5}", this.Name, this.Serial, this.ControlMaster, this.Stabled, this.Map, ((this.Spawner!=null)?this.Spawner.Location:"(null)")));
                    string loc = "(null)";
                    if (this.Spawner != null)
                    {
                        loc = this.Spawner.Location.ToString();
                    }
                    Logger.Log(LogType.Text, string.Format("{0}: {1}: Owner:{2} Stabled:{3} Map:{4} Spawner:{5}", this.Name, this.Serial, this.ControlMaster, this.Stabled, this.Map, loc));
                    Logger.Log(LogType.Text, new System.Diagnostics.StackTrace());
                    //Logger.Finish(); //Now occurs in finally block
                }
            }
            catch (Exception loggingEx)
            {
                LogHelper.LogException(loggingEx);
            }
            finally
            {
                if (Logger != null)
                {
                    try
                    {
                        Logger.Finish();
                    }
                    catch (Exception finallyException)
                    {
                        LogHelper.LogException(finallyException);
                    }
                }
            }

            base.Delete();
        }

        [Flags]
        public enum RelationshipFilter
        {
            None = 0x00,
            CheckOpposition = 0x01,         // is this a waring faction? (NPC)
            IgnorePCHate = 0x02,            // normally NPCs find all PCs as enemies. Ignore this rule
            Faction = 0x04 | IgnorePCHate,  // just look at Faction and Team opposition
        }

        public virtual bool IsFriend(Mobile m)
        {
            return IsFriend(m, RelationshipFilter.CheckOpposition);
        }

        public virtual bool IsFriend(Mobile m, RelationshipFilter filter)
        {
            // all other shards check OppositionGroup
            if (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.RenaissanceRules())
            {
                OppositionGroup g = this.OppositionGroup;

                if (g != null && g.IsEnemy(this, m))
                    return false;
            }

            //Pix: If we're a non-controlled summon, nothing is our friend
            if (m_bSummoned && !m_bControlled)
                return false;

            // Adam: is this a waring faction? (NPC)
            if ((filter & RelationshipFilter.CheckOpposition) > 0)
                if (IsOpposition(m))
                    return false;

            // Adam: If you are an ememy, you are not a friend (PC)
            if (IOBSystem.IsEnemy(this, m) == true)
                return false;

            // Adam: different teams are always waring (NPC)
            if (IsTeamOpposition(m) == true)
                return false;

            // Adam: Is this an IOB kinship? (PC)
            if (IOBSystem.IsFriend(this, m) == true)
                return true;

            BaseCreature c = m as BaseCreature;
            if (c != null)
            {
                //if both are tamed pets dont attack each other
                if (m_bControlled && c.m_bControlled)
                    return true;

                // same team?
                if (m_iTeam == c.m_iTeam)
                    return true;
            }

            // if it's a player, it's not a friend
            if ((filter & RelationshipFilter.IgnorePCHate) == 0)
                if (!(m is BaseCreature))
                    return false;

            // not recognized as a friend
            return false;
        }

        #region Allegiance
        public virtual Ethics.Ethic EthicAllegiance { get { return null; } }

        public enum Allegiance
        {
            None,
            Ally,
            Enemy
        }

        public virtual Allegiance GetFactionAllegiance(Mobile mob)
        {
            if (mob == null || mob.Map != Faction.Facet || FactionAllegiance == null)
                return Allegiance.None;

            Faction fac = Faction.Find(mob, true);

            if (fac == null)
                return Allegiance.None;

            return (fac == FactionAllegiance ? Allegiance.Ally : Allegiance.Enemy);
        }

        public virtual Allegiance GetEthicAllegiance(Mobile mob)
        {
            if (mob == null || mob.Map != Faction.Facet || EthicAllegiance == null)
                return Allegiance.None;

            Ethics.Ethic ethic = Ethics.Ethic.Find(mob, true);

            if (ethic == null)
                return Allegiance.None;

            return (ethic == EthicAllegiance ? Allegiance.Ally : Allegiance.Enemy);
        }
        #endregion

        // no longer virtual
        public bool IsEnemy(Mobile m)
        {
            return IsEnemy(m, RelationshipFilter.CheckOpposition);
        }

        public virtual bool IsEnemy(Mobile m, RelationshipFilter filter)
        {
            // all other shards check OppositionGroup
            if (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.RenaissanceRules())
            {
                OppositionGroup g = this.OppositionGroup;

                if (g != null && g.IsEnemy(this, m))
                    return true;
            }

            if (m is BaseGuard)
                return false;

            if (GetFactionAllegiance(m) == Allegiance.Ally)
                return false;

            Ethics.Ethic ourEthic = EthicAllegiance;
            Ethics.Player pl = Ethics.Player.Find(m, true);

            // new ethics system
            if (pl != null && pl.IsShielded && (ourEthic == null || ourEthic == pl.Ethic))
                return false;

            // old ethics system
            if (pl != null && pl.Mobile.CheckState(ExpirationFlagID.MonsterIgnore) && (ourEthic == null || ourEthic == pl.Ethic))
                return false;

            //Pix: If we're a non-controlled summon, everything is an enemy
            if (m_bSummoned && !m_bControlled)
                return true;

            // Adam: is this a waring faction? (NPC)
            if ((filter & RelationshipFilter.CheckOpposition) > 0)
                if (IsOpposition(m))
                    return true;

            // Adam: Is this an IOB kinship? (PC)
            if (IOBSystem.IsEnemy(this, m) == true)
                return true;

            // Adam: different teams are always waring (NPC)
            if (IsTeamOpposition(m) == true)
                return true;

            // Adam: If you are a friend, you are not an enemy (PC)
            if (IOBSystem.IsFriend(this, m) == true)
                return false;

            // don't hate PCs just because they are PCs
            if ((filter & RelationshipFilter.IgnorePCHate) == 0)
                if (!(m is BaseCreature))
                    return true;

            // don't hate summoned or controlled NPCs just because
            if ((filter & RelationshipFilter.Faction) == 0)
                if (m is BaseCreature)
                {
                    BaseCreature c = (BaseCreature)m;
                    return (((m_bSummoned || m_bControlled) != (c.m_bSummoned || c.m_bControlled))); //anything else attack whatever			
                }

            // doesn't seem to be an enemy
            return false;
        }

        public bool IsTeamOpposition(Mobile m)
        {
            if (m is BaseCreature)
            {
                BaseCreature c = (BaseCreature)m;
                return (m_iTeam != c.m_iTeam);
            }

            return false;
        }

        public virtual bool CheckControlChance(Mobile m)
        {
            return CheckControlChance(m, 0.0);
        }

        public virtual bool CheckControlChance(Mobile m, double offset)
        {
            double v = GetControlChance(m) + offset;

            if (Debug == true)
                this.Say("My control chance is {0}", v);

            if (v > Utility.RandomDouble())
                return true;

            PlaySound(GetAngerSound());

            if (Body.IsAnimal)
                Animate(10, 5, 1, true, false, 0);
            else if (Body.IsMonster)
                Animate(18, 5, 1, true, false, 0);

            return false;
        }

        public virtual bool CanBeControlledBy(Mobile m)
        {
            return (GetControlChance(m) > 0.0);
        }

        public virtual double ControlDifficulty()
        {
            return m_dMinTameSkill;
        }

        public virtual double GetControlChance(Mobile m)
        {
            if (m_dMinTameSkill <= 29.1 || m_bSummoned || m.AccessLevel >= AccessLevel.GameMaster)
                return 1.0;

            double dDifficultyFactor = ControlDifficulty();

            if (dDifficultyFactor > -24.9 && Server.SkillHandlers.AnimalTaming.CheckMastery(m, this))
                dDifficultyFactor = -24.9;

            int taming = (int)(m.Skills[SkillName.AnimalTaming].Value * 10);
            int lore = (int)(m.Skills[SkillName.AnimalLore].Value * 10);
            int difficulty = (int)(dDifficultyFactor * 10);
            int weighted = ((taming * 4) + lore) / 5;
            int bonus = weighted - difficulty;
            int chance;

            if (bonus > 0)
                chance = 700 + (bonus * 14);
            else
                chance = 700 + (bonus * 6);

            if (chance >= 0 && chance < 200)
                chance = 200;
            else if (chance > 990)
                chance = 990;

            int loyaltyValue = 1;

            if (m_Loyalty > PetLoyalty.Confused) // loyalty redo : removed *10
                loyaltyValue = (int)(m_Loyalty - PetLoyalty.Confused);

            chance -= (100 - loyaltyValue) * 10;

            return ((double)chance / 1000); //changed to / 1000 vs 10 that was returning results of 99 not 0.99
        }

        private static Type[] m_AnimateDeadTypes = new Type[]
            {
                typeof( MoundOfMaggots ), typeof( HellSteed ), typeof( SkeletalMount ),
                typeof( WailingBanshee ), typeof( Wraith ), typeof( SkeletalDragon ),
                typeof( LichLord ), typeof( FleshGolem ), typeof( Lich ),
                typeof( SkeletalKnight ), typeof( BoneKnight ), typeof( Mummy ),
                typeof( SkeletalMage ), typeof( BoneMagi ), typeof( PatchworkSkeleton )
            };

        public virtual bool IsAnimatedDead
        {
            get
            {
                if (!Summoned)
                    return false;

                Type type = this.GetType();

                bool contains = false;

                for (int i = 0; !contains && i < m_AnimateDeadTypes.Length; ++i)
                    contains = (type == m_AnimateDeadTypes[i]);

                return contains;
            }
        }

        public override void Damage(int amount, Mobile from, object source_weapon)
        {
            int oldHits = this.Hits;

            base.Damage(amount, from, source_weapon: source_weapon);

            if (SubdueBeforeTame && !Controlled)
            {
                if ((oldHits > (this.HitsMax / 10)) && (this.Hits <= (this.HitsMax / 10)))
                    PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "* The creature has been beaten into subjugation! *");
            }


        }

        public virtual bool DeleteCorpseOnDeath
        {
            get
            {
                return !Core.RuleSets.AOSRules() && m_bSummoned;
            }
        }

        public override void SetLocation(Point3D newLocation, bool isTeleport)
        {
            base.SetLocation(newLocation, isTeleport);

            if (isTeleport && m_AI != null)
                m_AI.OnTeleported();
        }

        public override ApplyPoisonResult ApplyPoison(Mobile from, Poison poison)
        {
            if (!Alive || IsDeadPet)
                return ApplyPoisonResult.Immune;

            //if ( Spells.Necromancy.EvilOmenSpell.CheckEffect( this ) )
            //return base.ApplyPoison( from, PoisonImpl.IncreaseLevel( poison ) );

            return base.ApplyPoison(from, poison);
        }

        public override bool CheckPoisonImmunity(Mobile from, Poison poison)
        {
            if (base.CheckPoisonImmunity(from, poison))
                return true;

            Poison p = this.PoisonImmune;

            return (p != null && p.Level >= poison.Level);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public PetLoyalty Loyalty
        {
            get
            {
                return m_Loyalty;
            }
            set
            {
                m_Loyalty = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public WayPoint CurrentWayPoint
        {
            get
            {
                return m_CurrentWayPoint;
            }
            set
            {
                m_CurrentWayPoint = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point2D TargetLocation
        {
            get
            {
                return m_TargetLocation;
            }
            set
            {
                m_TargetLocation = value;
            }
        }

        public virtual Mobile ConstantFocus
        {
            get { return m_ConstantFocus; }
            set { m_ConstantFocus = value; ; }

        }

        public virtual bool DisallowAllMoves
        {
            get
            {
                return false;
            }
        }

        public virtual bool InitialInnocent
        {
            get
            {
                return false;
            }
        }

        public virtual bool AlwaysMurderer
        {
            get
            {
                return false;
            }
        }

        public virtual bool AlwaysAttackable
        {
            get
            {
                return false;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int DamageMin { get { return m_DamageMin; } set { m_DamageMin = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int DamageMax { get { return m_DamageMax; } set { m_DamageMax = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int HitsMax
        {
            get
            {
                if (m_HitsMax >= 0)
                    return m_HitsMax;

                return Str;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int HitsMaxSeed
        {
            get { return m_HitsMax; }
            set { m_HitsMax = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int StamMax
        {
            get
            {
                if (m_StamMax >= 0)
                    return m_StamMax;

                return Dex;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int StamMaxSeed
        {
            get { return m_StamMax; }
            set { m_StamMax = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int ManaMax
        {
            get
            {
                if (m_ManaMax >= 0)
                    return m_ManaMax;

                return Int;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ManaMaxSeed
        {
            get { return m_ManaMax; }
            set { m_ManaMax = value; }
        }

        public virtual bool CanOpenDoors
        {
            get
            {
                return !this.Body.IsAnimal && !this.Body.IsSea;
            }
        }

        public virtual bool CanMoveOverObstacles
        {
            get
            {
                return this.Body.IsMonster;
            }
        }

        public virtual bool CanDestroyObstacles
        {
            get
            {
                // to enable breaking of furniture, 'return CanMoveOverObstacles;'
                return false;
            }
        }

        public override void OnDamage(int amount, Mobile from, bool willKill, object source_weapon)
        {
            RefreshLifespan();

            WeightOverloading.FatigueOnDamage(this, amount);

            InhumanSpeech speechType = this.SpeechType;

            if (speechType != null && !willKill)
                speechType.OnDamage(this, amount);

            // Adam: if players are able to damage a creature in a deactivated sector, activate the creature so it may heal
            if (m_AI != null)
                m_AI.Activate();

            base.OnDamage(amount, from, willKill, source_weapon: source_weapon);
        }

        public virtual void OnDamagedBySpell(Mobile from)
        {
        }

        public virtual void AlterDamageScalarFrom(Mobile caster, ref double scalar)
        {
        }

        public virtual void AlterDamageScalarTo(Mobile target, ref double scalar)
        {
        }

        public virtual void AlterMeleeDamageFrom(Mobile from, ref int damage)
        {
        }

        public virtual void AlterMeleeDamageTo(Mobile to, ref int damage)
        {
        }

        public virtual void CheckReflect(Mobile caster, ref bool reflect)
        {
        }

        public virtual void OnCarve(Mobile from, Corpse corpse)
        {
            int feathers = Feathers;
            int wool = Wool;
            int meat = Meat;
            int hides = Hides;
            int scales = Scales;
            int fishSteaks = FishSteaks;

            if ((feathers == 0 && wool == 0 && meat == 0 && hides == 0 && scales == 0 && fishSteaks == 0) || Summoned || IsBonded)
            {
                from.SendLocalizedMessage(500485); // You see nothing useful to carve from the corpse.
            }
            else
            {
                //	6/8/2004 - Pulse
                //		No longer doubles the resources from corpses if in Felucca, to re-add double resources
                //		un-comment the 6 lines of code below.
                //				if ( corpse.Map == Map.Felucca )
                //				{
                //					feathers *= 2;
                //					wool *= 2;
                //					hides *= 2;
                //				}

                new Blood(0x122D).MoveToWorld(corpse.Location, corpse.Map);

                if (feathers != 0)
                {
                    corpse.DropItem(new Feather(feathers));
                    from.SendLocalizedMessage(500479); // You pluck the bird. The feathers are now on the corpse.
                }

                if (wool != 0)
                {
                    corpse.DropItem(new Wool(wool));
                    from.SendLocalizedMessage(500483); // You shear it, and the wool is now on the corpse.
                }

                if (meat != 0)
                {
                    if (MeatType == MeatType.Ribs)
                        corpse.DropItem(new RawRibs(meat));
                    else if (MeatType == MeatType.Bird)
                        corpse.DropItem(new RawBird(meat));
                    else if (MeatType == MeatType.LambLeg)
                        corpse.DropItem(new RawLambLeg(meat));

                    from.SendLocalizedMessage(500467); // You carve some meat, which remains on the corpse.
                }

                if (hides != 0)
                {
                    if (HideType == HideType.Regular)
                        corpse.DropItem(new Hides(hides));
                    else if (HideType == HideType.Spined)
                        corpse.DropItem(new SpinedHides(hides));
                    else if (HideType == HideType.Horned)
                        corpse.DropItem(new HornedHides(hides));
                    else if (HideType == HideType.Barbed)
                        corpse.DropItem(new BarbedHides(hides));

                    from.SendLocalizedMessage(500471); // You skin it, and the hides are now in the corpse.
                }

                if (scales != 0)
                {
                    ScaleType sc = this.ScaleType;

                    switch (sc)
                    {
                        case ScaleType.Red: corpse.DropItem(new RedScales(scales)); break;
                        case ScaleType.Yellow: corpse.DropItem(new YellowScales(scales)); break;
                        case ScaleType.Black: corpse.DropItem(new BlackScales(scales)); break;
                        case ScaleType.Green: corpse.DropItem(new GreenScales(scales)); break;
                        case ScaleType.White: corpse.DropItem(new WhiteScales(scales)); break;
                        case ScaleType.Blue: corpse.DropItem(new BlueScales(scales)); break;
                        case ScaleType.All:
                            {
                                corpse.DropItem(new RedScales(scales));
                                corpse.DropItem(new YellowScales(scales));
                                corpse.DropItem(new BlackScales(scales));
                                corpse.DropItem(new GreenScales(scales));
                                corpse.DropItem(new WhiteScales(scales));
                                corpse.DropItem(new BlueScales(scales));
                                break;
                            }
                    }

                    from.SendMessage("You cut away some scales, but they remain on the corpse.");
                }

                if (fishSteaks != 0)
                {
                    corpse.DropItem(new FishSteak(fishSteaks));
                }

                corpse.Carved = true;

                if (corpse.IsCriminalAction(from))
                    from.CriminalAction(true);
            }
        }

        public virtual bool SpeedOverrideOK { get { return false; } }

        public const int DefaultRangePerception = 16;
        public const int OldRangePerception = 10;

        public BaseCreature(AIType ai,
            FightMode mode,
            int iRangePerception,
            int iRangeFight,
            double dActiveSpeed,
            double dPassiveSpeed)
        {

            if (iRangePerception == OldRangePerception)
                iRangePerception = DefaultRangePerception;

            m_Loyalty = PetLoyalty.WonderfullyHappy;

            m_CurrentAI = ai;
            m_DefaultAI = ai;

            m_iRangePerception = iRangePerception;
            m_iRangeFight = iRangeFight;

            m_FightMode = mode;

            m_iTeam = 0;

            if (SpeedOverrideOK == false)
                SpeedInfo.GetSpeeds(this, ref dActiveSpeed, ref dPassiveSpeed);

            m_dActiveSpeed = dActiveSpeed;
            m_dPassiveSpeed = dPassiveSpeed;
            m_dCurrentSpeed = dPassiveSpeed;

            m_arSpellAttack = new ArrayList();
            m_arSpellDefense = new ArrayList();

            m_bControlled = false;
            m_ControlMaster = null;
            m_ControlTarget = null;
            m_ControlOrder = OrderType.None;

            m_bTamable = false;

            m_Owners = new ArrayList();

            m_NextReAcquireTime = DateTime.UtcNow + ReacquireDelay;

            ChangeAIType(AI);

            InhumanSpeech speechType = this.SpeechType;

            if (speechType != null)
                speechType.OnConstruct(this);

            // our siege shard has 'at spawn' loot
            if (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.RenaissanceRules())
                GenerateLoot(true);     // 'at spawn' loot

            //new creature, give it a lifespan
            RefreshLifespan();

            InitializeGenes();

            // default for all creatures
            // pay special attention to the (version < 28) case in Deserialize()
            IsScaredOfScaryThings = true;
        }

        public BaseCreature(Serial serial)
            : base(serial)
        {
            m_arSpellAttack = new ArrayList();
            m_arSpellDefense = new ArrayList();

        }

        private static double[] m_StandardActiveSpeeds = new double[]
            {
                0.175, 0.1, 0.15, 0.2, 0.25, 0.3, 0.4, 0.5, 0.6, 0.8
            };

        private static double[] m_StandardPassiveSpeeds = new double[]
            {
                0.350, 0.2, 0.4, 0.5, 0.6, 0.8, 1.0, 1.2, 1.6, 2.0
            };

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            int version = 34;

            // version 34 m_SuppressNormalLoot
            // version 33, constant focus 
            // version 32, ad AI Serialization
            // version 31, add FightStyle parameter
            // version 30 maps old FightMode to new [flags] FightMode (so does 29)

            writer.Write((int)version); // version

            writer.Write((int)m_CurrentAI);
            writer.Write((int)m_DefaultAI);

            writer.Write((int)m_iRangePerception);
            writer.Write((int)m_iRangeFight);

            writer.Write((int)m_iTeam);

            writer.Write((double)m_dActiveSpeed);
            writer.Write((double)m_dPassiveSpeed);
            writer.Write((double)m_dCurrentSpeed);

            writer.Write((int)m_pHome.X);
            writer.Write((int)m_pHome.Y);
            writer.Write((int)m_pHome.Z);

            // Version 1
            writer.Write((int)m_iRangeHome);

            int i = 0;

            writer.Write((int)m_arSpellAttack.Count);
            for (i = 0; i < m_arSpellAttack.Count; i++)
            {
                writer.Write(m_arSpellAttack[i].ToString());
            }

            writer.Write((int)m_arSpellDefense.Count);
            for (i = 0; i < m_arSpellDefense.Count; i++)
            {
                writer.Write(m_arSpellDefense[i].ToString());
            }

            // Version 2
            writer.Write((int)m_FightMode);

            writer.Write((bool)m_bControlled);
            writer.Write((Mobile)m_ControlMaster);
            writer.Write((Mobile)m_ControlTarget);
            writer.Write((Point3D)m_ControlDest);
            writer.Write((int)m_ControlOrder);
            writer.Write((double)m_dMinTameSkill);
            // Removed in version 9
            //writer.Write( (double) m_dMaxTameSkill );
            writer.Write((bool)m_bTamable);
            writer.Write((bool)m_bSummoned);

            if (m_bSummoned)
                writer.WriteDeltaTime(m_SummonEnd);

            writer.Write((int)m_iControlSlots);

            // Version 3
            writer.Write((int)m_Loyalty);

            // Version 4
            writer.Write(m_CurrentWayPoint);

            // Verison 5
            writer.Write(m_SummonMaster);

            // Version 6
            writer.Write((int)m_HitsMax);
            writer.Write((int)m_StamMax);
            writer.Write((int)m_ManaMax);
            writer.Write((int)m_DamageMin);
            writer.Write((int)m_DamageMax);

            // Version 7
            // -- removed in version 18 --
            //writer.Write( (int) m_PhysicalResistance );
            //writer.Write( (int) m_PhysicalDamage );
            //writer.Write( (int) m_FireResistance );
            //writer.Write( (int) m_FireDamage );
            //writer.Write( (int) m_ColdResistance );
            //writer.Write( (int) m_ColdDamage );
            //writer.Write( (int) m_PoisonResistance );
            //writer.Write( (int) m_PoisonDamage );
            //writer.Write( (int) m_EnergyResistance );
            //writer.Write( (int) m_EnergyDamage );

            // Version 8
            writer.WriteMobileList(m_Owners, true);

            // Version 10
            writer.Write((bool)m_IsDeadPet);
            writer.Write((bool)m_IsBonded);
            writer.Write((DateTime)m_BondingBegin);
            writer.Write((DateTime)m_OwnerAbandonTime);

            // Version 11
            writer.Write((bool)m_HasGeneratedLoot);

            // Version 12 (Pix: statloss timer)
            writer.Write((DateTime)m_StatLossTime);

            // Version 13 (Pigpen: IOBAlignment)
            writer.Write((int)m_IOBAlignment);

            // Version 14 (Pix: IOBFollower/IOBLeader)
            writer.Write((bool)m_IOBFollower);
            writer.Write(m_IOBLeader);

            // Version 15 (Pix: Spawner)
            writer.Write(Spawner);

            // Version 16 (Pix: Lifespan)
            writer.WriteDeltaTime(m_lifespan);

            // removed in version 30
            //version 17 (Kit: preferred target AI additions
            //writer.Write( m_preferred );
            //writer.Write( (Mobile) m_preferredTargetType );
            //writer.Write ((int)Sortby);

            // version 18 - Adam: eliminate crazy resistances

            //version 19 (Kit NavStar variables
            writer.Write((Point3D)Destination);

            writer.Write((int)dest);

            writer.Write((NavBeacon)Nav);

            //version 20
            writer.Write((bool)m_bBardImmune);

            //versio 21
            writer.Write((DateTime)loyaltyfeed);

            //version 22 Add Flags
            writer.Write((int)m_Flags);

            // version 23
            // nothing different in Serialize - logic to adapt Loyalty in Deserialize

            // version 24
            writer.Write(m_ControlSlotModifier);
            writer.Write(m_Patience);
            writer.Write(m_Wisdom);
            writer.Write(m_Temper);
            writer.Write(m_MaxLoyalty);

            // version 25
            writer.Write(m_HitsRegenGene);
            writer.Write(m_ManaRegenGene);
            writer.Write(m_StamRegenGene);

            // version 26
            // nothing different - one-time logic to initialize genes

            // version 27
            writer.Write(m_LifespanMinutes);

            // version 28
            // do nothing - added for the conversion from IsScaredOfScaryThings to a value property
            // versions < 28 get the value TRUE, versions >= 28 get the value from the Flags

            // version 29 
            // do nothing - maps old FightMode to new FightMode [flags] 

            // version 30 
            // do nothing - maps old FightMode [flags] to new FightMode [flags] 

            // version 31, write FightStyle paramater for AI
            writer.Write((int)m_FightStyle);

            // version 32, write out the AI data
            if (AIObject != null)
                AIObject.Serialize(writer);

            //Version 33, constant focus
            writer.Write(m_ConstantFocus);

            // version 34
            writer.Write(m_SuppressNormalLoot);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_CurrentAI = (AIType)reader.ReadInt();
            m_DefaultAI = (AIType)reader.ReadInt();

            m_iRangePerception = reader.ReadInt();
            m_iRangeFight = reader.ReadInt();

            m_iTeam = reader.ReadInt();

            m_dActiveSpeed = reader.ReadDouble();
            m_dPassiveSpeed = reader.ReadDouble();
            m_dCurrentSpeed = reader.ReadDouble();

            double activeSpeed = m_dActiveSpeed;
            double passiveSpeed = m_dPassiveSpeed;

            if (SpeedOverrideOK == false)
                SpeedInfo.GetSpeeds(this, ref activeSpeed, ref passiveSpeed);

            if (activeSpeed != m_dActiveSpeed || passiveSpeed != m_dPassiveSpeed)
            {
                Console.WriteLine("Non standard creature speed detected for {0}. Patching...", this.ToString());
                m_dActiveSpeed = activeSpeed;
                m_dPassiveSpeed = passiveSpeed;
                m_dCurrentSpeed = m_dCurrentSpeed == m_dActiveSpeed ? m_dActiveSpeed : m_dPassiveSpeed;
            }

            if (m_iRangePerception == OldRangePerception)
                m_iRangePerception = DefaultRangePerception;

            m_pHome.X = reader.ReadInt();
            m_pHome.Y = reader.ReadInt();
            m_pHome.Z = reader.ReadInt();

            if (version >= 1)
            {
                m_iRangeHome = reader.ReadInt();

                int i, iCount;

                iCount = reader.ReadInt();
                for (i = 0; i < iCount; i++)
                {
                    string str = reader.ReadString();
                    Type type = Type.GetType(str);

                    if (type != null)
                    {
                        m_arSpellAttack.Add(type);
                    }
                }

                iCount = reader.ReadInt();
                for (i = 0; i < iCount; i++)
                {
                    string str = reader.ReadString();
                    Type type = Type.GetType(str);

                    if (type != null)
                    {
                        m_arSpellDefense.Add(type);
                    }
                }
            }
            else
            {
                m_iRangeHome = 0;
            }

            if (version >= 2)
            {
                m_FightMode = (FightMode)reader.ReadInt();

                m_bControlled = reader.ReadBool();
                m_ControlMaster = reader.ReadMobile();
                m_ControlTarget = reader.ReadMobile();
                m_ControlDest = reader.ReadPoint3D();
                m_ControlOrder = (OrderType)reader.ReadInt();

                m_dMinTameSkill = reader.ReadDouble();

                if (version < 9)
                    reader.ReadDouble();

                m_bTamable = reader.ReadBool();
                m_bSummoned = reader.ReadBool();

                if (m_bSummoned)
                {
                    m_SummonEnd = reader.ReadDeltaTime();
                    new UnsummonTimer(m_ControlMaster, this, m_SummonEnd - DateTime.UtcNow).Start();
                }

                m_iControlSlots = reader.ReadInt();
            }
            else
            {
                m_FightMode = FightMode.All | FightMode.Closest;

                m_bControlled = false;
                m_ControlMaster = null;
                m_ControlTarget = null;
                m_ControlOrder = OrderType.None;
            }

            if (version >= 3) // loyalty redo
            {
                m_Loyalty = (PetLoyalty)reader.ReadInt();

                if (version < 23)
                    m_Loyalty = (PetLoyalty)((int)m_Loyalty * 10);
            }
            else
                m_Loyalty = PetLoyalty.WonderfullyHappy;


            if (version >= 4)
                m_CurrentWayPoint = reader.ReadItem() as WayPoint;

            if (version >= 5)
                m_SummonMaster = reader.ReadMobile();

            if (version >= 6)
            {
                m_HitsMax = reader.ReadInt();
                m_StamMax = reader.ReadInt();
                m_ManaMax = reader.ReadInt();
                m_DamageMin = reader.ReadInt();
                m_DamageMax = reader.ReadInt();
            }

            if (version >= 7 && version < 18) // Adam: eliminate crazy resistances ver. 18
            {
                int dummy;
                dummy = reader.ReadInt();   // PhysicalResistance
                dummy = reader.ReadInt();   // PhysicalDamage
                dummy = reader.ReadInt();   // FireResistance
                dummy = reader.ReadInt();   // FireDamage
                dummy = reader.ReadInt();   // ColdResistance
                dummy = reader.ReadInt();   // ColdDamage
                dummy = reader.ReadInt();   // PoisonResistance
                dummy = reader.ReadInt();   // PoisonDamage
                dummy = reader.ReadInt();   // EnergyResistance
                dummy = reader.ReadInt();   // EnergyDamage
            }

            //if ( version >= 7 && version >= 18) // Adam: eliminate crazy resistances ver. 18
            //{
            //	m_PhysicalResistance = reader.ReadInt();
            //	m_PhysicalDamage = reader.ReadInt();
            //}

            if (version >= 8)
                m_Owners = reader.ReadMobileList();
            else
                m_Owners = new ArrayList();

            if (version >= 10)
            {
                m_IsDeadPet = reader.ReadBool();
                m_IsBonded = reader.ReadBool();
                m_BondingBegin = reader.ReadDateTime();
                m_OwnerAbandonTime = reader.ReadDateTime();
            }

            if (version >= 11)
                m_HasGeneratedLoot = reader.ReadBool();
            else
                m_HasGeneratedLoot = true;

            if (version >= 12)
            {
                m_StatLossTime = reader.ReadDateTime();
            }

            if (version >= 13) //Pigpen - Addition for IOB Sytem
            {
                m_IOBAlignment = (IOBAlignment)reader.ReadInt();
            }

            if (version >= 14) //Pix: IOBLeader/IOBFollower
            {
                m_IOBFollower = reader.ReadBool();
                m_IOBLeader = reader.ReadMobile();
            }

            if (version >= 15) //Pix: Spawner
            {
                m_Spawner = (Spawner)reader.ReadItem();
            }

            if (version >= 16) //Pix: Lifespan
            {
                m_lifespan = reader.ReadDeltaTime();
            }
            if (version >= 17 && version < 30) //Kit: preferred target ai
            {   // eliminated in version 30
                //m_preferred = reader.ReadBool();
                //m_preferredTargetType = reader.ReadMobile();
                //Sortby = (SortTypes)reader.ReadInt();
                reader.ReadBool();
                reader.ReadMobile();
                reader.ReadInt();
            }
            if (version >= 18) //Adam: eliminate stupid resistances
            {
                // see above - version 7
            }
            if (version >= 19) //Kit: NavStar
            {
                Destination = reader.ReadPoint3D();

                dest = (NavDestinations)reader.ReadInt();

                Nav = (NavBeacon)reader.ReadItem();
            }
            if (version >= 20) //Adam: convert BardImmune from an override to a property
            {
                m_bBardImmune = reader.ReadBool();
            }

            if (version >= 21)
            {
                loyaltyfeed = reader.ReadDateTime();
            }

            if (version >= 22)
            {
                m_Flags = (CreatureFlags)reader.ReadInt();
            }

            if (version >= 24)
            {
                m_ControlSlotModifier = reader.ReadDouble();
                m_Patience = reader.ReadInt();
                m_Wisdom = reader.ReadInt();
                m_Temper = reader.ReadInt();
                m_MaxLoyalty = reader.ReadInt();
            }

            if (version >= 25)
            {
                m_HitsRegenGene = reader.ReadDouble();
                m_ManaRegenGene = reader.ReadDouble();
                m_StamRegenGene = reader.ReadDouble();
            }

            // note the LESS THAN symbol instead of GTE
            // this is an example of run-once deserialization code - every old critter will run this once.
            if (version < 26)
                InitializeGenes();

            // version 27
            if (version >= 27)
                m_LifespanMinutes = reader.ReadInt();

            // we need to reset this because reading the Flags will have turned it off
            //	the flags value will obnly be valid when version >= 28
            if (version < 28)
                IsScaredOfScaryThings = true;

            /*	versions < 29 get their FightMode upgraded to the new [Flags] version
			public enum FightMode
			{
				None,			// Never focus on others
				Aggressor,		// Only attack Aggressors
				Strongest,		// Attack the strongest
				Weakest,		// Attack the weakest
				Closest, 		// Attack the closest
				Evil,			// Only attack aggressor -or- negative karma
				Criminal,		// Attack the criminals
				Player
			}
			 */
            if (version < 29)
            {
                switch ((int)m_FightMode)
                {   // now outdated values
                    case 0: m_FightMode = (FightMode)0x00; break;   /*None*/
                    case 1: m_FightMode = (FightMode)0x01; break;   /*Aggressor*/
                    case 2: m_FightMode = (FightMode)0x02; break;   /*Strongest*/
                    case 3: m_FightMode = (FightMode)0x04; break;   /*Weakest*/
                    case 4: m_FightMode = (FightMode)0x08; break;   /*Closest*/
                    case 5: m_FightMode = (FightMode)0x10; break;   /*Evil*/
                    case 6: m_FightMode = (FightMode)0x20; break;   /*Criminal*/
                    case 7: m_FightMode = (FightMode)0x40; break;   /*Player*/
                }
            }

            /* versions < 30 get their FightMode upgraded to the new [Flags] version
			public enum FightMode
			{
				None		= 0x00,		// Never focus on others
				Aggressor	= 0x01,		// Only attack Aggressors
				Strongest	= 0x02,		// Attack the strongest
				Weakest		= 0x04,		// Attack the weakest
				Closest		= 0x08, 	// Attack the closest
				Evil		= 0x10,		// Only attack aggressor -or- negative karma
				Criminal	= 0x20,		// Attack the criminals
				Player		= 0x40		// Attack Players (Vampires for feeding on blood)
			}
			 */
            if (version < 30)
            {
                switch ((int)m_FightMode)
                {
                    case 0x00 /*None*/		: m_FightMode = FightMode.None; break;
                    case 0x01 /*Aggressor*/	: m_FightMode = FightMode.Aggressor; break;
                    case 0x02 /*Strongest*/	: m_FightMode = FightMode.All | FightMode.Strongest; break;
                    case 0x04 /*Weakest*/	: m_FightMode = FightMode.All | FightMode.Weakest; break;
                    case 0x08 /*Closest*/	: m_FightMode = FightMode.All | FightMode.Closest; break;
                    case 0x10 /*Evil*/		: m_FightMode = FightMode.Aggressor | FightMode.Evil; break;
                    case 0x20 /*Criminal*/	: m_FightMode = FightMode.Aggressor | FightMode.Criminal; break;
                    case 0x40 /*Player*/	: m_FightMode = FightMode.All | FightMode.Closest; break;
                }
            }

            // new Fight Style for enhanced AI
            if (version >= 31)
                m_FightStyle = (FightStyle)reader.ReadInt();

            // version 32, read in the AI data, but we must construct the AI object first
            ChangeAIType(m_CurrentAI);
            if (version >= 32)
            {
                if (AIObject != null)
                    AIObject.Deserialize(reader);
            }

            if (version >= 33)
            {
                m_ConstantFocus = reader.ReadMobile();
            }

            if (version >= 34)
            {
                m_SuppressNormalLoot = reader.ReadBool();
            }

            // -------------------------------
            // After all the reading is done
            // -------------------------------
            RefreshLifespan();
            CheckStatTimers();
            AddFollowers();
        }

        public virtual bool IsHumanInTown()
        {
            return (Body.IsHuman && Region is Regions.GuardedRegion);
        }

        public virtual bool CheckGold(Mobile from, Item dropped)
        {
            if (dropped is Gold)
                return OnGoldGiven(from, (Gold)dropped);

            return false;
        }

        public virtual bool OnGoldGiven(Mobile from, Gold dropped)
        {
            if (CheckTeachingMatch(from))
            {
                if (Teach(m_Teaching, from, dropped.Amount, true))
                {
                    dropped.Delete();
                    return true;
                }
            }
            else if (IsHumanInTown())
            {
                Direction = GetDirectionTo(from);

                int oldSpeechHue = this.SpeechHue;

                this.SpeechHue = 0x23F;
                SayTo(from, "Thou art giving me gold?");

                if (dropped.Amount >= 400)
                    SayTo(from, "'Tis a noble gift.");
                else
                    SayTo(from, "Money is always welcome.");

                this.SpeechHue = 0x3B2;
                SayTo(from, 501548); // I thank thee.

                this.SpeechHue = oldSpeechHue;

                dropped.Delete();
                return true;
            }

            return false;
        }

        public override bool ShouldCheckStatTimers { get { return false; } }

        private static Type[] m_Eggs = new Type[]
            {
                typeof( FriedEggs ), typeof( Eggs )
            };

        private static Type[] m_Fish = new Type[]
            {
                typeof( FishSteak ), typeof( RawFishSteak )
            };

        private static Type[] m_GrainsAndHay = new Type[]
            {
                typeof( BreadLoaf ), typeof( FrenchBread )
            };

        private static Type[] m_Meat = new Type[]
            {
				/* Cooked */
				typeof( Bacon ), typeof( CookedBird ), typeof( Sausage ),
                typeof( Ham ), typeof( Ribs ), typeof( LambLeg ),
                typeof( ChickenLeg ),

				/* Uncooked */
				typeof( RawBird ), typeof( RawRibs ), typeof( RawLambLeg ),
                typeof( RawChickenLeg ),

				/* Body Parts */
				typeof( Head ), typeof( LeftArm ), typeof( LeftLeg ),
                typeof( Torso ), typeof( RightArm ), typeof( RightLeg )
            };

        private static Type[] m_FruitsAndVegies = new Type[]
            {
                typeof( HoneydewMelon ), typeof( YellowGourd ), typeof( GreenGourd ),
                typeof( Banana ), typeof( Bananas ), typeof( Lemon ), typeof( Lime ),
                typeof( Dates ), typeof( Grapes ), typeof( Peach ), typeof( Pear ),
                typeof( Apple ), typeof( Watermelon ), typeof( Squash ),
                typeof( Cantaloupe ), typeof( Carrot ), typeof( Cabbage ),
                typeof( Onion ), typeof( Lettuce ), typeof( Pumpkin )
            };

        private static Type[] m_Gold = new Type[]
            {
				// white wyrms eat gold..
				typeof( Gold )
            };

        private static Type[] m_Leather = new Type[]
            {
				// white wyrms eat gold..
				typeof( Leather )
            };

        public virtual bool CheckFoodPreference(Item f)
        {
            if (CheckFoodPreference(f, FoodType.Eggs, m_Eggs))
                return true;

            if (CheckFoodPreference(f, FoodType.Fish, m_Fish))
                return true;

            if (CheckFoodPreference(f, FoodType.GrainsAndHay, m_GrainsAndHay))
                return true;

            if (CheckFoodPreference(f, FoodType.Meat, m_Meat))
                return true;

            if (CheckFoodPreference(f, FoodType.FruitsAndVegies, m_FruitsAndVegies))
                return true;

            if (CheckFoodPreference(f, FoodType.Gold, m_Gold))
                return true;

            if (CheckFoodPreference(f, FoodType.Leather, m_Leather))
                return true;

            return false;
        }

        public virtual bool CheckFoodPreference(Item fed, FoodType type, Type[] types)
        {
            if ((FavoriteFood & type) == 0)
                return false;

            Type fedType = fed.GetType();
            bool contains = false;

            for (int i = 0; !contains && i < types.Length; ++i)
                contains = (fedType == types[i]);

            return contains;
        }

        public virtual bool CheckFeed(Mobile from, Item dropped)
        {
            if (!IsDeadPet && Controlled && ControlMaster == from && (dropped is Food || dropped is Gold || dropped is CookableFood || dropped is Head || dropped is LeftArm || dropped is LeftLeg || dropped is Torso || dropped is RightArm || dropped is RightLeg || dropped is KukuiNut || dropped is Leather))
            {
                Item f = dropped;

                if (CheckFoodPreference(f))
                {
                    int amount = f.Amount;

                    if (amount > 0)
                    {
                        bool happier = false;

                        int stamGain;

                        if (f is Gold)
                            stamGain = amount - 50;
                        else
                            stamGain = (amount * 15) - 50;

                        if (stamGain > 0)
                            Stam += stamGain;

                        for (int i = 0; i < amount; ++i)
                        {   // adam: make sure feeding always resets the LoyaltyCheck
                            if (0.5 >= Utility.RandomDouble())
                            {
                                bool bump = (int)m_Loyalty < MaxLoyalty; // loyalty redo
                                if (bump)
                                    m_Loyalty += Utility.RandomMinMax(7, 13); // loyalty redo

                                // if there was a bump in loyalty or the pet has not been feed in 5 minutes
                                if (bump || DateTime.UtcNow - LoyaltyCheck > TimeSpan.FromMinutes(5))
                                {
                                    happier = true;
                                    LoyaltyCheck = DateTime.UtcNow + TimeSpan.FromHours(1.0);
                                }
                                else
                                    DebugSay("I'm not hungry");
                            }
                        }

                        if (happier)
                            SayTo(from, 502060); // Your pet looks happier.

                        if (Body.IsAnimal)
                            Animate(3, 5, 1, true, false, 0);
                        else if (Body.IsMonster)
                            Animate(17, 5, 1, true, false, 0);

                        if (IsBondable && !IsBonded)
                        {
                            Mobile master = m_ControlMaster;

                            if (master != null)
                            {
                                if (m_dMinTameSkill <= 29.1 || master.Skills[SkillName.AnimalTaming].Value >= m_dMinTameSkill || this is SwampDragon || this is Ridgeback || this is SavageRidgeback)
                                {
                                    if (BondingBegin == DateTime.MinValue)
                                    {
                                        BondingBegin = DateTime.UtcNow;
                                    }
                                    else if ((BondingBegin + BondingDelay) <= DateTime.UtcNow)
                                    {
                                        IsBonded = true;
                                        BondingBegin = DateTime.MinValue;
                                        from.SendLocalizedMessage(1049666); // Your pet has bonded with you!
                                    }
                                }
                            }
                        }

                        dropped.Delete();
                        return true;
                    }
                }
            }

            return false;
        }

        public virtual void OnActionWander()
        {
        }

        public virtual void OnActionCombat()
        {
            RefreshLifespan();
        }

        public virtual void OnActionGuard()
        {
        }

        public virtual void OnActionHunt()
        {
        }

        public virtual void OnActionNavStar()
        {
        }

        public virtual void OnActionFlee()
        {
        }

        public virtual void OnActionInteract()
        {
        }

        public virtual void OnActionBackoff()
        {
        }

        // wea: base implementation of code triggered
        // by chasing AI action
        public virtual void OnActionChase()
        {
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (CheckFeed(from, dropped))
                return true;
            else if (CheckGold(from, dropped))
                return true;

            return base.OnDragDrop(from, dropped);
        }

        protected virtual BaseAI ForcedAI { get { return null; } }

        /// <summary>
        /// Change the current AI type WITHOUT setting the m_CurrentAI
        ///		I'm not sure why the RunUO engineers decided to write it like this, but it is better to set via public AIType AI
        /// Example: AI = AIType.AI_Melee
        /// This form correctly sets the m_CurrentAI
        /// </summary>
        /// <param name="NewAI"></param>
        public void ChangeAIType(AIType NewAI)
        {
            if (m_AI != null)
                m_AI.m_Timer.Stop();

            if (ForcedAI != null)
            {
                m_AI = ForcedAI;
                return;
            }

            m_AI = null;

            switch (NewAI)
            {
                case AIType.AI_Melee:
                    m_AI = new MeleeAI(this);
                    break;
                case AIType.AI_Animal:
                    m_AI = new AnimalAI(this);
                    break;
                case AIType.AI_Berserk:
                    m_AI = new BerserkAI(this);
                    break;
                case AIType.AI_Archer:
                    m_AI = new ArcherAI(this);
                    break;
                case AIType.AI_Healer:
                    m_AI = new HealerAI(this);
                    break;
                case AIType.AI_Vendor:
                    m_AI = new VendorAI(this);
                    break;
                case AIType.AI_Mage:
                    m_AI = new MageAI(this);
                    break;
                case AIType.AI_HumanMage:
                    m_AI = new HumanMageAI(this);
                    break;
                case AIType.AI_Predator:
                    m_AI = new MeleeAI(this);
                    break;
                case AIType.AI_Thief:
                    m_AI = new ThiefAI(this);
                    break;
                case AIType.AI_Council:
                    m_AI = new CouncilAI(this);
                    break;
                case AIType.AI_CouncilMember:
                    m_AI = new CouncilMemberAI(this);
                    break;
                case AIType.AI_Suicide:
                    m_AI = new SuicideAI(this);
                    break;
                case AIType.AI_Genie:
                    m_AI = new GenieAI(this);
                    break;
                case AIType.AI_BaseHybrid:
                    m_AI = new BaseHybridAI(this);
                    break;
                case AIType.AI_Vamp:
                    m_AI = new VampireAI(this);
                    break;
                case AIType.AI_Chicken:
                    m_AI = new ChickenAI(this);
                    break;
                case AIType.AI_Dragon:
                    m_AI = new DragonAI(this);
                    break;
                case AIType.AI_Hybrid:
                    m_AI = new HybridAI(this);
                    break;
                case AIType.AI_Guard:
                    m_AI = new GuardAI(this);
                    break;
                case AIType.AI_HumanMelee:
                    m_AI = new HumanMeleeAI(this);
                    break;
                case AIType.AI_TaxCollector:
                    m_AI = new TaxCollectorAI(this);
                    break;
            }
        }

        public void ForceTarget(Mobile from)
        {
            if (from == null || from.Deleted || !from.Alive) return;
            Combatant = from;
            ConstantFocus = from;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public AIType AI
        {
            get
            {
                return m_CurrentAI;
            }
            set
            {
                m_CurrentAI = value;

                if (m_CurrentAI == AIType.AI_Use_Default)
                {
                    m_CurrentAI = m_DefaultAI;
                }

                ChangeAIType(m_CurrentAI);
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool Debug
        {
            get
            {
                return GetFlag(CreatureFlags.Debug);
            }
            set
            {
                SetFlag(CreatureFlags.Debug, value);
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool DebugSpeed
        {
            get
            {
                return GetFlag(CreatureFlags.DebugSpeed);
            }
            set
            {
                SetFlag(CreatureFlags.DebugSpeed, value);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Team
        {
            get
            {
                return m_iTeam;
            }
            set
            {
                m_iTeam = value;

                OnTeamChange();
            }
        }

        public virtual void OnTeamChange()
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile FocusMob
        {
            get
            {
                return m_FocusMob;
            }
            set
            {
                m_FocusMob = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public FightMode FightMode
        {
            get
            {
                return m_FightMode;
            }
            set
            {
                m_FightMode = value;
            }
        }

        #region FightStyle
        private FightStyle m_FightStyle = FightStyle.Default;

        [CommandProperty(AccessLevel.GameMaster)]
        public FightStyle FightStyle
        {
            get
            {
                return m_FightStyle;
            }
            set
            {
                m_FightStyle = value;
            }
        }
        #endregion

        [CommandProperty(AccessLevel.GameMaster)]
        public int RangePerception
        {
            get
            {
                return m_iRangePerception;
            }
            set
            {
                m_iRangePerception = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RangeFight
        {
            get
            {
                return m_iRangeFight;
            }
            set
            {
                m_iRangeFight = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RangeHome
        {
            get
            {
                return m_iRangeHome;
            }
            set
            {
                m_iRangeHome = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double ActiveSpeed
        {
            get
            {
                return m_dActiveSpeed;
            }
            set
            {
                m_dActiveSpeed = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double PassiveSpeed
        {
            get
            {
                return m_dPassiveSpeed;
            }
            set
            {
                m_dPassiveSpeed = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double CurrentSpeed
        {
            get
            {
                return m_dCurrentSpeed;
            }
            set
            {
                if (m_dCurrentSpeed != value)
                {
                    m_dCurrentSpeed = value;

                    if (m_AI != null)
                        m_AI.OnCurrentSpeedChanged();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D Home
        {
            get
            {
                return m_pHome;
            }
            set
            {
                m_pHome = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Spawner Spawner
        {
            get { return m_Spawner; }
            set { m_Spawner = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D SpawnerLocation
        {
            get
            {
                if (m_Spawner == null)
                {
                    return Point3D.Zero;
                }
                else
                {
                    return m_Spawner.Location;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Controlled
        {
            get
            {
                return m_bControlled;
            }
            set
            {
                if (m_bControlled == value)
                    return;

                //refresh life if we change!
                RefreshLifespan();

                m_bControlled = value;
                Delta(MobileDelta.Noto);

                InvalidateProperties();
            }
        }

        public override void RevealingAction()
        {
            Spells.Sixth.InvisibilitySpell.RemoveTimer(this);

            base.RevealingAction();
        }

        public void RemoveFollowers()
        {
            if (m_ControlMaster != null)
            {
                m_ControlMaster.Followers -= ControlSlots;
            }
            else if (m_SummonMaster != null)
                m_SummonMaster.Followers -= ControlSlots;

            if (m_ControlMaster != null && m_ControlMaster.Followers < 0)
                m_ControlMaster.Followers = 0;

            if (m_SummonMaster != null && m_SummonMaster.Followers < 0)
                m_SummonMaster.Followers = 0;
        }

        public void AddFollowers()
        {
            if (m_ControlMaster != null)
                m_ControlMaster.Followers += ControlSlots;
            else if (m_SummonMaster != null)
                m_SummonMaster.Followers += ControlSlots;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile ControlMaster
        {
            get
            {
                return m_ControlMaster;
            }
            set
            {
                if (m_ControlMaster == value)
                    return;

                RemoveFollowers();
                m_ControlMaster = value;
                AddFollowers();

                Delta(MobileDelta.Noto);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile SummonMaster
        {
            get
            {
                return m_SummonMaster;
            }
            set
            {
                if (m_SummonMaster == value)
                    return;

                RemoveFollowers();
                m_SummonMaster = value;
                AddFollowers();

                Delta(MobileDelta.Noto);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile ControlTarget
        {
            get
            {
                return m_ControlTarget;
            }
            set
            {
                m_ControlTarget = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D ControlDest
        {
            get
            {
                return m_ControlDest;
            }
            set
            {
                m_ControlDest = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public OrderType ControlOrder
        {
            get
            {
                return m_ControlOrder;
            }
            set
            {
                m_ControlOrder = value;

                if (m_AI != null)
                    m_AI.OnCurrentOrderChanged();

                OnControlOrder(value);
            }
        }

        public virtual void OnControlOrder(OrderType order)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool BardProvoked
        {
            get
            {
                return m_bBardProvoked;
            }
            set
            {
                m_bBardProvoked = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool BardPacified
        {
            get
            {
                return m_bBardPacified;
            }
            set
            {
                m_bBardPacified = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile BardMaster
        {
            get
            {
                return m_bBardMaster;
            }
            set
            {
                m_bBardMaster = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile BardTarget
        {
            get
            {
                return m_bBardTarget;
            }
            set
            {
                m_bBardTarget = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime BardEndTime
        {
            get
            {
                return m_timeBardEnd;
            }
            set
            {
                m_timeBardEnd = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public double MinTameSkill
        {
            get
            {
                return m_dMinTameSkill;
            }
            set
            {
                m_dMinTameSkill = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Tamable
        {
            get
            {
                return m_bTamable;
            }
            set
            {
                m_bTamable = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool Summoned
        {
            get
            {
                return m_bSummoned;
            }
            set
            {
                if (m_bSummoned == value)
                    return;

                m_NextReAcquireTime = DateTime.UtcNow;

                m_bSummoned = value;
                Delta(MobileDelta.Noto);

                InvalidateProperties();
            }
        }

        double m_ControlSlotModifier;

        [Gene("Control Slot Mod", 0.014, 0.014, .4, .6, -.2, 1.2, GeneVisibility.Invisible)]
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public double ControlSlotModifier
        {
            get
            {
                return m_ControlSlotModifier;
            }
            set
            {
                m_ControlSlotModifier = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int ControlSlots
        {
            get
            {
                if (this.IOBFollower)
                {
                    if (IOBLeader is PlayerMobile)
                    {
                        PlayerMobile pm = (PlayerMobile)IOBLeader;
                        if (pm.IOBRank >= IOBRank.SecondTier)
                        {
                            return m_iControlSlots;
                        }
                        else if (pm.IOBRank == IOBRank.FirstTier)
                        {
                            return (m_iControlSlots + ((m_iControlSlots + 1) / 2)); //round up for 1.5X
                        }
                        else
                        {
                            return (m_iControlSlots * 2);
                        }
                    }
                }

                //default is standard if !IOBFollower (or if IOBLeader ! PlayerMobile)

                return (int)Math.Max(1, m_iControlSlots + Math.Floor(m_ControlSlotModifier));
                //return m_iControlSlots;
            }
            set
            {
                m_iControlSlots = value;
            }
        }

        // wea: 18/Mar/2007 Added new rarity property
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual short Rarity             // virtual, so overridable ;)
        {
            get
            {
                // Base this check on the creature's barding difficulty 
                double working = BaseInstrument.GetCreatureDifficulty(this);

                // Dragons are over 100... shadow wyrms are 140... i'm going to approximate to 5%
                // of half the creature difficulty
                working /= 20.0;

                // Cap it at 10
                if (working > 10.0)
                    working = 10.0;

                // Dont mess around with lower life forms - they're common 
                if (working < 1.0)
                    working = 0.0;

                // Return what we've worked out converted to an integer
                return Convert.ToInt16(working);
            }
        }

        public virtual bool NoHouseRestrictions { get { return false; } }
        public virtual bool IsHouseSummonable { get { return false; } }

        public virtual int Feathers { get { return 0; } set { } }
        public virtual int Wool { get { return 0; } set { } }

        public virtual MeatType MeatType { get { return MeatType.Ribs; } }
        public virtual int Meat { get { return 0; } set { } }

        public virtual int Hides { get { return 0; } set { } }
        public virtual HideType HideType { get { return HideType.Regular; } }

        public virtual int Scales { get { return 0; } set { } }
        public virtual ScaleType ScaleType { get { return ScaleType.Red; } }

        public virtual bool AutoDispel { get { return false; } }

        public virtual int FishSteaks { get { return 0; } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool IsScaryToPets { get { return GetFlag(CreatureFlags.ScaryToPets); } set { SetFlag(CreatureFlags.ScaryToPets, value); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool IsScaredOfScaryThings { get { return GetFlag(CreatureFlags.ScaredOfScaryThings); } set { SetFlag(CreatureFlags.ScaredOfScaryThings, value); } }

        public virtual bool IsScaryCondition() { return true; } //defualt to always scary but overridable for control

        public virtual bool CanRummageCorpses { get { return false; } }
        //return if creature is immune to the weapon
        public virtual void CheckWeaponImmunity(BaseWeapon wep, int damagein, out int damage)
        {
            damage = damagein;
        }

        public virtual void CheckSpellImmunity(SpellDamageType s, double damagein, out double damage)
        {
            damage = damagein;
        }

        public virtual void OnGotMeleeAttack(Mobile attacker)
        {
            if (AutoDispel && attacker is BaseCreature && ((BaseCreature)attacker).Summoned && !((BaseCreature)attacker).IsAnimatedDead)
                Dispel(attacker);
        }

        public virtual void Dispel(Mobile m)
        {
            Effects.SendLocationParticles(EffectItem.Create(m.Location, m.Map, EffectItem.DefaultDuration), 0x3728, 8, 20, 5042);
            Effects.PlaySound(m, m.Map, 0x201);

            m.Delete();
        }

        public virtual bool DeleteOnRelease { get { return m_bSummoned; } }

        public virtual void OnGaveMeleeAttack(Mobile defender)
        {
            Poison p = HitPoison;
            bool bWasPoisoned = defender.Poisoned;

            if (p != null && HitPoisonChance >= Utility.RandomDouble())
                defender.ApplyPoison(this, p);

            // Adam: add a chance to gain if we poisoned the defender on this hit
            if (defender.Poisoned == true)
                if (bWasPoisoned == false)
                    CheckSkill(SkillName.Poisoning, 0, 100);    // Passively check Poisoning for gain

            if (AutoDispel && defender is BaseCreature && ((BaseCreature)defender).Summoned && !((BaseCreature)defender).IsAnimatedDead)
                Dispel(defender);
        }

        public override void OnAfterDelete()
        {
            if (m_AI != null)
            {
                if (m_AI.m_Timer != null)
                    m_AI.m_Timer.Stop();

                m_AI = null;
            }

            FocusMob = null;

            //if ( IsAnimatedDead )
            //Spells.Necromancy.AnimateDeadSpell.Unregister( m_SummonMaster, this );

            base.OnAfterDelete();
        }

        #region DEBUG_SAY
        // Adam: Smart debug output. (anti spam implementation)
        //	Don't print the string unless it changes, or 5 seconds has passed.
        private string lastDebugString = null;
        private DateTime lastDebugStringTime = DateTime.UtcNow;
        private void DebugOut(string text)
        {
            TimeSpan sx = DateTime.UtcNow - lastDebugStringTime;
            if (lastDebugString != text || sx.TotalSeconds > 5)
            {
                this.PublicOverheadMessage(MessageType.Regular, 41, false, text);
                lastDebugString = text;
                lastDebugStringTime = DateTime.UtcNow;
            }
        }

        public void DebugSay(string text)
        {
            if (Debug)
                DebugOut(text);
        }

        public void DebugSay(string format, params object[] args)
        {
            if (Debug)
                DebugSay(String.Format(format, args));
        }

        public void DebugSay(CreatureFlags flags, string text)
        {
            if ((m_Flags & flags) != 0)
                DebugOut(text);
        }

        public void DebugSay(CreatureFlags flags, string format, params object[] args)
        {
            if ((m_Flags & flags) != 0)
                DebugSay(flags, String.Format(format, args));
        }

        #endregion DEBUG_SAY

        /*
		 * Will need to be givent a better name
		 *
		 * This function can be overriden.. so a "Strongest" mobile, can have a different definition depending
		 * on who check for value
		 * -Could add a FightMode.Prefered
		 *
		 */
        public virtual double GetValueFrom(Mobile m, FightMode acqType, bool bPlayerOnly)
        {
            if ((bPlayerOnly && m.Player) || !bPlayerOnly)
            {
                switch (acqType)
                {
                    case FightMode.Strongest:
                        return (m.Skills[SkillName.Tactics].Value + m.Str); //returns strongest mobile

                    case FightMode.Weakest:
                        return -m.Hits; // returns weakest mobile

                    default:
                        return -GetDistanceToSqrt(m); // returns closest mobile
                }
            }
            else
            {
                return double.MinValue;
            }
        }

        // Turn, - for left, + for right
        // Basic for now, need works
        public virtual void Turn(int iTurnSteps)
        {
            int v = (int)Direction;

            Direction = (Direction)((((v & 0x7) + iTurnSteps) & 0x7) | (v & 0x80));
        }

        public virtual void TurnInternal(int iTurnSteps)
        {
            int v = (int)Direction;

            SetDirection((Direction)((((v & 0x7) + iTurnSteps) & 0x7) | (v & 0x80)));
        }

        public bool IsHurt()
        {
            return (Hits != HitsMax);
        }

        public double GetHomeDistance()
        {
            return GetDistanceToSqrt(m_pHome);
        }

        public virtual int GetTeamSize(int iRange)
        {
            int iCount = 0;

            IPooledEnumerable eable = this.GetMobilesInRange(iRange);
            foreach (Mobile m in eable)
            {
                if (m is BaseCreature)
                {
                    if (((BaseCreature)m).Team == Team)
                    {
                        if (!m.Deleted)
                        {
                            if (m != this)
                            {
                                if (CanSee(m))
                                {
                                    iCount++;
                                }
                            }
                        }
                    }
                }
            }
            eable.Free();

            return iCount;
        }

        // Do my combatant is attaking me??
        public bool IsCombatantAnAggressor()
        {
            if (Combatant != null)
            {
                if (Combatant.Combatant == this)
                {
                    return true;
                }
            }
            return false;
        }

        private class IOBLeadEntry : ContextMenuEntry
        {
            private BaseCreature m_Mobile;

            public IOBLeadEntry(Mobile from, BaseCreature creature)
                : base(6116, 6) // join
            {
                m_Mobile = creature;
                Enabled = true;
            }
            public override void OnClick()
            {
                if (!Owner.From.CheckAlive())
                {
                    return;
                }

                if (Owner.From is PlayerMobile)
                {
                    PlayerMobile pm = (PlayerMobile)Owner.From;
                    if (m_Mobile.IOBAlignment == pm.IOBAlignment)
                    {
                        m_Mobile.AttemptIOBJoin(pm);
                    }
                }
            }
        }

        private class TameEntry : ContextMenuEntry
        {
            private BaseCreature m_Mobile;

            public TameEntry(Mobile from, BaseCreature creature)
                : base(6130, 6)
            {
                m_Mobile = creature;

                Enabled = Enabled && (from.Female ? creature.AllowFemaleTamer : creature.AllowMaleTamer);
            }

            public override void OnClick()
            {
                if (!Owner.From.CheckAlive())
                    return;

                Owner.From.TargetLocked = true;
                SkillHandlers.AnimalTaming.DisableMessage = true;

                if (Owner.From.UseSkill(SkillName.AnimalTaming))
                    Owner.From.Target.Invoke(Owner.From, m_Mobile);

                SkillHandlers.AnimalTaming.DisableMessage = false;
                Owner.From.TargetLocked = false;
            }
        }

        public virtual bool CanTeach { get { return false; } }

        public virtual bool CheckTeach(SkillName skill, Mobile from)
        {
            if (!CanTeach)
                return false;

            if (skill == SkillName.Stealth && from.Skills[SkillName.Hiding].Base < 80.0)
                return false;

            if (skill == SkillName.RemoveTrap && (from.Skills[SkillName.Lockpicking].Base < 50.0 || from.Skills[SkillName.DetectHidden].Base < 50.0))
                return false;

            if (!Core.RuleSets.AOSRules() && (skill == SkillName.Focus || skill == SkillName.Chivalry || skill == SkillName.Necromancy))
                return false;

            return true;
        }

        public enum TeachResult
        {
            Success,
            Failure,
            KnowsMoreThanMe,
            KnowsWhatIKnow,
            SkillNotRaisable,
            NotEnoughFreePoints
        }

        public virtual TeachResult CheckTeachSkills(SkillName skill, Mobile m, int maxPointsToLearn, ref int pointsToLearn, bool doTeach)
        {
            if (!CheckTeach(skill, m) || !m.CheckAlive())
                return TeachResult.Failure;

            Skill ourSkill = Skills[skill];
            Skill theirSkill = m.Skills[skill];

            if (ourSkill == null || theirSkill == null)
                return TeachResult.Failure;

            int baseToSet = ourSkill.BaseFixedPoint / 3;

            if (baseToSet > 420)
                baseToSet = 420;
            else if (baseToSet < 200)
                return TeachResult.Failure;

            if (baseToSet > theirSkill.CapFixedPoint)
                baseToSet = theirSkill.CapFixedPoint;

            pointsToLearn = baseToSet - theirSkill.BaseFixedPoint;

            if (maxPointsToLearn > 0 && pointsToLearn > maxPointsToLearn)
            {
                pointsToLearn = maxPointsToLearn;
                baseToSet = theirSkill.BaseFixedPoint + pointsToLearn;
            }

            if (pointsToLearn < 0)
                return TeachResult.KnowsMoreThanMe;

            if (pointsToLearn == 0)
                return TeachResult.KnowsWhatIKnow;

            if (theirSkill.Lock != SkillLock.Up)
                return TeachResult.SkillNotRaisable;

            int freePoints = m.Skills.Cap - m.Skills.Total;
            int freeablePoints = 0;

            if (freePoints < 0)
                freePoints = 0;

            for (int i = 0; (freePoints + freeablePoints) < pointsToLearn && i < m.Skills.Length; ++i)
            {
                Skill sk = m.Skills[i];

                if (sk == theirSkill || sk.Lock != SkillLock.Down)
                    continue;

                freeablePoints += sk.BaseFixedPoint;
            }

            if ((freePoints + freeablePoints) == 0)
                return TeachResult.NotEnoughFreePoints;

            if ((freePoints + freeablePoints) < pointsToLearn)
            {
                pointsToLearn = freePoints + freeablePoints;
                baseToSet = theirSkill.BaseFixedPoint + pointsToLearn;
            }

            if (doTeach)
            {
                int need = pointsToLearn - freePoints;

                for (int i = 0; need > 0 && i < m.Skills.Length; ++i)
                {
                    Skill sk = m.Skills[i];

                    if (sk == theirSkill || sk.Lock != SkillLock.Down)
                        continue;

                    if (sk.BaseFixedPoint < need)
                    {
                        need -= sk.BaseFixedPoint;
                        sk.BaseFixedPoint = 0;
                    }
                    else
                    {
                        sk.BaseFixedPoint -= need;
                        need = 0;
                    }
                }

                /* Sanity check */
                if (baseToSet > theirSkill.CapFixedPoint || (m.Skills.Total - theirSkill.BaseFixedPoint + baseToSet) > m.Skills.Cap)
                    return TeachResult.NotEnoughFreePoints;

                theirSkill.BaseFixedPoint = baseToSet;
            }

            return TeachResult.Success;
        }

        public virtual bool CheckTeachingMatch(Mobile m)
        {
            if (m_Teaching == (SkillName)(-1))
                return false;

            if (m is PlayerMobile)
                return (((PlayerMobile)m).Learning == m_Teaching);

            return true;
        }

        private SkillName m_Teaching = (SkillName)(-1);

        public virtual bool Teach(SkillName skill, Mobile m, int maxPointsToLearn, bool doTeach)
        {
            int pointsToLearn = 0;
            TeachResult res = CheckTeachSkills(skill, m, maxPointsToLearn, ref pointsToLearn, doTeach);

            switch (res)
            {
                case TeachResult.KnowsMoreThanMe:
                    {
                        Say(501508); // I cannot teach thee, for thou knowest more than I!
                        break;
                    }
                case TeachResult.KnowsWhatIKnow:
                    {
                        Say(501509); // I cannot teach thee, for thou knowest all I can teach!
                        break;
                    }
                case TeachResult.NotEnoughFreePoints:
                case TeachResult.SkillNotRaisable:
                    {
                        // Make sure this skill is marked to raise. If you are near the skill cap (700 points) you may need to lose some points in another skill first.
                        m.SendLocalizedMessage(501510, "", 0x22);
                        break;
                    }
                case TeachResult.Success:
                    {
                        if (doTeach)
                        {
                            Say(501539); // Let me show thee something of how this is done.
                            m.SendLocalizedMessage(501540); // Your skill level increases.

                            m_Teaching = (SkillName)(-1);

                            if (m is PlayerMobile)
                                ((PlayerMobile)m).Learning = (SkillName)(-1);
                        }
                        else
                        {
                            // I will teach thee all I know, if paid the amount in full.  The price is:
                            Say(1019077, AffixType.Append, String.Format(" {0}", pointsToLearn), "");
                            Say(1043108); // For less I shall teach thee less.

                            m_Teaching = skill;

                            if (m is PlayerMobile)
                                ((PlayerMobile)m).Learning = skill;
                        }

                        return true;
                    }
            }

            return false;
        }

        public int ReturnConfusedLoyalty(Mobile pet)
        {
            int temployalty = (int)this.Loyalty;

            temployalty /= 2;

            if (temployalty < 1)
                temployalty = 1;

            return temployalty;
        }


        public override void AggressiveAction(Mobile aggressor, bool criminal)
        {
            RefreshLifespan();

            base.AggressiveAction(aggressor, criminal);

            if (m_AI != null)
                m_AI.OnAggressiveAction(aggressor);

            if (aggressor is PlayerMobile)
            {

                PlayerMobile pm = (PlayerMobile)aggressor;

                if (pm == ControlMaster && Paralyzed)
                {

                    this.Loyalty = (PetLoyalty)ReturnConfusedLoyalty(this);
                    LoyaltyCheck = DateTime.UtcNow + TimeSpan.FromHours(1.0); //reset loyalty time, no double drop

                    ControlTarget = null;
                    ControlOrder = OrderType.None;
                }
            }

            if (aggressor is PlayerMobile) //IOB check!
            {
                PlayerMobile pm = (PlayerMobile)aggressor; //Pigpen - Addition for IOB System. Addition End at next Pigpen Comment.

                // wea: log harmful actions committed by master (initiation only)
                if (pm == ControlMaster && aggressor.ChangingCombatant && CoreAI.TempInt == 2)
                {
                    // Log this
                    LogHelper Logger = new LogHelper("allguardbug.log", false, true);
                    Logger.Log(LogType.Text, string.Format("{0}:{1}:{2}:{3}", pm, pm, "Aggress", this));
                    Logger.Finish();

                    // Send a message to all staff in range
                    IPooledEnumerable eable = this.GetClientsInRange(75);
                    foreach (NetState state in eable)
                    {
                        if (state.Mobile.AccessLevel >= AccessLevel.Counselor)
                            this.PrivateOverheadMessage(MessageType.Regular, 123, true, "My master just aggressed me!", state);
                    }
                }

                //Only punish actions towards Non-Controlled NPCS (no tames, no summons, no hires)
                if (IOBSystem.IsFriend(this, pm)
                    && !Tamable
                    && !Summoned
                    && !(this is BaseHire)
                    )
                {
                    //reset Kin Aggression time
                    pm.OnKinAggression();

                    //IF we have a IOBItem equipped, delete it
                    if (pm.IOBEquipped)
                    {
                        Item[] items = new Item[14];
                        items[0] = aggressor.FindItemOnLayer(Layer.Shoes);
                        items[1] = aggressor.FindItemOnLayer(Layer.Pants);
                        items[2] = aggressor.FindItemOnLayer(Layer.Shirt);
                        items[3] = aggressor.FindItemOnLayer(Layer.Helm);
                        items[4] = aggressor.FindItemOnLayer(Layer.Gloves);
                        items[5] = aggressor.FindItemOnLayer(Layer.Neck);
                        items[6] = aggressor.FindItemOnLayer(Layer.Waist);
                        items[7] = aggressor.FindItemOnLayer(Layer.InnerTorso);
                        items[8] = aggressor.FindItemOnLayer(Layer.MiddleTorso);
                        items[9] = aggressor.FindItemOnLayer(Layer.Arms);
                        items[10] = aggressor.FindItemOnLayer(Layer.Cloak);
                        items[11] = aggressor.FindItemOnLayer(Layer.OuterTorso);
                        items[12] = aggressor.FindItemOnLayer(Layer.OuterLegs);
                        items[13] = aggressor.FindItemOnLayer(Layer.InnerLegs);

                        bool bDeleteItem = false;
                        for (int i = 0; i <= 13; i++)
                        {
                            bDeleteItem = false;
                            if (items[i] is BaseClothing)
                            {
                                if (((BaseClothing)items[i]).IOBAlignment == this.IOBAlignment)
                                {
                                    bDeleteItem = true;
                                    pm.IOBEquipped = false;
                                }
                            }
                            if (items[i] is BaseArmor)
                            {
                                if (((BaseArmor)items[i]).IOBAlignment == this.IOBAlignment)
                                {
                                    bDeleteItem = true;
                                    pm.IOBEquipped = false;
                                }
                            }

                            if (bDeleteItem)
                            {
                                items[i].Delete();
                            }
                        }

                        AOS.Damage(aggressor, 50, 0, 100, 0, 0, 0, aggressor);
                        aggressor.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);
                        aggressor.PlaySound(0x307);

                        //Force into peace mode
                        aggressor.Warmode = false;
                    }
                }

            }

            StopFlee();

            ForceReAcquire();

            OrderType ct = ControlOrder;

            //erl: % chance to ignore re-aggressive acts (no change of combatant) when not in fight mode
            if ((m_ControlMaster != aggressor) &&
                (aggressor.ChangingCombatant || Utility.RandomDouble() > CoreAI.ReaggressIgnoreChance) &&
                (m_bControlled || m_bSummoned) &&
                (ct == OrderType.Come || ct == OrderType.Stay || ct == OrderType.Stop || ct == OrderType.None || ct == OrderType.Follow))
            {
                AttackOrderHack(aggressor);
            }
        }

        protected virtual void AttackOrderHack(Mobile aggressor)
        {
            // don't attack Scary creatures if we are ScaredOfScaryThings
            if (aggressor is BaseCreature && (((BaseCreature)aggressor).IsScaryToPets && ((BaseCreature)aggressor).IsScaryCondition()) && this.IsScaredOfScaryThings)
                return;

            ControlTarget = aggressor;
            ControlOrder = OrderType.Attack;
        }

        public virtual void AddCustomContextEntries(Mobile from, ArrayList list)
        {
        }

        public void IOBDismiss(bool bSuicide)
        {
            try
            {
                if (this.Spawner != null)
                {
                    if ((DateTime.UtcNow - IOBTimeAcquired) < (this.Spawner.MaxDelay))
                    {
                        ((PlayerMobile)IOBLeader).IOBJoinRestrictionTime = (IOBTimeAcquired + this.Spawner.MaxDelay);
                    }
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

            Controlled = false;
            ControlMaster = null;
            ControlOrder = OrderType.None;
            //note! Controled/ControlMaster MUST be set to false/none before IOBFollower/IOBLeader
            IOBFollower = false;
            IOBLeader = null;

            if (bSuicide)
            {
                new SuicideTimer(this).Start();
            }
        }

        private class SuicideTimer : Timer
        {
            private int m_tick;
            BaseCreature m_Creature;

            public SuicideTimer(BaseCreature bc)
                : base(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(2.0))
            {
                Priority = TimerPriority.OneSecond;
                m_tick = 0;
                m_Creature = bc;

                m_Creature.FightMode = FightMode.None;
                m_Creature.AI = AIType.AI_Animal;
            }

            protected override void OnTick()
            {
                switch (m_tick)
                {
                    case 0:
                        m_Creature.Say("So you find my services unsatisfactory?");
                        break;
                    case 1:
                        m_Creature.Say("Then I can not go on living...");
                        break;
                    case 2:
                        m_Creature.Emote("*takes poison*");
                        break;
                    case 3:
                        m_Creature.AI = AIType.AI_Suicide;
                        break;
                    default:
                        this.Stop();
                        break;
                }
                m_tick++;
            }
        }

        public void AttemptIOBDismiss()
        {
            ControlMaster.SendMessage("You have dismissed " + Name);
            IOBDismiss(true);
        }

        public void AttemptIOBJoin(PlayerMobile pm)
        {
            if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.IOBJoinEnabled) == false)
                return;

            if (this.Tamable == false && this.AI != AIType.AI_Suicide) //safety check
            {
                if (pm.IOBEquipped) //MUST have IOB Equipped
                {
                    int modifiedcontrolslots = m_iControlSlots;
                    if (pm.IOBRank >= IOBRank.SecondTier)
                    {
                        modifiedcontrolslots = m_iControlSlots;
                    }
                    else if (pm.IOBRank == IOBRank.FirstTier)
                    {
                        modifiedcontrolslots = (m_iControlSlots + ((m_iControlSlots + 1) / 2)); //round up for 1.5X
                    }
                    else
                    {
                        modifiedcontrolslots = (m_iControlSlots * 2);
                    }

                    if ((modifiedcontrolslots + pm.Followers <= pm.FollowersMax) &&
                        (this.IOBAlignment == pm.IOBAlignment) && //safety check
                        (this.IOBFollower == false)) //safety check
                    {
                        if (pm.IOBJoinRestrictionTime < DateTime.UtcNow)
                        {
                            this.IOBFollower = true;
                            this.IOBLeader = pm;
                            this.ControlMaster = pm;
                            this.Controlled = true;

                            pm.SendMessage(this.Name + " has joined you.");
                            this.IOBTimeAcquired = DateTime.UtcNow;
                        }
                        else
                        {
                            pm.SendMessage(this.Name + " refuses to join right now.");
                        }
                    }
                    else
                    {
                        pm.SendMessage("Your rank is not high enough to control this bretheren.");
                    }
                }
                else
                {
                    pm.SendMessage("This bretheren isn't fooled into following you.");
                }
            }
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (m_AI != null && Commandable)
                m_AI.GetContextMenuEntries(from, list);

            if (m_bTamable && !m_bControlled && from.Alive)
                list.Add(new TameEntry(from, this));

            if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.IOBJoinEnabled))
            {
                if (this.IOBAlignment != IOBAlignment.None && !this.IOBFollower && !this.Tamable && !this.Summoned)
                {
                    if (from is PlayerMobile)
                    {
                        if (((PlayerMobile)from).IOBAlignment == this.IOBAlignment && ((PlayerMobile)from).IOBEquipped)
                        {
                            list.Add(new IOBLeadEntry(from, this));
                        }
                    }
                }
            }

            AddCustomContextEntries(from, list);

            if (CanTeach && from.Alive)
            {
                Skills ourSkills = this.Skills;
                Skills theirSkills = from.Skills;

                for (int i = 0; i < ourSkills.Length && i < theirSkills.Length; ++i)
                {
                    Skill skill = ourSkills[i];
                    Skill theirSkill = theirSkills[i];

                    if (skill != null && theirSkill != null && skill.Base >= 60.0 && CheckTeach(skill.SkillName, from))
                    {
                        double toTeach = skill.Base / 3.0;

                        if (toTeach > 42.0)
                            toTeach = 42.0;

                        list.Add(new TeachEntry((SkillName)i, this, from, (toTeach > theirSkill.Base)));
                    }
                }
            }
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            InhumanSpeech speechType = this.SpeechType;

            if (speechType != null && (speechType.Flags & IHSFlags.OnSpeech) != 0 && from.InRange(this, 3))
                return true;

            return (m_AI != null && m_AI.HandlesOnSpeech(from) && from.InRange(this, m_iRangePerception));
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            InhumanSpeech speechType = this.SpeechType;

            if (speechType != null && speechType.OnSpeech(this, e.Mobile, e.Speech))
                e.Handled = true;
            else if (!e.Handled && m_AI != null && e.Mobile.InRange(this, m_iRangePerception))
                m_AI.OnSpeech(e);
        }

        public override bool IsHarmfulCriminal(Mobile target)
        {
            if ((Controlled && target == m_ControlMaster) || (Summoned && target == m_SummonMaster))
                return false;

            if (target is BaseCreature
                && ((BaseCreature)target).InitialInnocent
                && ((BaseCreature)target).Controlled == false)
            {
                return false;
            }

            if (target is PlayerMobile && ((PlayerMobile)target).PermaFlags.Count > 0)
                return false;

            return base.IsHarmfulCriminal(target);
        }

        public override void CriminalAction(bool message)
        {
            base.CriminalAction(message);

            if ((Controlled || Summoned))
            {
                if (m_ControlMaster != null && m_ControlMaster.Player)
                    m_ControlMaster.CriminalAction(false);
                else if (m_SummonMaster != null && m_SummonMaster.Player)
                    m_SummonMaster.CriminalAction(false);
            }
        }

        public override void DoHarmful(Mobile target, bool indirect)
        {
            base.DoHarmful(target, indirect);

            if (target == this || target == m_ControlMaster || target == m_SummonMaster || (!Controlled && !Summoned))
                return;

            List<AggressorInfo> list = this.Aggressors;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo ai = (AggressorInfo)list[i];

                if (ai.Attacker == target)
                    return;
            }

            list = this.Aggressed;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo ai = (AggressorInfo)list[i];

                if (ai.Defender == target)
                {
                    if (m_ControlMaster != null && m_ControlMaster.Player && m_ControlMaster.CanBeHarmful(target, false))
                        m_ControlMaster.DoHarmful(target, true);
                    else if (m_SummonMaster != null && m_SummonMaster.Player && m_SummonMaster.CanBeHarmful(target, false))
                        m_SummonMaster.DoHarmful(target, true);

                    return;
                }
            }
        }

        private static Mobile m_NoDupeGuards;

        public void ReleaseGuardDupeLock()
        {
            m_NoDupeGuards = null;
        }

        public void ReleaseGuardLock()
        {
            EndAction(typeof(GuardedRegion));
        }

        private DateTime m_IdleReleaseTime;

        public virtual bool CheckIdle()
        {
            if (Combatant != null)
                return false; // in combat.. not idling

            if (m_IdleReleaseTime > DateTime.MinValue)
            {
                // idling...

                if (DateTime.UtcNow >= m_IdleReleaseTime)
                {
                    m_IdleReleaseTime = DateTime.MinValue;
                    return false; // idle is over
                }

                return true; // still idling
            }

            if (95 > Utility.Random(100))
                return false; // not idling, but don't want to enter idle state

            m_IdleReleaseTime = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(15, 25));

            if (Body.IsHuman)
            {
                switch (Utility.Random(2))
                {
                    case 0: Animate(5, 5, 1, true, true, 1); break;
                    case 1: Animate(6, 5, 1, true, false, 1); break;
                }
            }
            else if (Body.IsAnimal)
            {
                switch (Utility.Random(3))
                {
                    case 0: Animate(3, 3, 1, true, false, 1); break;
                    case 1: Animate(9, 5, 1, true, false, 1); break;
                    case 2: Animate(10, 5, 1, true, false, 1); break;
                }
            }
            else if (Body.IsMonster)
            {
                switch (Utility.Random(2))
                {
                    case 0: Animate(17, 5, 1, true, false, 1); break;
                    case 1: Animate(18, 5, 1, true, false, 1); break;
                }
            }

            PlaySound(GetIdleSound());
            return true; // entered idle state
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            base.OnMovement(m, oldLocation);


            if (ReAcquireOnMovement)
                ForceReAcquire();

            InhumanSpeech speechType = this.SpeechType;

            if (speechType != null && m.AccessLevel <= AccessLevel.Player && this.CanSee(m))
                speechType.OnMovement(this, m, oldLocation);

            /* Begin notice sound */
            if (m.Player && m_FightMode != FightMode.Aggressor && m_FightMode != FightMode.None && Combatant == null && !Controlled && !Summoned && m.AccessLevel <= AccessLevel.Player)
            {
                // If this creature defends itself but doesn't actively attack (animal) or
                // doesn't fight at all (vendor) then no notice sounds are played..
                // So, players are only notified of agressive monsters

                // Monsters that are currently fighting are ignored

                // Controled or summoned creatures are ignored

                if (InRange(m.Location, 18) && !InRange(oldLocation, 18))
                {
                    if (Body.IsMonster)
                        Animate(11, 5, 1, true, false, 1);

                    PlaySound(GetAngerSound());
                }
            }
            /* End notice sound */

            if (m_NoDupeGuards == m)
                return;

            if (CanBandage && this.AI != AIType.AI_Suicide)
                doHeal();

            if (!Body.IsHuman || this is BaseGuard || Murderer || AlwaysMurderer || AlwaysAttackable || m.LongTermMurders < 5 || !m.InRange(Location, 12) || !m.Alive)
                return;

            Region reg = this.Region;

            if (reg is GuardedRegion)
            {
                GuardedRegion guardedRegion = (GuardedRegion)reg;

                if (guardedRegion.IsGuarded && guardedRegion.IsGuardCandidate(m) && this.CanSee(m) && BeginAction(typeof(GuardedRegion)))
                {
                    Say(1013037 + Utility.Random(16));
                    guardedRegion.CallGuards(this.Location);

                    Timer.DelayCall(TimeSpan.FromSeconds(5.0), new TimerCallback(ReleaseGuardLock));

                    m_NoDupeGuards = m;
                    Timer.DelayCall(TimeSpan.Zero, new TimerCallback(ReleaseGuardDupeLock));
                }
            }
        }

        public void doHeal()
        {
            int healed;

            if (DateTime.UtcNow >= NextBandageTime)
            {
                if (Poison != null)
                {
                    Poison = null;
                }
                else if (IsHurt())
                {
                    healed = Utility.Random(BandageMin, BandageMax);

                    if (this.HitsMax - this.Hits < healed)
                        healed = this.HitsMax - this.Hits;

                    this.Hits += healed;
                }

                NextBandageTime = DateTime.UtcNow + BandageDelay;
            }

            if (Poison == null && !IsHurt())
                NextBandageTime = DateTime.UtcNow + BandageDelay;
        }


        public void AddSpellAttack(Type type)
        {
            m_arSpellAttack.Add(type);
        }

        public void AddSpellDefense(Type type)
        {
            m_arSpellDefense.Add(type);
        }

        public Spell GetAttackSpellRandom()
        {
            if (m_arSpellAttack.Count > 0)
            {
                Type type = (Type)m_arSpellAttack[Utility.Random(m_arSpellAttack.Count)];

                object[] args = { this, null };
                return Activator.CreateInstance(type, args) as Spell;
            }
            else
            {
                return null;
            }
        }

        public Spell GetDefenseSpellRandom()
        {
            if (m_arSpellDefense.Count > 0)
            {
                Type type = (Type)m_arSpellDefense[Utility.Random(m_arSpellDefense.Count)];

                object[] args = { this, null };
                return Activator.CreateInstance(type, args) as Spell;
            }
            else
            {
                return null;
            }
        }

        public Spell GetSpellSpecific(Type type)
        {
            int i;

            for (i = 0; i < m_arSpellAttack.Count; i++)
            {
                if (m_arSpellAttack[i] == type)
                {
                    object[] args = { this, null };
                    return Activator.CreateInstance(type, args) as Spell;
                }
            }

            for (i = 0; i < m_arSpellDefense.Count; i++)
            {
                if (m_arSpellDefense[i] == type)
                {
                    object[] args = { this, null };
                    return Activator.CreateInstance(type, args) as Spell;
                }
            }

            return null;
        }

        public void SetDamage(int val)
        {
            m_DamageMin = val;
            m_DamageMax = val;
        }

        public void SetDamage(int min, int max)
        {
            m_DamageMin = min;
            m_DamageMax = max;
        }

        public void SetHits(int val)
        {
            SetHits(val, true);
        }

        public void SetHits(int val, bool translate)
        {
            if (val < 1000 && !Core.RuleSets.AOSRules() && translate)
                val = (val * 100) / 60;

            m_HitsMax = val;
            Hits = HitsMax;
        }

        public void SetHits(int min, int max)
        {
            SetHits(min, max, true);
        }

        public void SetHits(int min, int max, bool translate)
        {
            if (min < 1000 && !Core.RuleSets.AOSRules() && translate)
            {
                min = (min * 100) / 60;
                max = (max * 100) / 60;
            }

            m_HitsMax = Utility.RandomMinMax(min, max);
            Hits = HitsMax;
        }

        public void SetStam(int val)
        {
            m_StamMax = val;
            Stam = StamMax;
        }

        public void SetStam(int min, int max)
        {
            m_StamMax = Utility.RandomMinMax(min, max);
            Stam = StamMax;
        }

        public void SetMana(int val)
        {
            m_ManaMax = val;
            Mana = ManaMax;
        }

        public void SetMana(int min, int max)
        {
            m_ManaMax = Utility.RandomMinMax(min, max);
            Mana = ManaMax;
        }

        public void SetStr(int val)
        {
            RawStr = val;
            Hits = HitsMax;
        }

        public void SetStr(int min, int max)
        {
            RawStr = Utility.RandomMinMax(min, max);
            Hits = HitsMax;
        }

        public void SetDex(int val)
        {
            RawDex = val;
            Stam = StamMax;
        }

        public void SetDex(int min, int max)
        {
            RawDex = Utility.RandomMinMax(min, max);
            Stam = StamMax;
        }

        public void SetInt(int val)
        {
            RawInt = val;
            Mana = ManaMax;
        }

        public void SetInt(int min, int max)
        {
            RawInt = Utility.RandomMinMax(min, max);
            Mana = ManaMax;
        }

        public void SetSkill(SkillName name, double val)
        {
            Skills[name].BaseFixedPoint = (int)(val * 10);
        }

        public void SetSkill(SkillName name, double min, double max)
        {
            int minFixed = (int)(min * 10);
            int maxFixed = (int)(max * 10);

            Skills[name].BaseFixedPoint = Utility.RandomMinMax(minFixed, maxFixed);
        }

        public void SetFameLevel(int level)
        {
            switch (level)
            {
                case 1: Fame = Utility.RandomMinMax(0, 1249); break;
                case 2: Fame = Utility.RandomMinMax(1250, 2499); break;
                case 3: Fame = Utility.RandomMinMax(2500, 4999); break;
                case 4: Fame = Utility.RandomMinMax(5000, 9999); break;
                case 5: Fame = Utility.RandomMinMax(10000, 10000); break;
            }
        }

        public void SetKarmaLevel(int level)
        {
            switch (level)
            {
                case 0: Karma = -Utility.RandomMinMax(0, 624); break;
                case 1: Karma = -Utility.RandomMinMax(625, 1249); break;
                case 2: Karma = -Utility.RandomMinMax(1250, 2499); break;
                case 3: Karma = -Utility.RandomMinMax(2500, 4999); break;
                case 4: Karma = -Utility.RandomMinMax(5000, 9999); break;
                case 5: Karma = -Utility.RandomMinMax(10000, 10000); break;
            }
        }

        public static void Cap(ref int val, int min, int max)
        {
            if (val < min)
                val = min;
            else if (val > max)
                val = max;
        }

        public void PackPotion()
        {
            PackItem(Loot.RandomPotion());
        }

        public void PackPotion(double chance)
        {
            if (chance <= Utility.RandomDouble())
                return;

            PackItem(Loot.RandomPotion());
        }

        public void PackNecroScroll(int index)
        {
            if (!Core.RuleSets.AOSRules() || 0.05 <= Utility.RandomDouble())
                return;

            PackItem(Loot.Construct(Loot.NecromancyScrollTypes, index));
        }

        public void PackScroll(int minCircle, int maxCircle, double chance)
        {
            if (chance <= Utility.RandomDouble())
                return;

            PackScroll(minCircle, maxCircle);
        }
        public void PackScroll(int minCircle, int maxCircle)
        {
            PackScroll(Utility.RandomMinMax(minCircle, maxCircle));
        }

        public void PackScroll(int circle)
        {
            int min = (circle - 1) * 8;

            PackItem(Loot.RandomScroll(min, min + 7, SpellbookType.Regular));
        }

        public void PackMagicStuff(int minLevel, int maxLevel, double chance)
        {
            switch (Utility.Random(3))
            {
                case 0:
                    PackArmor(minLevel, maxLevel, chance);
                    break;
                case 1:
                    PackWeapon(minLevel, maxLevel, chance);
                    break;
                case 2:
                    PackMagicItem(minLevel, maxLevel, chance);
                    break;
            }
        }

        public void PackMagicEquipment(int minLevel, int maxLevel)
        {
            PackMagicEquipment(minLevel, maxLevel, 0.30, 0.15);
        }

        public void PackMagicEquipment(int minLevel, int maxLevel, double armorChance, double weaponChance)
        {
            if (Utility.RandomBool())
                PackArmor(minLevel, maxLevel, armorChance);
            else
                PackWeapon(minLevel, maxLevel, weaponChance);

        }

        public void PackMagicItem(int minLevel, int maxLevel)
        {
            PackMagicItem(minLevel, maxLevel, 0.30);
        }

        public void PackMagicItem(int minLevel, int maxLevel, double chance)
        {
            if (chance <= Utility.RandomDouble())
                return;

            Item item = Loot.RandomClothingOrJewelry();

            if (item == null)
                return;

            if (item is BaseClothing)
                ((BaseClothing)item).SetRandomMagicEffect(minLevel, maxLevel);
            else if (item is BaseJewel)
                ((BaseJewel)item).SetRandomMagicEffect(minLevel, maxLevel);

            PackItem(item);
        }

        public void PackMagicJewelry(int minLevel, int maxLevel, double chance)
        {
            if (chance <= Utility.RandomDouble())
                return;

            Item item = Loot.RandomJewelry();

            if (item == null)
                return;

            ((BaseJewel)item).SetRandomMagicEffect(minLevel, maxLevel);

            PackItem(item);
        }

        protected bool m_Spawning;
        protected int m_KillersLuck;
        protected bool Spawning { get { return m_Spawning; } }

        public virtual void GenerateLoot(bool spawning)
        {
            m_Spawning = spawning;

            if (!spawning)
                m_KillersLuck = LootPack.GetLuckChanceForKiller(this);

            if (spawning == true && (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules()))
                throw new ApplicationException("You cannot call GenerateLoot() at spawn time for Angel Island.");

            GenerateLoot();

            m_Spawning = false;
            m_KillersLuck = 0;
        }

        public virtual List<string[]> GetAnimalLorePages()
        {
            return null;
        }

        public virtual void GenerateLoot()
        {
        }

        public virtual int GetGold()
        {
            Container pack = Backpack;

            if (pack != null)
            {
                // how much gold is on the creature?
                int iAmountInPack = 0;
                Item[] golds = pack.FindItemsByType(typeof(Gold), true);
                foreach (Item g in golds)
                {
                    iAmountInPack += g.Amount;
                }

                return iAmountInPack;
            }

            return 0;
        }

        // spawners can now spawn special lootpacks with per item drop rates.
        public virtual void SpawnerLoot(Spawner spawner)
        {
            LogHelper Logger = null;

            try
            {
                if (spawner == null || spawner.Deleted == true)
                    return;

                if (spawner.LootPack == null || spawner.LootPack.Deleted == true)
                    return;

                Logger = new LogHelper("LootPack.log", false, true);

                if (spawner.LootPack is Container)
                {
                    if ((spawner.LootPack as Container).Factory)
                    {   // only one item from factory has a chance at a drop
                        if ((spawner.LootPack as Container).DropRate >= Utility.RandomDouble())
                        {
                            if ((spawner.LootPack as Container).Items.Count > 0)
                            {
                                Item item = (spawner.LootPack as Container).Items[Utility.Random((spawner.LootPack as Container).Items.Count)] as Item;
                                Item temp = RareFactory.DupeItem(item);
                                if (temp != null)
                                {
                                    Logger.Log(LogType.Mobile, this, string.Format("Dropped lootpack item {0}", temp));
                                    temp.DropRate = 1.0;    // should not be set, but lets be safe
                                    PackItem(temp);
                                }
                            }
                        }
                    }
                    else
                    {   // each item from a container has a chance at a drop
                        foreach (Item item in spawner.LootPack.Items)
                        {   // drop chance
                            if (item.DropRate >= Utility.RandomDouble())
                            {
                                Item temp = RareFactory.DupeItem(item);
                                if (temp != null)
                                {
                                    Logger.Log(LogType.Mobile, this, string.Format("Dropped lootpack item {0}", temp));
                                    temp.DropRate = 1.0;    // all this does is save the sizeof(double) for each item generated
                                    PackItem(temp);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // drop chance
                    if (spawner.LootPack.DropRate >= Utility.RandomDouble())
                    {
                        Item temp = RareFactory.DupeItem(RareFactory.DupeItem(spawner.LootPack));
                        if (temp != null)
                        {
                            Logger.Log(LogType.Mobile, this, string.Format("Dropped lootpack item {0}", temp));
                            temp.DropRate = 1.0;    // all this does is save the sizeof(double) for each item generated
                            PackItem(temp);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
            }
            finally
            {
                if (Logger != null)
                    Logger.Finish();
            }
        }

        public virtual void AddLoot(LootPack pack, int amount)
        {
            for (int i = 0; i < amount; ++i)
                AddLoot(pack);
        }

        public virtual void AddLoot(LootPack pack)
        {
            if (Summoned)
                return;

            Container backpack = Backpack;

            if (backpack == null)
            {
                backpack = new Backpack();

                backpack.Movable = false;

                AddItem(backpack);
            }

            pack.Generate(this, backpack, m_Spawning, m_KillersLuck);
        }

        public bool PackArmor(int minLevel, int maxLevel)
        {
            return PackArmor(minLevel, maxLevel, 1.0);
        }

        public bool PackArmor(int minLevel, int maxLevel, double chance)
        {
            if (chance <= Utility.RandomDouble())
                return false;

            if (maxLevel > 3)
                maxLevel = 3;

            Cap(ref minLevel, 0, 3);
            Cap(ref maxLevel, 0, 3);

            /*
			 * Old Code, commented by mith in favor of the new Magic Item system
			 * if ( Core.RuleSets.AOSRules() )
			 * {
			 * 		Item item = Loot.RandomArmorOrShieldOrJewelry();
			 *
			 * 		if ( item == null )
			 * 			return false;
			 *
			 * 		int attributeCount, min, max;
			 * 		GetRandomAOSStats( minLevel, maxLevel, out attributeCount, out min, out max );
			 *
			 * 		if ( item is BaseArmor )
			 *			BaseRunicTool.ApplyAttributesTo( (BaseArmor)item, attributeCount, min, max );
			 *		else if ( item is BaseJewel )
			 *			BaseRunicTool.ApplyAttributesTo( (BaseJewel)item, attributeCount, min, max );
			 *
			 *		PackItem( item );
			 * }
			 * else
			 * {
			 *		BaseArmor armor = Loot.RandomArmorOrShield();
			 *		if ( armor == null )
			 *			return false;
			 *
			 *		armor.ProtectionLevel = (ArmorProtectionLevel)RandomMinMaxScaled( minLevel, maxLevel );
			 *		armor.Durability = (ArmorDurabilityLevel)RandomMinMaxScaled( 0, maxLevel );
			 *
			 *		PackItem( armor );
			 * }
			 */

            BaseArmor armor = Loot.RandomArmorOrShield();

            if (armor == null)
                return false;

            ((BaseArmor)armor).ProtectionLevel = (ArmorProtectionLevel)RandomMinMaxScaled(minLevel, maxLevel);
            ((BaseArmor)armor).Durability = (ArmorDurabilityLevel)RandomMinMaxScaled(0, maxLevel);

            PackItem(armor);

            return true;
        }

        public static void GetRandomAOSStats(int minLevel, int maxLevel, out int attributeCount, out int min, out int max)
        {
            int v = RandomMinMaxScaled(minLevel, maxLevel);

            if (v >= 5)
            {
                attributeCount = Utility.RandomMinMax(2, 6);
                min = 20; max = 70;
            }
            else if (v == 4)
            {
                attributeCount = Utility.RandomMinMax(2, 4);
                min = 20; max = 50;
            }
            else if (v == 3)
            {
                attributeCount = Utility.RandomMinMax(2, 3);
                min = 20; max = 40;
            }
            else if (v == 2)
            {
                attributeCount = Utility.RandomMinMax(1, 2);
                min = 10; max = 30;
            }
            else
            {
                attributeCount = 1;
                min = 10; max = 20;
            }
        }

        public static int RandomMinMaxScaled(int min, int max)
        {
            if (min == max)
                return min;

            if (min > max)
            {
                int hold = min;
                min = max;
                max = hold;
            }

            /* Example:
			 *    min: 1
			 *    max: 5
			 *  count: 5
			 *
			 * total = (5*5) + (4*4) + (3*3) + (2*2) + (1*1) = 25 + 16 + 9 + 4 + 1 = 55
			 *
			 * chance for min+0 : 25/55 : 45.45%
			 * chance for min+1 : 16/55 : 29.09%
			 * chance for min+2 :  9/55 : 16.36%
			 * chance for min+3 :  4/55 :  7.27%
			 * chance for min+4 :  1/55 :  1.81%
			 */

            int count = max - min + 1;
            int total = 0, toAdd = count;

            for (int i = 0; i < count; ++i, --toAdd)
                total += toAdd * toAdd;

            int rand = Utility.Random(total);
            toAdd = count;

            int val = min;

            for (int i = 0; i < count; ++i, --toAdd, ++val)
            {
                rand -= toAdd * toAdd;

                if (rand < 0)
                    break;
            }

            return val;
        }

        public bool PackSlayer()
        {
            return PackSlayer(0.05);
        }

        public bool PackSlayer(double chance)
        {
            if (chance <= Utility.RandomDouble())
                return false;

            if (Utility.RandomBool())
            {
                BaseInstrument instrument = Loot.RandomInstrument();

                if (instrument != null)
                {
                    instrument.Slayer = SlayerGroup.GetLootSlayerType(GetType());
                    PackItem(instrument);
                }
            }
            else if (!Core.RuleSets.AOSRules())
            {
                BaseWeapon weapon = Loot.RandomWeapon();

                if (weapon != null)
                {
                    weapon.Slayer = SlayerGroup.GetLootSlayerType(GetType());
                    PackItem(weapon);
                }
            }

            return true;
        }

        public BaseWeapon PackSlayerWeapon(double chance)
        {
            if (chance <= Utility.RandomDouble())
                return null;

            BaseWeapon weapon = Loot.RandomWeapon();
            if (weapon != null)
            {
                weapon.Slayer = BaseRunicTool.GetRandomSlayer();
                PackItem(weapon);
            }

            return weapon;
        }


        // adam: Pack a random slayer Instrument, not tied to the current creature.
        public bool PackSlayerInstrument(double chance)
        {
            if (chance <= Utility.RandomDouble())
                return false;

            BaseInstrument instrument = Loot.RandomInstrument();

            if (instrument != null)
            {
                instrument.Slayer = BaseRunicTool.GetRandomSlayer();
                PackItem(instrument);
            }

            return true;
        }

        public bool PackWeapon(int minLevel, int maxLevel)
        {
            return PackWeapon(minLevel, maxLevel, 1.0);
        }

        public bool PackWeapon(int minLevel, int maxLevel, double chance)
        {
            if (chance <= Utility.RandomDouble())
                return false;

            if (maxLevel > 3)
                maxLevel = 3;

            Cap(ref minLevel, 0, 3);
            Cap(ref maxLevel, 0, 3);

            if (Core.RuleSets.AOSRules())
            {
                Item item = Loot.RandomWeaponOrJewelry();

                if (item == null)
                    return false;

                int attributeCount, min, max;
                GetRandomAOSStats(minLevel, maxLevel, out attributeCount, out min, out max);

                if (item is BaseWeapon)
                    BaseRunicTool.ApplyAttributesTo((BaseWeapon)item, attributeCount, min, max);
                else if (item is BaseJewel)
                    BaseRunicTool.ApplyAttributesTo((BaseJewel)item, attributeCount, min, max);

                PackItem(item);
            }
            else
            {
                BaseWeapon weapon = Loot.RandomWeapon();

                if (weapon == null)
                    return false;

                if (0.05 > Utility.RandomDouble())
                    weapon.Slayer = SlayerName.Silver;

                weapon.DamageLevel = (WeaponDamageLevel)RandomMinMaxScaled(minLevel, maxLevel);
                weapon.AccuracyLevel = (WeaponAccuracyLevel)RandomMinMaxScaled(0, maxLevel);
                weapon.DurabilityLevel = (WeaponDurabilityLevel)RandomMinMaxScaled(0, maxLevel);

                PackItem(weapon);
            }

            return true;
        }

        public void PackGold(int amount)
        {
            if (amount > 0)
                PackItem(new Gold(amount));
        }

        public void PackGold(int min, int max)
        {
            PackGold(Utility.RandomMinMax(min, max));
        }

        public void PackStatue(int min, int max)
        {
            PackStatue(Utility.RandomMinMax(min, max));
        }

        public void PackStatue(int amount)
        {
            for (int i = 0; i < amount; ++i)
                PackStatue();
        }

        public void PackStatue()
        {
            PackItem(Loot.RandomStatue());
        }

        public void PackStatue(double chance)
        {
            if (chance <= Utility.RandomDouble())
                return;

            PackStatue();
        }

        public void PackGem()
        {
            PackGem(1);
        }

        public void PackGem(int min, int max)
        {
            PackGem(Utility.RandomMinMax(min, max));
        }

        public void PackGem(int amount)
        {
            PackGem(amount, 1.0);
        }

        public void PackGem(int amount, double chance)
        {
            if (amount <= 0)
                return;

            if (chance <= Utility.RandomDouble())
                return;

            Item gem = Loot.RandomGem();

            gem.Amount = amount;

            PackItem(gem);
        }

        public void PackNecroReg(int min, int max)
        {
            PackNecroReg(Utility.RandomMinMax(min, max));
        }

        public void PackNecroReg(int amount)
        {
            for (int i = 0; i < amount; ++i)
                PackNecroReg();
        }

        public void PackNecroReg()
        {
            if (!Core.RuleSets.AOSRules())
                return;

            PackItem(Loot.RandomNecromancyReagent());
        }

        public void PackReg(int min, int max)
        {
            PackReg(Utility.RandomMinMax(min, max));
        }

        public void PackReg(int amount)
        {
            PackReg(amount, 1.0);
        }

        public void PackReg(int amount, double chance)
        {
            if (amount <= 0)
                return;

            if (chance <= Utility.RandomDouble())
                return;

            Item reg = Loot.RandomReagent();

            reg.Amount = amount;

            PackItem(reg);
        }

        public void PackItem(Item item)
        {   // adam: default is to try to pack an enchanted scroll
            PackItem(item, Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules() ? true : false);
        }

        public void PackItem(Type type, double chance)
        {
            PackItem(type, 1, chance);
        }

        public void PackItem(Type type, int amount, double chance)
        {
            if (chance <= Utility.RandomDouble())
                return;

            for (int yx = 0; yx < amount; yx++)
            {
                Item item = Loot.Construct(type);
                PackItem(item, Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules() ? true : false);
            }
        }

        public void PackItem(Item item, bool EScrollChance)
        {
            // erl: check for chance to drop enchanted scroll instead
            if (EScrollChance && Server.Engines.SDrop.SDropTest(item, CoreAI.EScrollChance))
            {
                // Drop a scroll instead of the item
                EnchantedScroll escroll = Loot.GenEScroll((object)item);

                // Delete the original item
                item.Delete();

                // Re-reference item to escroll and continue
                item = (Item)escroll;
            }

            if (Summoned || item == null)
            {
                if (item != null)
                    item.Delete();

                return;
            }

            Container pack = Backpack;

            if (pack == null)
            {
                pack = new Backpack();

                pack.Movable = false;

                AddItem(pack);
            }

            if (!item.Stackable || !pack.TryDropItem(this, item, false)) // try stack
                pack.DropItem(item); // failed, drop it anyway
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster && !Body.IsHuman)
            {
                Container pack = this.Backpack;

                if (pack != null)
                    pack.DisplayTo(from);
            }

            base.OnDoubleClick(from);
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            //doesnt seem to be used why the fuck is it here and why did it have bonded when everything is handled
            //with on single click??
            base.AddNameProperties(list);

            if (Controlled && Commandable)
            {
                if (Summoned)
                    list.Add(1049646); // (summoned)
                else
                    list.Add(502006); // (tame)
            }
        }

        private bool m_IOBFollower;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IOBFollower
        {
            get { return m_IOBFollower; }
            set { m_IOBFollower = value; }
        }

        private Mobile m_IOBLeader;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile IOBLeader
        {
            get { return m_IOBLeader; }
            set { m_IOBLeader = value; }
        }

        private DateTime m_IOBTimeAcquired;
        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime IOBTimeAcquired
        {
            get { return m_IOBTimeAcquired; }
            set { m_IOBTimeAcquired = value; }
        }


        public override void OnSingleClick(Mobile from)
        {
            BaseHire m_Hire = this as BaseHire;                         //added by Old Salty from here . . .

            if (m_Hire != null && m_Hire.IsHired)
            {
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, true, "(hired)", from.NetState);
            }
            else if (m_IOBFollower)
            {
                if (m_IOBLeader == from)
                {
                    PrivateOverheadMessage(MessageType.Regular, 0x3B2, true, "(following)", from.NetState);
                }
                else if (from.AccessLevel > AccessLevel.Player)
                {
                    if (m_IOBLeader != null)
                    {
                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, true, "(following - " + IOBLeader.Name + ")", from.NetState);
                    }
                }
            }
            else if (Controlled && Commandable)
            {
                int number;

                if (Summoned)
                    number = 1049646; // (summoned)
                else if (IsBonded)
                    number = 1049608;
                else
                    number = 502006; // (tame)

                PrivateOverheadMessage(MessageType.Regular, 0x3B2, number, from.NetState);
            }

            base.OnSingleClick(from);
        }

        public virtual int TreasureMapLevel { get { return 0; } }

        public void NewbieAllLayers()
        {
            try
            {
                //make sure cloths/weapons are newbied so they don't drop
                Item[] items = new Item[19];
                items[0] = this.FindItemOnLayer(Layer.Shoes);
                items[1] = this.FindItemOnLayer(Layer.Pants);
                items[2] = this.FindItemOnLayer(Layer.Shirt);
                items[3] = this.FindItemOnLayer(Layer.Helm);
                items[4] = this.FindItemOnLayer(Layer.Gloves);
                items[5] = this.FindItemOnLayer(Layer.Neck);
                items[6] = this.FindItemOnLayer(Layer.Waist);
                items[7] = this.FindItemOnLayer(Layer.InnerTorso);
                items[8] = this.FindItemOnLayer(Layer.MiddleTorso);
                items[9] = this.FindItemOnLayer(Layer.Arms);
                items[10] = this.FindItemOnLayer(Layer.Cloak);
                items[11] = this.FindItemOnLayer(Layer.OuterTorso);
                items[12] = this.FindItemOnLayer(Layer.OuterLegs);
                items[13] = this.FindItemOnLayer(Layer.InnerLegs);
                items[14] = this.FindItemOnLayer(Layer.Bracelet);
                items[15] = this.FindItemOnLayer(Layer.Ring);
                items[16] = this.FindItemOnLayer(Layer.Earrings);
                items[17] = this.FindItemOnLayer(Layer.OneHanded);
                items[18] = this.FindItemOnLayer(Layer.TwoHanded);
                for (int i = 0; i < 19; i++)
                {
                    if (items[i] != null)
                    {
                        items[i].LootType = LootType.Newbied;
                    }
                }
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
            }

        }

        // We want to ab able to suppress loot on GM created creatures  (we may be giving them a LootPack)
        [CommandProperty(AccessLevel.Administrator)]
        public virtual bool SuppressNormalLoot { get { return m_SuppressNormalLoot; } set { m_SuppressNormalLoot = value; } }
        private bool m_SuppressNormalLoot = false;

        public override bool OnBeforeDeath()
        {
            #region AI_Suicide && IOB stuff (obsolete?) (Angel Island Only)
            // make sure cloths/weapons are newbied so they don't drop
            if (this.AI == AIType.AI_Suicide || (this.IOBAlignment != IOBAlignment.None && this.Controlled))
                NewbieAllLayers();
            #endregion

            // drop treasure map
            if (!Summoned && !NoKillAwards && !IsBonded && TreasureMapLevel > 0 && (Map == Map.Felucca || Map == Map.Trammel) && TreasureMap.LootChance >= Utility.RandomDouble())
                PackItem(new TreasureMap(TreasureMapLevel, Map));

            #region Dungeon rares (Angel Island only)
            // most dungeon creatures with Str > 50 now have a chance to drop something rare
            //	1 in 2000 chance (good luck!)
            if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules())
            {
                Item rare = Loot.RareFactoryItem(0.0005);
                if (!Summoned && !NoKillAwards && !IsBonded && rare != null && this.Str > 50 && (SpellHelper.IsFeluccaDungeon(this.Map, this.Location) || SpellHelper.IsFeluccaWind(this.Map, this.Location)))
                {
                    LogHelper Logger = new LogHelper("BabyRareFactory.log", false, true);
                    Logger.Log(LogType.Mobile, this, string.Format("Dropped baby rare factory item {0}", rare));
                    Logger.Finish();
                    PackItem(rare);
                }
            }
            #endregion

            // normal pack loot
            if (!Summoned && !NoKillAwards && !m_HasGeneratedLoot)
            {
                m_HasGeneratedLoot = true;

                #region special no-loot case  (Angel Island Only)
                if (SuppressNormalLoot == true)
                {   // empty backpack of stuff that may have been added at mobile creation time (like the instrument on Gypsy)
                    if (this.Backpack != null && this.Backpack.Items != null)
                    {
                        while (this.Backpack.Items.Count > 0)
                        {
                            Item temp = (Item)this.Backpack.Items[0];
                            this.Backpack.RemoveItem(temp);
                            temp.Delete();
                        }
                    }
                }
                else
                    #endregion
                    GenerateLoot(false);

                #region Paragon (Angel Island Only)
                // give a little boost in loot for paragon creatures
                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules())
                {
                    if (Paragon == true)
                    {
                        int gold = GetGold();
                        if (gold > 0)
                            PackGold(gold / 2, gold);
                    }
                }
                #endregion

                /*#region KIN SYSTEM (Angel Island Only)
				if (Core.RuleSets.AngelIslandRules() || Core.UOAR)
				{
					// Is kin silver enabled?
					if (Engines.IOBSystem.KinSystemSettings.KinAwards == true)
					{
						// adjust gold drop and add silved for IOB Kin System
						int Silver, NewGold;
						if (KinAwards.CalcAwardInSilver(this, out Silver, out NewGold) == true)
							// delete old gold, add new gold and silver
							KinAwards.AdjustLootForKinAward(this, Silver, NewGold);
					}
				}
				#endregion*/

                #region Murderer gold adjust (Siege Perilous)
                // An additional penalty for being red on Siege Perilous includes the amount of gold found on the corpse of a creature 
                //	which they have killed will be one-third. For example, if a player would normally receive 600 gold off a monster, 
                //	if that player is instead red, he will receive 200 gold.
                // http://www.uoguide.com/Publish_13.6_(Siege_Perilous_Shards_Only)
                if ((Core.RuleSets.SiegeRules() && PublishInfo.Publish >= 13.6) || Core.RuleSets.MortalisRules())
                {   // calc new award amount if the damager is red

                    //creature must not be controlled 
                    if (this.ControlMaster == null)
                    {
                        // first find out how much gold this creature is dropping
                        int MobGold = this.GetGold();

                        // reds get 1/3 of usual gold
                        int NewGold = MobGold / 3;

                        // now calc the damagers.
                        List<DamageStore> list = BaseCreature.GetLootingRights(this.DamageEntries, this.HitsMax);

                        // see if the biggest damager is red
                        //	the list is sorted, so top damager is on top
                        if (list.Count > 0)
                        {
                            DamageStore ds = list[0] as DamageStore;
                            if (ds.m_HasRight && ds.m_Mobile is PlayerMobile && ds.m_Mobile.Murderer)
                            {   // first delete all dropped gold
                                Container pack = this.Backpack;
                                if (pack != null)
                                {
                                    // how much gold is on the creature?
                                    Item[] golds = pack.FindItemsByType(typeof(Gold), true);
                                    foreach (Item g in golds)
                                    {
                                        pack.RemoveItem(g);
                                        g.Delete();
                                    }

                                    this.PackGold(NewGold);
                                }
                            }
                        }
                    }
                }
                #endregion

                #region Spawner loot
                // drop special loot specified by the spawner
                if (Spawner != null)
                    SpawnerLoot(Spawner);
                #endregion

                #region InvokeCreatureGenerateLoot
                // do any special loot generation.
                //	we added this as AI 2.0 server birth to add special weapons to drops without adding that 
                //	logic to this class directly. 
                // this facility should be handy for future special-case drops
                EventSink.InvokeCreatureGenerateLoot(new CreatureGenerateLootEventArgs(this));
                #endregion
            }

            if (!NoKillAwards)
                DistributedLoot();

            if (IsAnimatedDead)
                Effects.SendLocationEffect(Location, Map, 0x3728, 13, 1, 0x461, 4);

            InhumanSpeech speechType = this.SpeechType;

            if (speechType != null)
                speechType.OnDeath(this);

            return base.OnBeforeDeath();
        }

        private bool m_NoKillAwards;

        public bool NoKillAwards
        {
            get { return m_NoKillAwards; }
            set { m_NoKillAwards = value; }
        }

        public Mobile GetMaster()
        {
            if (Controlled && ControlMaster != null)
                return ControlMaster;
            else if (Summoned && SummonMaster != null)
                return SummonMaster;

            return null;
        }

        public virtual void DistributedLoot()
        {
            // execute any dynamic loot generation engines
            foreach (Item ix in Items)
            {
                if (ix is OnBeforeDeath)
                    (ix as OnBeforeDeath).Process(this);
            }
        }

        public int ComputeBonusDamage(ArrayList list, Mobile m)
        {
            int bonus = 0;

            for (int i = list.Count - 1; i >= 0; --i)
            {
                DamageEntry de = (DamageEntry)list[i];

                if (de.Damager == m || !(de.Damager is BaseCreature))
                    continue;

                BaseCreature bc = (BaseCreature)de.Damager;
                Mobile master = null;

                if (bc.Controlled && bc.ControlMaster != null)
                    master = bc.ControlMaster;
                else if (bc.Summoned && bc.SummonMaster != null)
                    master = bc.SummonMaster;

                if (master == m)
                    bonus += de.DamageGiven;
            }

            return bonus;
        }

        private class FKEntry
        {
            public Mobile m_Mobile;
            public int m_Damage;

            public FKEntry(Mobile m, int damage)
            {
                m_Mobile = m;
                m_Damage = damage;
            }
        }

        #region LootingRights
        public static List<DamageStore> GetLootingRights(List<DamageEntry> damageEntries, int hitsMax)
        {
            List<DamageStore> rights = new List<DamageStore>();

            for (int i = damageEntries.Count - 1; i >= 0; --i)
            {
                if (i >= damageEntries.Count)
                    continue;

                DamageEntry de = damageEntries[i];

                if (de.HasExpired)
                {
                    damageEntries.RemoveAt(i);
                    continue;
                }

                int damage = de.DamageGiven;

                List<DamageEntry> respList = de.Responsible;

                if (respList != null)
                {
                    for (int j = 0; j < respList.Count; ++j)
                    {
                        DamageEntry subEntry = respList[j];
                        Mobile master = subEntry.Damager;

                        if (master == null || master.Deleted || !master.Player)
                            continue;

                        bool needNewSubEntry = true;

                        for (int k = 0; needNewSubEntry && k < rights.Count; ++k)
                        {
                            DamageStore ds = rights[k];

                            if (ds.m_Mobile == master)
                            {
                                ds.m_Damage += subEntry.DamageGiven;
                                needNewSubEntry = false;
                            }
                        }

                        if (needNewSubEntry)
                            rights.Add(new DamageStore(master, subEntry.DamageGiven));

                        damage -= subEntry.DamageGiven;
                    }
                }

                Mobile m = de.Damager;

                if (m == null || m.Deleted || !m.Player)
                    continue;

                if (damage <= 0)
                    continue;

                bool needNewEntry = true;

                for (int j = 0; needNewEntry && j < rights.Count; ++j)
                {
                    DamageStore ds = rights[j];

                    if (ds.m_Mobile == m)
                    {
                        ds.m_Damage += damage;
                        needNewEntry = false;
                    }
                }

                if (needNewEntry)
                    rights.Add(new DamageStore(m, damage));
            }

            if (rights.Count > 0)
            {
                rights[0].m_Damage = (int)(rights[0].m_Damage * 1.25);  //This would be the first valid person attacking it.  Gets a 25% bonus.  Per 1/19/07 Five on Friday

                if (rights.Count > 1)
                    rights.Sort(); //Sort by damage

                int topDamage = rights[0].m_Damage;
                int minDamage;

                if (hitsMax >= 3000)
                    minDamage = topDamage / 16;
                else if (hitsMax >= 1000)
                    minDamage = topDamage / 8;
                else if (hitsMax >= 200)
                    minDamage = topDamage / 4;
                else
                    minDamage = topDamage / 2;

                for (int i = 0; i < rights.Count; ++i)
                {
                    DamageStore ds = rights[i];

                    ds.m_HasRight = (ds.m_Damage >= minDamage);
                }
            }

            return rights;
        }
        #endregion

        public override void OnDeath(Container c)
        {
            if (IsBonded)
            {
                #region Bonded pet
                int sound = this.GetDeathSound();

                if (sound >= 0)
                    Effects.PlaySound(this, this.Map, sound);

                Warmode = false;

                Poison = null;
                Combatant = null;

                Hits = 0;
                Stam = 0;
                Mana = 0;

                IsDeadPet = true;
                ControlTarget = ControlMaster;
                ControlOrder = OrderType.Follow;

                //Bonded pet will take statloss until this time
                // initially 3.0 hours from death.
                m_StatLossTime = DateTime.UtcNow + TimeSpan.FromHours(3.0);

                ProcessDeltaQueue();
                SendIncomingPacket();
                SendIncomingPacket();

                List<AggressorInfo> aggressors = this.Aggressors;

                for (int i = 0; i < aggressors.Count; ++i)
                {
                    AggressorInfo info = (AggressorInfo)aggressors[i];
                    if (info.Attacker is BaseGuard)
                        c.Delete();

                    if (info.Attacker.Combatant == this)
                        info.Attacker.Combatant = null;
                }

                List<AggressorInfo> aggressed = this.Aggressed;

                for (int i = 0; i < aggressed.Count; ++i)
                {
                    AggressorInfo info = (AggressorInfo)aggressed[i];

                    if (info.Defender.Combatant == this)
                        info.Defender.Combatant = null;
                }

                Mobile owner = this.ControlMaster;

                if (owner == null || owner.Deleted || owner.Map != this.Map || !owner.InRange(this, 12) || !this.CanSee(owner) || !this.InLOS(owner))
                {
                    if (this.OwnerAbandonTime == DateTime.MinValue)
                        this.OwnerAbandonTime = DateTime.UtcNow;
                }
                else
                {
                    this.OwnerAbandonTime = DateTime.MinValue;
                }

                CheckStatTimers();
                #endregion
            }
            else
            {
                #region New RunUO 2.1
                if (!Summoned && !m_NoKillAwards)
                {
                    int totalFame = Fame / 100;
                    int totalKarma = -Karma / 100;
                    if (Map == Map.Felucca)
                    {
                        totalFame += ((totalFame / 10) * 3);
                        totalKarma += ((totalKarma / 10) * 3);
                    }

                    List<DamageStore> list = GetLootingRights(this.DamageEntries, this.HitsMax);
                    List<Mobile> titles = new List<Mobile>();
                    List<int> fame = new List<int>();
                    List<int> karma = new List<int>();

                    bool givenQuestKill = false;
                    bool givenFactionKill = false;
                    bool givenToTKill = false;
                    bool givenEthicKill = false;

                    for (int i = 0; i < list.Count; ++i)
                    {
                        DamageStore ds = list[i];

                        if (!ds.m_HasRight)
                            continue;

                        Party party = Engines.PartySystem.Party.Get(ds.m_Mobile);

                        if (party != null)
                        {
                            int divedFame = totalFame / party.Members.Count;
                            int divedKarma = totalKarma / party.Members.Count;

                            for (int j = 0; j < party.Members.Count; ++j)
                            {
                                PartyMemberInfo info = party.Members[j] as PartyMemberInfo;

                                if (info != null && info.Mobile != null)
                                {
                                    int index = titles.IndexOf(info.Mobile);

                                    if (index == -1)
                                    {
                                        titles.Add(info.Mobile);
                                        fame.Add(divedFame);
                                        karma.Add(divedKarma);
                                    }
                                    else
                                    {
                                        fame[index] += divedFame;
                                        karma[index] += divedKarma;
                                    }
                                }
                            }
                        }
                        else
                        {
                            titles.Add(ds.m_Mobile);
                            fame.Add(totalFame);
                            karma.Add(totalKarma);
                        }

                        #region Paragon
#if notused
						OnKilledBy(ds.m_Mobile);
#endif
                        #endregion

                        if (!givenFactionKill)
                        {
                            givenFactionKill = true;
                            Faction.HandleDeath(this, ds.m_Mobile);
                        }

                        if (Core.OldEthics)
                            if (!givenEthicKill)
                            {
                                givenEthicKill = true;
                                Ethics.Ethic.HandleDeath(this, ds.m_Mobile);
                            }

                        Region region = ds.m_Mobile.Region;

                        #region Tokuno
#if notused
						if (!givenToTKill && (Map == Map.Tokuno || region.IsPartOf("Yomotsu Mines") || region.IsPartOf("Fan Dancer's Dojo")))
						{
							givenToTKill = true;
							TreasuresOfTokuno.HandleKill(this, ds.m_Mobile);
						}
#endif
                        #endregion

                        if (givenQuestKill)
                            continue;

                        PlayerMobile pm = ds.m_Mobile as PlayerMobile;

                        if (pm != null)
                        {
                            QuestSystem qs = pm.Quest;

                            if (qs != null)
                            {
                                qs.OnKill(this, c);
                                givenQuestKill = true;
                            }
                        }
                    }
                    for (int i = 0; i < titles.Count; ++i)
                    {
                        Titles.AwardFame(titles[i], fame[i], true);
                        Titles.AwardKarma(titles[i], karma[i], true);
                    }
                }
                #endregion

                #region obsolete
#if obsolete
				if (!Summoned && !m_NoKillAwards)
				{
					int totalFame = Fame / 100;
					int totalKarma = -Karma / 100;

					ArrayList list = GetLootingRights(this.DamageEntries);

					bool givenQuestKill = false;

					for (int i = 0; i < list.Count; ++i)
					{
						DamageStore ds = (DamageStore)list[i];

						if (!ds.m_HasRight)
							continue;

						// Adam: distribute Fame/Karma
						Party p = Server.Engines.PartySystem.Party.Get(ds.m_Mobile);
						if (p != null && p.Leader != null)
						{
							int partyFame = totalFame / p.Count;
							int partyKarma = totalKarma / p.Count;
							foreach (PartyMemberInfo mi in p.Members)
							{
								Mobile m = mi.Mobile;
								if (m != null)
								{
									Titles.AwardFame(m, partyFame, true);
									Titles.AwardKarma(m, partyKarma, true);
								}
							}
						}
						else
						{
							Titles.AwardFame(ds.m_Mobile, totalFame, true);
							Titles.AwardKarma(ds.m_Mobile, totalKarma, true);
						}
						if (givenQuestKill)
							continue;

						PlayerMobile pm = ds.m_Mobile as PlayerMobile;

						if (pm != null)
						{
							QuestSystem qs = pm.Quest;

							if (qs != null)
							{
								qs.OnKill(this, c);
								givenQuestKill = true;
							}
						}
					}
				}
#endif
                #endregion

                #region IOBSystem
                //SMD: Oct. 2007: Added kin power points
                /*if (Engines.IOBSystem.KinSystemSettings.PointsEnabled && true == false)//plasma: no power points for now !  take this true==false back out later (or refactor this and put this code in IOBSytem with the other power point allocation code :< ).
				{

					if (this.IOBAlignment != IOBAlignment.None && this.ControlSlots >= 3)
					{
						try
						{
							//Award Kin Power Points.
							//Determine who gets the points:
							if (this.LastKiller is PlayerMobile)
							{
								PlayerMobile pmLK = LastKiller as PlayerMobile;
								if (pmLK != null &&
									pmLK.IOBAlignment != this.IOBAlignment &&
									pmLK.IOBAlignment != IOBAlignment.OutCast &&
									pmLK.IOBAlignment != IOBAlignment.Healer
									)
								{
									double awarded = pmLK.AwardKinPowerPoints(1.0);
									if (awarded > 0)
									{
										pmLK.SendMessage("You have received {0:0.00} power points.", awarded);
									}
								}
							}
						}
						catch (Exception kinppex)
						{
							LogHelper.LogException(kinppex, "Problem while awarding kin power points.");
						}
					}
				}*/
                #endregion

                base.OnDeath(c);

                if (DeleteCorpseOnDeath)
                    c.Delete();

                #region [ On GuardKill ]
                //this little bit added by Old Salty
                if (!this.Player)
                {
                    foreach (DamageEntry de in DamageEntries)
                    {
                        if (de.Damager is BaseGuard && c != null)
                            c.Delete();
                    }
                }
                #endregion
            }
        }

        /* To save on cpu usage, RunUO creatures only reAcquire creatures under the following circumstances:
		 *  - 10 seconds have elapsed since the last time it tried
		 *  - The creature was attacked
		 *  - Some creatures, like dragons, will reAcquire when they see someone move
		 *
		 * This functionality appears to be implemented on OSI as well
		 */

        private DateTime m_NextReAcquireTime;

        public DateTime NextReacquireTime { get { return m_NextReAcquireTime; } set { m_NextReAcquireTime = value; } }

        public virtual TimeSpan ReacquireDelay { get { return TimeSpan.FromSeconds(10.0); } }
        public virtual bool ReAcquireOnMovement { get { return false; } }

        public void ForceReAcquire()
        {
            m_NextReAcquireTime = DateTime.MinValue;
        }

        public override void OnDelete()
        {
            SetControlMaster(null);
            SummonMaster = null;

            base.OnDelete();
        }

        public override bool CanBeHarmful(Mobile target, bool message, bool ignoreOurBlessedness)
        {
            try
            {
                RegionControl regstone = null;
                CustomRegion reg = null;
                if (target != null && !target.Deleted && target.Map != null)
                    reg = CustomRegion.FindDRDTRegion(target);
                if (reg != null)
                    regstone = reg.GetRegionControler();

                //if your in a region area spells will fail if disallowed, prevents the run outside of area precast
                //run back into region then release spell ability
                if (this != null && target != null && this.Region != target.Region && regstone != null && regstone.NoExternalHarmful
                    && this.AccessLevel == AccessLevel.Player)
                {
                    this.SendMessage("You cannot harm them in that area.");
                    return false;
                }

            }
            catch (NullReferenceException e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("{0} Caught exception.", e.Message);
                Console.WriteLine(e.Source);
                Console.WriteLine(e.StackTrace);
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

            if ((target is BaseVendor && ((BaseVendor)target).IsInvulnerable) || target is PlayerVendor || target is TownCrier)
            {
                if (message)
                {
                    if (target.Title == null)
                        SendMessage("{0} the vendor cannot be harmed.", target.Name);
                    else
                        SendMessage("{0} {1} cannot be harmed.", target.Name, target.Title);
                }

                return false;
            }

            return base.CanBeHarmful(target, message, ignoreOurBlessedness);
        }

        public override bool CanBeRenamedBy(Mobile from)
        {
            bool ret = base.CanBeRenamedBy(from);

            if (Controlled && from == ControlMaster)
                ret = true;

            return ret;
        }

        public bool SetControlMaster(Mobile m)
        {
            if (m == null)
            {
                //DebugSay("I'm being released");
                ControlMaster = null;
                Controlled = false;
                ControlTarget = null;
                ControlOrder = OrderType.None;
                Guild = null;
                Delta(MobileDelta.Noto);
            }
            else
            {
                //DebugSay("I'm being tamed");
                if (m.Followers + ControlSlots > m.FollowersMax)
                {
                    m.SendLocalizedMessage(1049607); // You have too many followers to control that creature.
                    return false;
                }

                CurrentWayPoint = null;//so tamed animals don't try to go back

                ControlMaster = m;
                Controlled = true;
                ControlTarget = null;
                ControlOrder = OrderType.Come;
                Guild = null;

                BardMaster = null;
                BardProvoked = false;
                BardPacified = false;
                BardTarget = null;
                BardEndTime = DateTime.UtcNow;

                Delta(MobileDelta.Noto);
            }

            OnControlMasterChanged(m);

            return true;
        }

        protected virtual void OnControlMasterChanged(Mobile m)
        {
        }

        private static bool m_Summoning;

        public static bool Summoning
        {
            get { return m_Summoning; }
            set { m_Summoning = value; }
        }

        public static bool Summon(BaseCreature creature, Mobile caster, Point3D p, int sound, TimeSpan duration)
        {
            return Summon(creature, true, caster, p, sound, duration);
        }

        public static bool Summon(BaseCreature creature, bool controled, Mobile caster, Point3D p, int sound, TimeSpan duration)
        {
            if (caster.Followers + creature.ControlSlots > caster.FollowersMax)
            {
                caster.SendLocalizedMessage(1049645); // You have too many followers to summon that creature.
                creature.Delete();
                return false;
            }

            m_Summoning = true;

            if (controled)
                creature.SetControlMaster(caster);

            creature.RangeHome = 10;
            creature.Summoned = true;

            creature.SummonMaster = caster;

            Container pack = creature.Backpack;

            if (pack != null)
            {
                for (int i = pack.Items.Count - 1; i >= 0; --i)
                {
                    if (i >= pack.Items.Count)
                        continue;

                    ((Item)pack.Items[i]).Delete();
                }
            }

            if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules())
            {
                // apply Spirit Speak bonus
                creature.SetHits((int)(creature.Hits + creature.Hits * (caster.Skills.SpiritSpeak.Value / 200)), false);
                duration = TimeSpan.FromSeconds(duration.TotalSeconds + duration.TotalSeconds * (caster.Skills.SpiritSpeak.Value / 100));
            }

            new UnsummonTimer(caster, creature, duration).Start();
            creature.m_SummonEnd = DateTime.UtcNow + duration;

            creature.MoveToWorld(p, caster.Map);

            Effects.PlaySound(p, creature.Map, sound);

            m_Summoning = false;

            return true;
        }

        private static bool EnableRummaging = true;

        private const double ChanceToRummage = 0.5; // 50%

        private const double MinutesToNextRummageMin = 1.0;
        private const double MinutesToNextRummageMax = 4.0;

        private const double MinutesToNextChanceMin = 0.25;
        private const double MinutesToNextChanceMax = 0.75;

        private DateTime m_NextRummageTime;
        private DateTime m_LoyaltyWarning;

        public virtual bool CanBreath { get { return HasBreath && !Summoned; } }
        public virtual bool IsDispellable { get { return Summoned && !IsAnimatedDead; } }

        public virtual void OnThink()
        {
            // Record our last thought. See Hibernating
            m_LastThought = DateTime.UtcNow;

            //loyalty msg check, give warning msg every 5 minutes if at confused loyalty.
            if (DateTime.UtcNow >= this.m_LoyaltyWarning)
            {
                if (this.Loyalty <= PetLoyalty.Confused) // loyalty redo
                {
                    this.Say(1043270, this.Name); // * ~1_NAME~ looks around desperately *
                    this.PlaySound(this.GetIdleSound());
                }
                m_LoyaltyWarning = DateTime.UtcNow + TimeSpan.FromMinutes(5.0);
            }

            //check that IOBFollower's leader is still wearing an IOB
            if (IOBFollower)
            {
                if (IOBLeader != null && IOBLeader is PlayerMobile)
                {
                    if (((PlayerMobile)IOBLeader).IOBEquipped == false)
                    {
                        IOBDismiss(true);
                    }
                }
                else
                {
                    IOBDismiss(true);
                }
            }

            if (Controlled || IOBFollower || Hits < HitsMax)
                RefreshLifespan();

            if (EnableRummaging && CanRummageCorpses && !Summoned && !Controlled && DateTime.UtcNow >= m_NextRummageTime)
            {
                double min, max;

                if (ChanceToRummage > Utility.RandomDouble() && Rummage())
                {
                    min = MinutesToNextRummageMin;
                    max = MinutesToNextRummageMax;
                }
                else
                {
                    min = MinutesToNextChanceMin;
                    max = MinutesToNextChanceMax;
                }

                double delay = min + (Utility.RandomDouble() * (max - min));
                m_NextRummageTime = DateTime.UtcNow + TimeSpan.FromMinutes(delay);
            }

            if (HasBreath && !Summoned && DateTime.UtcNow >= m_NextBreathTime) // tested: controled dragons do breath fire, what about summoned skeletal dragons?
            {
                Mobile target = this.Combatant;

                if (target != null && target.Alive && !target.IsDeadBondedPet && CanBeHarmful(target) && target.Map == this.Map && !IsDeadBondedPet && target.InRange(this, BreathRange) && InLOS(target) && !BardPacified)
                    BreathStart(target);

                m_NextBreathTime = DateTime.UtcNow + TimeSpan.FromSeconds(BreathMinDelay + (Utility.RandomDouble() * BreathMaxDelay));
            }

            if (!(MyAura == AuraType.None) && DateTime.UtcNow >= NextAuraTime)
            {
                try
                {
                    ArrayList list = new ArrayList();

                    IPooledEnumerable eable = this.GetMobilesInRange(AuraRange);
                    foreach (Mobile mt in eable)
                    {

                        if (mt != null && !mt.Deleted && mt != this && AuraTarget(mt) && CanBeHarmful(mt))
                            list.Add(mt);
                    }
                    eable.Free();

                    foreach (Mobile m in list)
                    {
                        if (m is PlayerMobile && m.AccessLevel == AccessLevel.Player && MyAura != AuraType.Fear || m is BaseCreature && (((BaseCreature)m).Controlled || ((BaseCreature)m).Team != this.Team) && MyAura != AuraType.Fear)
                        {
                            // Adam: add IsDeadBondedPet to the test to keep from attacking dead bonded pets
                            if (m.Map == this.Map && m.Alive && !m.IsDeadBondedPet)
                            {
                                m.Damage(Utility.Random(AuraMin, AuraMax), this, source_weapon: this);
                                DoHarmful(m);
                                NextAuraTime = DateTime.UtcNow + NextAuraDelay;
                                m.Paralyzed = false;

                                switch (MyAura)
                                {
                                    case AuraType.Ice: m.SendMessage("You feel extremely cold!"); break;
                                    case AuraType.Fire: m.SendMessage("You feel extremely hot!"); break;
                                    case AuraType.Poison: m.SendMessage("Your lungs fill with poisonous gas!"); break;
                                    case AuraType.Hate: m.FixedParticles(0x374A, 10, 15, 5013, EffectLayer.Waist); break;
                                    default: break;
                                }
                            }
                        }
                        //Fear Aura repells pets.
                        if (m is BaseCreature && ((BaseCreature)m).Controlled && MyAura == AuraType.Fear)
                        {

                            if (m.Map == this.Map && m.Alive && !m.IsDeadBondedPet)
                            {
                                ((BaseCreature)m).ControlOrder = OrderType.None;
                                ((BaseCreature)m).Combatant = null;
                                ((BaseCreature)m).FocusMob = null;
                                ((BaseCreature)m).AIObject.DoActionFlee();
                                NextAuraTime = DateTime.UtcNow + NextAuraDelay;
                                m.FixedParticles(0x374A, 10, 15, 5013, EffectLayer.Waist);

                                if (((BaseCreature)m).ControlMaster != null)
                                    ((BaseCreature)m).SayTo(((BaseCreature)m).ControlMaster, "your pet is afraid of this creature and flee's in terror.");


                            }
                        }
                    }
                }

                catch (Exception e)
                {
                    LogHelper.LogException(e);
                    Console.WriteLine("Exception (non-fatal) caught in BaseCreature.OnThink: " + e.Message);
                    Console.WriteLine(e.Source);
                    Console.WriteLine(e.StackTrace);
                }
            }
        }

        public virtual bool Rummage()
        {
            Corpse toRummage = null;

            IPooledEnumerable eable = this.GetItemsInRange(2);
            foreach (Item item in eable)
            {
                if (item is Corpse && item.Items.Count > 0)
                {
                    toRummage = (Corpse)item;
                    break;
                }
            }
            eable.Free();

            if (toRummage == null)
                return false;

            Container pack = this.Backpack;

            if (pack == null)
                return false;

            ArrayList items = toRummage.Items;

            bool rejected;
            LRReason reason;

            for (int i = 0; i < items.Count; ++i)
            {
                Item item = (Item)items[Utility.Random(items.Count)];

                Lift(item, item.Amount, out rejected, out reason);

                if (!rejected && Drop(this, new Point3D(-1, -1, 0)))
                {
                    // *rummages through a corpse and takes an item*
                    PublicOverheadMessage(MessageType.Emote, 0x3B2, 1008086);
                    return true;
                }
            }

            return false;
        }

        public void Pacify(Mobile master, DateTime endtime)
        {
            BardPacified = true;
            BardEndTime = endtime;
        }

        public override Mobile GetDamageMaster(Mobile damagee)
        {
            if (m_bBardProvoked && damagee == m_bBardTarget)
                return m_bBardMaster;
            else if (m_bControlled && m_ControlMaster != null)
                return m_ControlMaster;
            else if (m_bSummoned && m_SummonMaster != null)
                return m_SummonMaster;

            return base.GetDamageMaster(damagee);
        }

        public void Provoke(Mobile master, Mobile target, bool bSuccess)
        {
            BardProvoked = true;

            this.PublicOverheadMessage(MessageType.Emote, EmoteHue, false, "*looks furious*");

            if (bSuccess)
            {
                PlaySound(GetIdleSound());

                BardMaster = master;
                BardTarget = target;
                Combatant = target;
                BardEndTime = DateTime.UtcNow + TimeSpan.FromSeconds(30.0);

                if (target is BaseCreature)
                {
                    BaseCreature t = (BaseCreature)target;

                    t.BardProvoked = true;

                    t.BardMaster = master;
                    t.BardTarget = this;
                    t.Combatant = this;
                    t.BardEndTime = DateTime.UtcNow + TimeSpan.FromSeconds(30.0);
                }
            }
            else
            {
                PlaySound(GetAngerSound());

                BardMaster = master;
                BardTarget = target;
            }
        }

        public bool FindMyName(string str, bool bWithAll)
        {
            int i, j;

            string name = this.Name;

            if (name == null || str.Length < name.Length)
                return false;

            string[] wordsString = str.Split(' ');
            string[] wordsName = name.Split(' ');

            for (j = 0; j < wordsName.Length; j++)
            {
                string wordName = wordsName[j];

                bool bFound = false;
                for (i = 0; i < wordsString.Length; i++)
                {
                    string word = wordsString[i];

                    if (Insensitive.Equals(word, wordName))
                        bFound = true;

                    if (bWithAll && Insensitive.Equals(word, "all"))
                        return true;
                }

                if (!bFound)
                    return false;
            }

            return true;
        }

        public static void TeleportPets(Mobile master, Point3D loc, Map map)
        {
            TeleportPets(master, loc, map, false);
        }

        public static void TeleportPets(Mobile master, Point3D loc, Map map, bool onlyBonded)
        {
            ArrayList move = new ArrayList();

            IPooledEnumerable eable = master.GetMobilesInRange(3);
            foreach (Mobile m in eable)
            {
                if (m is BaseCreature)
                {
                    BaseCreature pet = (BaseCreature)m;

                    if (pet.Controlled && pet.ControlMaster == master)
                    {
                        if (!onlyBonded || pet.IsBonded)
                        {
                            if (pet.ControlOrder == OrderType.Guard || pet.ControlOrder == OrderType.Follow || pet.ControlOrder == OrderType.Come)
                                move.Add(pet);
                        }
                    }
                }
            }
            eable.Free();

            foreach (Mobile m in move)
            {
                // some overland mobs are afraid of magic and will not enter!
                if (m is BaseOverland && (m as BaseOverland).GateTravel == false)
                {
                    BaseOverland bo = m as BaseOverland;
                    bo.OnMoongate();
                    continue;
                }
                // okay, move the pet
                m.MoveToWorld(loc, map);

                // wea: make the pet visible if it isn't already
                if (m.Hidden == true)
                    m.Hidden = false;
            }
        }

        #region " AIEntrance Stable Code "
        public static void StablePets(Mobile master)
        {
            ArrayList stable = new ArrayList();

            // your pets need to be with you, you can't use prison as an auto-find pets system
            IPooledEnumerable eable = master.GetMobilesInRange(3);
            foreach (Mobile m in eable)
            {
                if (m is BaseCreature)
                {
                    BaseCreature pet = (BaseCreature)m;

                    if (pet.Controlled && pet.ControlMaster == master && !pet.Summoned)
                    {
                        if (!m.Alive)
                            pet.Resurrect();

                        stable.Add(pet);
                    }
                }
            }
            eable.Free();

            foreach (BaseCreature pet in stable)
            {
                if (master.Stabled.Count <= GetMaxStabled(master))
                {
                    // stable pet and charge
                    AnimalTrainer.StablePet(master, pet, true);
                }
            }
        }

        public static int GetMaxStabled(Mobile from)
        {
            // the absolute max
            return Math.Max(GetMaxEconomyStabled(from), GetMaxPremiumStabled(from));
        }

        // pets stored > GetMaxEconomyStabled < GetMaxPremiumStabled are charged a premimum
        public static int GetMaxPremiumStabled(Mobile from)
        {
            // virtually unlimited pet stables for GM
            //	we impose an actual cap to avoid any 'fill up the server' type exploits
            return from.Skills[SkillName.Herding].Value == 100.0 ? 256 : 0;
        }

        // pets stored <= GetMaxEconomyStabled are charged the OSI rate
        public static int GetMaxEconomyStabled(Mobile from)
        {
            // standard OSI calcs
            double taming = from.Skills[SkillName.AnimalTaming].Value;
            double anlore = from.Skills[SkillName.AnimalLore].Value;
            double vetern = from.Skills[SkillName.Veterinary].Value;
            double sklsum = taming + anlore + vetern;

            int max;

            if (sklsum >= 240.0)
                max = 5;
            else if (sklsum >= 200.0)
                max = 4;
            else if (sklsum >= 160.0)
                max = 3;
            else
                max = 2;

            if (taming >= 100.0)
                max += (int)((taming - 90.0) / 10);

            if (anlore >= 100.0)
                max += (int)((anlore - 90.0) / 10);

            if (vetern >= 100.0)
                max += (int)((vetern - 90.0) / 10);

            // Angel Island herding bonus
            max += (int)from.Skills[SkillName.Herding].Value / 20;

            return max;
        }
        #endregion

        public virtual void ResurrectPet()
        {
            if (!IsDeadPet)
                return;

            OnBeforeResurrect();

            Poison = null;

            Warmode = false;

            Hits = 10;
            Stam = StamMax;
            Mana = 0;

            ProcessDeltaQueue();

            IsDeadPet = false;

            Effects.SendPacket(Location, Map, new BondedStatus(0, this.Serial, 0));

            this.SendIncomingPacket();
            this.SendIncomingPacket();

            OnAfterResurrect();

            Mobile owner = this.ControlMaster;

            if (owner == null || owner.Deleted || owner.Map != this.Map || !owner.InRange(this, 12) || !this.CanSee(owner) || !this.InLOS(owner))
            {
                if (this.OwnerAbandonTime == DateTime.MinValue)
                    this.OwnerAbandonTime = DateTime.UtcNow;
            }
            else
            {
                this.OwnerAbandonTime = DateTime.MinValue;
            }

            CheckStatTimers();
        }

        public override bool CanBeDamaged()
        {
            if (IsDeadPet)
                return false;

            return base.CanBeDamaged();
        }

        public virtual bool PlayerRangeSensitive { get { return true; } }

        public override void OnSectorDeactivate()
        {
            if (PlayerRangeSensitive && m_AI != null)
                m_AI.Deactivate();

            base.OnSectorDeactivate();
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active { get { return m_AI != null ? m_AI.Active : false; } }

        // Adam: remove the check for PlayerRangeSensitive
        // edge case: if a creatures PlayerRangeSensitive is based on their health (Hits < Hits Max) and a player causes delayed damage with an 
        //	Explosion or Explosion potion, but leaves the sector before the damage is done, the creature will be deactivated, and when the player reenters
        //	the creature will remain deactivated. We should always activate on a sector enter.
        public override void OnSectorActivate()
        {
            if (m_AI != null)
                m_AI.Activate();

            base.OnSectorActivate();
        }

        // used for deleting creatures in houses
        private int m_RemoveStep;

        [CommandProperty(AccessLevel.GameMaster)]
        public int RemoveStep { get { return m_RemoveStep; } set { m_RemoveStep = value; } }

        private DateTime m_LastThought = DateTime.UtcNow;
        public bool Hibernating
        {   // A creature is considered Hibernating if he has not 'thought' in the last minute
            get { return DateTime.UtcNow > m_LastThought + TimeSpan.FromMinutes(1); }
        }

        #region Lifespan Code
        const int MinHours = 8; const int MaxHours = 16;
        private int m_LifespanMinutes = Utility.RandomMinMax(MinHours * 60, MaxHours * 60);
        private DateTime m_lifespan;
        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Lifespan
        {
            get { return m_lifespan - DateTime.UtcNow; }
            set { m_lifespan = DateTime.UtcNow + value; m_LifespanMinutes = (int)value.TotalMinutes; }
        }

        // Adam: Certain mobs are shorter lived
        public virtual void RefreshLifespan()
        {
            m_lifespan = DateTime.UtcNow + TimeSpan.FromMinutes(m_LifespanMinutes);
        }

        public bool IsPassedLifespan()
        {
            if (DateTime.UtcNow > m_lifespan)
                return true;
            else
                return false;
        }
        #endregion

        private int m_Wisdom, m_Patience, m_Temper, m_MaxLoyalty;
        private double m_HitsRegenGene, m_ManaRegenGene, m_StamRegenGene;

        [Gene("Hits Regen Rate", .9, 1.1, .6, 1.4)]
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual double HitsRegenGene
        {
            get
            {
                return m_HitsRegenGene;
            }
            set
            {
                m_HitsRegenGene = value;
            }
        }

        public override TimeSpan HitsRegenRate
        {
            get
            {
                return TimeSpan.FromSeconds(base.HitsRegenRate.TotalSeconds * HitsRegenGene);
            }
        }

        [Gene("Mana Regen Rate", .9, 1.1, .6, 1.4)]
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual double ManaRegenGene
        {
            get
            {
                return m_ManaRegenGene;
            }
            set
            {
                m_ManaRegenGene = value;
            }
        }

        public override TimeSpan ManaRegenRate
        {
            get
            {
                return TimeSpan.FromSeconds(base.ManaRegenRate.TotalSeconds * ManaRegenGene);
            }
        }

        [Gene("Stam Regen Rate", .9, 1.1, .6, 1.4)]
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual double StamRegenGene
        {
            get
            {
                return m_StamRegenGene;
            }
            set
            {
                m_StamRegenGene = value;
            }
        }

        public override TimeSpan StamRegenRate
        {
            get
            {
                return TimeSpan.FromSeconds(base.StamRegenRate.TotalSeconds * StamRegenGene);
            }
        }

        [Gene("Max Loyalty", 0.05, -0.02, 120, 140, 0, 140, GeneVisibility.Invisible)]
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int MaxLoyalty
        {
            get
            {
                return m_MaxLoyalty;
            }
            set
            {
                m_MaxLoyalty = value;
            }
        }

        [Gene("Temper", .05, .05, 40, 60, 0, 100, GeneVisibility.Tame)]
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int Temper
        {
            get
            {
                return m_Temper;
            }
            set
            {
                m_Temper = value;
            }
        }

        [Gene("Patience", .05, .05, 40, 60, 0, 100, GeneVisibility.Tame)]
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int Patience
        {
            get
            {
                return m_Patience;
            }
            set
            {
                m_Patience = value;
            }
        }

        [Gene("Wisdom", .05, .05, 40, 60, 0, 100, GeneVisibility.Tame)]
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int Wisdom
        {
            get
            {
                return m_Wisdom;
            }
            set
            {
                m_Wisdom = value;
            }
        }

        [Gene("Gender", 0, 1.0, 0, 1.0)]
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual double GenderGene
        {
            get
            {
                if (Female)
                    return 1.0;
                else
                    return 0;
            }
            set
            {
                if (value > 0.5)
                    Female = true;
                else
                    Female = false;
            }
        }

        #region Breeding

        public virtual bool BreedingEnabled
        {
            get
            {
                return false;
            }
        }

        public virtual string DescribeGene(PropertyInfo prop, GeneAttribute attr)
        {
            double val = (Convert.ToDouble(prop.GetValue(this, null)) - attr.BreedMin) / (attr.BreedMax - attr.BreedMin);
            switch (attr.Name)
            {
                case "Temper":
                    {
                        if (val < .2)
                            return "Angelic";
                        if (val < .4)
                            return "Happy";
                        if (val <= .6)
                            return "Even";
                        if (val <= .8)
                            return "Disagreeable";
                        else
                            return "Caustic";
                    }
                case "Patience":
                    {
                        if (val < .2)
                            return "Headlong";
                        if (val < .4)
                            return "Anxious";
                        if (val <= .6)
                            return "Reserved";
                        if (val <= .8)
                            return "Mild";
                        else
                            return "Gentle";
                    }
                case "Wisdom":
                    {
                        if (val < .2)
                            return "Foolish";
                        if (val < .4)
                            return "Short-sighted";
                        if (val <= .6)
                            return "Thoughtful";
                        if (val <= .8)
                            return "Sage";
                        else
                            return "Learned";
                    }
                case "Gender":
                    {
                        if (val == 1.0)
                            return "Female";
                        else
                            return "Male";
                    }
                default:
                    {
                        if (val < .2)
                            return "Extremely Low";
                        if (val < .4)
                            return "Low";
                        if (val <= .6)
                            return "Average";
                        if (val <= .8)
                            return "High";
                        else
                            return "Extremely High";
                    }
            }
        }

        public virtual void ValidateGenes()
        {
        }

        public virtual bool CheckBreedWith(BaseCreature male)
        {
            return true;
        }

        public BaseCreature BreedWith(BaseCreature male)
        {
            if (!BreedingEnabled)
                return null;
            if (!Female || male.Female)
                return null; // must call BreedWith on the female, and lezzies can't make babies!
            if (male.GetType() != this.GetType())
                return null; // cannot cross-breed
            if (!CheckBreedWith(male))
                return null; // some other check failed

            BaseCreature child = null;
            try
            {
                child = (BaseCreature)GetType().GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes);

                PropertyInfo[] props = GetType().GetProperties();
                System.ComponentModel.TypeConverter doubleconv = System.ComponentModel.TypeDescriptor.GetConverter(typeof(double));

                for (int i = 0; i < props.Length; i++)
                {
                    // note: props[i].GetCustomAttributes() does not traverse inheritance tree!
                    GeneAttribute attr = (GeneAttribute)Attribute.GetCustomAttribute(props[i], typeof(GeneAttribute), true);
                    if (attr == null)
                        continue;

                    double high = Convert.ToDouble(props[i].GetValue(this, null));
                    double low = Convert.ToDouble(props[i].GetValue(male, null));
                    if (high < low)
                    {
                        double t = high;
                        high = low;
                        low = t;
                    }

                    double lowrange = low - attr.LowFactor * (attr.BreedMax - attr.BreedMin);
                    double highrange = high + attr.HighFactor * (attr.BreedMax - attr.BreedMin);

                    if (props[i].PropertyType == typeof(int) &&
                        attr.MinVariance == GeneAttribute.DefaultMinVariance)
                    {
                        if (attr.LowFactor * (attr.BreedMax - attr.BreedMin) < 1)
                            lowrange = low - 1;
                        if (attr.HighFactor * (attr.BreedMax - attr.BreedMin) < 1)
                            highrange = high + 1;
                    }
                    else if (highrange - lowrange < attr.MinVariance)
                    {
                        lowrange -= attr.MinVariance / 2;
                        highrange += attr.MinVariance / 2;
                    }

                    if (lowrange > highrange) // shouldn't ever happen, sanity check
                    {
                        Exception ex = new Exception(String.Format("Sanity Check: Child range for {0} was inverted.\r\nLowFactor: {1}\r\nHighFactor: {2}\r\nMinVariance: {3}", attr.GetType().FullName, attr.LowFactor, attr.HighFactor, attr.MinVariance));
                        LogHelper.LogException(ex);
                        lowrange = low;
                        highrange = high;
                    }

                    double childval = Utility.RandomDouble() * (highrange - lowrange) + lowrange;

                    if (childval < attr.BreedMin)
                        childval = attr.BreedMin;
                    if (childval > attr.BreedMax)
                        childval = attr.BreedMax;

                    props[i].SetValue(child, doubleconv.ConvertTo(childval, props[i].PropertyType), null);
                }

                ValidateGenes();
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine(e.ToString());
                if (child != null)
                    child.Delete();
                return null;
            }

            return child;
        }

        protected void InitializeGenes()
        {
            try
            {
                PropertyInfo[] props = GetType().GetProperties();

                System.ComponentModel.TypeConverter doubleconv = System.ComponentModel.TypeDescriptor.GetConverter(typeof(double));
                for (int i = 0; i < props.Length; i++)
                {
                    GeneAttribute attr = (GeneAttribute)Attribute.GetCustomAttribute(props[i], typeof(GeneAttribute), true);
                    if (attr == null)
                        continue;

                    double value = attr.SpawnMin + Utility.RandomDouble() * (attr.SpawnMax - attr.SpawnMin);

                    props[i].SetValue(this, doubleconv.ConvertTo(value, props[i].PropertyType), null);
                }

                ValidateGenes();
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine(e.ToString());
                Delete();
            }
        }
        #endregion Breeding

        #region SkillCheck

        protected override double GainChance(Skill skill, double chance, bool success)
        {
            if (Controlled)
                return base.GainChance(skill, chance, success) * 2;
            else
                return base.GainChance(skill, chance, success);
        }

        protected override bool AllowGain(Skill skill, object obj)
        {
            if (Region != null && Region.IsJailRules)
                return false;

            if (IsDeadPet)
                return false;

            return base.AllowGain(skill, obj);
        }

        protected override double StatGainChance(Skill skill, Stat stat)
        {
            if (TestCenter.Enabled && Controlled)
                return base.StatGainChance(skill, stat) * 20.0;
            else
                return base.StatGainChance(skill, stat);
        }

        #endregion

        #region Pack Potions
        private static Type[] m_StrongPotions = new Type[]
        {
            typeof( GreaterHealPotion ), typeof( GreaterHealPotion ), typeof( GreaterHealPotion ),
            typeof( GreaterCurePotion ), typeof( GreaterCurePotion ), typeof( GreaterCurePotion ),
            typeof( GreaterStrengthPotion ), typeof( GreaterStrengthPotion ),
            typeof( GreaterAgilityPotion ), typeof( GreaterAgilityPotion ),
            typeof( TotalRefreshPotion ), typeof( TotalRefreshPotion ),
            typeof( GreaterExplosionPotion )
        };

        private static Type[] m_WeakPotions = new Type[]
        {
            typeof( HealPotion ), typeof( HealPotion ), typeof( HealPotion ),
            typeof( CurePotion ), typeof( CurePotion ), typeof( CurePotion ),
            typeof( StrengthPotion ), typeof( StrengthPotion ),
            typeof( AgilityPotion ), typeof( AgilityPotion ),
            typeof( RefreshPotion ), typeof( RefreshPotion ),
            typeof( ExplosionPotion )
        };

        public void PackStrongPotions(int min, int max)
        {
            PackStrongPotions(Utility.RandomMinMax(min, max));
        }

        public void PackStrongPotions(int count)
        {
            for (int i = 0; i < count; ++i)
                PackStrongPotion();
        }

        public void PackStrongPotion()
        {
            Item item = Loot.Construct(m_StrongPotions);
            item.LootType = LootType.Special;   // don't drop, but can be stolen
            PackItem(item);
        }

        public void PackWeakPotions(int min, int max)
        {
            PackWeakPotions(Utility.RandomMinMax(min, max));
        }

        public void PackWeakPotions(int count)
        {
            for (int i = 0; i < count; ++i)
                PackWeakPotion();
        }

        public void PackWeakPotion()
        {
            Item item = Loot.Construct(m_WeakPotions);
            item.LootType = LootType.Special;   // don't drop, but can be stolen
            PackItem(item);
        }
        #endregion
    }

    public class LoyaltyTimer : Timer
    {
        private static TimeSpan InternalDelay = TimeSpan.FromMinutes(5.0);

        public static void Initialize()
        {
            new LoyaltyTimer().Start();
        }

        public LoyaltyTimer()
            : base(InternalDelay, InternalDelay)
        {

            Priority = TimerPriority.FiveSeconds;
        }


        protected override void OnTick()
        {

            ArrayList toRelease = new ArrayList();

            // added array for wild creatures in house regions to be removed
            ArrayList toRemove = new ArrayList();

            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is BaseMount && ((BaseMount)m).Rider != null)
                    continue;

                if (m is BaseCreature)
                {
                    BaseCreature c = (BaseCreature)m;

                    if (c.IsDeadPet)
                    {
                        Mobile owner = c.ControlMaster;

                        //Pix: 10/7/04 - if we're stabled dead, then we shouldn't have
                        // any chance to be abandoned.
                        if (c.IsStabled)
                        {
                            c.OwnerAbandonTime = DateTime.MinValue;
                        }
                        else
                        {
                            if (owner == null || owner.Deleted || owner.Map != c.Map || !owner.InRange(c, 12) || !c.CanSee(owner) || !c.InLOS(owner))
                            {
                                if (c.OwnerAbandonTime == DateTime.MinValue)
                                    c.OwnerAbandonTime = DateTime.UtcNow;
                                else if ((c.OwnerAbandonTime + c.BondingAbandonDelay) <= DateTime.UtcNow)
                                    toRemove.Add(c);
                            }
                            else
                            {
                                c.OwnerAbandonTime = DateTime.MinValue;
                            }
                        }
                    }
                    else if (c.Controlled && c.Commandable && c.Loyalty > PetLoyalty.None && c.Map != Map.Internal)
                    {
                        Mobile owner = c.ControlMaster;

                        // changed loyalty decrement
                        if (DateTime.UtcNow >= c.LoyaltyCheck)
                        {
                            c.Loyalty -= Utility.RandomMinMax(7, 13); // loyalty redo
                            c.LoyaltyCheck = DateTime.UtcNow + TimeSpan.FromHours(1.0);

                            if (c.Loyalty <= PetLoyalty.Unhappy)
                            {
                                c.Say(1043270, c.Name); // * ~1_NAME~ looks around desperately *
                                c.PlaySound(c.GetIdleSound());
                            }
                        }

                        c.OwnerAbandonTime = DateTime.MinValue;

                        if (c.Loyalty <= PetLoyalty.None) // loyalty redo
                            toRelease.Add(c);
                    }

                    // added lines to check if a wild creature in a house region has to be removed or not
                    if (!c.Controlled && c.Region is HouseRegion && c.CanBeDamaged())
                    {
                        c.RemoveStep++;

                        if (c.RemoveStep >= 20)
                            toRemove.Add(c);
                    }
                    else
                    {
                        c.RemoveStep = 0;
                    }
                }
            }

            foreach (BaseCreature c in toRelease)
            {
                c.Say(1043255, c.Name); // ~1_NAME~ appears to have decided that is better off without a master!
                c.Loyalty = PetLoyalty.WonderfullyHappy;
                c.IsBonded = false;
                c.BondingBegin = DateTime.MinValue;
                c.OwnerAbandonTime = DateTime.MinValue;
                c.ControlTarget = null;
                //c.ControlOrder = OrderType.Release;
                if (c.IOBFollower)
                {
                    c.IOBDismiss(true);
                }
                else
                {
                    c.AIObject.DoOrderRelease(); // this will prevent no release of creatures left alone with AI disabled (and consequent bug of Followers)
                }
            }
            // added code to handle removing of wild creatures in house regions
            foreach (BaseCreature c in toRemove)
            {
                c.Delete();
            }

        }
    }

    public enum GeneVisibility
    {
        Invisible = 0,
        Tame = 1,
        Wild = 2
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class GeneAttribute : Attribute
    {
        private double m_LowFactor, m_HighFactor;
        private double m_SpawnMin, m_SpawnMax, m_BreedMin, m_BreedMax;
        private GeneVisibility m_Visibility;
        private double m_MinVariance;
        private string m_Name;

        public const double DefaultMinVariance = -1;

        public string Name
        {
            get
            {
                return m_Name;
            }
        }

        public double LowFactor
        {
            get
            {
                return m_LowFactor;
            }
        }

        public double HighFactor
        {
            get
            {
                return m_HighFactor;
            }
        }

        public double SpawnMin
        {
            get
            {
                return m_SpawnMin;
            }
        }

        public double SpawnMax
        {
            get
            {
                return m_SpawnMax;
            }
        }

        public double BreedMin
        {
            get
            {
                return m_BreedMin;
            }
        }

        public double BreedMax
        {
            get
            {
                return m_BreedMax;
            }
        }

        public GeneVisibility Visibility
        {
            get
            {
                return m_Visibility;
            }
        }

        public double MinVariance
        {
            get
            {
                return m_MinVariance;
            }
        }

        public GeneAttribute()
            : this("")
        {
        }

        public GeneAttribute(string name)
            : this(name, 20, 80)
        {
        }

        public GeneAttribute(string name, double spawnmin, double spawnmax)
            : this(name, spawnmin, spawnmax, spawnmin - (spawnmax - spawnmin) / 2, spawnmax + (spawnmax - spawnmin) / 2)
        {
        }

        public GeneAttribute(string name, double spawnmin, double spawnmax, double breedmin, double breedmax)
            : this(name, 0.05, 0.05, spawnmin, spawnmax, breedmin, breedmax, GeneVisibility.Invisible)
        {
        }

        public GeneAttribute(string name, double lowfactor, double highfactor, double spawnmin, double spawnmax, double breedmin, double breedmax,
            GeneVisibility vis)
            : this(name, lowfactor, highfactor, spawnmin, spawnmax, breedmin, breedmax, vis, DefaultMinVariance)
        {
        }

        public GeneAttribute(string name, double lowfactor, double highfactor, double spawnmin, double spawnmax, double breedmin, double breedmax,
            GeneVisibility vis, double minvariance)
        {
            m_LowFactor = lowfactor;
            m_HighFactor = highfactor;
            m_SpawnMin = spawnmin;
            m_SpawnMax = spawnmax;
            m_BreedMin = breedmin;
            m_BreedMax = breedmax;
            m_Visibility = vis;
            m_MinVariance = minvariance;
            m_Name = name;

            // sanity checks
            if (m_LowFactor < 0 && m_HighFactor < 0)
            {
                m_LowFactor = 0;
                m_HighFactor = 0;
            }
            if (m_SpawnMin > m_SpawnMax)
            {
                double t = m_SpawnMin;
                m_SpawnMin = m_SpawnMax;
                m_SpawnMax = t;
            }
            if (m_BreedMin > m_BreedMax)
            {
                double t = m_BreedMin;
                m_BreedMin = m_BreedMax;
                m_BreedMax = t;
            }

            if (m_MinVariance < -1)
                m_MinVariance = -1;
        }
    }
}
