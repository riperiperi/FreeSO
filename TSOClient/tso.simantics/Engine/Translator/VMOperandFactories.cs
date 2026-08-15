using System;
using System.Collections.Generic;
using FSO.SimAntics.Engine.Primitives;
using FSO.SimAntics.Primitives;

namespace FSO.SimAntics.Engine
{
    /// <summary>
    /// Compile-time constructors for every primitive operand model. Mono on WASM
    /// fails Activator.CreateInstance (Arg_NoDefCTor) for types that are never
    /// directly constructed anywhere in the compiled app — the same failure the
    /// IFF chunk factories work around. VMTranslator consults this table first
    /// and only falls back to Activator for types not listed (e.g. mods).
    /// </summary>
    public static class VMOperandFactories
    {
        public static readonly Dictionary<Type, Func<VMPrimitiveOperand>> ByType = new Dictionary<Type, Func<VMPrimitiveOperand>>()
        {
            { typeof(VMAnimateSimOperand), () => new VMAnimateSimOperand() },
            { typeof(VMBreakPointOperand), () => new VMBreakPointOperand() },
            { typeof(VMBurnOperand), () => new VMBurnOperand() },
            { typeof(VMChangeActionStringOperand), () => new VMChangeActionStringOperand() },
            { typeof(VMChangeSuitOrAccessoryOperand), () => new VMChangeSuitOrAccessoryOperand() },
            { typeof(VMCreateObjectInstanceOperand), () => new VMCreateObjectInstanceOperand() },
            { typeof(VMDialogOperand), () => new VMDialogOperand() },
            { typeof(VMDropOntoOperand), () => new VMDropOntoOperand() },
            { typeof(VMDropOperand), () => new VMDropOperand() },
            { typeof(VMExpressionOperand), () => new VMExpressionOperand() },
            { typeof(VMFindBestActionOperand), () => new VMFindBestActionOperand() },
            { typeof(VMFindBestObjectForFunctionOperand), () => new VMFindBestObjectForFunctionOperand() },
            { typeof(VMFindLocationForOperand), () => new VMFindLocationForOperand() },
            { typeof(VMGenericTS1CallOperand), () => new VMGenericTS1CallOperand() },
            { typeof(VMGenericTSOCallOperand), () => new VMGenericTSOCallOperand() },
            { typeof(VMGetDirectionToOperand), () => new VMGetDirectionToOperand() },
            { typeof(VMGetDistanceToOperand), () => new VMGetDistanceToOperand() },
            { typeof(VMGetTerrainInfoOperand), () => new VMGetTerrainInfoOperand() },
            { typeof(VMGosubFoundActionOperand), () => new VMGosubFoundActionOperand() },
            { typeof(VMGotoRelativePositionOperand), () => new VMGotoRelativePositionOperand() },
            { typeof(VMGotoRoutingSlotOperand), () => new VMGotoRoutingSlotOperand() },
            { typeof(VMGrabOperand), () => new VMGrabOperand() },
            { typeof(VMIdleForInputOperand), () => new VMIdleForInputOperand() },
            { typeof(VMInventoryOperationsOperand), () => new VMInventoryOperationsOperand() },
            { typeof(VMInvokePluginOperand), () => new VMInvokePluginOperand() },
            { typeof(VMLookTowardsOperand), () => new VMLookTowardsOperand() },
            { typeof(VMOldRelationshipOperand), () => new VMOldRelationshipOperand() },
            { typeof(VMOnlineJobsCallOperand), () => new VMOnlineJobsCallOperand() },
            { typeof(VMPlaySoundOperand), () => new VMPlaySoundOperand() },
            { typeof(VMPushInteractionOperand), () => new VMPushInteractionOperand() },
            { typeof(VMRandomNumberOperand), () => new VMRandomNumberOperand() },
            { typeof(VMReachOperand), () => new VMReachOperand() },
            { typeof(VMRefreshOperand), () => new VMRefreshOperand() },
            { typeof(VMRelationshipOperand), () => new VMRelationshipOperand() },
            { typeof(VMRemoveObjectInstanceOperand), () => new VMRemoveObjectInstanceOperand() },
            { typeof(VMRunFunctionalTreeOperand), () => new VMRunFunctionalTreeOperand() },
            { typeof(VMRunTreeByNameOperand), () => new VMRunTreeByNameOperand() },
            { typeof(VMSetBalloonHeadlineOperand), () => new VMSetBalloonHeadlineOperand() },
            { typeof(VMSetMotiveChangeOperand), () => new VMSetMotiveChangeOperand() },
            { typeof(VMSetToNextOperand), () => new VMSetToNextOperand() },
            { typeof(VMShowStringOperand), () => new VMShowStringOperand() },
            { typeof(VMSleepOperand), () => new VMSleepOperand() },
            { typeof(VMSnapOperand), () => new VMSnapOperand() },
            { typeof(VMSpecialEffectOperand), () => new VMSpecialEffectOperand() },
            { typeof(VMStopAllSoundsOperand), () => new VMStopAllSoundsOperand() },
            { typeof(VMSysLogOperand), () => new VMSysLogOperand() },
            { typeof(VMTS1InventoryOperationsOperand), () => new VMTS1InventoryOperationsOperand() },
            { typeof(VMTS1MakeNewCharacterOperand), () => new VMTS1MakeNewCharacterOperand() },
            { typeof(VMTestObjectTypeOperand), () => new VMTestObjectTypeOperand() },
            { typeof(VMTestSimInteractingWithOperand), () => new VMTestSimInteractingWithOperand() },
            { typeof(VMTransferFundsOperand), () => new VMTransferFundsOperand() },
        };

        public static VMPrimitiveOperand Create(Type operandModel)
        {
            if (ByType.TryGetValue(operandModel, out var factory)) return factory();
            return (VMPrimitiveOperand)Activator.CreateInstance(operandModel);
        }
    }
}
