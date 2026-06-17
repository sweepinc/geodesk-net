/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Xunit;

namespace GeoDesk.Tests;

/// <summary>
/// Serializes all tests that open the shared monaco.gol fixture. The store uses
/// best-effort file-region locking, so two FeatureStores opening the same file in
/// parallel can collide; placing the GOL-opening test classes in one collection makes
/// xUnit run them sequentially.
/// </summary>
[CollectionDefinition("GolFixture")]
public class GolFixtureCollection
{
}
