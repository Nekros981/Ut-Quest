namespace CampusQuest.Rectoria
{
    /// <summary>Six unique receipts; all exact-sum combinations are accepted.</summary>
    public sealed class ReceiptSelection
    {
        public const int Target = 1850;
        public const int Count = 6;
        private static readonly int[] Amounts = { 300, 450, 600, 700, 850, 1000 };
        private readonly bool[] selected = new bool[Count];
        public static int AmountAt(int index) => Amounts[index];
        public bool IsSelected(int index) => index >= 0 && index < Count && selected[index];
        public int Total
        {
            get { int sum = 0; for (int i = 0; i < Count; i++) if (selected[i]) sum += Amounts[i]; return sum; }
        }
        public bool IsExact => Total == Target;
        public bool Toggle(int index)
        {
            if (index < 0 || index >= Count) return false;
            selected[index] = !selected[index]; return true;
        }
        public void Clear() { for (int i = 0; i < Count; i++) selected[i] = false; }
    }
}
