/*
 * Copyright JsonCons.Net authors. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 *  http://aws.amazon.com/apache2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

namespace AWS.Lambda.Powertools.JMESPath.Expressions
{
    /// <summary>
    /// A slice of a list or string.
    /// </summary>
    internal readonly struct Slice
    {
        /// <summary>
        /// The start of the slice.
        /// </summary>
        private readonly int? _start;
        /// <summary>
        /// The stop of the slice.
        /// </summary>
        private readonly int? _stop;

        /// <summary>
        /// The step of the slice.
        /// </summary>
        public int Step {get;}

        public Slice(int? start, int? stop, int step) 
        {
            _start = start;
            _stop = stop;
            Step = step;
        }

        /// <summary>
        /// Gets the start of the slice.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public int GetStart(int size)
        {
            // 2024-04-19: Powertools addition.
            if (!_start.HasValue) return Step >= 0 ? 0 : size;
            var len = _start.Value >= 0 ? _start.Value : size + _start.Value;
            return len <= size ? len : size;
        }

        /// <summary>
        /// Gets the stop of the slice.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public int GetStop(int size)
        {
            // 2024-04-19: Powertools addition.
            if (!_stop.HasValue) return Step >= 0 ? size : -1;
            var len = _stop.Value >= 0 ? _stop.Value : size + _stop.Value;
            return len <= size ? len : size;
        }
    }
}
