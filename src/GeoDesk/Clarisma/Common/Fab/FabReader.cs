/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Globalization;
using System.IO;
using System.Text;
using Clarisma.Common.Util;

namespace Clarisma.Common.Fab;

// TODO: Use simple = instead of := for literal values without comments and sub-keys
/// <remarks>Ported from Java <c>com.clarisma.common.fab.FabReader</c>.</remarks>
public class FabReader
{
    protected int tabSize = 4;
    private const int MAX_NESTING_LEVELS = 32;
    private readonly int[] indentStack;
    private int currentNestingLevel;
    /// <summary>Key which has not been dispatched.</summary>
    private string? openKey;
    /// <summary>Value that has not been dispatched.</summary>
    private readonly StringBuilder openValue;
    protected string? fileName;
    /// <summary>The current line (1-based)</summary>
    protected int lineNumber;
    /// <summary>The key in the current line, or null if none</summary>
    private string? key;
    /// <summary>The value in the current line, or null if none</summary>
    private string? value;
    /// <summary>
    /// the column at which the key or value begins in the current line, or -1 if empty line
    /// </summary>
    private int lineIndent;
    /// <summary>The column at which the last key appeared</summary>
    private int keyIndent;
    /// <summary>
    /// If a key's value is spread over multiple lines, this is the column where the
    /// values should line up. -1 if no multi-line value has been seen yet.
    /// </summary>
    private int valueIndent;
    /// <summary>
    /// If true, keys and comments in the lines following a key are ignored, and instead
    /// are included as part of the value.
    /// </summary>
    private bool literalMode;

    public FabReader()
    {
        openValue = new StringBuilder();
        indentStack = new int[MAX_NESTING_LEVELS];
        valueIndent = -1;
    }

    protected virtual void BeginKey(string key, string value)
    {
        BeginKey(key);
        KeyValue("value", value);
    }

    protected virtual void BeginKey(string key)
    {
    }

    protected virtual void KeyValue(string key, string value)
    {
        System.Console.Out.Write(JavaFormat.Format("VALUE [%s] = [%s]\n", key, value));
    }

    protected virtual void EndKey()
    {
    }

    protected virtual void Error(string msg)
    {
        throw new FabException(
            string.Format(CultureInfo.InvariantCulture, "{0}:{1}: {2}",
            fileName == null ? "<none>" : fileName,
            lineNumber, msg));
    }

    protected void Error(string msg, params object?[] args)
    {
        Error(JavaFormat.Format(msg, args));
    }

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

    private void ParseLine(string line)
    {
        int pos = 0;
        lineIndent = 0;
        for (; pos < line.Length; pos++)
        {
            char ch = line[pos];
            if (ch == '\t')
            {
                lineIndent += tabSize - (lineIndent % tabSize);
                continue;
            }
            if (!char.IsWhiteSpace(ch)) break;
            lineIndent++;
        }
        int valueStart = pos;
        int valueEnd = line.Length;
        int keyStart = pos;
        int keyEnd = -1;
        if (lineIndent < valueIndent) literalMode = false;
        if (!literalMode)
        {
            for (; pos < line.Length; pos++)
            {
                char ch = line[pos];
                if (keyEnd < 0 && ch == ':')
                {
                    if (pos == line.Length - 1 ||
                        char.IsWhiteSpace(
                        line[pos + 1]))
                    {
                        keyEnd = pos;
                        pos++;
                        literalMode = false;
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
                            literalMode = true;
                            continue;
                        }
                    }
                }
                if (ch == '/')
                {
                    // Check for comment start:
                    // "//" followed by whitespace
                    int lineLen = line.Length;
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
                    char ch = line[valueStart];
                    if (!char.IsWhiteSpace(ch)) break;
                }
            }
            // trim trailing whitespace
            for (; valueEnd > 0; valueEnd--)
            {
                char ch = line[valueEnd - 1];
                if (!char.IsWhiteSpace(ch)) break;
            }
        }
        key = (keyEnd > keyStart) ?
            line.Substring(keyStart, keyEnd - keyStart) : null;
        value = (valueEnd > valueStart) ?
            line.Substring(valueStart, valueEnd - valueStart) : null;
        if (key == null && value == null) lineIndent = -1;
        if (key != null)
        {
            valueIndent = -1;
        }
        else if (valueIndent < 0)
        {
            valueIndent = lineIndent;
        }
    }

    private void PushIndent()
    {
        indentStack[currentNestingLevel] = keyIndent;
        currentNestingLevel++;
    }

    private void PopIndent(int targetIndent)
    {
        while (currentNestingLevel > 0 && keyIndent != targetIndent)
        {
            currentNestingLevel--;
            keyIndent = indentStack[currentNestingLevel];
            EndKey();
        }
        if (keyIndent != targetIndent)
        {
            Error("Unexpected indentation");
        }
    }

    /// <summary>
    /// Dispatches a pending key or key/value.
    /// </summary>
    /// <param name="keepOpen">if true, begins a block, else simply dispatches a key/value</param>
    private void Dispatch(bool keepOpen)
    {
        if (openValue.Length > 0)
        {
            string val = openValue.ToString().Trim();
            if (keepOpen)
            {
                BeginKey(openKey!, val);
            }
            else
            {
                KeyValue(openKey!, val);
            }
            openValue.Length = 0;
            valueIndent = -1;
            openKey = null;
        }
        else if (openKey != null)
        {
            if (keepOpen)
            {
                BeginKey(openKey);
            }
            else
            {
                KeyValue(openKey, "");
            }
        }
    }

    public void Read(TextReader @in)
    {
        for (; ; )
        {
            lineNumber++;
            string? line = @in.ReadLine();
            if (line == null) break;
            ParseLine(line);
            if (key != null)
            {
                if (lineIndent < keyIndent)
                {
                    Dispatch(false);
                    PopIndent(lineIndent);
                }
                else if (lineIndent > keyIndent)
                {
                    Dispatch(true);
                    PushIndent();
                }
                else
                {
                    Dispatch(false);
                }
                openKey = key;
                keyIndent = lineIndent;
                if (value != null) openValue.Append(value);
            }
            else if (value != null)
            {
                if (lineIndent <= keyIndent ||
                    lineIndent < valueIndent ||
                    openKey == null)
                {
                    Error("Expected key");
                }
                else
                {
                    if (openValue.Length > 0) openValue.Append('\n');
                    if (lineIndent > valueIndent)
                    {
                        int padding = lineIndent - valueIndent;
                        for (int i = 0; i < padding; i++) openValue.Append(' ');
                    }
                    openValue.Append(value);
                }
            }
        }
        Dispatch(false);
        PopIndent(0);
    }

    public void Read(Stream @in)
    {
        Read(new StreamReader(@in));
    }

    public void ReadFile(string fileName)
    {
        using (TextReader @in = new StreamReader(fileName))
        {
            Read(@in);
        }
    }
}
