namespace Minicast.App.MiniCap
{
    public class MiniCapBanner
    {
        public int Version       { get; set; }
        public int Length        { get; set; }
        public int Pid           { get; set; }
        public int RealWidth     { get; set; }
        public int RealHeight    { get; set; }
        public int VirtualWidth  { get; set; }
        public int VirtualHeight { get; set; }
        public int Orientation   { get; set; }
        public int Quirks        { get; set; }


        /// <inheritdoc />
        public override string ToString()
        {
            return "Version       : "   + this.Version       +
                   "\nLength        : " + this.Length        +
                   "\nPid           : " + this.Pid           +
                   "\nRealWidth     : " + this.RealWidth     +
                   "\nRealHeight    : " + this.RealHeight    +
                   "\nVirtualWidth  : " + this.VirtualWidth  +
                   "\nVirtualHeight : " + this.VirtualHeight +
                   "\nOrientation   : " + this.Orientation   +
                   "\nQuirks        : " + this.Quirks;
        }
    }
}