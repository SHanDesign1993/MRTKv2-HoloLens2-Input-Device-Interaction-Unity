using DigitalRuby.FastLineRenderer;
using ForceDirected;
using Lattice;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using System;
using System.Diagnostics;
using UnityEngine.UI;
namespace xRLab.ForceDirected
{
    public class VizGraph : MonoBehaviour
    {
        [Header("Graph Settings")]
        [SerializeField]
        Text nodetext;
        [SerializeField]
        Text edgetext;
        [SerializeField]
        Text resulttext;
       
        [SerializeField]
        Text fpstext;
        [SerializeField]
        float fpsrate;
        private int frameCount;
        private Stopwatch stopwatch = new Stopwatch();
        [Range(0.0f, 1.0f)]
        private float frameSampleRate = 0.1f;

        [SerializeField]
        protected VizNode NodeObj;
        [SerializeField]
        protected FastLineRenderer FastLineObj;

        [SerializeField]
        protected bool DevelopmentMode = false;

        protected World Model = new World();
        protected List<VizNode> Nodes = new List<VizNode>();
        protected List<VizEdge> Edges = new List<VizEdge>();
        protected List<FastLineRenderer> FastLines = new List<FastLineRenderer>();

        public bool IsUpdating { get; private set; } = false;

        Task UpdateTask = null;
        Coroutine UpdateModelRoutine = null;
        Coroutine UpdateEdgesRoutine = null;

        BaseGroup TestGroup1, TestGroup2, TestGroup3;
        //private NLogger logger;

        Guid Id;
        int CurrentLineIndex = 0;
        static List<VizGraph> Graphs = new List<VizGraph>();

        static VizGraph GetGraph(Guid id)
        {
            return Graphs.FirstOrDefault(x => x.Id == id);
        }

        struct NodeUpdateJob : IJobParallelForTransform
        {
            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Vector> Locations;

            public void Execute(int i, TransformAccess transform)
            {
                transform.localPosition = VizUtility.GetVector3(Locations[i]);
            }
        }

        TransformAccessArray NodeTransformArray;
        JobHandle NodeUpdateJobHandle;
        protected bool IsUpdatingNodes = false;

        struct EdgeUpdateJob : IJobParallelFor
        {
            [ReadOnly]
            public Guid Id;

            [ReadOnly]
            public int LineIndex;

            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Vector3> StartPos;

            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Vector3> EndPos;

            public void Execute(int i)
            {
                VizGraph graph = GetGraph(Id);
                if (graph ==  null) return;
                VizEdge edge = graph.Edges[i];

                edge.LineProps.Start = StartPos[i];
                edge.LineProps.End = EndPos[i];

                if (edge.IsEdgeBoundary)
                    graph.FastLines[LineIndex].AppendCurve(edge.LineProps, edge.CtrPoints.Item1, edge.CtrPoints.Item2, 16, false, false, 0);
                else
                    graph.FastLines[LineIndex].AddLine(edge.LineProps);
            }
        }

        JobHandle EdgeUpdateJobHandle;
        protected bool IsUpdatingEdges = false;

        Transform NodesPivot;
        protected Transform FastLinesPivot;
        Coroutine testingRoutine;

        // Use this for initialization
        protected void Start()
        {
            Initializ();

            if (DevelopmentMode)
            {
                InitTestGroups();
                QualitySettings.vSyncCount = 0;
            }
            stopwatch.Reset();
            stopwatch.Start();
        }

        protected void OnDestroy()
        {
            if (NodeTransformArray.isCreated)
                NodeTransformArray.Dispose();

            ClearGraph();
            Graphs.Remove(this);
        }

        public void GroupNodes()
        {
            BaseGroup[] groups = { TestGroup1, TestGroup2, TestGroup3 };

            Nodes.ForEach(node =>
            {
                if (node.Node.Group == null)
                    node.Node.Group = groups[UnityEngine.Random.Range(0, 3)];
            });

            StartUpdateModel();
        }

        public void UnGroupNodes()
        {
            Nodes.ForEach(node =>
            {
                node.Node.Group = null;
            });

            StartUpdateModel();
        }

        void UpdateInfo()
        {
            nodetext.text = Nodes.Count.ToString();
            edgetext.text = Edges.Count.ToString();
        }

        void OnGUI()
        {
            if (DevelopmentMode)
            {
                GUI.Label(new Rect(10, 50, 200, 20), "A: add 50 nodes");
                GUI.Label(new Rect(10, 70, 200, 20), "G: group nodes");
                GUI.Label(new Rect(10, 90, 200, 20), "H: ungroup nodes");
                GUI.Label(new Rect(10, 110, 200, 20), "T: test performance");
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (DevelopmentMode)
            {
                if (Input.GetKeyDown(KeyCode.A))
                {
                    StartCoroutine(AddTestNodes(50, true));
                }

                if (Input.GetKeyDown(KeyCode.P))
                {
                    UnityEngine.Debug.LogFormat("Node Number: {0}", Nodes.Count);
                }

                if (Input.GetKeyDown(KeyCode.U))
                {
                    StartUpdateModel(10);
                }

                if (Input.GetKeyDown(KeyCode.G))
                {
                    BaseGroup[] groups = { TestGroup1, TestGroup2, TestGroup3 };

                    Nodes.ForEach(node =>
                    {
                        if (node.Node.Group == null)
                            node.Node.Group = groups[UnityEngine.Random.Range(0, 3)];
                    });

                    StartUpdateModel();
                }

                if (Input.GetKeyDown(KeyCode.H))
                {
                    Nodes.ForEach(node =>
                    {
                        node.Node.Group = null;
                    });

                    StartUpdateModel();
                }

                if (Input.GetKeyDown(KeyCode.L))
                {
                    Nodes.ForEach(node =>
                    {
                        node.Node.IsLocked = (UnityEngine.Random.Range(0, 2) == 0) ? true : false;
                    });

                    StartUpdateModel();
                }

                if (Input.GetKeyDown(KeyCode.K))
                {
                    Nodes.ForEach(node =>
                    {
                        node.Node.IsLocked = false;
                    });

                    StartUpdateModel();
                }

                if (Input.GetKeyDown(KeyCode.T))
                    TestPerformance();
            }
        }

        void LateUpdate()
        {
            TryNodeUpdateJob();
            TryEdgeUpdateJob();
            UpdateFrameRate();
        }

        public void Initializ()
        {
            Id = Guid.NewGuid();
            Graphs.Add(this);

            NodesPivot = new GameObject("Nodes").transform;
            NodesPivot.SetParent(transform, false);

            FastLinesPivot = new GameObject("FastLines").transform;
            FastLinesPivot.SetParent(transform, false);

            FastLines.Add(Instantiate(FastLineObj, FastLinesPivot));
            FastLines.Add(Instantiate(FastLineObj, FastLinesPivot));
        }

        void InitTestGroups()
        {
            TestGroup1 = new BaseGroup()
            {
                Name = "Group1",
                Origin = new Vector(-100, 50, 0),
                Factor = new Vector(8, 2, 2)
            };

            TestGroup2 = new BaseGroup()
            {
                Name = "Group2",
                Origin = new Vector(0, -50, 0),
                Factor = new Vector(8, 2, 2)
            };

            TestGroup3 = new BaseGroup()
            {
                Name = "Group3",
                Origin = new Vector(100, 50, 0),
                Factor = new Vector(8, 2, 2)
            };
        }

        public void StartUpdateModel(float time = 3.0f)
        {
            StartUpdateModel(time, true);
        }

        public void StartUpdateModel(float time, bool dynamic = true)
        {
            if (this == null) return;
            StopUpdateModel();
            UpdateNodeTransformArray();
            UpdateModelRoutine = StartCoroutine(UpdateModel(time, dynamic));
        }

        public void StopUpdateModel()
        {
            IsUpdating = false;

            if (UpdateModelRoutine != null && this != null)
                StopCoroutine(UpdateModelRoutine);
        }

        void UpdateNodeTransformArray()
        {
            if (NodeTransformArray.isCreated)
                NodeTransformArray.SetTransforms(Nodes.Select(x => x.transform).ToArray());
            else
                NodeTransformArray = new TransformAccessArray(Nodes.Select(x => x.transform).ToArray());
        }

        public void UpdateNodes()
        {
            UpdateNodeTransformArray();
            InitNodeUpdateJob();
        }

        void InitNodeUpdateJob()
        {
            NodeUpdateJob job = new NodeUpdateJob()
            {
                Locations = new NativeArray<Vector>(
                    Nodes.Select(x => x.Node.Location).ToArray(), Allocator.TempJob)
            };

            NodeUpdateJobHandle = job.Schedule(NodeTransformArray);
            IsUpdatingNodes = true;
        }

        void TryNodeUpdateJob()
        {
            if (IsUpdatingNodes)
            {
                if (NodeUpdateJobHandle.IsCompleted)
                {
                    if (IsUpdating)
                    {
                        InitNodeUpdateJob();
                    }
                    else
                    {
                        IsUpdatingNodes = false;
                        NodeTransformArray.Dispose();
                    }
                }
            }
            else if (IsUpdating)
            {
                InitNodeUpdateJob();
            }
        }

        public void UpdateEdges()
        {
            InitEdgeUpdateJob();
        }

        void InitEdgeUpdateJob()
        {
            NativeArray<Vector3> start = new NativeArray<Vector3>(
                    Edges.Select(x => x.StartNode.transform.localPosition).ToArray(), Allocator.TempJob);
            NativeArray<Vector3> end = new NativeArray<Vector3>(
                    Edges.Select(x => x.EndNode.transform.localPosition).ToArray(), Allocator.TempJob);

            EdgeUpdateJob job = new EdgeUpdateJob()
            {
                Id = Id,
                LineIndex = CurrentLineIndex,
                StartPos = start,
                EndPos = end
            };

            FastLines[CurrentLineIndex].transform.localScale = Vector3.one;
            FastLines[CurrentLineIndex].Reset();
            FastLines[CurrentLineIndex].Material.EnableKeyword("DISABLE_CAPS");

            EdgeUpdateJobHandle = job.Schedule(Math.Min(start.Length, end.Length), 16);
            IsUpdatingEdges = true;
        }

        void TryEdgeUpdateJob()
        {
            if (IsUpdatingEdges)
            {
                if (EdgeUpdateJobHandle.IsCompleted)
                {
                    FastLines[CurrentLineIndex].Apply();
                    FastLines[CurrentLineIndex].transform.localScale = FastLines[CurrentLineIndex].transform.lossyScale;
                    FastLines[CurrentLineIndex].gameObject.SetActive(true);
                    ResetLineMeshAngles(FastLines[CurrentLineIndex]);
                    CurrentLineIndex = (++CurrentLineIndex) % 2;
                    FastLines[CurrentLineIndex].gameObject.SetActive(false);

                    if (IsUpdating)
                        InitEdgeUpdateJob();
                    else
                        IsUpdatingEdges = false;
                }
            }
            else if (IsUpdating)
            {
                InitEdgeUpdateJob();
            }
        }

        void UpdateFrameRate()
        {
            ++frameCount;
            float elapsedSeconds = stopwatch.ElapsedMilliseconds * 0.001f;

            if (elapsedSeconds >= frameSampleRate)
            {
                // Update frame rate text.
                int frameRate = (int)(1.0f / (elapsedSeconds / frameCount));
                fpsrate = Mathf.Clamp(frameRate, 0, 120);
                fpstext.text = "FPS: " + fpsrate.ToString();
                frameCount = 0;
                stopwatch.Reset();
                stopwatch.Start();
            }

        }

        public void TestPerformance()
        {
            testingRoutine = StartCoroutine(TestPerformanceCoroutine(24,1));
        }

        IEnumerator TestPerformanceCoroutine(float expectedFPS , int spawnedperframe)
        {
            resulttext.text = "TEST STARTED with FPS(" + fpsrate + ")";
            while (fpsrate >= expectedFPS)
            {
                yield return AddTestNodes(spawnedperframe, false);

                if (Nodes.Count>0 && Nodes.Count % 50 == 0)
                {
                    yield return new WaitForSeconds(0.2f);
                    resulttext.text = "CURRENT FPS: " + fpsrate;

                    if (fpsrate < expectedFPS)
                    {
                        resulttext.text = "TEST COMPLETED with FPS(" + fpsrate + ") & nodes(" + Nodes.Count + ") + edges(" + Edges.Count + ")";
                        StartUpdateModel(Nodes.Count / 50);
                        break;
                    }
                    else
                        StartUpdateModel(0.5f);
                }

                yield return null;
            }

            yield return new WaitForSeconds(0.2f);
            if (fpsrate >= expectedFPS)
                yield return TestPerformanceCoroutine(expectedFPS, spawnedperframe);
            else
                resulttext.text = "TEST COMPLETED with FPS(" + fpsrate + ")  nodes(" + Nodes.Count + ") + edges(" + Edges.Count + ")";

        }

        IEnumerator AddTestNodes(int number , bool isdynamic)
        {
            int SpawnedPerFrame = 100;
            int count = SpawnedPerFrame;

            for (int i = 0; i < number; i++)
            {
                VizNode start;

                if (Nodes.Count < 2)
                {
                    start = CreateNode();
                    AddNode(start);
                }
                else
                {
                    start = Nodes[UnityEngine.Random.Range(0, Nodes.Count - 1)];
                }

                VizNode end = CreateNode();
                AddNode(end);
                Connect(start, end);

                if (--count < 0)
                {
                    StartUpdateModel();
                    yield return null;
                    count = SpawnedPerFrame;
                }
            }
            UpdateInfo();
            if(isdynamic)
                StartUpdateModel();
        }

        protected IEnumerator UpdateModel(float time, bool dynamic = true)
        {
            IsUpdating = dynamic;

            float duration = 0;

            while (duration < time)
            {
                duration += Time.deltaTime;

                if (UpdateTask == null)
                {
                    UpdateTask = Task.Run(() => Model.Update());
                }
                else if (UpdateTask.Status == TaskStatus.RanToCompletion)
                {
                    UpdateTask = Task.Run(() => Model.Update());
                }

                yield return null;
            }

            UpdateModelRoutine = null;
            IsUpdating = false;
        }

        public VizNode CreateNode()
        {
            return CreateNode(UnityEngine.Random.Range(1, 4));
        }

        public VizNode CreateNode(object data)
        {
            VizNode nodeObj = Instantiate<VizNode>(NodeObj, NodesPivot);
            nodeObj.Init(new Node(), data, this);
            Vector3 v = UnityEngine.Random.insideUnitSphere + Vector3.one;
            nodeObj.Node.Location = new Vector(v.x, v.y, v.z);

            return nodeObj;
        }

        public void AddNode(VizNode node)
        {
            Nodes.Add(node);
            Model.Add(node.Node);
        }

        protected virtual VizEdge CreateVizEdge()
        {
            return new VizEdge();
        }

        public VizEdge Connect(VizNode start, VizNode end)
        {
            //VizEdge edge = Instantiate<VizEdge>(EdgeObj, EdgesPivot);
            VizEdge edge = new VizEdge();
            edge.Init(Model.Connect(start.Node, end.Node), start, end, this);

            Edges.Add(edge);

            return edge;
        }

        protected void ResetLineMeshAngles(FastLineRenderer line) {
            var meshes = line.transform.GetComponentsInChildren<MeshRenderer>();
            foreach (var m in meshes)
                m.transform.localEulerAngles = Vector3.zero;
        }

        protected virtual void ClearGraph()
        {            
            foreach (var node in Nodes)
                Destroy(node.gameObject);

            foreach (var fastLine in FastLines)
                fastLine.Reset();

            Model.Clear();
            Nodes.Clear();
            Edges.Clear();
            Resources.UnloadUnusedAssets();
        }
        
    }
}
