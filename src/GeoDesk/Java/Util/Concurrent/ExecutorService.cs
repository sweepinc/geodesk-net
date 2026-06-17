/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Java.Util.Concurrent;

// PORT: subset of java.util.concurrent.ExecutorService. Only the lifecycle methods
// the port actually calls (shutdown / awaitTermination) are represented here;
// task submission lives on the concrete ForkJoinPool, matching how the Java code
// casts the executor back to ForkJoinPool before submitting.
public abstract class ExecutorService
{
    /// <summary>
    /// Initiates an orderly shutdown in which previously submitted tasks are
    /// executed, but no new tasks will be accepted.
    /// </summary>
    public abstract void Shutdown();

    /// <summary>
    /// Blocks until all tasks have completed execution after a shutdown request,
    /// or the timeout occurs, whichever happens first.
    /// </summary>
    public abstract bool AwaitTermination(long timeout, TimeUnit unit);
}
