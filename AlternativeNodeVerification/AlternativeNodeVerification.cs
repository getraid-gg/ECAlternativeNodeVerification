using BepInEx;
using Harmony;
using HEdit;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YS_Node;

namespace AlternativeNodeVerification
{
    [BepInPlugin(GUID, PluginName, PluginVersion)]
    public class AlternativeNodeVerification : BaseUnityPlugin
    {
        public const string GUID = "com.getraid.ec.alternativenodeverification";
        public const string PluginName = "Alternative Node Verification";
        public const string PluginVersion = "1.0.0";

        HarmonyInstance Harmony;
        private void Awake()
        {
            Harmony = HarmonyInstance.Create(GUID);
            
            Harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static NodeCheck.CheckInfo CheckNodes(NodeControl nodeController)
        {
            var checkInfo = new NodeCheck.CheckInfo();

            var reverseNodeConnections = CreateReverseNodeConnections(nodeController.dictNode);
            var startConnectedNodes = GetStartConnectedNodes(checkInfo, nodeController);
            var endConnectedNodes = GetEndConnectedNodes(checkInfo, nodeController, reverseNodeConnections);
            
            var unreachableNodes = new List<NodeUI>();
            var deadEndNodes = new List<NodeUI>();

            foreach (var node in nodeController.dictNode.Values)
            {
                var nodeBase = node.nodeBase;
                var childUIDs = nodeBase.childUID;

                if (startConnectedNodes.Contains(node))
                {
                    if (endConnectedNodes.Contains(node))
                    {
                        checkInfo.canEnd = true;
                    }
                    else
                    {
                        // For some reason, the built-in verification counts reachable dead-end nodes as unreachable,
                        // so we mimic this behaviour to prevent issues (though the actual lists are never used
                        // in vanilla code - the game just checks if there are >0 unreachable/dead end nodes)
                        unreachableNodes.Add(node);
                        deadEndNodes.Add(node);
                    }

                    if (nodeBase.uid != "node_end" &&
                        nodeBase.uid != "node_start")
                    {
                        var hasChild = childUIDs.Any((s) => !string.IsNullOrEmpty(s));
                        if (!hasChild)
                        {
                            checkInfo.deadEnd = true;
                        }
                    }

                    checkInfo.disconnectedOutput |= GetIsNodeOutputDisconnected(nodeBase, childUIDs);
                }
                else
                {
                    unreachableNodes.Add(node);
                }
            }
            
            checkInfo.lstUnreachable = unreachableNodes;

            checkInfo.lstUnreachableWithoutIsolated = deadEndNodes;

            return checkInfo;
        }

        private static Dictionary<NodeUI, List<NodeUI>> CreateReverseNodeConnections(Dictionary<string, NodeUI> nodes)
        {
            var reverseNodeConnections = new Dictionary<NodeUI, List<NodeUI>>();
            foreach (var pair in nodes)
            {
                reverseNodeConnections.Add(pair.Value, new List<NodeUI>());
            }

            foreach (var parentNode in nodes.Values)
            {
                foreach (var childUID in parentNode.nodeBase.childUID)
                {
                    if (!string.IsNullOrEmpty(childUID))
                    {
                        reverseNodeConnections[nodes[childUID]].Add(parentNode);
                    }
                }
            }

            return reverseNodeConnections;
        }

        private static bool GetIsNodeOutputDisconnected(NodeBase nodeBase, string[] childUIDs)
        {
            if (nodeBase.kind == NodeKind.ADV)
            {
                var adv = Singleton<HEditData>.Instance.GetADVPart(nodeBase.uid);
                if (adv != null)
                {
                    var endCut = adv.cuts[adv.cuts.Count - 1];

                    var options = endCut.GetOptions();
                    if (options == null || options.Count == 0)
                    {
                        // No options, so it's disconnected if it has no child
                        if (string.IsNullOrEmpty(childUIDs[0]))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < options.Count; i++)
                        {
                            if (string.IsNullOrEmpty(childUIDs[i]))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            else if (nodeBase.kind == NodeKind.H)
            {
                for (int i = 0; i < childUIDs.Length; i++)
                {
                    if (nodeBase.endConditionType[i] != EndConditionType.NotUsed
                        && string.IsNullOrEmpty(childUIDs[i]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static HashSet<NodeUI> GetStartConnectedNodes(NodeCheck.CheckInfo checkInfo, NodeControl nodeController)
        {
            var startNode = nodeController.dictNode["node_start"];
            var visitedNodes = new HashSet<NodeUI>();
            
            GatherNodesForward(startNode, checkInfo, nodeController, visitedNodes);
            return visitedNodes;
        }

        private static void GatherNodesForward(NodeUI currentNode, NodeCheck.CheckInfo checkInfo, NodeControl nodeController, HashSet<NodeUI> visitedNodes)
        {
            var isNewNode = visitedNodes.Add(currentNode);
            if (!isNewNode) { return; }
            
            foreach (var childUID in currentNode.nodeBase.childUID)
            {
                if (!string.IsNullOrEmpty(childUID))
                {
                    GatherNodesForward(nodeController.dictNode[childUID], checkInfo, nodeController, visitedNodes);
                }
            }
        }

        private static HashSet<NodeUI> GetEndConnectedNodes(NodeCheck.CheckInfo checkInfo, NodeControl nodeController, Dictionary<NodeUI, List<NodeUI>> reverseNodeConnections)
        {
            var endNode = nodeController.dictNode["node_end"];
            var visitedNodes = new HashSet<NodeUI>();

            GatherNodesBackward(endNode, checkInfo, nodeController, reverseNodeConnections, visitedNodes);

            return visitedNodes;
        }

        private static void GatherNodesBackward(NodeUI currentNode, NodeCheck.CheckInfo checkInfo, NodeControl nodeController, Dictionary<NodeUI, List<NodeUI>> reverseNodeConnections, HashSet<NodeUI> visitedNodes)
        {
            var isNewNode = visitedNodes.Add(currentNode);
            if (!isNewNode) { return; }

            var parents = reverseNodeConnections[currentNode];
            foreach (var parent in parents)
            {
                GatherNodesBackward(parent, checkInfo, nodeController, reverseNodeConnections, visitedNodes);
            }
        }
    }
}
