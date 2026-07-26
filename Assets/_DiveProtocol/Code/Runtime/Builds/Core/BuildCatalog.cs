using System.Collections.Generic;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Static runtime catalog for the first playable build set.
    /// </summary>
    public static class BuildCatalog
    {
        private static readonly List<BuildUpgradeDefinition> Definitions = new()
        {
            new(
                BuildUpgradeId.RedMarrow_Overdraft,
                BuildBranch.RedMarrow,
                BuildUpgradeKind.Core,
                "赤髓透支",
                "你的血液会替代钥匙、弹药与意志。",
                "可消耗 HP 制造弹药；可对允许的非主线门执行血债强开；HP 低于 35% 时，枪械伤害 +20%，移动速度 +12%。",
                0),
            new(
                BuildUpgradeId.RedMarrow_CoagulationReflex,
                BuildBranch.RedMarrow,
                BuildUpgradeKind.Component,
                "凝血反射",
                "主动失血后短暂变硬。",
                "玩家主动消耗 HP 后，3 秒内受到伤害降低 30%。",
                1,
                BuildUpgradeId.RedMarrow_Overdraft),
            new(
                BuildUpgradeId.RedMarrow_ExcessAdrenaline,
                BuildBranch.RedMarrow,
                BuildUpgradeKind.Component,
                "过量肾上腺",
                "濒危时动作变得更快。",
                "HP 低于 30% 时，Reload Speed +30%，Interaction Speed +25%。",
                1,
                BuildUpgradeId.RedMarrow_Overdraft),
            new(
                BuildUpgradeId.RedMarrow_BloodBulletCompression,
                BuildBranch.RedMarrow,
                BuildUpgradeKind.Component,
                "血弹压缩",
                "把更多东西塞进子弹里，也把代价塞进身体里。",
                "用 HP 制造弹药时额外获得 2 发弹药，但下一次治疗效果降低 50%。",
                1,
                BuildUpgradeId.RedMarrow_Overdraft),
            new(
                BuildUpgradeId.RedMarrow_OrganCollateral,
                BuildBranch.RedMarrow,
                BuildUpgradeKind.Component,
                "器官抵押",
                "身体会自动拿未来换现在。",
                "每关首次 HP 低于 20% 时立即恢复 10 HP，但本关最大 HP 降低 10 点。",
                1,
                BuildUpgradeId.RedMarrow_Overdraft),

            new(
                BuildUpgradeId.OpticNerve_Calibration,
                BuildBranch.OpticNerve,
                BuildUpgradeKind.Core,
                "视神经校准",
                "你的眼睛会先于意识锁定危险。",
                "瞄准敌人 0.8 秒后施加标记；被标记敌人受到枪械伤害 +15%；攻击被标记敌人时有 20% 概率返还 1 发弹药。",
                0),
            new(
                BuildUpgradeId.OpticNerve_CalmShot,
                BuildBranch.OpticNerve,
                BuildUpgradeKind.Component,
                "冷静射击",
                "安静时，枪口知道自己该去哪里。",
                "玩家站立或低速移动时开火，枪械伤害 +20%。",
                1,
                BuildUpgradeId.OpticNerve_Calibration),
            new(
                BuildUpgradeId.OpticNerve_JointRupture,
                BuildBranch.OpticNerve,
                BuildUpgradeKind.Component,
                "关节破坏",
                "连续命中会让目标失去节奏。",
                "连续命中同一个敌人 2 次后，敌人移动速度降低 35%，持续 3 秒。",
                1,
                BuildUpgradeId.OpticNerve_Calibration),
            new(
                BuildUpgradeId.OpticNerve_MarkRecycle,
                BuildBranch.OpticNerve,
                BuildUpgradeKind.Component,
                "标记回收",
                "被确认的目标会把资源还给你。",
                "击杀被标记敌人时恢复 1 发弹药，并触发 2 秒视野增强事件。",
                1,
                BuildUpgradeId.OpticNerve_Calibration),
            new(
                BuildUpgradeId.OpticNerve_SafeDistance,
                BuildBranch.OpticNerve,
                BuildUpgradeKind.Component,
                "安全距离",
                "距离给判断留出余地。",
                "玩家与敌人距离超过 6 Unity Units 时命中，有 10% 概率造成 +50% 枪械伤害。",
                1,
                BuildUpgradeId.OpticNerve_Calibration),

            new(
                BuildUpgradeId.Humus_Sympathy,
                BuildBranch.HumusSymbiosis,
                BuildUpgradeKind.Core,
                "腐殖共感",
                "腐烂物不再排斥你。它们会认出你。",
                "靠近尸体、污染物或生物组织时获得共感层数；每层环境伤害 -5%；每层对近距离敌人造成 1 点/秒污染伤害；最多 5 层。",
                0),
            new(
                BuildUpgradeId.Humus_DeadMatterWhisper,
                BuildBranch.HumusSymbiosis,
                BuildUpgradeKind.Component,
                "死物低语",
                "死去的东西开始说话。",
                "检查尸体或异常残留时，可以读取额外提示文本。",
                1,
                BuildUpgradeId.Humus_Sympathy),
            new(
                BuildUpgradeId.Humus_PollutionCoat,
                BuildBranch.HumusSymbiosis,
                BuildUpgradeKind.Component,
                "污染外衣",
                "靠近你时，它们会先碰到别的东西。",
                "共感层数 >= 3 时，敌人第一次攻击玩家有 30% 概率被短暂硬直。",
                1,
                BuildUpgradeId.Humus_Sympathy),
            new(
                BuildUpgradeId.Humus_CadaverDelay,
                BuildBranch.HumusSymbiosis,
                BuildUpgradeKind.Component,
                "借尸拖延",
                "复苏的尸体会迟疑。",
                "尸体复活时可进入 4 秒临时混乱状态，等待后续复活 AI 接入。",
                1,
                BuildUpgradeId.Humus_Sympathy),
            new(
                BuildUpgradeId.Humus_AbnormalMetabolism,
                BuildBranch.HumusSymbiosis,
                BuildUpgradeKind.Component,
                "异常代谢",
                "异常环境开始像营养液一样流动。",
                "处于 AbnormalZone 内时缓慢恢复 HP，但武器精准度降低。",
                1,
                BuildUpgradeId.Humus_Sympathy)
        };

        private static readonly Dictionary<BuildUpgradeId, BuildUpgradeDefinition> Lookup = BuildLookup();

        public static IReadOnlyList<BuildUpgradeDefinition> AllDefinitions => Definitions;

        public static bool TryGet(BuildUpgradeId id, out BuildUpgradeDefinition definition)
        {
            return Lookup.TryGetValue(id, out definition);
        }

        public static BuildUpgradeDefinition Get(BuildUpgradeId id)
        {
            return Lookup[id];
        }

        private static Dictionary<BuildUpgradeId, BuildUpgradeDefinition> BuildLookup()
        {
            Dictionary<BuildUpgradeId, BuildUpgradeDefinition> lookup = new();
            for (int i = 0; i < Definitions.Count; i++)
            {
                lookup[Definitions[i].Id] = Definitions[i];
            }

            return lookup;
        }
    }
}
