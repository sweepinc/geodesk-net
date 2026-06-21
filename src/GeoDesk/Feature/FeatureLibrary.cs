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
/// A Geographic Object Library (GOL): the disposable root handle for an open feature store. It is
/// itself a <see cref="WorldView"/>, so it acts as an <see cref="IFeatureQuery"/> over all features
/// in the library, and owns the underlying <see cref="FeatureStore"/> that its views read from.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.FeatureLibrary</c>.</remarks>
public class FeatureLibrary : WorldView, IDisposable
{

    /// <summary>
    /// Opens the Geographic Object Library at the given path and returns its root handle.
    /// </summary>
    /// <param name="path">the path of the GOL file</param>
    /// <remarks>
    /// The returned <see cref="FeatureLibrary"/> is the disposable root and is itself an
    /// <see cref="IFeatureQuery"/> collection of all features. To call <see cref="IFeatureQuery"/>' default
    /// convenience methods (e.g. <c>ContainingXY</c>) directly on the root, reference it as
    /// <see cref="IFeatureQuery"/>. Ported from Java <c>com.geodesk.feature.Features.open(String)</c>.
    /// </remarks>
    public static FeatureLibrary Open(string path)
    {
        return new FeatureLibrary(path);
    }

    /// <summary>
    /// Creates a <c>FeatureLibrary</c> instance associated with an existing GOL file.
    /// </summary>
    /// <param name="path">the path of the GOL file</param>
    /// <remarks>Internal: callers open a library via <see cref="Open(string)"/>.
    /// Ported from Java <c>com.geodesk.feature.FeatureLibrary(String)</c>.</remarks>
    internal FeatureLibrary(string path) :
        base(new FeatureStore(path))
    {

    }

    /// <summary>
    /// The underlying <see cref="FeatureStore"/> backing this library.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureLibrary.store()</c>.</remarks>
    internal FeatureStore Store => store;

    /// <summary>
    /// The JTS (NetTopologySuite) <see cref="GeometryFactory"/> used to build geometries for features
    /// in this library, configured with the library's coordinate model.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureLibrary.geometryFactory()</c>.</remarks>
    public GeometryFactory GeometryFactory => store.GeometryFactory();

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

    /// <summary>
    /// Disposes the library by closing it and releasing its resources. Equivalent to <see cref="Close"/>.
    /// </summary>
    /// <remarks>Port-only adapter: IDisposable maps to Java's <c>AutoCloseable.close()</c>; delegates to <see cref="Close"/>.</remarks>
    public void Dispose()
    {
        Close();
    }

}
