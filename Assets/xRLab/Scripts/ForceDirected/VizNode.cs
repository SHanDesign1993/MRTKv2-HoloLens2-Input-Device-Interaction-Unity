using ForceDirected;
using UnityEngine;
using TMPro;

namespace xRLab.ForceDirected
{
    [System.Serializable]
    public class VizNode : MonoBehaviour //: PointerEventTrigger
    {
        [SerializeField]
        public TextMeshPro Text;

        public Node Node { get; private set; }

        public object Data { get; set; }

        public VizGraph Graph { get; private set; }


        public void Init(Node node, object data, VizGraph graph)
        {
            Node = node;
            Data = data;
            Graph = graph;
            Text.text = Data.ToString();
        }
    }
}