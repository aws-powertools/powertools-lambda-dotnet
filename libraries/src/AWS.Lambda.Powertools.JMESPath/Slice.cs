using System;

namespace AWS.Lambda.Powertools.JMESPath
{
    internal readonly struct Slice
    {
        private readonly int? _start;
        private readonly int? _stop;

        public int Step {get;}

        public Slice(int? start, int? stop, int step) 
        {
            _start = start;
            _stop = stop;
            Step = step;
        }

        public int GetStart(int size)
        {
            if (_start.HasValue)
            {
                var len = _start.Value >= 0 ? _start.Value : size + _start.Value;
                return len <= size ? len : size;
            }
            else
            {
                if (Step >= 0)
                {
                    return 0;
                }
                else 
                {
                    return size;
                }
            }
        }

        public int GetStop(int size)
        {
            if (_stop.HasValue)
            {
                var len = _stop.Value >= 0 ? _stop.Value : size + _stop.Value;
                return len <= size ? len : size;
            }
            else
            {
                return Step >= 0 ? size : -1;
            }
        }
    };

}
