﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MinesweeperSolver
{
    public class Solver
    {
        /*private void PrintCell(int x, int y)
        {
            var cell = cells[x, y];
            var s = $"cells[{x}, {y}] = {cell} | ";
            if (cell == Cell.Opened)
                s += $"cellContents[{x}, {y}] = {cellContents[x, y]}";
            else
                s += $"cellBombChance[{x}, {y}] = {cellBombChance[x, y]}";
            Console.WriteLine(s);
        }*/

        //private double[,] cellBombChance;
        private Window window;
        private static readonly Random random = new Random();
        private static readonly Point[] pointNeighbors =
            { Point.Up, Point.Down, Point.Left, Point.Right, Point.TopLeft, Point.TopRight, Point.BottomLeft, Point.BottomRight };

        public int RisksTaken { get; private set; }
        public int MinesFlagged { get; private set; }

        public Solver(Window window)
        {
            this.window = window;
        }

        public void Solve(bool newGame)
        {
            MinesFlagged = 0;
            RisksTaken = -1; // -1 because the first click is never a risk

            if (newGame)
                window.OpenCell(random.Next(window.FieldWidth), random.Next(window.FieldHeight));

            int asd = 0;
            while (true)
            {
                asd++;
                window.Update();

                if (window.GameOver)
                    break;

                FlagAllObviousCells();

                bool impact = OpenAllObviousCells();

                if (!impact)
                {
                    if (asd > 1)
                        Thread.Sleep(3000);
                    return;
                    //TankAlgorithm();
                    RisksTaken++;
                }

                Thread.Sleep(10); // may be not needed
            }
        }

        private void TankAlgorithm()
        {
            var islands = GetIslands(GetPointsToSolve());

            // solve islands
            // input: islands (List<List<Point>>)
            // output: mine chance for every closed cell neighboring a cell with a number (Dictionary<Point, double>)


        }

        private Dictionary<Point, double> SolveIsland(List<Point> island)
        {

        }

        private List<bool[]> GetPermutations(int mines, int cells)
        {
            if (mines >= cells)
                throw new ArgumentException("IMPOSSIBRU");

            var permutations = new List<bool[]>();
            GetPermutations(permutations, new bool[cells], 0, mines);
            return permutations;
        }

        private void GetPermutations(List<bool[]> permutations, bool[] current, int fixedCount, int availableMineCount)
        {
            var currentClone = (bool[])current.Clone();

            var mineAvailable = currentClone.Count(e => e) < availableMineCount;
            bool i = mineAvailable;
            for (int cases = mineAvailable ? 2 : 1; cases > 0; cases--, i = false)
            {
                currentClone[fixedCount] = i;

                if (currentClone.Length == fixedCount)
                    permutations.Add(currentClone);
                else
                    GetPermutations(permutations, currentClone, fixedCount + 1, availableMineCount);
            }
        }

        private List<List<Point>> GetIslands(List<Point> pointsToSolve)
        {
            var islands = new List<List<Point>>();
            foreach (var point in pointsToSolve)
            {
                List<Point> islandThisPointBelongsTo = null;
                foreach (var island in islands)
                    foreach (var islandPoint in island)
                        if (GetValidNeighbors(islandPoint).Any(p => p == point))
                        {
                            islandThisPointBelongsTo = island;
                            goto assigningPointToIsland;
                        }

                    assigningPointToIsland:

                if (islandThisPointBelongsTo == null)
                    islands.Add(new List<Point> { point });
                else
                    islandThisPointBelongsTo.Add(point);
            }
            return islands;
        }

        private List<Point> GetPointsToSolve()
        {
            var pointsToSolve = new List<Point>();
            for (int x = 0; x < window.FieldWidth; x++)
                for (int y = 0; y < window.FieldHeight; y++)
                    if (window.GetCell(x, y) == Window.Cell.Opened && IsNumber(window.GetCellContents(x, y))
                        && GetValidNeighbors(new Point(x, y)).Any(neighbor => window.GetCell(neighbor.X, neighbor.Y) == Window.Cell.Closed))
                    // ^ this is optimizable for sure (dont get all neighbors, get one then check then get another one)
                    {
                        pointsToSolve.Add(new Point(x, y));
                    }
            return pointsToSolve;
        }

        private void FlagAllObviousCells()
        {
            for (int x = 0; x < window.FieldWidth; x++)
                for (int y = 0; y < window.FieldHeight; y++)
                    if (window.GetCell(x, y) == Window.Cell.Opened)
                    {
                        var cc = window.GetCellContents(x, y);
                        if (IsNumber(cc))
                        {
                            var notOpenedNeighbors = GetValidNeighbors(new Point(x, y)).Where(neighbor => window.GetCell(neighbor) != Window.Cell.Opened).ToList();
                            if (notOpenedNeighbors.Count == ToInt(cc))
                                foreach (var neighbor in notOpenedNeighbors.Where(neighbor => window.GetCell(neighbor) == Window.Cell.Closed))
                                {
                                    window.FlagCell(neighbor);
                                    MinesFlagged++;
                                }
                        }
                    }
        }

        private bool OpenAllObviousCells()
        {
            var impact = false;
            for (int x = 0; x < window.FieldWidth; x++)
                for (int y = 0; y < window.FieldHeight; y++)
                    if (window.GetCell(x, y) == Window.Cell.Opened)
                    {
                        var cc = window.GetCellContents(x, y);
                        if (IsNumber(cc))
                        {
                            var p = new Point(x, y);
                            var neighbors = GetValidNeighbors(p);
                            if (neighbors.Count(neighbor => window.GetCell(neighbor) == Window.Cell.Flagged) == ToInt(cc)
                                && neighbors.Any(neighbor => window.GetCell(neighbor) == Window.Cell.Closed))
                            {
                                window.MassOpenCell(p);
                                impact = true;
                            }
                        }
                    }

            return impact;
        }

        private static bool IsNumber(Window.CellContents cc)
            => Window.CellContents.One <= cc && cc <= Window.CellContents.Eight;

        private static int ToInt(Window.CellContents cc)
            => cc - Window.CellContents.Empty;

        private bool IsValid(Point p)
            => p.X >= 0 && p.X < window.FieldWidth && p.Y >= 0 && p.Y < window.FieldHeight;

        private List<Point> GetValidNeighbors(Point p)
        {
            var neighbors = new List<Point>();
            foreach (var neighbor in pointNeighbors)
            {
                var n = p + neighbor;
                if (IsValid(n))
                    neighbors.Add(n);
            }
            return neighbors;
        }
    }
}
