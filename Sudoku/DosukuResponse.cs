using System.Text.Json.Serialization;

namespace Sudoku
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(DosukuResponse))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }

    public class DosukuResponse
    {
        public Newboard newboard { get; set; }
    }

    public class Newboard
    {
        public Grid[] grids { get; set; }
        public int results { get; set; }
        public string message { get; set; }
    }

    public class Grid
    {
        public int[][] value { get; set; }
        public int[][] solution { get; set; }
        public string difficulty { get; set; }
    }

}
