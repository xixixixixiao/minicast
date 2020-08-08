namespace Minicast.App.MiniTouch
{
    public class MiniTouchBanner
    {
        public int    Version     { get; set; }
        public int    MaxContacts { get; set; }
        public int    MaxX        { get; set; }
        public int    MaxY        { get; set; }
        public int    MaxPressure { get; set; }
        public int    Pid         { get; set; }
        public double PercentX    { get; set; }
        public double PercentY    { get; set; }
    }
}