using System;
using System.Collections.Generic;
using Lattice;

namespace ForceDirected {

    /// <summary>
    /// Represents a world model of nodes and edges. 
    /// </summary>
    public class World {

        /// <summary>
        /// The multiplicative factor for origin attraction of nodes. 
        /// </summary>
        public const double OriginFactor = 2e4;

        /// <summary>
        /// The distance softening factor for origin attraction of nodes. 
        /// </summary>
        public const double OriginEpsilon = 7000;

        /// <summary>
        /// The distance within which origin attraction of nodes becomes weaker. 
        /// </summary>
        public const double OriginWeakDistance = 100;

        /// <summary>
        /// The multiplicative factor for repulsion between nodes. 
        /// </summary>
        public const double RepulsionFactor = -300;

        /// <summary>
        /// The distance softening factor for repulsion between nodes. 
        /// </summary>
        public const double RepulsionEpsilon = 2;

        /// <summary>
        /// The multiplicative factor for edge spring stiffness. 
        /// </summary>
        public const double EdgeFactor = 0.1;

        /// <summary>
        /// The ideal length of edges. 
        /// </summary>
        public const double EdgeLength = 10;

        /// <summary>
        /// The number of nodes in the world model. 
        /// </summary>
        public int NodeCount {
            get {
                return _nodes.Count;
            }
        }

        /// <summary>
        /// The number of edges in the world model. 
        /// </summary>
        public int EdgeCount {
            get {
                return _edges.Count;
            }
        }

        /// <summary>
        /// The collection of nodes in the world model. 
        /// </summary>
        private List<Node> _nodes = new List<Node>();

        /// <summary>
        /// The collection of edges in the world model. 
        /// </summary>
        private List<Edge> _edges = new List<Edge>();

        /// <summary>
        /// The lock required to modify the nodes collection. 
        /// </summary>
        private object _nodeLock = new object();

        /// <summary>
        /// Constructs a world model. 
        /// </summary>
        public World() {

        }

        /// <summary>
        /// Adds a node to the world model. 
        /// </summary>
        /// <param name="node">The node to add to the world model.</param>
        public void Add(Node node) {
            lock (_nodeLock)
                _nodes.Add(node);
        }

        /// <summary>
        /// Adds a collection of nodes to the world model.
        /// </summary>
        /// <param name="nodes">The collection of nodes to add to the world model.</param>
        public void AddRange(IEnumerable<Node> nodes) {
            lock (_nodeLock)
                _nodes.AddRange(nodes);
        }

        /// <summary>
        /// Connects two nodes in the world model. 
        /// </summary>
        /// <param name="a">A node to connect.</param>
        /// <param name="b">A node to connect.</param>
        public Edge Connect(Node a, Node b) {
            if (a == b)
                throw new ArgumentException("Cannot connect a node to itself.");
            lock (_nodeLock) {
                a.Connected.Add(b);
                b.Connected.Add(a);

                Edge edge = new Edge(a, b);
                _edges.Add(edge);

                return edge;
            }
        }

        public double Radius = 0;


        /// <summary>
        /// Advances the world model by one frame. 
        /// </summary>
        public void Update() {

            // Update nodes.
            lock (_nodeLock) {

                // Update the nodes and determine required tree width. 
                double halfWidth = 0;
                foreach (Node node in _nodes) {
                    node.Update();
                    halfWidth = Math.Max(Math.Abs(node.Location.X), halfWidth);
                    halfWidth = Math.Max(Math.Abs(node.Location.Y), halfWidth);
                    halfWidth = Math.Max(Math.Abs(node.Location.Z), halfWidth);
                }
                Radius = halfWidth;

                // Build tree for node repulsion. 
                Octree tree = new Octree(2.1 * halfWidth);
                foreach (Node node in _nodes)
                    tree.Add(node);

                Threading.Parallel.ForEach(_nodes, node => {

                    // Apply repulsion between nodes. 
                    tree.Accelerate(node);

                    // Apply origin attraction of nodes. 
                    //Vector originDisplacementUnit = -node.Location.Unit();
                    Vector origin = (node.Group == null) ? Vector.Zero : node.Group.Origin;
                    Vector originDisplacementUnit = Unit(origin - node.Location);
                    double originDistance = (origin - node.Location).Magnitude();

                    double attractionCofficient = OriginFactor;
                    if (originDistance < OriginWeakDistance)
                        attractionCofficient *= originDistance / OriginWeakDistance;

                    // Apply group's attraction factor
                    //attractionCofficient *= (node.Group == null) ? 1 : node.Group.Factor;
                    //node.Acceleration += originDisplacementUnit * attractionCofficient / (originDistance + OriginEpsilon);

                    Vector originAcceleration = originDisplacementUnit * attractionCofficient / (originDistance + OriginEpsilon);

                    if (node.Group != null)
                    {
                        originAcceleration.X *= node.Group.Factor.X;
                        originAcceleration.Y *= node.Group.Factor.Y;
                        originAcceleration.Z *= node.Group.Factor.Z;
                    }

                    node.Acceleration += originAcceleration;

                    // Apply edge spring forces. 
                    foreach (Node other in node.Connected) {

                        Vector displacement = node.Location.To(other.Location);
                        //Vector direction = displacement.Unit();
                        Vector direction = Unit(displacement);
                        double distance = displacement.Magnitude();
                        //double idealLength = EdgeLength + node.Radius + other.Radius;

                        // Set a large mass if it's in different groups
                        double mass = (node.Group != other.Group) ? 1000 : node.Mass;
                        double idealLength = EdgeLength + Node.GetRadius(mass) + other.Radius;

                        node.Acceleration += direction * EdgeFactor * (distance - idealLength) / mass;
                        //node.Acceleration += direction * EdgeFactor * (distance - idealLength) / node.Mass;
                    }
                });
            }
        }

        Vector Unit(Vector vector)
        {
            if (vector.Magnitude() == 0)
                return PseudoRandom.Vector(1).Unit();
            else
                return vector.Unit();
        }

        public void Clear()
        {
            lock (_nodeLock)
            {
                _nodes.Clear();
                _edges.Clear();
            }

            Radius = 0;
        }
    }
}
