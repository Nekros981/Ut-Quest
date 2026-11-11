using System;
using System.Collections.Generic;

namespace CampusQuest.IDGS
{
    /// <summary>4x4 orthogonal toggle board. No persistence or Unity input responsibilities.</summary>
    public sealed class IDGSVirusBoard
    {
        public const int Size = 4, CellCount = 16;
        static readonly int[] ToggleMasks = BuildMasks();
        static readonly HashSet<int> TrivialStates = BuildTrivialStates();
        readonly int[] scramble;
        public int InitialMask { get; }
        public int StateMask { get; private set; }
        public int Moves { get; private set; }
        public bool IsSolved => StateMask == 0;
        public int InfectedCount
        {
            get { int count = 0; for (int value = StateMask; value != 0; value &= value - 1) count++; return count; }
        }
        public bool IsInfected(int index) => index >= 0 && index < CellCount && (StateMask & (1 << index)) != 0;

        public IDGSVirusBoard(int seed)
        {
            var random = new Random(seed);
            int[] order = new int[CellCount];
            int[] chosen = null;
            int state = 0;
            for (int attempt = 0; attempt < 64; attempt++)
            {
                for (int i = 0; i < CellCount; i++) order[i] = i;
                for (int i = CellCount - 1; i > 0; i--)
                {
                    int other = random.Next(i + 1), saved = order[i]; order[i] = order[other]; order[other] = saved;
                }
                int length = random.Next(4, 7); state = 0;
                for (int i = 0; i < length; i++) state ^= ToggleMasks[order[i]];
                if (TrivialStates.Contains(state)) continue;
                chosen = new int[length]; Array.Copy(order, chosen, length); break;
            }
            // A bounded, known four-move fallback also starts unsolved and nontrivial.
            scramble = chosen ?? new[] { 0, 3, 12, 15 };
            state = 0; foreach (int index in scramble) state ^= ToggleMasks[index];
            InitialMask = StateMask = state;
        }
        public bool Toggle(int index)
        {
            if (index < 0 || index >= CellCount) return false;
            StateMask ^= ToggleMasks[index]; Moves++; return true;
        }
        public void Reset() { StateMask = InitialMask; Moves = 0; }
        static int[] BuildMasks()
        {
            var masks = new int[CellCount];
            for (int i = 0; i < CellCount; i++)
            {
                int x = i % Size, y = i / Size, mask = 1 << i;
                if (x > 0) mask |= 1 << (i - 1);
                if (x < Size - 1) mask |= 1 << (i + 1);
                if (y > 0) mask |= 1 << (i - Size);
                if (y < Size - 1) mask |= 1 << (i + Size);
                masks[i] = mask;
            }
            return masks;
        }
        static HashSet<int> BuildTrivialStates()
        {
            var states = new HashSet<int> { 0 };
            for (int a = 0; a < CellCount; a++)
            {
                states.Add(ToggleMasks[a]);
                for (int b = a + 1; b < CellCount; b++)
                {
                    states.Add(ToggleMasks[a] ^ ToggleMasks[b]);
                    for (int c = b + 1; c < CellCount; c++) states.Add(ToggleMasks[a] ^ ToggleMasks[b] ^ ToggleMasks[c]);
                }
            }
            return states;
        }
#if UNITY_EDITOR
        public int[] ScrambleForValidation() => (int[])scramble.Clone();
#endif
    }
}
