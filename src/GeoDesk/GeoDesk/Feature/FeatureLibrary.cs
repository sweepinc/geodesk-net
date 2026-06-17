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

/// A Geographic Object Library containing features.
public class FeatureLibrary : WorldView, IDisposable
{
    /// Creates a <c>FeatureLibrary</c> instance associated with an existing GOL file.
    ///
    /// <deprecated>Use <see cref="Features.Open(string)"/> instead.</deprecated>
    public FeatureLibrary(string path)
        : base(new FeatureStore(path))
    {
    }

    public GeometryFactory GeometryFactory()
    {
        return store.GeometryFactory();
    }

    /// Closes the library and releases its resources.
    ///
    /// **Important**: Do not call the methods of any collections or features you have
    /// retrieved from this library after you've closed it. Doing so leads to undefined
    /// results and may cause a segmentation fault.
    public void Close()
    {
        store.Close();
    }

    public void Dispose()
    {
        Close();
    }

    // TODO: remove from public API
    /// @hidden
    public FeatureStore Store()
    {
        return store;
    }
}
