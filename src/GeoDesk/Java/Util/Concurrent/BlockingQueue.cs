/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Java.Util.Concurrent;

// PORT: subset of java.util.concurrent.BlockingQueue used by the query engine.
public interface BlockingQueue<E>
{
    /// <summary>
    /// Inserts the element into this queue, returning <c>true</c> on success
    /// (mirrors <c>Queue.add</c>; an unbounded queue never rejects).
    /// </summary>
    bool Add(E e);

    /// <summary>
    /// Retrieves and removes the head of this queue, waiting if necessary
    /// until an element becomes available.
    /// </summary>
    E Take();
}
