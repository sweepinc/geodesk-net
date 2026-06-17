/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Concurrent;

namespace Java.Util.Concurrent;

// PORT: java.util.concurrent.LinkedBlockingQueue backed by .NET BlockingCollection.
// Used unbounded (Java's no-arg constructor), so Add() always succeeds and Take()
// blocks until an element is available.
public class LinkedBlockingQueue<E> : BlockingQueue<E>
{
    private readonly BlockingCollection<E> queue = new BlockingCollection<E>();

    public bool Add(E e)
    {
        queue.Add(e);
        return true;
    }

    public E Take()
    {
        return queue.Take();
    }
}
