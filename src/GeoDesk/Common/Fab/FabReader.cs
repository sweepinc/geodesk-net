/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Globalization;
using System.IO;
using System.Text;

using GeoDesk.Common.Util;

namespace GeoDesk.Common.Fab;

// TODO: Use simple = instead of := for literal values without comments and sub-keys
/// <remarks>Ported from Java <c>com.clarisma.common.fab.FabReader</c>.</remarks>
internal class FabReader
{

    protected int tabSize = 4;
    const int MAX_NESTING_LEVELS = 32;
    readonly int[] _indentStack;
    int _currentNestingLevel;
    /// <summary>Key which has not been dispatched.</summary>
    string? _openKey;
    /// <summary>Value that has not been dispatched.</summary>
    readonly StringBuilder _openValue;
    protected string? fileName;
    /// <summary>The current line (1-based)</summary>
    protected int lineNumber;
    /// <summary>The key in the current line, or null if none</summary>
    string? _key;
    /// <summary>The value in the current line, or null if none</summary>
    string? _value;
    /// <summary>
    /// the column at which the key or value begins in the current line,
    /// or -1 if empty line
    /// </summary>
    int _lineIndent;
    /// <summary>The column at which the last key appeared</summary>
    int _keyIndent;
    /// <summary>
    /// If a key's value is spread over multiple lines, this is the column
    /// where the values should line up. If a value appears to the right,
    /// the extra whitespace becomes part of the value.
    /// A value may not appear to the left
    /// -1 if no multi-line value has been seen yet
    /// </summary>
    int _valueIndent;
    /// <summary>
    /// If true, keys and comments in the lines following a key are ignored,
    /// and instead are included as part of the value (This makes it possible
    /// to have a text value that includes a colon followed by a space)
    /// </summary>
    bool _literalMode;

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabReader()</c>.</remarks>
    public FabReader()
    {
        _openValue = new StringBuilder();
        _indentStack = new int[MAX_NESTING_LEVELS];
        _valueIndent = -1;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabReader.beginKey(String, String)</c>.</remarks>
    protected virtual void BeginKey(string key, string value)
    {
        BeginKey(key);
        KeyValue("value", value);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabReader.beginKey(String)</c>.</remarks>
    protected virtual void BeginKey(string key)
    {
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabReader.keyValue(String, String)</c>.</remarks>
    protected virtual void KeyValue(string key, string value)
    {
        System.Console.Out.Write(JavaFormat.Format("VALUE [%s] = [%s]\n", key, value));
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabReader.endKey()</c>.</remarks>
    protected virtual void EndKey()
    {
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabReader.error(String)</c>.</remarks>
    protected virtual void Error(string msg)
    {
        throw new FabException(
            string.Format(CultureInfo.InvariantCulture, "{0}:{1}: {2}",
            fileName == null ? "<none>" : fileName,
            lineNumber, msg));
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabReader.error(String, Object...)</c>.</remarks>
    protected void Error(string msg, params object?[] args)
    {
        Error(JavaFormat.Format(msg, args));
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabReader.toInt(String)</c>.</remarks>
    protected int ToInt(string s)
    {
        try
        {
            return int.Parse(s, CultureInfo.InvariantCulture);
        }
        catch (System.FormatException)
        {
            Error("Expected number instead of %s", s);
            return 0;
        }
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabReader.parseLine(String)</c>.</remarks>
    void ParseLine(string line)
    {
        var pos = 0;
        _lineIndent = 0;
        for (; pos < line.Length; pos++)
        {
            var ch = line[pos];
            if (ch == '\t')
            {
                _lineIndent += tabSize - (_lineIndent % tabSize);
                continue;
            }
            if (!char.IsWhiteSpace(ch)) break;
            _lineIndent++;
        }
        var valueStart = pos;
        var valueEnd = line.Length;
        var keyStart = pos;
        var keyEnd = -1;
        if (_lineIndent < _valueIndent) _literalMode = false;
        if (!_literalMode)
        {
            for (; pos < line.Length; pos++)
            {
                var ch = line[pos];
                if (keyEnd < 0 && ch == ':')
                {
                    if (pos == line.Length - 1 ||
                        char.IsWhiteSpace(
                        line[pos + 1]))
                    {
                        keyEnd = pos;
                        pos++;
                        _literalMode = false;
                        continue;
                    }
                    if (line[pos + 1] == '=')
                    {
                        if (pos == line.Length - 2 ||
                            char.IsWhiteSpace(
                            line[pos + 2]))
                        {
                            keyEnd = pos;
                            pos += 2;
                            _literalMode = true;
                            continue;
                        }
                    }
                }
                if (ch == '/')
                {
                    // Check for comment start:
                    // "//" followed by whitespace
                    var lineLen = line.Length;
                    if (pos > lineLen - 2) break;
                    if (line[pos + 1] == '/' && (pos + 2 == lineLen ||
                        char.IsWhiteSpace(line[pos + 2])))
                    {
                        valueEnd = pos;
                        break;
                    }
                }
            }
            if (keyEnd >= 0)
            {
                // If a key has been found, a potential value starts after the key;
                // trim leading whitespace
                valueStart = keyEnd + 2;
                for (; valueStart < line.Length; valueStart++)
                {
                    var ch = line[valueStart];
                    if (!char.IsWhiteSpace(ch)) break;
                }
            }
            // trim trailing whitespace
            for (; valueEnd > 0; valueEnd--)
            {
                var ch = line[valueEnd - 1];
                if (!char.IsWhiteSpace(ch)) break;
            }
        }
        _key = (keyEnd > keyStart) ?
            line.Substring(keyStart, keyEnd - keyStart) : null;
        _value = (valueEnd > valueStart) ?
            line.Substring(valueStart, valueEnd - valueStart) : null;
        if (_key == null && _value == null) _lineIndent = -1;
        if (_key != null)
        {
            _valueIndent = -1;
        }
        else if (_valueIndent < 0)
        {
            _valueIndent = _lineIndent;
        }
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabReader.pushIndent()</c>.</remarks>
    void PushIndent()
    {
        _indentStack[_currentNestingLevel] = _keyIndent;
        _currentNestingLevel++;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabReader.popIndent(int)</c>.</remarks>
    void PopIndent(int targetIndent)
    {
        while (_currentNestingLevel > 0 && _keyIndent != targetIndent)
        {
            _currentNestingLevel--;
            _keyIndent = _indentStack[_currentNestingLevel];
            EndKey();
        }
        if (_keyIndent != targetIndent)
        {
            Error("Unexpected indentation");
        }
    }

    /// <summary>
    /// Dispatches a pending key or key/value.
    /// </summary>
    /// <param name="keepOpen">if true, begins a block, else simply dispatches a key/value</param>
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabReader.dispatch(boolean)</c>.</remarks>
    void Dispatch(bool keepOpen)
    {
        if (_openValue.Length > 0)
        {
            var val = _openValue.ToString().Trim();
            if (keepOpen)
            {
                BeginKey(_openKey!, val);
            }
            else
            {
                KeyValue(_openKey!, val);
            }
            _openValue.Length = 0;
            _valueIndent = -1;
            _openKey = null;
        }
        else if (_openKey != null)
        {
            if (keepOpen)
            {
                BeginKey(_openKey);
            }
            else
            {
                KeyValue(_openKey, "");
            }
        }
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabReader.read(BufferedReader)</c>.</remarks>
    public void Read(TextReader @in)
    {
        for (; ; )
        {
            lineNumber++;
            var line = @in.ReadLine();
            if (line == null) break;
            ParseLine(line);
            if (_key != null)
            {
                if (_lineIndent < _keyIndent)
                {
                    Dispatch(false);
                    PopIndent(_lineIndent);
                }
                else if (_lineIndent > _keyIndent)
                {
                    Dispatch(true);
                    PushIndent();
                }
                else
                {
                    Dispatch(false);
                }
                _openKey = _key;
                _keyIndent = _lineIndent;
                if (_value != null) _openValue.Append(_value);
            }
            else if (_value != null)
            {
                if (_lineIndent <= _keyIndent ||
                    _lineIndent < _valueIndent ||
                    _openKey == null)
                {
                    Error("Expected key");
                }
                else
                {
                    if (_openValue.Length > 0) _openValue.Append('\n');
                    if (_lineIndent > _valueIndent)
                    {
                        var padding = _lineIndent - _valueIndent;
                        for (var i = 0; i < padding; i++) _openValue.Append(' ');
                    }
                    _openValue.Append(_value);
                }
            }
        }
        Dispatch(false);
        PopIndent(0);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabReader.read(InputStream)</c>.</remarks>
    public void Read(Stream @in)
    {
        Read(new StreamReader(@in));
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabReader.readFile(String)</c>.</remarks>
    public void ReadFile(string fileName)
    {
        using (TextReader @in = new StreamReader(fileName))
        {
            Read(@in);
        }
    }

}
