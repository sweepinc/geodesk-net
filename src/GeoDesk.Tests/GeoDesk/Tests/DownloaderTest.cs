/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using GeoDesk.Common.Util;

using Xunit;

namespace GeoDesk.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.DownloaderTest</c> (java.net HTTP → HttpClient).</remarks>
public class DownloaderTest
{

    /// <remarks>Ported from Java <c>com.geodesk.tests.DownloaderTest.testDownload()</c>.</remarks>
    [Fact]
    public async Task TestDownload()
    {
        var sourceUrl = "http://data.geodesk.com/switzerland";
        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var conn = new HttpClient(handler);

        Log.Debug("Original URL: %s", sourceUrl);
        var resp = await conn.GetAsync(sourceUrl);

        Log.Debug("Response code: %d", (int)resp.StatusCode);
        Log.Debug("Response msg:  %s", resp.ReasonPhrase);
        foreach (var entry in resp.Headers)
        {
            Console.WriteLine("Key : " + entry.Key + " ,Value : " + string.Join(",", entry.Value));
        }

        // TODO: get Content-Length and Last-Modified

        var bytes = await resp.Content.ReadAsByteArrayAsync();
        // PORT: Java writes to a hard-coded c:\geodesk\debug path; here the artifact goes to the
        // test output directory (TestSettings.OutputPath), which is created if necessary.
        await File.WriteAllBytesAsync(Path.Combine(TestSettings.OutputPath(), "download.html"), bytes);
    }

}
