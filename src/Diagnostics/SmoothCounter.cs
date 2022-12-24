namespace Bang.Diagnostics
{
    /// <summary>
    /// Class used to smooth the counter of performance ticks.
    /// </summary>
    public class SmoothCounter
    {
        private int _index = 0;

        private int _totalDeltaTime = 0;
        private int[] _previousTime;

        private int _longestTime = 0;

        private int _totalEntitiesCount = 0;
        private int[] _previousEntityCount;
        
        private readonly int _sampleSize;
        
        /// <summary>
        /// Average of counter time value over the sample size.
        /// </summary>
        public int AverageTime => (int)MathF.Round(_totalDeltaTime / (float)_sampleSize);

        /// <summary>
        /// Average of entities over the sample size.
        /// </summary>
        public int AverageEntities => (int)MathF.Round(_totalEntitiesCount / (float)_sampleSize);

        /// <summary>
        /// Maximum value over the sample size.
        /// </summary>
        public int MaximumTime => _longestTime;

        public SmoothCounter(int size = 1000) => (_sampleSize, _previousTime, _previousEntityCount) = (size, new int[size], new int[size]);

        public void Clear()
        {
            _index = 0;

            _totalDeltaTime = 0;
            _totalEntitiesCount = 0;

            _longestTime = 0;

            _previousTime = new int[_sampleSize];
            _previousEntityCount = new int[_sampleSize];
        }

        public void Update(int ms, int totalEntities)
        {
            _index++;

            if (_index == _sampleSize) 
            { 
                _index = 0;
                _longestTime = 0;
            }
            
            if (ms > _longestTime)
            {
                _longestTime = ms;
            }

            _totalDeltaTime -= _previousTime[_index];
            _totalDeltaTime += ms;

            _previousTime[_index] = ms;

            _totalEntitiesCount -= _previousEntityCount[_index];
            _totalEntitiesCount += totalEntities;

            _previousEntityCount[_index] = totalEntities;
        }
    }
}
