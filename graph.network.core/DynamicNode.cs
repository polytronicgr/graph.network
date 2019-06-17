using System;
using System.Collections.Generic;

namespace graph.network.core
{
    public class DynamicNode : Node
    {
        private readonly Action<Node, GraphNet> onAdd;
        private readonly Action<Node, GraphNet> onRemove;
        private readonly Action<Node, GraphNet, Node, List<NodePath>> onProcess;
        private readonly Func<Node, GraphNet, NodePath, bool> isPathValid;

        public DynamicNode(
            object value, Action<Node, GraphNet> onAdd = null
            , Action<Node, GraphNet> onRemove = null
            , Action<Node, GraphNet, Node, List<NodePath>> onProcess = null
            , Func<Node, GraphNet, NodePath, bool> isPathValid = null) : base(value)
        {
            this.onAdd = onAdd;
            this.onRemove = onRemove;
            this.isPathValid = isPathValid;
            this.onProcess = onProcess;
        }

        public override void OnAdd(GraphNet graph)
        {
            if (onAdd == null)
            {
                base.OnAdd(graph);
            }
            else
            {
                onAdd(this, graph);
            }
        }

        public override void OnRemove(GraphNet graph)
        {
            if (onRemove == null)
            {
                base.OnRemove(graph);
            }
            else
            {
                onRemove(this, graph);
            }
        }

        public override void OnProcess(GraphNet graph, Node input, List<NodePath> paths)
        {
            if (onProcess == null)
            {
                base.OnProcess(graph, input, paths);
            }
            else
            {
                onProcess(this, graph, input, paths);
            }
        }

        public override bool IsPathValid(GraphNet graph, NodePath path)
        {
            if (isPathValid == null)
            {
                return base.IsPathValid(graph, path);
            }
            else
            {
                return isPathValid(this, graph, path);
            }
        }
    }
}
