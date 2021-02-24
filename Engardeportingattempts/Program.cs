using System;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using System.Threading.Tasks;
using Noggog;
//TODO: delete this:
#pragma warning disable CA1416
namespace Engardeportingattempts
{
    public class Program
    {
        static ModKey Engarde = ModKey.FromNameAndExtension("Engarde.esp");
        static Lazy<Settings> Settings = null!;
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetAutogeneratedSettings(
                    nickname: "Settings",
                    path: "settings.json",
                    out Settings)
                .Run(args, new RunPreferences()
                {
                    ActionsForEmptyArgs = new RunDefaultPatcher()
                    {
                        IdentifyingModKey = "MCTPatch.esp",
                        TargetRelease = GameRelease.SkyrimSE,
                    }
                });
        }
        private static void PatchArmor(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, Dictionary<string, FormKey> mct_keywords)
        {
            foreach (var armor in state.LoadOrder.PriorityOrder.WinningOverrides<IArmorGetter>())
            {
                if (!armor.TemplateArmor.IsNull)
                {
                    continue;
                }
                if (!armor.BodyTemplate?.FirstPersonFlags.HasFlag(BipedObjectFlag.Body) ?? true)
                {
                    continue;
                }
                if (armor.BodyTemplate!.ArmorType == ArmorType.LightArmor)
                {
                    var armorCopy = state.PatchMod.Armors.GetOrAddAsOverride(armor);
                    if (armorCopy.Keywords == null)
                    {
                        armorCopy.Keywords = new ExtendedList<IFormLink<IKeywordGetter>>();
                    }
                    mct_keywords.TryGetValue("MCT_StaggerResist1", out FormKey staggerResistKeyword);
                    armorCopy.Keywords.Add(staggerResistKeyword);
                }
                else if (armor.BodyTemplate!.ArmorType == ArmorType.HeavyArmor)
                {
                    var armorCopy = state.PatchMod.Armors.GetOrAddAsOverride(armor);
                    if (armorCopy.Keywords == null)
                    {
                        armorCopy.Keywords = new ExtendedList<IFormLink<IKeywordGetter>>();
                    }
                    mct_keywords.TryGetValue("MCT_StaggerResist2", out FormKey staggerResistKeyword);
                    mct_keywords.TryGetValue("MCT_ArmoredKW", out FormKey armoredKeyword);
                    armorCopy.Keywords.Add(staggerResistKeyword);
                    armorCopy.Keywords.Add(armoredKeyword);
                }
            }
        }
        /*private static readonly (string Key, uint Id)[] keywordsTuple =
        {
            ("MCT_ArmoredKW", 0x0028FF),
            ("MCT_WeakAgainstArmored", 0x0E3805),
            ("MCT_CanCritHigh", 0x0E3806),
            ("MCT_CanCritMed", 0x111124),
            ("MCT_CanCritLow", 0x111125),
            ("MCT_CritImmune", 0x111126
            "MCT_InjuryAttackSpeed", 0x13EA56
            "MCT_InjuryBleed", 0x13EA57
            "MCT_InjuryStun", 0x13EA58
            "MCT_InjuryKnockDown", 0x13EA59
            "MCT_InjuryMoveSpeed", 0x13EA5A
            "MCT_PowerAttackCoolDownKW", 0x22C9DC
            "MCT_StaggerImmune", 0x24A510
            "MCT_StaggerResist0", 0x2561C8
            "MCT_StaggerResist1", 0x2561C9
            "MCT_StaggerResist2", 0x2561CA
            "MCT_StaggerResist3", 0x2561CB
            "MCT_StaggerResist4", 0x2561CC
            "MCT_StaggerPower0", 0x25775B
            "MCT_StaggerPower1", 0x25775C
            "MCT_StaggerPower2", 0x25775D
            "MCT_StaggerPower3", 0x25775E
            "MCT_StaggerPower4", 0x25775F
            "MCT_SprintAttack", 0x26D5A1
            "MCT_NormalAttackRight", 0x26E067
            "MCT_NormalAttackLeft", 0x26E068
            "MCT_StompAttack", 0x2700B5
            "MCT_GiantRaceKW", 0x270618
            "MCT_StaminaControlledKW", 0x27210E
            "MCT_CanEnrage", 0x272675
            "MCT_InjuryEffect", 0x272BD9
            "MCT_DragonRaceKW", 0x273140
            "MCT_DragonTailAttackLeft", 0x27416B
            "MCT_DragonTailAttackRight", 0x27416D
            "MCT_DragonTailAttack", 0x27416E
             "MCT_BlockableSpell", 0x277748
             "MCT_PowerBlockingKW", 0x27DDB1
             "MCT_VerticalAttack", 0x2808D7
             "MCT_PenetratesArmorKW", 0x281901
             "MCT_WerewolfRaceKW", 0x28DB00
             "MCT_PaddedKW", 0x0028FD
             "MCT_DefensiveAttack", 0x2936A2
             "MCT_SweepAttack", 0x294169
        };*/
        private static void ChangeGlobalShortValue(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, IGlobalGetter global, short? value)
        {
                var globalCopy = state.PatchMod.Globals.GetOrAddAsOverride(global);
                var globalShort = (IGlobalShort)globalCopy;
                globalShort.Data = value;
        }

        private static readonly (string, uint)[] globalIDs =
        {
            ("sprintToSneak", 0x289FB6),
            ("attackSpeedFix", 0x24747E),
            ("playerAttackControl", 0x24644D),
            ("powerAttackControl", 0x24644E),
            ("staggerByArrow", 0x2551A0),
            ("powerAttackCooldown", 0x2659C0),
            ("CGOIntegration", 0x28A519)
        };

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {

            Dictionary<string, FormKey> mct_keywords;
            Dictionary<string, IGlobalShortGetter> globals = new Dictionary<string, IGlobalShortGetter>();

            if (state.LoadOrder.TryGetIfEnabled(Engarde, out var listing))
            {
                mct_keywords = listing.Mod!.Keywords.ToDictionary(x => x.EditorID!, x => x.FormKey);
            }
            else
            {
                throw new Exception("Engarde.esp not active in your load order or doesn`t exist!");
            }


            foreach(var globalID in globalIDs)
            {
                FormKey globalForm = Engarde.MakeFormKey(globalID.Item2);
                if (!state.LinkCache.TryResolve<IGlobalShortGetter>(globalForm, out var globalLink))
                    throw new Exception($"Unable to find required global: {globalForm}");
                globals.Add(globalLink.EditorID!, globalLink);
            }

            IGlobalShortGetter? global;
            if (globals.TryGetValue("MCT_SprintToSneakEnabled", out global))
            {
                if (Settings.Value.sprintToSneak)
                {
                    ChangeGlobalShortValue(state, global, 1);
                }
                else
                {
                    ChangeGlobalShortValue(state, global, 0);
                }
            }


            if (Settings.Value.fixAttackSpeed)
            {
                if (globals.TryGetValue("MCT_AttackSpeedFixEnabled", out global))
                {
                    ChangeGlobalShortValue(state, global, 1);
                }
            }
            else
            {
                if (globals.TryGetValue("MCT_AttackSpeedFixEnabled", out global))
                {
                    ChangeGlobalShortValue(state, global, 0);
                }
            }

            if (Settings.Value.basicAttackTweaks)
            {
                if (globals.TryGetValue("MCT_PlayerAttackControlEnabled", out global))
                {
                    ChangeGlobalShortValue(state, global, 1);
                }
            }
            else
            {
                if (globals.TryGetValue("MCT_PlayerAttackControlEnabled", out global))
                {
                    ChangeGlobalShortValue(state, global, 0);
                }
            }

            PatchArmor(state, mct_keywords);

        }

       
    }
}
