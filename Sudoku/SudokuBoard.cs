﻿using System.Collections;
using System.Drawing;
using System.Numerics;

using SudokuInt = uint;

namespace Sudoku
{
    public class SudokuBoard
    {
        protected SudokuInt[,] BoardState;
        public int BoardSize { get; protected set; }

        public SudokuInt[] Rows { get; protected set; }
        public SudokuInt[] Columns { get; protected set; }
        public SudokuInt[,] Squares { get; protected set; }

        public int TotalCells { get => BoardState.Length; }
        public int SolvedCells { get; protected set; }
        public int UnsolvedCells { get => TotalCells - SolvedCells; }

        public SudokuBoard(int size)
        {
            BoardSize = size;
            Rows = new SudokuInt[BoardSize * BoardSize];
            Columns = new SudokuInt[BoardSize * BoardSize];
            Squares = new SudokuInt[BoardSize,BoardSize];
            BoardState = new SudokuInt[Rows.Length,Columns.Length];
            SolvedCells = 0;
        }

        public SudokuBoard(SudokuBoard copy)
        {
            BoardSize = copy.BoardSize;

            Rows = new SudokuInt[BoardSize * BoardSize];
            Columns = new SudokuInt[BoardSize * BoardSize];
            Squares = new SudokuInt[BoardSize, BoardSize];
            BoardState = new SudokuInt[Rows.Length, Columns.Length];

            for (int row = 0; row < Rows.Length; row++)
            {
                for(int col = 0; col < Columns.Length; col++)
                {
                    BoardState[row, col] = copy.BoardState[row, col];
                }
            }
            copy.Rows.CopyTo(Rows, 0);
            copy.Columns.CopyTo(Columns, 0);
            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    Squares[row, col] = copy.Squares[row, col];
                }
            }
            SolvedCells = copy.SolvedCells;
        }

        public void SetCell(SudokuCell cmd)
        {
            if(cmd.X < 0 || cmd.Y < 0 || cmd.X > Columns.Length - 1 || cmd.Y > Rows.Length - 1)
                throw new ArgumentException($"[{cmd.X},{cmd.Y}] is outside of the board.");
            if (BoardState[cmd.X, cmd.Y] != 0 && BoardState[cmd.X, cmd.Y] != cmd.Value)
                throw new Exception($"[{cmd.X},{cmd.Y}] has already been filled.");

            BoardState[cmd.X, cmd.Y] = cmd.Value;
            Rows[cmd.X] |= cmd.Value;
            Columns[cmd.Y] |= cmd.Value;
            Squares[(int)cmd.X / BoardSize, (int)cmd.Y / BoardSize] |= cmd.Value;
            SolvedCells++;
        }

        public List<SudokuCell> SolveRound(bool apply = false)
        {
            var solved = new List<SudokuCell>();
            SudokuInt[,] cellOptions = GetCellOptions();

            for (int row = 0; row < Rows.Length; row++)
            {
                for (int col = 0; col < Columns.Length; col++)
                {
                    if (BitOperations.PopCount(cellOptions[row, col]) == 1)
                        solved.Add(new SudokuCell(row, col, cellOptions[row, col]));
                }
            }

            for (int i = 0; i < BoardSize * BoardSize; i++)
            {
                SudokuInt checkValue = (SudokuInt)1 << i;

                for (int row = 0; row < Rows.Length; row++)
                {
                    int count = 0;
                    int foundAt = -1;

                    for (int col = 0; col < Columns.Length; col++)
                    {
                        if ((cellOptions[row, col] & checkValue) > 0)
                        {
                            foundAt = col;
                            count++;
                        }
                    }

                    if (count == 1)
                    {
                        solved.Add(new SudokuCell(row, foundAt, checkValue));
                        cellOptions[row, foundAt] = checkValue;
                    }
                }

                for (int col = 0; col < Columns.Length; col++)
                {
                    int count = 0;
                    int foundAt = -1;

                    for (int row = 0; row < Rows.Length; row++)
                    {
                        if ((cellOptions[row, col] & checkValue) > 0)
                        {
                            foundAt = row;
                            count++;
                        }
                    }

                    if (count == 1)
                    {
                        solved.Add(new SudokuCell(foundAt, col, checkValue));
                        cellOptions[foundAt, col] = checkValue;
                    }
                }

                for (int groupRow = 0; groupRow < BoardSize; groupRow++)
                {
                    for (int groupCol = 0; groupCol < BoardSize; groupCol++)
                    {
                        int count = 0;
                        int foundRow = -1;
                        int foundCol = -1;

                        for (int innerRow = 0; innerRow < BoardSize; innerRow++)
                        {
                            for (int innerCol = 0; innerCol < BoardSize; innerCol++)
                            {
                                int row = groupRow * BoardSize + innerRow;
                                int col = groupCol * BoardSize + innerCol;

                                if ((cellOptions[row, col] & checkValue) > 0)
                                {
                                    foundRow = row;
                                    foundCol = col;
                                    count++;
                                }
                            }
                        }

                        if (count == 1)
                        {
                            solved.Add(new SudokuCell(foundRow, foundCol, checkValue));
                            cellOptions[foundRow, foundCol] = checkValue;
                        }
                    }
                }
            }

            var ret = new List<SudokuCell>();

            foreach (var solve in solved)
            {
                if (BoardState[solve.X, solve.Y] == 0)
                    ret.Add(solve);
            }

            if (apply)
                foreach (var cmd in ret)
                    SetCell(cmd);

            return ret;
        }

        public SudokuInt[,] GetCellOptions()
        {
            var cellOptions = new SudokuInt[Rows.Length, Columns.Length];

            for (int row = 0; row < Rows.Length; row++)
                for (int col = 0; col < Columns.Length; col++)
                    cellOptions[row, col] = GetAvailableValues(row, col);
            return cellOptions;
        }

        public uint GetAvailableValues(int x, int y)
        {
            if (BoardState[x, y] != 0)
                return BoardState[x, y];

            var row = Rows[x];
            var col = Columns[y];
            var sq = Squares[(int)x / BoardSize, (int)y / BoardSize];
            SudokuInt sizeMask = (SudokuInt)(Math.Pow(2, BoardSize * BoardSize)) - 1;
            SudokuInt availableValues = ~(row | col | sq);
            return availableValues & sizeMask;
        }

        public override int GetHashCode()
        {
            return (int)(Rows[0] | Columns[0] | Squares[0,0]);
        }

        public override bool Equals(object? obj)
        {
            var other = obj as SudokuBoard;
            if(other == null) return false;

            for(int i = 0; i < Rows.Length; i++)
            {
                if (Rows[i] != other.Rows[i])
                    return false;
            }

            for (int i = 0; i < Columns.Length; i++)
            {
                if (Columns[i] != other.Columns[i])
                    return false;
            }

            for (int i = 0; i < Squares.GetLength(0); i++)
            {
                for(int j = 0; j < Squares.GetLength(1); j++)
                {
                    if (Squares[i, j] != other.Squares[i, j])
                        return false;
                }
            }

            for(int i = 0; i < BoardState.GetLength(0); i++)
            {
                for (int j = 0; j < BoardState.GetLength(1); j++)
                {
                    if (BoardState[i, j] != other.BoardState[i, j])
                        return false;
                }
            }

            return true;
        }

        public SudokuInt[,] GetBoardState() => (SudokuInt[,])BoardState.Clone();
    }
}
