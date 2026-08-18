#if !UNITY_EDITOR

using EFT.Ballistics;
using EFT.InventoryLogic;
using GamePanelHUDHit.Models;
using HarmonyLib;
using MonoMod.Cil;
using MonoMod.Utils;

namespace GamePanelHUDHit
{
    public partial class GamePanelHUDHitPlugin
    {
        private static void ApplyDamage(ILContext il)
        {
            var codes = il.Instrs;

            var cursor = new ILCursor(il);

            var processor = il.IL;

            var callApplyDurabilityDamage = cursor.GotoNext(x =>
                x.MatchCall(AccessTools.Method(typeof(ArmorComponent), "ApplyDurabilityDamage")));

            codes.InsertRange(
                //Parameters Start Index
                callApplyDurabilityDamage.Index - 2, new[]
                {
                    //Get ArmorModel Instance
                    processor.Create(Mono.Cecil.Cil.OpCodes.Call,
                        AccessTools.PropertyGetter(typeof(ArmorModel), nameof(ArmorModel.Instance))),
                    //Get DamageInfo
                    processor.Create(Mono.Cecil.Cil.OpCodes.Ldarg_1),
                    //Convert ref struct - DamageInfoStruct renamed to EFT.Ballistics.DamageInfo.
                    //NOTE: this is raw IL injection into ArmorComponent.ApplyDurabilityDamage;
                    //the type rename is mechanical but the instruction-index math below
                    //(callApplyDurabilityDamage.Index - 2, .Prev.Previous) assumes the method's
                    //parameter layout is unchanged - only verifiable by actually testing in-game.
                    processor.Create(Mono.Cecil.Cil.OpCodes.Ldobj, typeof(DamageInfo)),
                    //Get ApplyDurabilityDamage first parameter
                    callApplyDurabilityDamage.Prev.Previous,
                    processor.Create(Mono.Cecil.Cil.OpCodes.Callvirt,
                        AccessTools.Method(typeof(ArmorModel), nameof(ArmorModel.Set)))
                });
        }
    }
}

#endif