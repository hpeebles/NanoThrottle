namespace NanoThrottle
{
    public readonly struct InstanceCountChangedNotification
    {
        public InstanceCountChangedNotification(int oldInstanceCount, int newInstanceCount)
        {
            OldInstanceCount = oldInstanceCount;
            NewInstanceCount = newInstanceCount;
        }
        
        public int OldInstanceCount { get; }
        public int NewInstanceCount { get; }
    }
}