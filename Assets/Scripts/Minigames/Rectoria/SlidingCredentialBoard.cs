using System;
using System.Collections.Generic;

namespace CampusQuest.Rectoria
{
    /// <summary>Eight tiles and one blank. Scrambles are reachable by construction.</summary>
    public sealed class SlidingCredentialBoard
    {
        private readonly int[] cells = new int[9];
        private readonly int[] initial = new int[9];
        public int Moves { get; private set; }
        public int Blank { get; private set; }
        public int ScrambleLength { get; private set; }
        public int TileAt(int index) => cells[index];
        public int[] Capture() => (int[])cells.Clone();
        public bool IsSolved
        {
            get { for (int i = 0; i < 9; i++) if (cells[i] != (i + 1) % 9) return false; return true; }
        }

        public SlidingCredentialBoard(int seed)
        {
            var random = new Random(seed);
            for (int i = 0; i < 9; i++) cells[i] = (i + 1) % 9;
            Blank = 8;
            int previous = -1;
            for (int step = 0; step < 10 || IsSolved; step++)
            {
                var choices = new List<int>(4);
                for (int i = 0; i < 9; i++) if (i != previous && Adjacent(i, Blank)) choices.Add(i);
                int next = choices[random.Next(choices.Count)];
                previous = Blank; SwapWithBlank(next); ScrambleLength++;
            }
            Array.Copy(cells, initial, 9); Moves = 0;
        }

        public static bool Adjacent(int a, int b) => a >= 0 && a < 9 && b >= 0 && b < 9
            && Math.Abs(a % 3 - b % 3) + Math.Abs(a / 3 - b / 3) == 1;

        public bool TryMove(int index)
        {
            if (IsSolved || !Adjacent(index, Blank)) return false;
            SwapWithBlank(index); Moves++; return true;
        }

        private void SwapWithBlank(int index) { cells[Blank] = cells[index]; cells[index] = 0; Blank = index; }
        public void Restart()
        {
            Array.Copy(initial, cells, 9); Blank = Array.IndexOf(cells, 0); Moves = 0;
        }
    }
}
