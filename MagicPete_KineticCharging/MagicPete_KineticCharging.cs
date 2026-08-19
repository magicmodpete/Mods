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
            Registrar.Register("CommandKineticThrow");
            Registrar.Register("CommandKineticBomb");
            Registrar.Register("CommandToggleKineticWeapon");
            Registrar.Register("GetMeleeWeaponDamage");
            base.Register(Object, Registrar);
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "CommandKineticThrow")
            {
                return HandleKineticThrow();
            }
            else if (E.ID == "CommandKineticBomb")
            {
                return HandleKineticBomb();
            }
            else if (E.ID == "CommandToggleKineticWeapon")
            {
                ToggleKineticWeapon();
            }
            else if (E.ID == "GetMeleeWeaponDamage")
            {
                if (IsKineticWeaponActive())
                {
                    E.SetParameter("Bonus", E.GetIntParameter("Bonus") + Level);
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
            
            // Apply kinetic charge to item
            item.AddPart(new MagicPete_KineticChargedItem(Level, body));
            
            // Perform throw
            body.PerformThrow(item, targetCell);
            
            CooldownMyActivatedAbility(KineticThrowAbilityID, 10);
            body.UseEnergy(1000, "Mutation");
            
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
                if (obj.HasPart("Furniture") || obj.HasPart("Wall") || (obj.Blueprint != null && (obj.Blueprint.Contains("Wall") || obj.Blueprint.Contains("Furniture"))))
                {
                    target = obj;
                    break;
                }
            }

            if (target == null)
            {
                Popup.Show("You must target furniture or a wall.");
                return false;
            }

            target.AddPart(new MagicPete_KineticExplosion(Level, body));
            Popup.Show($"{target.The}{target.DisplayNameOnly} begins to vibrate violently!");
            
            CooldownMyActivatedAbility(KineticBombAbilityID, 20);
            body.UseEnergy(1000, "Mutation");

            return true;
        }
    }

    [Serializable]
    public class MagicPete_KineticChargedItem : IPart
    {
        public int Level;
        public GameObject Creator;

        public MagicPete_KineticChargedItem()
        {
        }

        public MagicPete_KineticChargedItem(int level, GameObject creator)
        {
            Level = level;
            Creator = creator;
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
                ParentObject.Destroy();
                return false;
            }
            return base.FireEvent(E);
        }

        private void Explode()
        {
            if (ParentObject != null)
            {
                ParentObject.Explode(Force: 10000, Owner: Creator, BonusDamage: $"{Level}d6+{Level}");
            }
        }
    }

    [Serializable]
    public class MagicPete_KineticExplosion : IPart
    {
        public int Level;
        public int Timer = 3;
        public GameObject Creator;

        public MagicPete_KineticExplosion()
        {
        }

        public MagicPete_KineticExplosion(int level, GameObject creator)
        {
            Level = level;
            Creator = creator;
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register("EndTurn");
            base.Register(Object, Registrar);
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "EndTurn")
            {
                Timer--;
                if (Timer <= 0)
                {
                    Explode();
                }
            }
            return base.FireEvent(E);
        }

        private void Explode()
        {
            if (ParentObject != null)
            {
                ParentObject.Explode(Force: 10000, Owner: Creator, BonusDamage: $"{Level + 2}d8+{Level}");
                ParentObject.Destroy();
            }
        }
    }
}