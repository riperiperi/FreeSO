namespace VoronoiLib.Structures
{
    internal class FortuneCircleEvent : FortuneEvent
    {
        internal VPoint Lowest { get; }
        internal double YCenter { get; }
        internal RBTreeNode<BeachSection> ToDelete { get; }

        internal FortuneCircleEvent(VPoint lowest, double yCenter, RBTreeNode<BeachSection> toDelete)
        {
            Lowest = lowest;
            YCenter = yCenter;
            ToDelete = toDelete;
        }

        public int CompareTo(FortuneEvent other)
        {
            var c = Y.CompareTo(other.Y);
            return c == 0 ? X.CompareTo(other.X) : c;
        }

        public double X => Lowest.X;
        public double Y => Lowest.Y;
    }
}
