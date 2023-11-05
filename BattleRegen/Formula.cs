using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace BattleRegen
{
    /// <summary>
    /// Represents a formula used by the mod for health regen calculation. When the game starts, Battle Regeneration will search for
    /// all *.battleregen.cs files inside the ModuleData folder of every mod, if it exists, and then compile and load them,
    /// constructing a single instance of each class that derives from <see cref="Formula"/> for use by the main mod.
    /// </summary>
    public abstract class Formula : IComparable<Formula>
    {
        private static List<Formula>? formulas;

        /// <summary>
        /// Returns a list of formulas loaded by the mod.
        /// </summary>
        public static IEnumerable<Formula> Formulas
        {
            get
            {
                if (formulas == null)
                {
                    formulas = new List<Formula>();
                    foreach (var type in AccessTools.AllTypes().AsParallel().Where(x => x.IsSubclassOf(typeof(Formula))))
                    {
                        try
                        {
                            if (!typeof(Formula).IsAssignableFrom(type))
                                Debug.Print($"[BattleRegen] {type.FullName} is not a Formula subtype");
                            else if (formulas.Any(x => x.GetType() == type))
                                Debug.Print($"[BattleRegen] {type.FullName} is already added");
                            else
                            {
                                var formula = (Activator.CreateInstance(type) as Formula)!;
                                formulas.RemoveAll(x => x.Id == formula.Id);
                                formulas.Add(formula);
                            }
                        }
                        catch (Exception e)
                        {
                            string error = $"[BattleRegen] Failed to add an instance of {type.FullName} as a formula due to an exception.\n{e}";
                            Debug.Print(error);
                            InformationManager.DisplayMessage(new InformationMessage(error));
                        }
                    }
                }
                return formulas;
            }
        }

        /// <summary>
        /// The name of the formula.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The ID of the formula.
        /// </summary>
        public abstract string Id { get; }

        /// <summary>
        /// The priority of the formula in a list of <see cref="Formula"/>s. A <see cref="Formula"/> with a lower
        /// <see cref="Priority">Priority</see> is placed ahead in such a list. The default value is 0.
        /// </summary>
        public virtual int Priority => 0;

        /// <summary>
        /// Returns the localized name of the formula.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return new TextObject(Name).ToString();
        }

        /// <summary>
        /// Performs calculation with this formula, given regeneration data, and returns a value. Subclasses should provide their own implementation of this method,
        /// for this method returns zero by default. All parameters are passed by reference.
        /// </summary>
        /// <param name="data">The regeneration data information. See <see cref="RegenDataInfo"/>.</param>
        /// <returns>The modified regeneration rate.</returns>
        public abstract float Calculate(ref RegenDataInfo data);

        /// <summary>
        /// Compares the current formula with the other formula. Formulas are sorted first by Priority and then by Id.
        /// </summary>
        /// <param name="other">The object to be compared to.</param>
        /// <returns>A value indicating the current object's relative position to the other.</returns>
        public int CompareTo(Formula other)
        {
            int comparison = Priority.CompareTo(other.Priority);
            return comparison == 0 ? Id.CompareTo(other.Id) : comparison;
        }
    }
}
