using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LetMeCompress
{
    [BepInPlugin("dev.weilbyte.letmecompress", "Let Me Compress", "0.1.0")]
    [BepInProcess("WarOnTheSea.exe")]
    public class LetMeCompress : BaseUnityPlugin {
        static BepInEx.Logging.ManualLogSource _Logger = null;
        void Awake() {
            _Logger = Logger;
            Harmony.CreateAndPatchAll(typeof(LetMeCompress));
        }

        // Time compression always allowed.
        [HarmonyPatch(typeof(CombatInterface), "AllowCompression")]
        [HarmonyPrefix]
        static bool CombatInterface_AllowCompression_Patch(ref bool __result) {
            __result = true;
            return false;
        }
        
        // No 1x time compression limit when in combat.
        [HarmonyPatch(typeof(EngagementManager), "FixedUpdate")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> EngagementManager_FixedUpdate_Patch(IEnumerable<CodeInstruction> instructions)
        {
            bool found = false;
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Ldarg_0) {
                    if (codes[i-1].opcode == OpCodes.Stfld) {
                        if (codes[i+1].opcode == OpCodes.Ldfld) {
                            if (codes[i+2].opcode == OpCodes.Ldc_I4_0) {
                                codes[i].opcode = OpCodes.Ret;
                                found = true;
                            }
                        }
                    }
                }
            }

            if (!found) {
                _Logger.LogError("Could not find opcode pattern to patch for EngagementManager.FixedUpdate().");
            }

            return codes.AsEnumerable();
        }

        // No 1x time compression limit when air units attack target.
        [HarmonyPatch(typeof(UnitAIAir), "AttackUnit")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> UnitAIAir_AttackUnit_Patch(IEnumerable<CodeInstruction> instructions)
        {
            bool found = false;
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldloc_0)
                {
                    if (codes[i - 1].opcode == OpCodes.Callvirt)
                    {
                        if (codes[i + 1].opcode == OpCodes.Ldsfld)
                        {
                            if (codes[i + 2].opcode == OpCodes.Ldfld) {
                                codes[i].opcode = OpCodes.Ret;
                                found = true;
                            }
                        }
                    }
                }
            }

            if (!found)
            {
                _Logger.LogError("Could not find opcode pattern to patch for UnitAIAir.AttackUnit().");
            }

            return codes.AsEnumerable();
        }
    }
}
