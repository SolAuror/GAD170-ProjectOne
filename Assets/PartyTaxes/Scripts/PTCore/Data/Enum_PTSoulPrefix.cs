using UnityEngine;

namespace PartyTaxes
{
    public enum SoulAttribute { Might, Agility, Constitution, Sense, Luck }

    /// <summary>
    /// A name prefix that can be randomly applied to a soul, e.g. "Mighty Salen the Human".
    /// The modifier is clamped to the range [-2, +3] and applied to one attribute.
    /// Configure instances in the PTSoulGen Inspector array.
    /// </summary>
    [System.Serializable]
    public class PTSoulPrefix
    {
        [Tooltip("The word that appears before the soul's first name, e.g. 'Mighty'.")]
        public string prefixName = "Strong";

        [Tooltip("Which attribute this prefix modifies.")]
        public SoulAttribute attribute = SoulAttribute.Might;

        [Tooltip("Stat modifier. Positive = bonus, negative = debuff. Clamped to [-2, +3].")]
        [Range(-2, 3)]
        public int modifier = 1;

        /// <summary>
        /// Applies this prefix's modifier to the given soul (clamped to [1, 99]).
        /// </summary>
        public void Apply(PTSoul soul)
        {
            int amount = Mathf.Clamp(modifier, -2, 3);
            switch (attribute)
            {
                case SoulAttribute.Might:        soul.atrMight        = Mathf.Clamp(soul.atrMight        + amount, 1, 99); break;
                case SoulAttribute.Agility:      soul.atrAgility      = Mathf.Clamp(soul.atrAgility      + amount, 1, 99); break;
                case SoulAttribute.Constitution: soul.atrConstitution = Mathf.Clamp(soul.atrConstitution + amount, 1, 99); break;
                case SoulAttribute.Sense:        soul.atrSense        = Mathf.Clamp(soul.atrSense        + amount, 1, 99); break;
                case SoulAttribute.Luck:         soul.atrLuck         = Mathf.Clamp(soul.atrLuck         + amount, 1, 99); break;
            }
        }
    }
}
