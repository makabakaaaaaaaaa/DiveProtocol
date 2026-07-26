using UnityEngine;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Marks corpses, pollution, biomass, or anomalies that can feed Humus Symbiosis.
    /// </summary>
    public sealed class SymbiosisSource : MonoBehaviour
    {
        [SerializeField] private SymbiosisSourceType sourceType;
        [SerializeField, Min(0.1f)] private float radius = 2.5f;
        [SerializeField] private bool grantsStacks = true;
        [SerializeField] private string hintText;

        public SymbiosisSourceType SourceType => sourceType;
        public float Radius => radius;
        public bool GrantsStacks => grantsStacks;
        public string HintText => hintText;
    }
}
