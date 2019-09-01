using Harmony;
using System;
using YS_Node;

namespace AlternativeNodeVerification
{
    [HarmonyPatch(typeof(NodeCheck))]
    [HarmonyPatch(nameof(NodeCheck.CheckNode))]
    [HarmonyPatch(new Type[] { typeof(NodeControl) })]
    public static class Hooks
    {
		private static bool Prefix() => false;

        private static void Postfix(ref NodeCheck.CheckInfo __result, NodeControl nodeCtrl) => __result = AlternativeNodeVerification.CheckNodes(nodeCtrl);
    }
}
