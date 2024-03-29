﻿using QuickGraph;
using System.ComponentModel;

namespace graph.network.core
{
    public class Edge : TaggedEdge<Node, Node>, INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;
        public Edge(Node subject, Node predicate, Node obj) : base(subject, obj, predicate)
        {
            Subject = subject;
            Predicate = predicate;
            Obj = obj;
        }

        public Node Subject { get; }
        public Node Predicate { get; }
        public Node Obj { get; }
        public bool Internal { get; set; }
        public bool Inverted { get; set; }

        public string ShortId
        {
            get { return Predicate?.ShortId; }
        }

        public double CurrentProbabilty { get; set; } = 1;

        public override bool Equals(object obj)
        {
            var id = ToString();
            if (id == null && obj != null) return false;
            if (id != null && obj == null) return false;
            if (id == null && obj == null) return true;
            if (id.Equals(obj.ToString())) return true;


            return false;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            //NOTE: could cache this
            return$"{Subject.Value}|{Predicate.Value}|{Obj.Value}";
        }
    }
}
