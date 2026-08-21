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
    [Serializable]
    public class MagicPete_KineticCharging : BaseMutation
    {
        public Guid KineticThrowAbilityID = Guid.Empty;
        public Guid KineticBombAbilityID = Guid.Empty;
        public Guid KineticWeaponAbilityID = Guid.Empty;

        public MagicPete_KineticCharging()
        {
        }

        public override bool CanLevel() => true;

        public override string GetDescription() => "You can infuse objects and weapons with raw kinetic energy, turning them into volatile explosives or deadly implements.";

        public override string GetLevelText(int Level)
        {
            return $"Kinetic Throw: Increased range and damage for thrown items. Items explode on impact.\n" +
                   $"Kinetic Bomb: Infuse objects to explode after 3 turns.\n" +
                   $"Kinetic Weapon: +{Level} melee damage bonus. Always active, can be toggled off.\n" +
                   $"Explosion Damage: {Level}d6 + {Level}\n" +
                   $"Range Bonus: +{Level}";
        }

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade)
                || ID == CommandEvent.ID;
        }

        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == "CommandKineticThrow")
            {
                HandleKineticThrow();
                return false;
            }
            if (E.Command == "CommandKineticBomb")
            {
                HandleKineticBomb();
                return false;
            }
            if (E.Command == "CommandToggleKineticWeapon")
            {
                ToggleKineticWeapon();
                return false;
            }
            return base.HandleEvent(E);
        }

        public override bool Mutate(GameObject GO, int Level)
        {
            KineticThrowAbilityID = AddMyActivatedAbility(
                Name: "Kinetic Throw",
                Command: "CommandKineticThrow",
                Class: "Mental Mutation",
                Description: "Charge a throwable item and hurl it with explosive force.",
                Icon: "T",
                UITileDefault: Renderable.UITile("kineticthrow.png", 'w', 'M')
            );
            KineticBombAbilityID = AddMyActivatedAbility(
                Name: "Kinetic Bomb",
                Command: "CommandKineticBomb",
                Class: "Mental Mutation",
                Description: "Charge a piece of furniture or a wall to explode after 3 turns.",
                Icon: "C",
                UITileDefault: Renderable.UITile("kineticcharge.png", 'w', 'M')
            );
            KineticWeaponAbilityID = AddMyActivatedAbility(
                Name: "Kinetic Weapon",
                Command: "CommandToggleKineticWeapon",
                Class: "Mental Mutation",
                Description: "Toggle your kinetic weapon charge.",
                Icon: "W",
                Toggleable: true,
                DefaultToggleState: true,
                ActiveToggle: true,
                UITileDefault: Renderable.UITile("kineticweapon.png", 'w', 'M')
            );
            return base.Mutate(GO, Level);
        }

        public override bool Unmutate(GameObject GO)
        {
            RemoveMyActivatedAbility(ref KineticThrowAbilityID);
            RemoveMyActivatedAbility(ref KineticBombAbilityID);
            RemoveMyActivatedAbility(ref KineticWeaponAbilityID);
            return base.Unmutate(GO);
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register("GetMeleeWeaponDamage");
            Registrar.Register("AIGetOffensiveMutationList");
            base.Register(Object, Registrar);
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "GetMeleeWeaponDamage")
            {
                if (IsKineticWeaponActive())
                {
                    E.SetParameter("Bonus", E.GetIntParameter("Bonus") + Level);
                }
            }
            else if (E.ID == "AIGetOffensiveMutationList")
            {
                int distance = E.GetIntParameter("Distance");
                if (distance <= Level + 10 && IsMyActivatedAbilityUsable(KineticThrowAbilityID))
                {
                    List<string> list = E.GetParameter("List") as List<string>;
                    if (list != null && !list.Contains("CommandKineticThrow"))
                    {
                        list.Add("CommandKineticThrow");
                    }
                }
            }
            return base.FireEvent(E);
        }

        private void ToggleKineticWeapon()
        {
            ToggleMyActivatedAbility(KineticWeaponAbilityID);
        }

        private bool IsKineticWeaponActive()
        {
            return IsMyActivatedAbilityToggledOn(KineticWeaponAbilityID);
        }

        private bool HandleKineticThrow()
        {
            GameObject body = ParentObject;
            
            GameObject item = body.GetFirstThrownWeapon();
            if (item == null)
            {
                Popup.Show("You must have an item in your thrown weapon slot.");
                return false;
            }

            // Target selection
            List<Cell> targetCells = PickLine(Level + 10, AllowVis.OnlyVisible);
            if (targetCells == null || targetCells.Count <= 1) return false;

            Cell targetCell = targetCells[targetCells.Count - 1];
            
            // Remove one item from stack to prevent charging the entire stack
            GameObject itemToThrow = item.RemoveOne();

            // Apply kinetic charge to item
            itemToThrow.AddPart(new MagicPete_KineticChargedItem(Level, body));
            
            // Perform throw
            body.PerformThrow(itemToThrow, targetCell);
            
            CooldownMyActivatedAbility(KineticThrowAbilityID, 10);
            body.UseEnergy(1000, "Mutation Mental Kinetic Charging");
            
            return true;
        }

        private bool HandleKineticBomb()
        {
            GameObject body = ParentObject;
            
            // Pick adjacent object
            Cell cell = PickDirection();
            if (cell == null) return false;

            GameObject target = null;
            foreach (var obj in cell.GetObjects())
            {
                if (obj.HasPart("Furniture") || obj.HasPart("Wall") || obj.HasPart("Door") || obj.HasTag("Wall") || obj.HasTag("Furniture") || (obj.Blueprint != null && (obj.Blueprint.Contains("Wall") || obj.Blueprint.Contains("Furniture"))))
                {
                    target = obj;
                    break;
                }
            }

            if (target == null)
            {
                AddPlayerMessage("No suitable target found for Kinetic Bomb.");
                return false;
            }

            if (target.HasPart(typeof(MagicPete_KineticExplosion)) || target.HasPart("MagicPete_KineticExplosion"))
            {
                AddPlayerMessage($"{target.The}{target.DisplayNameOnly} is already kinetically charged!");
                return false;
            }

            MagicPete_KineticExplosion explosion = new MagicPete_KineticExplosion(Level, body);
            target.AddPart(explosion);
            target.ParticleText($"[:{explosion.Timer}:]", 'Y');
            AddPlayerMessage($"{target.The}{target.DisplayNameOnly} begins to vibrate violently.");
            CooldownMyActivatedAbility(KineticBombAbilityID, 20);
            body.UseEnergy(1000, "Mutation Mental Kinetic Charging");

            return true;
        }
    }

    [Serializable]
    public class MagicPete_KineticChargedItem : IPart
    {
        public int Level = 1;
        public GameObject Creator;

        public MagicPete_KineticChargedItem()
        {
        }

        public MagicPete_KineticChargedItem(int level, GameObject creator)
        {
            Level = level;
            Creator = creator;
        }

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade)
                || ID == GetDisplayNameEvent.ID
                || ID == GetShortDescriptionEvent.ID;
        }

        public override bool HandleEvent(GetDisplayNameEvent E)
        {
            if (E.Understood())
            {
                E.AddTag("{{M|[kinetically charged]}}");
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            E.Postfix.Append("\n{{M|It crackles with volatile kinetic energy and will explode upon impact.}}");
            return base.HandleEvent(E);
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register("ProjectileHit");
            base.Register(Object, Registrar);
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "ProjectileHit")
            {
                Explode();
                if (GameObject.Validate(ParentObject))
                {
                    ParentObject.Destroy();
                }
                return false;
            }
            return base.FireEvent(E);
        }

        private void Explode()
        {
            if (GameObject.Validate(ParentObject))
            {
                GameObject obj = ParentObject;
                obj.RemovePart(this);
                obj.Explode(Force: 0, Owner: Creator, BonusDamage: $"{Level}d6+{Level}");
            }
        }
    }

    [Serializable]
    public class MagicPete_KineticExplosion : IPart
    {
        public int Level = 1;
        public int Timer = 3;
        public GameObject Creator;

        [NonSerialized]
        private long lastTickTurn = -1;

        public MagicPete_KineticExplosion()
        {
            lastTickTurn = The.Game != null ? The.Game.Turns : -1;
        }

        public MagicPete_KineticExplosion(int level, GameObject creator)
        {
            Level = level;
            Creator = creator;
            lastTickTurn = The.Game != null ? The.Game.Turns : -1;
        }

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade)
                || ID == EndTurnEvent.ID
                || ID == GetDisplayNameEvent.ID
                || ID == GetShortDescriptionEvent.ID;
        }

        public override bool HandleEvent(EndTurnEvent E)
        {
            Tick();
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetDisplayNameEvent E)
        {
            if (E.Understood())
            {
                E.AddTag($"{{R|[vibrating: {Timer}t]}}");
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            E.Postfix.Append($"\n{{R|It vibrates violently with unstable kinetic energy ({Timer} turns remaining until detonation)!}}");
            return base.HandleEvent(E);
        }

        public void Tick()
        {
            long currentTurn = The.Game != null ? The.Game.Turns : -1;
            if (currentTurn != -1 && currentTurn == lastTickTurn)
            {
                return;
            }
            lastTickTurn = currentTurn;

            Timer--;
            if (Timer > 0 && GameObject.Validate(ParentObject) && ParentObject.CurrentCell != null)
            {
                ParentObject.ParticleText($"[:{Timer}:]", 'Y');
                AddPlayerMessage($"{ParentObject.The}{ParentObject.DisplayNameOnly} will detonate in {Timer} {(Timer == 1 ? "turn" : "turns")}.");
            }
            if (Timer <= 0)
            {
                Explode();
            }
        }

        private void Explode()
        {
            if (GameObject.Validate(ParentObject))
            {
                GameObject obj = ParentObject;
                obj.RemovePart(this);
                obj.Explode(Force: 0, Owner: Creator, BonusDamage: $"{Level + 2}d8+{Level}");
                if (GameObject.Validate(obj))
                {
                    obj.Destroy();
                }
            }
        }
    }
}