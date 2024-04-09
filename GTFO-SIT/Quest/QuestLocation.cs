namespace GTFO
{
    internal sealed class QuestLocation
    {
        public QuestLocation()
        {
        }

        public QuestLocation(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }
    }
}