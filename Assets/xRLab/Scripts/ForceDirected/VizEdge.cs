using DigitalRuby.FastLineRenderer;
using ForceDirected;
using System;
using UnityEngine;

namespace xRLab.ForceDirected
{
    public class VizEdge : MonoBehaviour
    {
        public Edge Edge { get; private set; }
        [SerializeField]
        public VizNode StartNode { get; private set; }
        [SerializeField]
        public VizNode EndNode { get; private set; }
        public VizGraph Graph { get; private set; }

        public FastLineRendererProperties LineProps = new FastLineRendererProperties();

        public bool IsEdgeBoundary = false;
        public Tuple<Vector3, Vector3> CtrPoints = new Tuple<Vector3, Vector3>(Vector3.zero, Vector3.zero);


        public virtual void Init(Edge edge, VizNode start, VizNode end, VizGraph graph)
        {
            Edge = edge;
            StartNode = start;
            EndNode = end;
            Graph = graph;
        }

        public void SetCtrPoints(Tuple<Vector3, Vector3> points)
        {
            if (points == null)
                return;

            if (points.Item1 == null || points.Item2 == null)
                return;

            IsEdgeBoundary = true;
            CtrPoints = points;
        }
    }
}