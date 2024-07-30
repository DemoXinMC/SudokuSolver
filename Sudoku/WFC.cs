using System.Numerics;

namespace Sudoku
{
    public class WFC
    {
        protected List<SudokuBoard> Branches { get; set; }
        public int BranchCount { get =>  Branches.Count; }

        public WFC(SudokuBoard baseState)
        {
            Branches = new List<SudokuBoard>();
            Branches.Add(baseState);
        }

        public void GenerateBranches(int maxEntropy)
        {
            var newBranches = new List<SudokuBoard>();

            foreach (var branch in Branches)
            {
                var options = branch.GetCellOptions();

                var validBranchCells = new List<KeyValuePair<int, int>>();

                for (int row = 0; row < options.GetLength(0); row++)
                {
                    for (int col = 0; col < options.GetLength(1); col++)
                    {
                        var optionCount = BitOperations.PopCount(options[row, col]);

                        if (optionCount <= maxEntropy)
                            validBranchCells.Add(new KeyValuePair<int, int>(row, col));
                    }
                }

                foreach (var cell in validBranchCells)
                {
                    for (int i = 0; i < branch.BoardSize * branch.BoardSize; i++)
                    {
                        var fillValue = (uint)1 << i;
                        if ((options[cell.Key, cell.Value] & fillValue) > 0)
                        {
                            var newBoard = new SudokuBoard(branch);
                            newBoard.SetCell(new SudokuCell(cell.Key, cell.Value, fillValue));
                            newBranches.Add(newBoard);
                        }
                    }
                }
            }

            Branches.Clear();
            Branches.AddRange(newBranches);
        }

        public int ClearDupes()
        {
            var dupes = new List<SudokuBoard>();

            for (int i = 0; i < Branches.Count; i++)
            {
                if (dupes.Contains(Branches[i]))
                    continue;

                for (int j = i; j < Branches.Count; j++)
                {
                    if (Branches[i].Equals(Branches[j]))
                    {
                        dupes.Add(Branches[j]);
                    }
                }
            }

            foreach (var dupe in dupes)
                Branches.Remove(dupe);
            return dupes.Count;
        }

        public int RunSolves()
        {
            int totalMoves = 0;
            foreach(SudokuBoard board in Branches)
            {
                int solvedCells = -1;
                while(solvedCells != 0)
                {
                    var moves = board.SolveRound(true);
                    solvedCells = moves.Count;
                }
                totalMoves += solvedCells;
            }
            return totalMoves;
        }

        public SudokuBoard? GetSolution()
        {
            foreach(SudokuBoard board in Branches)
            {
                if (board.UnsolvedCells == 0)
                    return board;
            }

            return null;
        }
    }
}
