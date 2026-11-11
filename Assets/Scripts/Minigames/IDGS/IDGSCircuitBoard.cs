using System;
using System.Collections.Generic;

namespace CampusQuest.IDGS
{
    public sealed class IDGSCircuitBoard
    {
        public const int North = 1, East = 2, South = 4, West = 8;
        public const int Entry = 3, Exit = 5;
        private readonly int[] pieces = new int[9];
        private readonly int[] solution = new int[9];
        private readonly bool[] powered = new bool[9];
        private static readonly int[][] Routes = {
            new[] {3,0,1,2,5}, new[] {3,6,7,8,5}, new[] {3,4,1,2,5},
            new[] {3,4,7,8,5}, new[] {3,0,1,4,7,8,5}, new[] {3,6,7,4,1,2,5}
        };
        public bool IsSolved { get; private set; }
        public int PoweredCount { get; private set; }
        public int MaskAt(int index) => pieces[index];
        public bool IsPowered(int index) => powered[index];

        public IDGSCircuitBoard(int seed)
        {
            var random = new Random(seed);
            for (int i = 0; i < 9; i++)
                solution[i] = Turn(random.Next(2) == 0 ? North | South : North | East, random.Next(4));
            var route = Routes[random.Next(Routes.Length)];
            for (int i = 0; i < route.Length; i++)
            {
                int before = i == 0 ? West : Toward(route[i], route[i - 1]);
                int after = i == route.Length - 1 ? East : Toward(route[i], route[i + 1]);
                solution[route[i]] = before | after;
            }
            for (int i = 0; i < 9; i++) pieces[i] = Turn(solution[i], random.Next(4));
            Evaluate();
            // Every entry shape has an orientation without a west port within two turns.
            while (IsSolved) Rotate(Entry);
        }

        public bool Rotate(int index)
        {
            if (index < 0 || index >= 9) return false;
            pieces[index] = Turn(pieces[index], 1);
            Evaluate();
            return true;
        }

        public static int Turn(int mask, int turns)
        {
            for (int i = 0; i < turns % 4; i++) mask = ((mask << 1) & 15) | (mask >> 3);
            return mask;
        }

        private static int Toward(int from, int to)
        {
            if (to == from - 3) return North;
            if (to == from + 3) return South;
            return to > from ? East : West;
        }

        private void Evaluate()
        {
            Array.Clear(powered, 0, powered.Length);
            PoweredCount = 0;
            IsSolved = false;
            if ((pieces[Entry] & West) == 0) return;
            var queue = new Queue<int>();
            queue.Enqueue(Entry);
            powered[Entry] = true;
            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                PoweredCount++;
                Visit(index, index - 3, North, South, index >= 3, queue);
                Visit(index, index + 1, East, West, index % 3 < 2, queue);
                Visit(index, index + 3, South, North, index < 6, queue);
                Visit(index, index - 1, West, East, index % 3 > 0, queue);
            }
            IsSolved = powered[Exit] && (pieces[Exit] & East) != 0;
        }

        private void Visit(int from, int to, int port, int reciprocal, bool inBounds, Queue<int> queue)
        {
            if (!inBounds || powered[to] || (pieces[from] & port) == 0 || (pieces[to] & reciprocal) == 0) return;
            powered[to] = true;
            queue.Enqueue(to);
        }
#if UNITY_EDITOR
        public int SolutionMaskForValidation(int index) => solution[index];
#endif
    }
}
