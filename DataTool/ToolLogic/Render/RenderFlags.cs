using DataTool.Flag;

namespace DataTool.ToolLogic.Render {
    public class RenderFlags : ICLIFlags {
        [CLIFlag(Default = 1080, Flag = "height", Help = "Screen Height", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagInt" })]
        [Alias("h")]
        public int Height;

        [CLIFlag(Help = "Output path", Positional = 2, Required = true)]
        public string OutputPath;

        [CLIFlag(Default = 1920, Flag = "width", Help = "Screen Width", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagInt" })]
        [Alias("w")]
        public int Width;

        public override bool Validate() { return true; }
    }
}
