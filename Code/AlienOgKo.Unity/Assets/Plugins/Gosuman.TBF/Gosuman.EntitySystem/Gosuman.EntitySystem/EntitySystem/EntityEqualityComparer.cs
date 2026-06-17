using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Gosuman.EntitySystem
{
    public struct EntityEqualityComparer : IEqualityComparer<Entity>
    {
        public bool Equals([AllowNull] Entity x, [AllowNull] Entity y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.Id.Equals(y.Id);
        }

        public int GetHashCode(Entity obj) => obj.Id.GetHashCode();
    }
}
