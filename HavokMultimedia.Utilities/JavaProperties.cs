/*
Copyright (c) 2021 Steven Foster (steven.d.foster@gmail.com)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HavokMultimedia.Utilities
{
    public static class JavaPropertiesExtensions
    {
        public static IEnumerable<string> GetPropertyNames(this JavaProperties properties)
        {
            var enumerator = properties.PropertyNames();
            while (enumerator.MoveNext())
            {
                var s = enumerator.Current?.ToString();
                if (s.TrimOrNull() != null)
                {
                    yield return s;
                }
            }
        }

        public static Dictionary<string, List<string>> ToDictionary(this JavaProperties properties)
        {
            var d = new Dictionary<string, List<string>>();
            foreach (var key in properties.GetPropertyNames())
            {
                var k = key.TrimOrNull();
                if (k != null)
                {
                    var v = properties.GetProperty(key);
                    d.AddToList(k, v);
                }
            }

            return d;
        }

        public static void LoadFile(this JavaProperties properties, string filename, Encoding encoding = null)
        {
            if (encoding == null) encoding = Constant.ENCODING_UTF8_WITHOUT_BOM;
            using (var fs = Util.FileOpenRead(filename))
            {
                properties.Load(fs, encoding);
            }
        }

        public static void LoadFromString(this JavaProperties properties, string data)
        {
            byte[] byteArray = Constant.ENCODING_UTF8_WITHOUT_BOM.GetBytes(data);
            using (var stream = new MemoryStream(byteArray))
            {
                properties.Load(stream, Constant.ENCODING_UTF8_WITHOUT_BOM);
            }
        }
    }

    /// <summary>
    /// Hold Java style properties as key-value pairs and allow them to be loaded from or
    /// saved to a ".properties" file. The file is stored with character set ISO-8859-1 which extends US-ASCII
    /// (the characters 0-127 are the same) and forms the first part of the Unicode character set.  Within the
    /// application <see cref="string"/> are Unicode - but all values outside the basic US-ASCII set are escaped.
    /// https://github.com/Kajabity/Kajabity-Tools/
    /// </summary>
    public class JavaProperties : Hashtable
    {
        /// <summary>
        /// A reference to an optional set of default properties - these values are returned
        /// if the value has not been loaded from a ".properties" file or set programatically.
        /// </summary>
        protected Hashtable defaults;

        /// <summary>
        /// Gets a reference to the ISO-8859-1 encoding (code page 28592). This is the Java standard for .properties files.
        /// </summary>
        internal static Encoding DefaultEncoding => Constant.ENCODING_UTF8_WITHOUT_BOM; //Encoding.GetEncoding(28592);

        /// <summary>
        /// An empty constructor that doesn't set the defaults.
        /// </summary>
        public JavaProperties()
        {
        }

        /// <summary>
        /// Use this constructor to provide a set of default values.  The default values are kept separate
        /// to the ones in this instant.
        /// </summary>
        /// <param name="defaults">A Hashtable that holds a set of defafult key value pairs to
        /// return when the requested key has not been set.</param>
        public JavaProperties(Hashtable defaults) => this.defaults = defaults;

        /// <summary>
        /// Load Java Properties from a stream expecting the format as described in <see cref="JavaPropertyReader"/>.
        /// </summary>
        /// <param name="streamIn">An input stream to read properties from.</param>
        /// <exception cref="ParseException">If the stream source is invalid.</exception>
        public void Load(Stream streamIn)
        {
            var reader = new JavaPropertyReader( this );
            reader.Parse(streamIn);
        }

        /// <summary>
        /// Load Java Properties from a stream with the specified encoding and
        /// expecting the format as described in <see cref="JavaPropertyReader"/>.
        /// </summary>
        /// <param name="streamIn">An input stream to read properties from.</param>
        /// <param name="encoding">The stream's encoding.</param>
        public void Load(Stream streamIn, Encoding encoding)
        {
            var reader = new JavaPropertyReader( this );
            reader.Parse(streamIn, encoding);
        }

        /// <summary>
        /// Store the contents of this collection of properties to the stream in the format
        /// used for Java ".properties" files using an instance of <see cref="JavaPropertyWriter"/>.
        /// The keys and values will be minimally escaped to ensure special characters are read back
        /// in properly.  Keys are not sorted.  The file will begin with a comment identifying the
        /// date - and an additional comment may be included.
        /// </summary>
        /// <param name="streamOut">An output stream to write the properties to.</param>
        /// <param name="comments">Optional additional comment to include at the head of the output.</param>
        /// <param name="encoding"></param>
        public void Store(Stream streamOut, string comments, Encoding encoding = null)
        {
            var writer = new JavaPropertyWriter( this );
            writer.Write(streamOut, comments, encoding ?? DefaultEncoding);
        }

        /// <summary>
        /// Get the value for the specified key value.  If the key is not found, then return the
        /// default value - and if still not found, return null.
        /// </summary>
        /// <param name="key">The key whose value should be returned.</param>
        /// <returns>The value corresponding to the key - or null if not found.</returns>
        public string GetProperty(string key)
        {
            var objectValue = this[ key ];
            if (objectValue != null) return objectValue.ToString();
            if (defaults != null) return defaults[key]?.ToString();
            return null;
        }

        /// <summary>
        /// Get the value for the specified key value.  If the key is not found, then return the
        /// default value - and if still not found, return <c>defaultValue</c>.
        /// </summary>
        /// <param name="key">The key whose value should be returned.</param>
        /// <param name="defaultValue">The default value if the key is not found.</param>
        /// <returns>The value corresponding to the key - or null if not found.</returns>
        public string GetProperty(string key, string defaultValue) => GetProperty(key) ?? defaultValue;

        /// <summary>
        /// Set the value for a property key.  The old value is returned - if any.
        /// </summary>
        /// <param name="key">The key whose value is to be set.</param>
        /// <param name="newValue">The new value off the key.</param>
        /// <returns>The original value of the key - as a string.</returns>
        public string SetProperty(string key, string newValue)
        {
            var oldValue = this[key]?.ToString();
            this[key] = newValue;
            return oldValue;
        }

        /// <summary>
        /// Returns an enumerator of all the properties available in this instance - including the
        /// defaults.
        /// </summary>
        /// <returns>An enumarator for all of the keys including defaults.</returns>
        public IEnumerator PropertyNames()
        {
            Hashtable combined;
            if (defaults != null)
            {
                combined = new Hashtable(defaults);
                for (var e = Keys.GetEnumerator(); e.MoveNext();)
                {
                    var key = e.Current?.ToString();
                    combined.Add(key, this[key]);
                }
            }
            else
            {
                combined = new Hashtable(this);
            }

            return combined.Keys.GetEnumerator();
        }
    }

    /// <summary>
    /// This class reads Java style properties from an input stream.
    /// https://github.com/Kajabity/Kajabity-Tools/
    /// </summary>
    public class JavaPropertyReader
    {
        private const int MATCH_end_of_input = 1;
        private const int MATCH_terminator = 2;
        private const int MATCH_whitespace = 3;
        private const int MATCH_any = 4;

        private const int ACTION_add_to_key = 1;
        private const int ACTION_add_to_value = 2;
        private const int ACTION_store_property = 3;
        private const int ACTION_escape = 4;
        private const int ACTION_ignore = 5;

        private const int STATE_start = 0;
        private const int STATE_comment = 1;
        private const int STATE_key = 2;
        private const int STATE_key_escape = 3;
        private const int STATE_key_ws = 4;
        private const int STATE_before_separator = 5;
        private const int STATE_after_separator = 6;
        private const int STATE_value = 7;
        private const int STATE_value_escape = 8;
        private const int STATE_value_ws = 9;
        private const int STATE_finish = 10;

        private const int bufferSize =  1000;

        private static readonly int [][] states = new int[][] {
            new int[]{//STATE_start
                MATCH_end_of_input, STATE_finish,           ACTION_ignore,
                MATCH_terminator,   STATE_start,            ACTION_ignore,
                '#',                STATE_comment,          ACTION_ignore,
                '!',                STATE_comment,          ACTION_ignore,
                MATCH_whitespace,   STATE_start,            ACTION_ignore,
                '\\',               STATE_key_escape,       ACTION_escape,
                ':',                STATE_after_separator,  ACTION_ignore,
                '=',                STATE_after_separator,  ACTION_ignore,
                MATCH_any,          STATE_key,              ACTION_add_to_key,
            },
            new int[]{//STATE_comment
                MATCH_end_of_input, STATE_finish,           ACTION_ignore,
                MATCH_terminator,   STATE_start,            ACTION_ignore,
                MATCH_any,          STATE_comment,          ACTION_ignore,
            },
            new int[]{//STATE_key
                MATCH_end_of_input, STATE_finish,           ACTION_store_property,
                MATCH_terminator,   STATE_start,            ACTION_store_property,
                MATCH_whitespace,   STATE_before_separator, ACTION_ignore,
                '\\',               STATE_key_escape,       ACTION_escape,
                ':',                STATE_after_separator,  ACTION_ignore,
                '=',                STATE_after_separator,  ACTION_ignore,
                MATCH_any,          STATE_key,              ACTION_add_to_key,
            },
            new int[]{//STATE_key_escape
                MATCH_terminator,   STATE_key_ws,           ACTION_ignore,
                MATCH_any,          STATE_key,              ACTION_add_to_key,
            },
            new int[]{//STATE_key_ws
                MATCH_end_of_input, STATE_finish,           ACTION_store_property,
                MATCH_terminator,   STATE_start,            ACTION_store_property,
                MATCH_whitespace,   STATE_key_ws,           ACTION_ignore,
                '\\',               STATE_key_escape,       ACTION_escape,
                ':',                STATE_after_separator,  ACTION_ignore,
                '=',                STATE_after_separator,  ACTION_ignore,
                MATCH_any,          STATE_key,              ACTION_add_to_key,
            },
            new int[]{//STATE_before_separator
                MATCH_end_of_input, STATE_finish,           ACTION_store_property,
                MATCH_terminator,   STATE_start,            ACTION_store_property,
                MATCH_whitespace,   STATE_before_separator, ACTION_ignore,
                '\\',               STATE_value_escape,     ACTION_escape,
                ':',                STATE_after_separator,  ACTION_ignore,
                '=',                STATE_after_separator,  ACTION_ignore,
                MATCH_any,          STATE_value,            ACTION_add_to_value,
            },
            new int[]{//STATE_after_separator
                MATCH_end_of_input, STATE_finish,           ACTION_store_property,
                MATCH_terminator,   STATE_start,            ACTION_store_property,
                MATCH_whitespace,   STATE_after_separator,  ACTION_ignore,
                '\\',               STATE_value_escape,     ACTION_escape,
                MATCH_any,          STATE_value,            ACTION_add_to_value,
            },
            new int[]{//STATE_value
                MATCH_end_of_input, STATE_finish,           ACTION_store_property,
                MATCH_terminator,   STATE_start,            ACTION_store_property,
                '\\',               STATE_value_escape,     ACTION_escape,
                MATCH_any,          STATE_value,            ACTION_add_to_value,
            },
            new int[]{//STATE_value_escape
                MATCH_terminator,   STATE_value_ws,         ACTION_ignore,
                MATCH_any,          STATE_value,            ACTION_add_to_value
            },
            new int[]{//STATE_value_ws
                MATCH_end_of_input, STATE_finish,           ACTION_store_property,
                MATCH_terminator,   STATE_start,            ACTION_store_property,
                MATCH_whitespace,   STATE_value_ws,         ACTION_ignore,
                '\\',               STATE_value_escape,     ACTION_escape,
                MATCH_any,          STATE_value,            ACTION_add_to_value,
            }
        };

        private static readonly string[] stateNames = new string[] {
            "STATE_start",
            "STATE_comment",
            "STATE_key",
            "STATE_key_escape",
            "STATE_key_ws",
            "STATE_before_separator",
            "STATE_after_separator",
            "STATE_value",
            "STATE_value_escape",
            "STATE_value_ws",
            "STATE_finish"
        };

        private readonly Hashtable hashtable;
        private bool escaped = false;
        private StringBuilder keyBuilder = new StringBuilder();
        private StringBuilder valueBuilder = new StringBuilder();

        // we now use a BinaryReader, which supports encodings
        private BinaryReader reader = null;

        private int savedChar;

        private bool saved = false;

        /// <summary>
        /// Construct a reader passing a reference to a Hashtable (or JavaProperties) instance
        /// where the keys are to be stored.
        /// </summary>
        /// <param name="hashtable">A reference to a hashtable where the key-value pairs can be stored.</param>
        public JavaPropertyReader(Hashtable hashtable) => this.hashtable = hashtable;

        private bool Matches(int match, int ch)
        {
            switch (match)
            {
                case MATCH_end_of_input:
                    return ch == -1;

                case MATCH_terminator:
                    if (ch == '\r')
                    {
                        if (PeekChar() == '\n')
                        {
                            saved = false;
                        }
                        return true;
                    }
                    else if (ch == '\n')
                    {
                        return true;
                    }
                    return false;

                case MATCH_whitespace:
                    return ch == ' ' || ch == '\t' || ch == '\f';

                case MATCH_any:
                    return true;

                default:
                    return ch == match;
            }
        }

        private void DoAction(int action, int ch)
        {
            switch (action)
            {
                case ACTION_add_to_key:
                    keyBuilder.Append(EscapedChar(ch));
                    escaped = false;
                    break;

                case ACTION_add_to_value:
                    valueBuilder.Append(EscapedChar(ch));
                    escaped = false;
                    break;

                case ACTION_store_property:
                    //Debug.WriteLine( keyBuilder.ToString() + "=" + valueBuilder.ToString() );
                    // Corrected to avoid duplicate entry errors - thanks to David Tanner.
                    hashtable[keyBuilder.ToString()] = valueBuilder.ToString();
                    keyBuilder.Length = 0;
                    valueBuilder.Length = 0;
                    escaped = false;
                    break;

                case ACTION_escape:
                    escaped = true;
                    break;

                //case ACTION_ignore:
                default:
                    escaped = false;
                    break;
            }
        }

        private char EscapedChar(int ch)
        {
            if (escaped)
            {
                switch (ch)
                {
                    case 't':
                        return '\t';

                    case 'r':
                        return '\r';

                    case 'n':
                        return '\n';

                    case 'f':
                        return '\f';

                    case 'u':
                        var uch = 0;
                        for (var i = 0; i < 4; i++)
                        {
                            ch = NextChar();
                            if (ch >= '0' && ch <= '9')
                            {
                                uch = (uch << 4) + ch - '0';
                            }
                            else if (ch >= 'a' && ch <= 'z')
                            {
                                uch = (uch << 4) + ch - 'a' + 10;
                            }
                            else if (ch >= 'A' && ch <= 'Z')
                            {
                                uch = (uch << 4) + ch - 'A' + 10;
                            }
                            else
                            {
                                throw new ParseException("Invalid Unicode character.");
                            }
                        }
                        return (char)uch;
                }
            }

            return (char)ch;
        }

        private int NextChar()
        {
            if (saved)
            {
                saved = false;
                return savedChar;
            }

            return ReadCharSafe();
        }

        private int PeekChar()
        {
            if (saved)
            {
                return savedChar;
            }

            saved = true;
            return savedChar = ReadCharSafe();
        }

        /// <summary>
        /// A method to substitute calls to <c>stream.ReadByte()</c>.
        /// The <see cref="JavaPropertyReader" /> now uses a <see cref="BinaryReader"/> to read properties.
        /// Unlike a plain stream, the <see cref="BinaryReader"/> will not return -1 when the stream end is reached,
        /// instead an <see cref="IOException" /> is to be thrown.
        /// <para>
        /// In this method we perform a check if the stream is already processed to the end, and return <c>-1</c>.
        /// </para>
        /// </summary>
        /// <returns></returns>
        private int ReadCharSafe()
        {
            if (reader.BaseStream.Position == reader.BaseStream.Length)
            {
                // We have reached the end of the stream. The reder will throw exception if we call Read any further.
                // We just return -1 now;
                return -1;
            }
            // reader.ReadChar() will take into account the encoding.
            return reader.ReadChar();
        }

        /// <summary>
        /// <para>Load key value pairs (properties) from an input Stream expected to have ISO-8859-1 encoding (code page 28592).
        /// The input stream (usually reading from a ".properties" file) consists of a series of lines (terminated
        /// by \r, \n or \r\n) each a key value pair, a comment or a blank line.</para>
        ///
        /// <para>Leading whitespace (spaces, tabs, formfeeds) are ignored at the start of any line - and a line that is empty or
        /// contains only whitespace is blank and ignored.</para>
        ///
        /// <para>A line with the first non-whitespace character is a '#' or '!' is a comment line and the rest of the line is
        /// ignored.</para>
        ///
        /// <para>If the first non-whitespace character is not '#' or '!' then it is the start of a key.  A key is all the
        /// characters up to the first whitespace or a key/value separator - '=' or ':'.</para>
        ///
        /// <para>The separator is optional.  Any whitespace after the key or after the separator (if present) is ignored.</para>
        ///
        /// <para>The first non-whitespace character after the separator (or after the key if no separator) begins the value.
        /// The value may include whitespace, separators, or comment characters.</para>
        ///
        /// <para>Any unicode character may be included in either key or value by using escapes preceded by the escape
        /// character '\'.</para>
        ///
        /// <para>The following special cases are defined:</para>
        /// <code>
        /// 	'\t' - horizontal tab.
        /// 	'\f' - form feed.
        /// 	'\r' - return
        /// 	'\n' - new line
        /// 	'\\' - add escape character.
        ///
        /// 	'\ ' - add space in a key or at the start of a value.
        /// 	'\!', '\#' - add comment markers at the start of a key.
        /// 	'\=', '\:' - add a separator in a key.
        /// </code>
        ///
        /// <para>Any unicode character using the following escape:</para>
        /// <code>
        /// 	'\uXXXX' - where XXXX represents the unicode character code as 4 hexadecimal digits.
        /// </code>
        ///
        /// <para>Finally, longer lines can be broken by putting an escape at the very end of the line.  Any leading space
        /// (unless escaped) is skipped at the beginning of the following line.</para>
        ///
        /// Examples
        /// <code>
        /// 	a-key = a-value
        /// 	a-key : a-value
        /// 	a-key=a-value
        /// 	a-key a-value
        /// </code>
        ///
        /// <para>All the above will result in the same key/value pair - key "a-key" and value "a-value".</para>
        /// <code>
        /// 	! comment...
        /// 	# another comment...
        /// </code>
        ///
        /// <para>The above are two examples of comments.</para>
        /// <code>
        /// 	Honk\ Kong = Near China
        /// </code>
        ///
        /// <para>The above shows how to embed a space in a key - key is "Hong Kong", value is "Near China".</para>
        /// <code>
        /// 	a-longer-key-example = a really long value that is \
        /// 			split over two lines.
        /// </code>
        ///
        /// <para>An example of a long line split into two.</para>
        /// </summary>
        /// <param name="stream">The input stream that the properties are read from.</param>
        public void Parse(Stream stream) => Parse(stream, null);

        /// <summary>
        /// <para>Load key value pairs (properties) from an input Stream expected to have ISO-8859-1 encoding (code page 28592).
        /// The input stream (usually reading from a ".properties" file) consists of a series of lines (terminated
        /// by \r, \n or \r\n) each a key value pair, a comment or a blank line.</para>
        ///
        /// <para>Leading whitespace (spaces, tabs, formfeeds) are ignored at the start of any line - and a line that is empty or
        /// contains only whitespace is blank and ignored.</para>
        ///
        /// <para>A line with the first non-whitespace character is a '#' or '!' is a comment line and the rest of the line is
        /// ignored.</para>
        ///
        /// <para>If the first non-whitespace character is not '#' or '!' then it is the start of a key.  A key is all the
        /// characters up to the first whitespace or a key/value separator - '=' or ':'.</para>
        ///
        /// <para>The separator is optional.  Any whitespace after the key or after the separator (if present) is ignored.</para>
        ///
        /// <para>The first non-whitespace character after the separator (or after the key if no separator) begins the value.
        /// The value may include whitespace, separators, or comment characters.</para>
        ///
        /// <para>Any unicode character may be included in either key or value by using escapes preceded by the escape
        /// character '\'.</para>
        ///
        /// <para>The following special cases are defined:</para>
        /// <code>
        /// 	'\t' - horizontal tab.
        /// 	'\f' - form feed.
        /// 	'\r' - return
        /// 	'\n' - new line
        /// 	'\\' - add escape character.
        ///
        /// 	'\ ' - add space in a key or at the start of a value.
        /// 	'\!', '\#' - add comment markers at the start of a key.
        /// 	'\=', '\:' - add a separator in a key.
        /// </code>
        ///
        /// <para>Any unicode character using the following escape:</para>
        /// <code>
        /// 	'\uXXXX' - where XXXX represents the unicode character code as 4 hexadecimal digits.
        /// </code>
        ///
        /// <para>Finally, longer lines can be broken by putting an escape at the very end of the line.  Any leading space
        /// (unless escaped) is skipped at the beginning of the following line.</para>
        ///
        /// Examples
        /// <code>
        /// 	a-key = a-value
        /// 	a-key : a-value
        /// 	a-key=a-value
        /// 	a-key a-value
        /// </code>
        ///
        /// <para>All the above will result in the same key/value pair - key "a-key" and value "a-value".</para>
        /// <code>
        /// 	! comment...
        /// 	# another comment...
        /// </code>
        ///
        /// <para>The above are two examples of comments.</para>
        /// <code>
        /// 	Honk\ Kong = Near China
        /// </code>
        ///
        /// <para>The above shows how to embed a space in a key - key is "Hong Kong", value is "Near China".</para>
        /// <code>
        /// 	a-longer-key-example = a really long value that is \
        /// 			split over two lines.
        /// </code>
        ///
        /// <para>An example of a long line split into two.</para>
        /// </summary>
        /// <param name="stream">The input stream that the properties are read from.</param>
        /// <param name="encoding">The <see cref="System.Text.Encoding">encoding</see> that is used to read the properies file stream.</param>
        public void Parse(Stream stream, Encoding encoding)
        {
            var bufferedStream = new BufferedStream( stream, bufferSize );
            // the default encoding ISO-8859-1 (codepabe 28592) will be used if we do not pass explicitly different encoding
            var parserEncoding = encoding ?? JavaProperties.DefaultEncoding;
            reader = new BinaryReader(bufferedStream, parserEncoding);

            var state = STATE_start;
            do
            {
                var ch = NextChar();

                var matched = false;

                for (var s = 0; s < states[state].Length; s += 3)
                {
                    if (Matches(states[state][s], ch))
                    {
                        //Debug.WriteLine( stateNames[ state ] + ", " + (s/3) + ", " + ch + (ch>20?" (" + (char) ch + ")" : "") );
                        matched = true;
                        DoAction(states[state][s + 2], ch);

                        state = states[state][s + 1];
                        break;
                    }
                }

                if (!matched)
                {
                    throw new ParseException("Unexpected character at " + 1 + ": <<<" + ch + ">>>");
                }
            } while (state != STATE_finish);
        }
    }

    /// <summary>
    /// Use this class for writing a set of key value pair strings to an output stream using the Java properties format.
    /// https://github.com/Kajabity/Kajabity-Tools/
    /// </summary>
    public class JavaPropertyWriter
    {
        private static readonly char[] HEX = "0123456789ABCDEF".ToCharArray();

        private Hashtable hashtable;

        /// <summary>
        /// Construct an instance of this class.
        /// </summary>
        /// <param name="hashtable">The Hashtable (or JavaProperties) instance
        /// whose values are to be written.</param>
        public JavaPropertyWriter(Hashtable hashtable) => this.hashtable = hashtable;

        /// <summary>
        /// Escape the string as a Key with character set ISO-8859-1 -
        /// the characters 0-127 are US-ASCII and we will escape any others.  The passed string is Unicode which extends
        /// ISO-8859-1 - so all is well.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private string EscapeKey(string s)
        {
            var buf = new StringBuilder();
            var first = true;

            foreach (var c in s)
            {
                //  Avoid confusing with a comment: '!' (33), '#' (35).
                if (first)
                {
                    first = false;
                    if (c == '!' || c == '#')
                    {
                        buf.Append('\\');
                    }
                }

                switch (c)
                {
                    case '\t':  //  =09 U+0009  HORIZONTAL TABULATION   \t
                        buf.Append('\\').Append('t');
                        break;

                    case '\n':  //  =0A U+000A  LINE FEED               \n
                        buf.Append('\\').Append('n');
                        break;

                    case '\f':  //  =0C U+000C  FORM FEED               \f
                        buf.Append('\\').Append('f');
                        break;

                    case '\r':  //  =0D U+000D  CARRIAGE RETURN         \r
                        buf.Append('\\').Append('r');
                        break;

                    case ' ':   //  32: ' '
                    case ':':   //  58: ':'
                    case '=':   //  61: '='
                    case '\\':  //  92: '\'
                        buf.Append('\\').Append(c);
                        break;

                    default:
                        if (c > 31 && c < 127)
                        {
                            buf.Append(c);
                        }
                        else
                        {
                            buf.Append('\\').Append('u');
                            buf.Append(HEX[(c >> 12) & 0xF]);
                            buf.Append(HEX[(c >> 8) & 0xF]);
                            buf.Append(HEX[(c >> 4) & 0xF]);
                            buf.Append(HEX[c & 0xF]);
                        }
                        break;
                }
            }

            return buf.ToString();
        }

        private string EscapeValue(string s)
        {
            var buf = new StringBuilder();
            var first = true;

            foreach (var c in s)
            {
                //  Handle value starting with whitespace.
                if (first)
                {
                    first = false;
                    if (c == ' ')
                    {
                        buf.Append('\\').Append(' ');
                        continue;
                    }
                    else if (c == '\t')    //  =09 U+0009  HORIZONTAL TABULATION   \t
                    {
                        buf.Append('\\').Append('t');
                        continue;
                    }
                }

                switch (c)
                {
                    case '\t':  //  =09 U+0009  HORIZONTAL TABULATION   \t
                        buf.Append('\t');  //OK after first position.
                        break;

                    case '\n':  //  =0A U+000A  LINE FEED               \n
                        buf.Append('\\').Append('n');
                        break;

                    case '\f':  //  =0C U+000C  FORM FEED               \f
                        buf.Append('\\').Append('f');
                        break;

                    case '\r':  //  =0D U+000D  CARRIAGE RETURN         \r
                        buf.Append('\\').Append('r');
                        break;

                    case '\\':  //  92: '\'
                        buf.Append('\\').Append(c);
                        break;

                    default:
                        if (c > 31 && c < 127)
                        {
                            buf.Append(c);
                        }
                        else
                        {
                            buf.Append('\\').Append('u');
                            buf.Append(HEX[(c >> 12) & 0xF]);
                            buf.Append(HEX[(c >> 8) & 0xF]);
                            buf.Append(HEX[(c >> 4) & 0xF]);
                            buf.Append(HEX[c & 0xF]);
                        }
                        break;
                }
            }

            return buf.ToString();
        }

        /// <summary>
        /// Write the properties to the output stream.
        /// </summary>
        /// <param name="stream">The output stream where the properties are written.</param>
        /// <param name="comments">Optional comments that are placed at the beginning of the output.</param>
        /// <param name="encoding"></param>
        public void Write(Stream stream, string comments, Encoding encoding)
        {
            //  Create a writer to output to an ISO-8859-1 encoding (code page 28592).
            var writer = new StreamWriter( stream, encoding);

            //TODO: Confirm correct codepage:
            //  28592              iso-8859-2                   Central European (ISO)
            //  28591              iso-8859-1                   Western European (ISO)
            //  from http://msdn.microsoft.com/en-us/library/system.text.encodinginfo.getencoding.aspx

            if (comments != null)
            {
                writer.WriteLine("# " + comments);
            }

            writer.WriteLine("# " + DateTime.Now.ToString());

            for (var e = hashtable.Keys.GetEnumerator(); e.MoveNext();)
            {
                var key = e.Current.ToString();
                var val = hashtable[ key ].ToString();

                writer.WriteLine(EscapeKey(key) + "=" + EscapeValue(val));
            }

            writer.Flush();
        }
    }

    /// <summary>
	/// An exception thrown by <see cref="JavaPropertyReader"/> when parsing a properties stream.
    /// https://github.com/Kajabity/Kajabity-Tools/
	/// </summary>
	public class ParseException : System.Exception
    {
        /// <summary>
        /// Construct an exception with an error message.
        /// </summary>
        /// <param name="message">A descriptive message for the exception</param>
        public ParseException(string message) : base(message)
        {
        }
    }
}
