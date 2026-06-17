/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Java.Util.Concurrent;

namespace GeoDesk.Feature.Query;

// TODO: do we need this base class?
/// <remarks>Ported from Java <c>com.geodesk.feature.query.QueryTask</c>.</remarks>
public abstract class QueryTask : ForkJoinTask<QueryResults>
{

    // PORT: Java declares these protected, but they are also accessed across sibling task
    // instances within the same package (e.g. RTreeQueryTask reads parent.query). C# protected
    // does not permit cross-instance access between sibling types, so these are internal (the
    // closest equivalent to Java's package-or-subclass access).
    internal readonly Query query;
    internal QueryResults? results;

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.QueryTask(Query)</c>.</remarks>
    internal QueryTask(Query query)
    {
        this.query = query;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.QueryTask.getRawResult()</c>.</remarks>
    public override QueryResults GetRawResult()
    {
        return results!;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.QueryTask.setRawResult(QueryResults)</c>.</remarks>
    protected override void SetRawResult(QueryResults value)
    {
        results = value;
    }

}
