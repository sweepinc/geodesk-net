/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Concurrent;

namespace Java.Util.Concurrent;

// PORT: java.util.concurrent.LinkedBlockingQueue backed by .NET BlockingCollection. Used
// unbounded (Java's no-arg constructor), so Add() always succeeds and Take() blocks until an
// element is available.
/// <remarks>Ported from Java <c>java.util.concurrent.LinkedBlockingQueue</c>.</remarks>
public class LinkedBlockingQueue<E> : BlockingQueue<E>
{

    readonly BlockingCollection<E> _queue = new BlockingCollection<E>();

    /// <remarks>Ported from Java <c>java.util.concurrent.LinkedBlockingQueue.add(E)</c>.</remarks>
    public bool Add(E e)
    {
        _queue.Add(e);
        return true;
    }

    /// <remarks>Ported from Java <c>java.util.concurrent.LinkedBlockingQueue.take()</c>.</remarks>
    public E Take()
    {
        return _queue.Take();
    }

}
