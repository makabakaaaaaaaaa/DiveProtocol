using System.Collections.Generic;

namespace DiveProtocol.Builds
{
    /// <summary>Static runtime definitions for the fixed four-node build progression.</summary>
    public static class BuildCatalog
    {
        private static readonly List<BuildUpgradeDefinition> Definitions = new()
        {
            new(BuildUpgradeId.RedMarrow_Overdraft, BuildBranch.RedMarrow, BuildUpgradeKind.Core,
                "Blood Debt Core", "Low-health damage +20%, move speed +15%.",
                "At or below 50% HP, gain 20% gun damage and 15% movement speed.", 0),
            new(BuildUpgradeId.RedMarrow_BloodBulletCompression, BuildBranch.RedMarrow, BuildUpgradeKind.Component,
                "Blood Compression", "Max HP -10; gun damage +15%.",
                "Compresses the body into a more dangerous weapon platform.", 1, BuildUpgradeId.RedMarrow_Overdraft),
            new(BuildUpgradeId.RedMarrow_CoagulationReflex, BuildBranch.RedMarrow, BuildUpgradeKind.Component,
                "Coagulation Reflex", "Lethal damage leaves you at 1 HP. 60s cooldown.",
                "A last reflex keeps the heart beating through a lethal hit.", 1, BuildUpgradeId.RedMarrow_Overdraft),
            new(BuildUpgradeId.RedMarrow_OrganCollateral, BuildBranch.RedMarrow, BuildUpgradeKind.Component,
                "Blood Debt Transfer", "Health-spend actions cost 50% less HP.",
                "Moves the immediate cost of blood debt away from the present moment.", 1, BuildUpgradeId.RedMarrow_Overdraft),
            new(BuildUpgradeId.RedMarrow_ExcessAdrenaline, BuildBranch.RedMarrow, BuildUpgradeKind.Component,
                "Predatory Recovery", "Kills restore 5 HP.",
                "Every confirmed kill buys the body a little more time.", 2, BuildUpgradeId.RedMarrow_Overdraft),
            new(BuildUpgradeId.RedMarrow_BloodEconomy, BuildBranch.RedMarrow, BuildUpgradeKind.Component,
                "Lean Circulation", "Health-spend actions cost another 25% less HP.",
                "A smaller payment keeps each blood action viable deeper in the facility.", 2, BuildUpgradeId.RedMarrow_Overdraft),
            new(BuildUpgradeId.RedMarrow_LowHealthAmplifier, BuildBranch.RedMarrow, BuildUpgradeKind.Component,
                "Adrenal Overload", "Further improves low-health damage and movement.",
                "Makes the Blood Debt threshold more dangerous to everything except you.", 2, BuildUpgradeId.RedMarrow_Overdraft),
            new(BuildUpgradeId.RedMarrow_SacrificeProtocol, BuildBranch.RedMarrow, BuildUpgradeKind.Component,
                "Sacrifice Protocol", "At 20% HP, gain +50% damage for 10 seconds.",
                "The final reserve converts imminent collapse into a brief killing window.", 3, BuildUpgradeId.RedMarrow_Overdraft),

            new(BuildUpgradeId.OpticNerve_Calibration, BuildBranch.OpticNerve, BuildUpgradeKind.Core,
                "Clearance Core", "Aim at an enemy to mark it; marked targets take +15% damage.",
                "Sustained aim locks a target after 0.8 seconds and exposes it to extra gun damage.", 0),
            new(BuildUpgradeId.OpticNerve_CalmShot, BuildBranch.OpticNerve, BuildUpgradeKind.Component,
                "Neural Overclock", "Reload speed +25%.",
                "The build modifier is ready for the existing reload path when that weapon action is enabled.", 1, BuildUpgradeId.OpticNerve_Calibration),
            new(BuildUpgradeId.OpticNerve_JointRupture, BuildBranch.OpticNerve, BuildUpgradeKind.Component,
                "Weak Point Analysis", "Marked hits gain a 25% chance to deal critical damage.",
                "Marked anatomy reveals a second, more profitable line of fire.", 1, BuildUpgradeId.OpticNerve_Calibration),
            new(BuildUpgradeId.OpticNerve_MarkRecycle, BuildBranch.OpticNerve, BuildUpgradeKind.Component,
                "Recovery Protocol", "Killing a marked enemy restores 1 ammo.",
                "Confirmed targets return a single round to the weapon reserve.", 1, BuildUpgradeId.OpticNerve_Calibration),
            new(BuildUpgradeId.OpticNerve_SafeDistance, BuildBranch.OpticNerve, BuildUpgradeKind.Component,
                "Long Sight", "Gun damage +20% beyond 6m.",
                "Distance becomes a stable advantage instead of an uncertain gamble.", 2, BuildUpgradeId.OpticNerve_Calibration),
            new(BuildUpgradeId.OpticNerve_MarkPersistence, BuildBranch.OpticNerve, BuildUpgradeKind.Component,
                "Persistent Trace", "Marks last 8 seconds instead of 5.",
                "A target remains legible long enough to turn information into action.", 2, BuildUpgradeId.OpticNerve_Calibration),
            new(BuildUpgradeId.OpticNerve_AimDiscipline, BuildBranch.OpticNerve, BuildUpgradeKind.Component,
                "Aim Discipline", "Marked-target damage gains another +10%.",
                "Clear sight and patient aim compound into a more reliable kill.", 2, BuildUpgradeId.OpticNerve_Calibration),
            new(BuildUpgradeId.OpticNerve_PerfectPrediction, BuildBranch.OpticNerve, BuildUpgradeKind.Component,
                "Perfect Prediction", "The first hit on each marked target is a critical hit.",
                "The opening shot arrives where the target was about to be.", 3, BuildUpgradeId.OpticNerve_Calibration),

            new(BuildUpgradeId.Humus_Sympathy, BuildBranch.HumusSymbiosis, BuildUpgradeKind.Core,
                "Symbiosis Core", "Adaptation regeneration restores HP every second.",
                "Regeneration is 1/2/3/4 HP per second in Drainage/Containment/Maintenance/Facility Core.", 0),
            new(BuildUpgradeId.Humus_DeadMatterWhisper, BuildBranch.HumusSymbiosis, BuildUpgradeKind.Component,
                "Decay Metabolism", "Adaptation regeneration gains +1 HP per second.",
                "The body turns the facility's pressure into slow, persistent repair.", 1, BuildUpgradeId.Humus_Sympathy),
            new(BuildUpgradeId.Humus_PollutionCoat, BuildBranch.HumusSymbiosis, BuildUpgradeKind.Component,
                "Living Adaptation", "Incoming damage reduced by 15%.",
                "The skin begins answering impacts before they fully register.", 1, BuildUpgradeId.Humus_Sympathy),
            new(BuildUpgradeId.Humus_CadaverDelay, BuildBranch.HumusSymbiosis, BuildUpgradeKind.Component,
                "Parasitic Response", "Enemy hits have a 25% chance to trigger a small counterattack.",
                "Damage still lands; the attacker pays for staying close.", 1, BuildUpgradeId.Humus_Sympathy),
            new(BuildUpgradeId.Humus_AbnormalMetabolism, BuildBranch.HumusSymbiosis, BuildUpgradeKind.Component,
                "Accelerated Regeneration", "Adaptation regeneration gains another +1 HP per second.",
                "The healing cadence becomes more insistent with every lower level.", 2, BuildUpgradeId.Humus_Sympathy),
            new(BuildUpgradeId.Humus_ExpandedVessel, BuildBranch.HumusSymbiosis, BuildUpgradeKind.Component,
                "Expanded Vessel", "Max HP +20.",
                "The body grows a larger margin for continued adaptation.", 2, BuildUpgradeId.Humus_Sympathy),
            new(BuildUpgradeId.Humus_EnvironmentalTolerance, BuildBranch.HumusSymbiosis, BuildUpgradeKind.Component,
                "Environmental Tolerance", "Environmental damage reduced by 30%.",
                "Hazards become less persuasive without requiring a special zone system.", 2, BuildUpgradeId.Humus_Sympathy),
            new(BuildUpgradeId.Humus_CompleteSymbiosis, BuildBranch.HumusSymbiosis, BuildUpgradeKind.Component,
                "Complete Symbiosis", "Facility Core regeneration is fixed at 4 HP per second.",
                "The final adaptation reaches the Facility Core's intended sustained-recovery rate.", 3, BuildUpgradeId.Humus_Sympathy)
        };

        private static readonly Dictionary<BuildUpgradeId, BuildUpgradeDefinition> Lookup = BuildLookup();

        public static IReadOnlyList<BuildUpgradeDefinition> AllDefinitions => Definitions;

        public static bool TryGet(BuildUpgradeId id, out BuildUpgradeDefinition definition) => Lookup.TryGetValue(id, out definition);

        public static BuildUpgradeDefinition Get(BuildUpgradeId id) => Lookup[id];

        private static Dictionary<BuildUpgradeId, BuildUpgradeDefinition> BuildLookup()
        {
            var lookup = new Dictionary<BuildUpgradeId, BuildUpgradeDefinition>();
            foreach (BuildUpgradeDefinition definition in Definitions) lookup[definition.Id] = definition;
            return lookup;
        }
    }
}
