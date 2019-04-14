using System;
using System.Collections.Generic;
using Lattice;

namespace ForceDirected {

    /// <summary>
    /// Represents a node in the graph. 
    /// </summary>
    public class Node {

        /// <summary>
        /// The multiplicative factor that gives the rate at which velocity is 
        /// dampened after each frame. 
        /// </summary>
        private const double VelocityDampening = 0.4;

        /// <summary>
        /// The expected maximum radius. A random location is generated with each 
        /// coordinate in the interval [0, RadiusRange). 
        /// </summary>
        private const double RadiusRange = 1000;

        /// <summary>
        /// Returns the radius defined for the given mass value. 
        /// </summary>
        /// <param name="mass">The mass to calculate a radius for.</param>
        /// <returns>The radius defined for the given mass value.</returns>
        public static double GetRadius(double mass) {
            return 0.8 * Math.Pow(mass, 1 / 3.0);
        }

        /// <summary>
        /// The collection of nodes the node is connected to. 
        /// </summary>
        public HashSet<Node> Connected;

        /// <summary>
        /// The location of the node. 
        /// </summary>
        public Vector Location = Vector.Zero;

        /// <summary>
        /// The velocity of the node. 
        /// </summary>
        public Vector Velocity = Vector.Zero;

        /// <summary>
        /// The acceleration applied to the node. 
        /// </summary>
        public Vector Acceleration = Vector.Zero;

        public IGroup Group;
        public bool IsLocked;

        /// <summary>
        /// The mass of the node. 
        /// </summary>
        public double Mass {
            get {
                return Connected.Count > 0 ? Connected.Count : 1;
            }
        }

        /// <summary>
        /// The radius of the node. 
        /// </summary>
        public double Radius {
            get {
                return GetRadius(Mass);
            }
        }

        public Node()
        {
            Location = Vector.Zero;
            Velocity = Vector.Zero;
            Acceleration = Vector.Zero;
            Connected = new HashSet<Node>();
        }

        /// <summary>
        /// Returns whether the node is connected to the given node.
        /// </summary>
        /// <param name="other">A potentially connected node.</param>
        /// <returns>Whether the node is connected to the given nod</returns>
        public bool IsConnectedTo(Node other) {
            return Connected.Contains(other);
        }

        /// <summary>
        /// Updates the properties of the node such as location, velocity, and 
        /// applied acceleration. This method should be invoked at each time step. 
        /// </summary>
        public void Update() {
            if (!IsLocked)
            {
                Velocity += Acceleration;
                Location += Velocity;
                Velocity *= VelocityDampening;
            }
            Acceleration = Vector.Zero;
        }
    }
}
