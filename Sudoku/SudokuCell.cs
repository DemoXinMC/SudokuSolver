using SudokuInt = uint;

namespace Sudoku
{
    public class SudokuCell
    {
        public int Row { get; set; }
        public int Column { get; set; }
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

        public SudokuCell(int row, int col, char display)
        {
            Row = row;
            Column = col;
            Display = display;
        }

        public SudokuCell(int x, int y, SudokuInt value)
        {
            Row = x;
            Column = y;
            Value = value;
        }

        public override string ToString()
        {
            return $"[{Row},{Column}] {Display}";
        }
    }
}
