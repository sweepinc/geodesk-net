/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;

namespace Clarisma.Common.Fab;

// PORT-BLOCKED: The Java implementation drives an org.xml.sax.ContentHandler, emitting
// SAX startElement/characters/endElement events for each FAB key. .NET has no built-in
// SAX (push) parsing API equivalent, so this reader has no clean mapping without a
// third-party SAX library. A future pass could adapt it to System.Xml.XmlWriter or a
// custom handler interface. The class is unused elsewhere in the library.
public class SaxFabReader : FabReader
{
    public void Read(TextReader @in, string baseElement, object handler)
    {
        throw new NotImplementedException(
            "PORT-BLOCKED: SaxFabReader requires an org.xml.sax ContentHandler equivalent.");
    }
}
