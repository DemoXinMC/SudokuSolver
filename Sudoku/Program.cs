using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Sudoku
{
    internal class Program
    {
        private static string HeaderRow = "";
        private static string DivRow1 = "";
        private static string DivRow2 = "";
        private static string FooterRow = "";
        private static int BoardSize = 3;

        private static char[][]? testBoard = [
                [' ','5',' ',/**/'9',' ',' ',/**/'4',' ','7'],
                [' ',' ','8',/**/' ',' ',' ',/**/' ',' ',' '],
                [' ',' ',' ',/**/' ','7',' ',/**/' ','5','2'],
                /*******************************************/
                ['2',' ',' ',/**/'5','1',' ',/**/' ',' ','4'],
                [' ',' ',' ',/**/' ',' ',' ',/**/' ',' ',' '],
                ['3',' ',' ',/**/' ',' ','6',/**/'8',' ',' '],
                /*******************************************/
                [' ',' ',' ',/**/'6','5',' ',/**/' ','7',' '],
                [' ','4',' ',/**/' ',' ','3',/**/'5',' ',' '],
                ['8',' ',' ',/**/' ',' ',' ',/**/' ',' ','1'],
            ];

        static void Main(string[] args)
        {
            Console.WriteLine("Sudoku Solver v1");
            Console.WriteLine("-- DemoXin");
            Console.WriteLine();

            while (BoardSize == 0)
            {
                Console.WriteLine("Enter Board Size: ");
                var input = Console.ReadLine();
                try
                {
                    BoardSize = int.Parse(input);
                }
                catch { }
            }

            var sudoku = new SudokuBoard(BoardSize);
            BuildStaticStrings();

            if (testBoard != null)
            {
                
                for (int i = 0; i < BoardSize * BoardSize; i++)
                {
                    for (int j = 0; j < BoardSize * BoardSize; j++)
                    {
                        if (testBoard[i][j] != ' ')
                            sudoku.SetCell(new SudokuCell(i, j, testBoard[i][j]));
                    }
                }
                Console.WriteLine("Loaded Test Board:");
                RenderBoard(sudoku.GetBoardState());
            }

            var running = true;
            while(running)
            {
                Console.WriteLine("Enter Command: ");
                var input = Console.ReadLine();
                var parsed = input.Split(" ");
                if (parsed.Length == 0)
                    continue;
                switch(parsed[0].ToUpper())
                {
                    case "ADD":
                        if (parsed.Length != 4)
                            continue;
                        var cmd = new SudokuCell(int.Parse(parsed[1]), int.Parse(parsed[2]), char.Parse(parsed[3]));
                        sudoku.SetCell(cmd);
                        break;
                    case "OPTIONS":
                        if (parsed.Length != 3)
                            continue;
                        var options = sudoku.GetAvailableValues(int.Parse(parsed[1]), int.Parse(parsed[2]));
                        var outBuilder = new StringBuilder();
                        for (int i = 0; i < BoardSize * BoardSize; i++)
                        {
                            if ((options & (1 << i)) > 0)
                            {
                                outBuilder.Append(SudokuCharacterMap.GetChar((uint)1 << i));
                            }
                        }
                        Console.WriteLine(outBuilder.ToString());
                        break;
                    case "NEXT":
                        foreach(var solvable in sudoku.SolveRound())
                        {
                            Console.WriteLine(solvable.ToString());
                        }
                        break;
                    case "SOLVENEXT":
                        var timer = new Stopwatch();
                        timer.Start();
                        foreach (var solvable in sudoku.SolveRound(true))
                        {
                            Console.WriteLine(solvable.ToString());
                        }
                        timer.Stop();
                        Console.WriteLine($"Moves generated in {timer.ElapsedMilliseconds}ms");
                        RenderBoard(sudoku.GetBoardState());
                        break;
                    case "ATTEMPTSOLVE":
                        if (parsed.Length != 3)
                            continue;
                        var maxTurns = int.Parse(parsed[1]);
                        var maxOptions = int.Parse(parsed[2]);
                        Console.WriteLine($"Attempting to Solve within {maxTurns} turns anc cells with {maxOptions} or less options...");
                        AttemptCollapseSolver(sudoku, maxTurns, maxOptions);
                        break;
                    case "RENDER":
                        RenderBoard(sudoku.GetBoardState());
                        break;
                    case "EXIT":
                        running = false;
                        break;
                }
            }
        }

        static void BuildStaticStrings()
        {
            var headerBuilder = new StringBuilder();
            headerBuilder.Append("╔");
            for(int i = 0; i < BoardSize; i++)
            {
                for(int j = 0; j < BoardSize; j++)
                {
                    headerBuilder.Append("═┬");
                }
                headerBuilder.Remove(headerBuilder.Length - 1, 1);
                headerBuilder.Append("╦");
            }
            headerBuilder.Remove(headerBuilder.Length - 1, 1);
            headerBuilder.Append("╗");
            HeaderRow = headerBuilder.ToString();
            FooterRow = headerBuilder.ToString().Replace("╔", "╚").Replace("╗", "╝").Replace("╦", "╩").Replace("┬", "┴");
            DivRow1 = headerBuilder.ToString().Replace("╔", "╠").Replace("╗", "╣").Replace("╦", "╬").Replace("┬", "╬");
            DivRow2 = headerBuilder.ToString().Replace("╔", "├").Replace("╗", "┤").Replace("╦", "┼").Replace("┬", "┼").Replace("═", "─");
        }

        static SudokuBoard? AttemptCollapseSolver(SudokuBoard boardState, int maxTurns, int maxOptions)
        {
            var wfcTimer = new Stopwatch();
            wfcTimer.Start();

            Console.WriteLine("Solving Current Puzzle to branches...");
            var baseMoves = -1;
            while (baseMoves != 0)
            {
                var moves = boardState.SolveRound(true);
                baseMoves = moves.Count;
            }

            var wfc = new WFC(boardState);
            for(int i = 0; i < maxTurns; i++)
            {
                Console.WriteLine($"Turn {i+1}: {wfc.BranchCount} Branches");
                var timer = new Stopwatch();
                timer.Start();
                wfc.GenerateBranches(maxOptions);
                timer.Stop();
                var branchCreationTime = timer.ElapsedMilliseconds;
                Console.WriteLine($"\t{wfc.BranchCount} new Branches calculated in {branchCreationTime}ms");
                timer.Restart();
                var duplicateBranches = wfc.ClearDupes();
                //var duplicateBranches = 0;
                var dupeRemovalTime = timer.ElapsedMilliseconds;
                Console.WriteLine($"\t{duplicateBranches} duplicates removed in {dupeRemovalTime}ms");
                timer.Restart();
                var cellsSolved = wfc.RunSolves();
                var solverTime = timer.ElapsedMilliseconds;
                Console.WriteLine($"\tCalculated {cellsSolved} moves removed in {solverTime}ms");

                if(wfc.GetSolution() != null)
                {
                    Console.WriteLine("Found a solution!");
                    break;
                }

                if(cellsSolved == 0)
                {
                    Console.WriteLine("No branches have any moves.  How did this happen?");
                    break;
                }

                Console.WriteLine($"\tTotal Turn Time: {branchCreationTime + dupeRemovalTime + solverTime}ms");
                //Console.WriteLine($"\tTotal Turn Time: {branchCreationTime + solverTime}ms");
            }

            Console.WriteLine($"Branching Solver ran in {wfcTimer.ElapsedMilliseconds}ms.");
            return wfc.GetSolution();
        }

        static void RenderBoard(uint[,] boardState)
        {
            var outputBuilder = new StringBuilder();
            outputBuilder.AppendLine(HeaderRow);
            
            for (int i = 0; i < boardState.GetLength(0); i++)
            {
                var rowBuilder = new StringBuilder();
                if(i != 0)
                {
                    if (i % BoardSize == 0)
                        outputBuilder.AppendLine(DivRow1);
                    else
                        outputBuilder.AppendLine(DivRow2);
                }
                for (int j = 0; j < boardState.GetLength(1); j++)
                {
                    if (j % BoardSize == 0)
                        rowBuilder.Append('║');
                    else
                        rowBuilder.Append('│');
                    rowBuilder.Append(SudokuCharacterMap.GetChar(boardState[i, j]));  
                }
                rowBuilder.Append("║");
                outputBuilder.AppendLine(rowBuilder.ToString()); 
            }
            
            outputBuilder.AppendLine(FooterRow);
            Console.WriteLine(outputBuilder.ToString());
        }
    }
}
