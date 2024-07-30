using SudokuInt = uint;

namespace Sudoku
{
    public class SudokuCell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public SudokuInt Value { get; set; }
        public char Display
        {
            get
            {
                return SudokuCharacterMap.GetChar(Value);
            }
            set
            {
                Value = SudokuCharacterMap.GetValue(value);
            }
        }

        public SudokuCell(int x, int y, char display)
        {
            X = x;
            Y = y;
            Display = display;
        }

        public SudokuCell(int x, int y, SudokuInt value)
        {
            X = x;
            Y = y;
            Value = value;
        }

        public override string ToString()
        {
            return $"[{X},{Y}] {Display}";
        }
    }
}
