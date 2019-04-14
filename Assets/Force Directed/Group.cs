using Lattice;
using System;

namespace ForceDirected
{
    public interface IGroup
    {
        Guid Id { get; }
        string Name { get; }
        Vector Origin { get; }
        Vector Factor { get; }
    }

    public class BaseGroup : IGroup
    {
        public Guid Id { get; private set; }
        public string Name { get; set; } = string.Empty;
        public Vector Origin { get; set; } = Vector.Zero;
        public Vector Factor { get; set; } = new Vector(1, 1, 1);

        public BaseGroup()
        {
            Id = Guid.NewGuid();
        }
    }
}