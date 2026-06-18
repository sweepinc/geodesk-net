/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

using GeoDesk.Feature.Query;
using GeoDesk.Feature.Store;

using NetTopologySuite.Geometries;

namespace GeoDesk.Feature;

/// <summary>
/// A Geographic Object Library containing features.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.FeatureLibrary</c>.</remarks>
public class FeatureLibrary : WorldView, IDisposable
{

    /// <summary>
    /// Creates a <c>FeatureLibrary</c> instance associated with an existing GOL file.
    /// </summary>
    /// <param name="path">the path of the GOL file</param>
    /// <remarks>
    /// Deprecated: use <see cref="Features.Open(string)"/> instead.
    /// Ported from Java <c>com.geodesk.feature.FeatureLibrary(String)</c>.
    /// </remarks>
    public FeatureLibrary(string path) :
        base(new FeatureStore(path))
    {

    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureLibrary.geometryFactory()</c>.</remarks>
    public GeometryFactory GeometryFactory()
    {
        return store.GeometryFactory();
    }

    /// <summary>
    /// Closes the library and releases its resources.
    /// <para>
    /// <b>Important</b>: Do not call the methods of any collections or features you have retrieved
    /// from this library after you've closed it. Doing so leads to undefined results and may cause
    /// a segmentation fault.
    /// </para>
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureLibrary.close()</c>.</remarks>
    public void Close()
    {
        store.Close();
    }

    /// <remarks>Port-only adapter: IDisposable maps to Java's <c>AutoCloseable.close()</c>; delegates to <see cref="Close"/>.</remarks>
    public void Dispose()
    {
        Close();
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureLibrary.store()</c>.</remarks>
    internal FeatureStore Store()
    {
        return store;
    }

}
