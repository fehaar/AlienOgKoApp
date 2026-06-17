using System;

namespace Gosuman.TBF
{
    public static class Extensions
    {
        public static int RandomInRange(this Range range, Random random) => random.Next(range.Start.Value, range.End.Value);
    }
}
