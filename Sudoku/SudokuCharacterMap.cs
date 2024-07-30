using SudokuInt = uint;

namespace Sudoku
{
    public static class SudokuCharacterMap
    {
        private static Dictionary<char, SudokuInt> CharToValue = [];
        private static Dictionary<SudokuInt, char> ValueToChar = [];

        static SudokuCharacterMap()
        {
            char[] symbols = [
                '1', '2', '3', '4', '5', '6', '7', '8', '9',
                '0', 'A', 'B', 'C', 'D', 'E', 'F',
                'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O',
                'P', 'Q', 'R', 'S', 'T', 'U', 'V',
                /*'W', 'X',
                'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g',
                'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
                'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'*/
                ];

            for(int i = 0; i < symbols.Length; i++)
            {
                CharToValue.Add(symbols[i], (SudokuInt)(1 << i));
                ValueToChar.Add((SudokuInt)(1 << i), symbols[i]);
            }
        }

        public static SudokuInt GetValue(char chr)
        {
            if(CharToValue.TryGetValue(chr, out var value))
                return value;
            return SudokuInt.MaxValue;
        }

        public static char GetChar(SudokuInt value)
        {
            if (value == 0) return ' ';
            if(ValueToChar.ContainsKey(value))
                return ValueToChar[value];
            throw new Exception("How did this happen?");
        }
    }
}
