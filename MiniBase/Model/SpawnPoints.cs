using System.Collections.Generic;

namespace MiniBase.Model
{
    public struct SpawnPoints
    {
        public ISet<Vector2I> OnFloor;
        public ISet<Vector2I> OnCeil;
        public ISet<Vector2I> InGround;
        public ISet<Vector2I> InAir;
        public ISet<Vector2I> InLiquid;
    }
}