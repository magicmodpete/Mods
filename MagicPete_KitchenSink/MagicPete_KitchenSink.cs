using System;
using System.Collections.Generic;
using ConsoleLib.Console;
using XRL;
using XRL.Rules;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts.Mutation
{
    //[Serializable] //Is it needed?
    public class MagicPete_KitchenSink : BaseMutation
    {
    	public override bool AllowStaticRegistration()
		{
			return true;
		}

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade))
			{
				return ID == AIGetOffensiveAbilityListEvent.ID || ID == PooledEvent<CommandEvent>.ID;
			}
			return true;
		}

		public override string GetDescription()
		{
			return "You GetDescription().";
		}

		public override string GetLevelText(int Level)
		{
			return "GetLevelText";
		}

		public int GetCooldown(int Level)
		{
			return 25; //TODO: scale with level once unique mutations can be boosted by rapid advancements and ego
		}

		public override void CollectStats(Templates.StatCollector stats, int Level)
		{
			/*stats.Set("Range", 12);
			stats.CollectCooldownTurns(MyActivatedAbility(ActivatedAbilityID), GetCooldown(Level));*/
		}

		public override bool HandleEvent(AIGetOffensiveAbilityListEvent E)
		{
			/*if (E.Distance > 3 && E.Distance <= 12 && IsMyActivatedAbilityAIUsable(ActivatedAbilityID))
			{
				E.Add("CommandMissileStrike");
			}*/
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(CommandEvent E)
		{
			/*if (E.Command == "CommandMissileStrike")
			{
				Cell cell = PickDestinationCell(12, RequireCombat: true, Label: "Launch Missile", Snap: true);
				if (cell == null)
				{
					return false;
				}

				GameObject widget = GameObjectFactory.Factory.CreateObject("Widget");
				widget.AddPart(new DelayedMissileStrike(ParentObject, 4, ParentObject.GetPhase()));

				cell.AddObject(widget);
				PlayWorldSound("Sounds/Missile/Fires/Heavy Weapons/sfx_missile_missileLauncher_fire");

				MissileWeaponVFXConfiguration vfx = MissileWeaponVFXConfiguration.next();
				CombatJuiceManager.startDelay();
				vfx.addStep(0, ParentObject.CurrentCell.Location);
				vfx.addStep(0, cell.Location);
				vfx.setPathProjectileVFX(0, "MissileWeaponsEffects/vls_laser", "duration::1;;beamColor0::#FFFFFF;;beamColor1::#FFFFFF");
				CombatJuiceManager.endDelay();
				CombatJuice.missileWeaponVFX(vfx);

				if (!ParentObject.IsPlayer())
				{
					ParentObject.Brain.RemoveGoalsDescendedFrom<IMovementGoal>();
					ParentObject.Brain.PushGoal(new FleeLocation(cell, (200 - ParentObject.Stat("MoveSpeed", 100)) * 3 / 100));
				}

				UseEnergy(1000, "Physical Mutation MissileStrike");
				CooldownMyActivatedAbility(ActivatedAbilityID, GetCooldown(Level));
			}*/
			return base.HandleEvent(E);
		}

		public override bool Mutate(GameObject GO, int Level)
		{
			//ActivatedAbilityID = AddMyActivatedAbility("Launch Missile", "CommandMissileStrike", "Physical Mutations");
			return base.Mutate(GO, Level);
		}

		public override bool Unmutate(GameObject GO)
		{
			//RemoveMyActivatedAbility(ref ActivatedAbilityID);
			return base.Unmutate(GO);
		}
    }
}