namespace ObjectOrientedDB.FileStorage
{
    public struct BSTNode
    {
        public long Low;
        public long High;

        public BSTNode(long Low, long High)
        {
            this.Low = Low;
            this.High = High;
        }
    }
}