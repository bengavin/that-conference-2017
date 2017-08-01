using Windows.UI;

namespace ThatPiSample.Models
{
    public class Beacon
    {
        public Beacon(string name, string id, Color color)
        {
            Identifier = id;
            DisplayName = name;
            DisplayColor = color;
        }

        public string Identifier { get; private set; }
        public string DisplayName { get; private set; }
        public Color DisplayColor { get; private set; }

        public double SignalStrength { get; set; }
    }
}
