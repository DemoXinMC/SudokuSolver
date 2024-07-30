using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SudokuInt = uint;

namespace Sudoku
{
    internal class Program
    {
        private static string HeaderRow = "";
        private static string DivRow1 = "";
        private static string DivRow2 = "";
        private static string FooterRow = "";
        private static int BoardSize = 3;

        private static readonly char[][]? testBoard = [
                ['8',' ',' ',/**/' ','4','1',/**/'9','3',' '],
                [' ',' ',' ',/**/'3',' ',' ',/**/' ',' ',' '],
                [' ','2','3',/**/'7',' ',' ',/**/'4','6',' '],
                /*******************************************/
                [' ','6',' ',/**/' ','8',' ',/**/' ',' ',' '],
                ['1',' ',' ',/**/'9',' ','7',/**/' ',' ','6'],
                [' ',' ',' ',/**/' ','1',' ',/**/' ','7',' '],
                /*******************************************/
                [' ','9','8',/**/' ',' ','4',/**/'7','1',' '],
                [' ',' ',' ',/**/' ',' ','3',/**/' ',' ',' '],
                [' ','5','4',/**/'1','7',' ',/**/' ',' ','8'],
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
                if (input == null)
                    continue;
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
                        var solution = AttemptCollapseSolver(sudoku, maxTurns, maxOptions);
                        if(solution != null)
                            sudoku = solution;
                        break;
                    case "RENDER":
                        RenderBoard(sudoku.GetBoardState());
                        break;
                    case "DOSUKU":
                        if (parsed.Length != 4)
                            continue;
                        DownloadAndSolveDosuku(int.Parse(parsed[1]), int.Parse(parsed[2]), int.Parse(parsed[3]));
                        break;
                    case "EXIT":
                        running = false;
                        break;
                }
            }
        }

        private static void DownloadAndSolveDosuku(int numPuzzles, int maxTurns, int maxEntropy)
        {
            var puzzles = new List<SudokuBoard>();
            var solutions = new List<SudokuBoard>();
            var difficulties = new List<string>();
            Console.WriteLine($"Getting {numPuzzles} from the Dosuku API...");

            using (var client = new HttpClient())
            {
                var url = "https://sudoku-api.vercel.app/api/dosuku?query={newboard(limit:%NUM){grids{value,solution,difficulty},results,message}}";
                url = url.Replace("%NUM", numPuzzles.ToString());
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = client.GetAsync(url).Result;
                if (response.IsSuccessStatusCode)
                {
                    var dosukuResponse = response.Content.ReadFromJsonAsync(SourceGenerationContext.Default.DosukuResponse).Result;
                    if(dosukuResponse == null)
                    {
                        Console.WriteLine("Got null response.");
                        return;
                    }
                    foreach (var board in dosukuResponse.newboard.grids)
                    {
                        var newPuzzle = new SudokuBoard(3);
                        var newSolution = new SudokuBoard(3);
                        for (int row = 0; row < 9; row++)
                        {
                            for (int col = 0; col < 9; col++)
                            {
                                if (board.value[row][col] != 0)
                                    newPuzzle.SetCell(new SudokuCell(row, col, SudokuCharacterMap.GetValue(board.value[row][col].ToString()[0])));
                                newSolution.SetCell(new SudokuCell(row, col, (SudokuInt)board.value[row][col]));
                            }
                        }
                        puzzles.Add(newPuzzle);
                        solutions.Add(newSolution);
                        difficulties.Add(board.difficulty);
                    }
                }
                else
                {
                    Console.WriteLine("Failed to retrieve puzzles: {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                }
            }

            if (puzzles.Count <= 0)
            {
                Console.WriteLine($"Didn't get any puzzles.");
                return;
            }

            Console.WriteLine($"Got {puzzles.Count} puzzles.");

            var i = 0;
            foreach(var puzzle in puzzles)
            {
                Console.WriteLine($"Solving Puzzle {i+1}: {difficulties[i]}");
                var before = RenderBoard(puzzle.GetBoardState());

                if (puzzle.SolvedCells < 17)
                {
                    Console.WriteLine($"Puzzle is malformed!  There are only {puzzle.SolvedCells} given values.");
                    i++;
                    continue;
                }

                var res = AttemptCollapseSolver(puzzle, maxTurns, maxEntropy);
                if(res == null)
                {
                    Console.WriteLine(before);
                    Console.WriteLine($"Solving Failed for Puzzle {i+1}!");
                    i++;
                    continue;
                }

                var beforeLines = before.Split(before.Last(), StringSplitOptions.RemoveEmptyEntries);
                var after = RenderBoard(res.GetBoardState(), false);
                var afterLines = after.Split(after.Last(), StringSplitOptions.RemoveEmptyEntries);
                if (beforeLines.Length != 19 || afterLines.Length != 19)
                {
                    Console.WriteLine($"Failed to render? {beforeLines.Length} / {afterLines.Length} lines.");
                    i++;
                    continue;
                }

                var outputBuilder = new StringBuilder();
                for(int lineNum = 0; lineNum < beforeLines.Length; lineNum++)
                {
                    outputBuilder.Append('\t');
                    outputBuilder.Append(beforeLines[lineNum]);
                    if(lineNum == beforeLines.Length / 2)
                        outputBuilder.Append("    ");
                    else
                        outputBuilder.Append(" -> ");
                    outputBuilder.AppendLine(afterLines[lineNum]);
                }

                outputBuilder.AppendLine();
                outputBuilder.AppendLine($"Matches? {(res == solutions[i] ? "Yes" : "No")}");

                
                Console.WriteLine();
                i++;
            }
        }

        static void BuildStaticStrings()
        {
            var headerBuilder = new StringBuilder();
            headerBuilder.Append('╔');
            for(int i = 0; i < BoardSize; i++)
            {
                for(int j = 0; j < BoardSize; j++)
                {
                    headerBuilder.Append("═┬");
                }
                headerBuilder.Remove(headerBuilder.Length - 1, 1);
                headerBuilder.Append('╦');
            }
            headerBuilder.Remove(headerBuilder.Length - 1, 1);
            headerBuilder.Append('╗');
            HeaderRow = headerBuilder.ToString();
            FooterRow = headerBuilder.ToString().Replace("╔", "╚").Replace("╗", "╝").Replace("╦", "╩").Replace("┬", "┴");
            DivRow1 = headerBuilder.ToString().Replace("╔", "╠").Replace("╗", "╣").Replace("╦", "╬").Replace("┬", "╬");
            DivRow2 = headerBuilder.ToString().Replace("╔", "├").Replace("╗", "┤").Replace("╦", "┼").Replace("┬", "┼").Replace("═", "─");
        }

        static SudokuBoard? AttemptCollapseSolver(SudokuBoard boardState, int maxTurns, int maxOptions)
        {
            var wfcTimer = new Stopwatch();
            wfcTimer.Start();

            var passes = 0;
            var passMoves = -1;
            var turnZeroMoves = 0;
            while (passMoves != 0)
            {
                passes++;
                var moves = boardState.SolveRound(true);
                passMoves = moves.Count;
                turnZeroMoves += moves.Count;
            }
            Console.WriteLine($"Turn 0: {turnZeroMoves} moves in {passes} passes.");

            if (boardState.UnsolvedCells == 0)
            {
                Console.WriteLine("Solved without entering the multiverse!");
                return boardState;
            }

            var wfc = new WFC(boardState);
            for(int i = 0; i < maxTurns; i++)
            {
                long maxBoardStates = 1;
                var cellOptions = boardState.GetCellOptions();
                for (var row = 0; row < boardState.Rows.Length; row++)
                {
                    for (var col = 0; col < boardState.Columns.Length; col++)
                    {
                        maxBoardStates *= BitOperations.PopCount(cellOptions[row, col]);
                    }
                }
                Console.WriteLine($"Remaining Possible Board States: {maxBoardStates}");

                var debugBuilder = new StringBuilder();
                var startingStates = wfc.BranchCount;
                Console.WriteLine($"Turn {i+1}: {startingStates} Theories");
                var timer = new Stopwatch();
                timer.Start();
                wfc.GenerateBranches(maxOptions);
                var newStates = wfc.BranchCount - startingStates;
                var newStatesTime = timer.ElapsedMilliseconds;
                debugBuilder.Append('\t');
                debugBuilder.Append($"{newStates} New Theories in {newStatesTime}ms");

                timer.Restart();
                var duplicateStates = wfc.ClearDupes();
                var dupeRemovalTime = timer.ElapsedMilliseconds;
                debugBuilder.Append('\t');
                debugBuilder.Append($"{duplicateStates} Duplicate Theories Cleared in {dupeRemovalTime}ms");

                timer.Restart();
                var cellsSolved = wfc.RunSolves();
                var solverTime = timer.ElapsedMilliseconds;
                debugBuilder.Append('\t');
                debugBuilder.Append($"{cellsSolved} Moves in {solverTime}ms");
                debugBuilder.AppendLine();
                
                var solution = wfc.GetSolution();

                debugBuilder.Append($"\tTotal Turn Time: {newStatesTime + dupeRemovalTime + solverTime}ms");
                debugBuilder.Append('\t');
                debugBuilder.Append($"Solved: {(solution != null ? "Yes" : "No")}");
                Console.WriteLine(debugBuilder.ToString());
                if (solution != null)
                    break;
            }

            Console.WriteLine($"Solver ran in {wfcTimer.ElapsedMilliseconds}ms.");
            var res = wfc.GetSolution();
            wfc.Free();
            return res;
        }

        static string RenderBoard(uint[,] boardState, bool print = true)
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
                rowBuilder.Append('║');
                outputBuilder.AppendLine(rowBuilder.ToString()); 
            }
            
            outputBuilder.AppendLine(FooterRow);
            if(print)
                Console.WriteLine(outputBuilder.ToString());
            return outputBuilder.ToString();
        }
    }
}
