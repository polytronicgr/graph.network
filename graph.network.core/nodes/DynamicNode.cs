using System;

namespace graph.network.core.nodes
{
    public class DynamicNode : Node
    {
        private readonly Action<Node, GraphNet> onAdd;
        private readonly Action<Node, GraphNet> onRemove;
        private readonly Action<Node, GraphNet> onTrain;

        public DynamicNode(object value, Action<Node, GraphNet> onAdd =null, Action<Node, GraphNet> onRemove = null, Action<Node, GraphNet> onTrain = null) : base(value)
        {
            this.onAdd = onAdd;
            this.onRemove = onRemove;
            this.onTrain = onTrain;
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
            if (onRemove== null)
            {
                base.OnRemove(graph);
            }
            else
            {
                onRemove(this, graph);
            }
        }

        public override void OnTrain(GraphNet graph)
        {
            if (onTrain == null)
            {
                base.OnTrain(graph);
            }
            else
            {
                onTrain(this, graph);
            }
        }
    }
}
