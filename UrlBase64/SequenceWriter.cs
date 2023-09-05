#if WITH_SPAN
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NeoSmart.Utils
{
    /// <summary>
    /// Provides an API for creating a ReadOnlySequence from disjoint Memory&lt;T&gt; or Span&lt;T&gt; instances.
    /// </summary>
    class SequenceWriter<T>
    {
        /// <summary>
        /// A statically-allocated, shared segment used to represent an empty sequence.
        /// </summary>
        private static readonly Segment<T> NoSegment = new Segment<T>(0, Array.Empty<T>());

        /// <summary>
        /// The list of segments we are combining into one ReadOnlySequence&lt;T&gt;
        /// </summary>
        private readonly List<Segment<T>> _segments = new List<Segment<T>>();

        // Avoid conditional expressions and nullable types by pointing newly
        // created instances to a shared read-only, zero-length segment.
        private Segment<T> _firstSegment = NoSegment;
        private Segment<T> _lastSegment = NoSegment;

        /// <summary>
        /// The cumulative index across all segments added to our collection.
        /// </summary>
        private long _index = 0;

        /// <summary>
        /// Add a ReadOnlyMemory&lt;T&gt; segment to the <see cref="SequenceWriter{T}"/>,
        /// making it a part of the final ReadOnlySequence&lt;T&gt; instance accessible via
        /// <see cref="Sequence"/>.
        /// </summary>
        public void Write(ReadOnlyMemory<T> memory)
        {
            var segment = new Segment<T>(_index, memory);

            if (_segments.Count == 0)
            {
                _firstSegment = segment;
            }
            else
            {
                _lastSegment.SetNext(segment);
            }

            _segments.Add(segment);
            _index += memory.Length;
            _lastSegment = segment;
        }

        /// <summary>
        /// Add a Span&lt;T&gt; segment to the <see cref="SequenceWriter{T}"/>,
        /// making it a part of the final ReadOnlySequence&lt;T&gt; instance accessible via
        /// <see cref="Sequence"/>.
        /// </summary>
        //public void Write(ReadOnlySpan<T> span)
        //{
        //    var segment = new Segment<T>(_index, span);

        //    if (_segments.Count == 0)
        //    {
        //        _firstSegment = segment;
        //    }
        //    else
        //    {
        //        _lastSegment.SetNext(segment);
        //    }

        //    _segments.Add(segment);
        //    _index += span.Length;
        //    _lastSegment = segment;
        //}

        /// <summary>
        /// Retrieve a <see cref="ReadOnlySequence{T}"/> consisting of the various, disjoint
        /// <see cref="ReadOnlyMemory{T}"/> instances added to this <see cref"SequenceWriter{T}"/>
        /// until this point. The resulting sequence is not affected by any future writes to the
        /// <see cref="SequenceWriter{T}"/> instance.
        /// </summary>
        public ReadOnlySequence<T> Sequence
        {
            get
            {
                // We always need a defensive copy of _lastSegment as it is updated if/when a
                // new segment is added. We only need a defensive copy of _firstSegment if it
                // is also the last segment. In the special case of a zero-length sequence,
                // no copies are needed as NoSegment is never updated.
                Segment<T> firstSegment, lastSegment;

                switch (_segments.Count)
                {
                    case 0:
                        firstSegment = _firstSegment;
                        lastSegment = _lastSegment;
                        break;
                    case 1:
                        firstSegment = new Segment<T>(_firstSegment);
                        lastSegment = new Segment<T>(_lastSegment);
                        break;
                    default:
                        firstSegment = _firstSegment;
                        lastSegment = new Segment<T>(_lastSegment);
                        break;
                }

                return new ReadOnlySequence<T>(firstSegment, 0, lastSegment, Math.Max(0, lastSegment.Length - 1));
            }
        }
    }

    class Segment<T> : ReadOnlySequenceSegment<T>
    {
        public int Length => base.Memory.Length;

        public Segment(long index, ReadOnlyMemory<T> memory)
        {
            base.Memory = memory;
            base.RunningIndex = index;
        }

        public Segment(Segment<T> copyFrom)
        {
            base.Memory = copyFrom.Memory;
            base.RunningIndex = copyFrom.RunningIndex;
            base.Next = copyFrom.Next;
        }

        internal void SetNext(Segment<T> segment)
        {
            base.Next = segment;
        }
    }
}
#endif
