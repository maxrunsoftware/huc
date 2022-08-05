// Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace MaxRunSoftware.Utilities;

public abstract partial class XUnitTestBase
{
    private static readonly char[] textChars = Constant.Chars_Printable.ToArray();

    private int seed = Random.Shared.Next();

    protected int Seed
    {
        get
        {
            lock (locker) { return seed; }
        }
        set
        {
            lock (locker)
            {
                if (random != null) throw new InvalidOperationException($"Could not set {nameof(Seed)} to {value} because {nameof(Random)} has already been initialized with {seed}");

                seed = value;
            }
        }
    }

    private Random random;

    protected Random Random
    {
        get
        {
            lock (locker)
            {
                if (random != null) return random;
                var s = Seed;
                random = new Random(s);
                Debug($"Created: Random({s})");
                return random;
            }
        }
    }

    protected byte[] RandomBinary(
        // ReSharper disable once ParameterHidesMember
        int? seed = null,
        int lengthMin = 1,
        int lengthMax = 100_000
    )
    {
        lock (locker)
        {
            var randomSeed = seed ?? Random.Next();
            Debug($"{nameof(RandomBinary)}(seed: {randomSeed})");
            var r = new Random(randomSeed);
            var length = r.Next(lengthMin, lengthMax);
            var array = new byte[length];
            r.NextBytes(array);
            Debug($"{nameof(RandomBinary)} generated random byte[{array.Length}] with seed {randomSeed}");
            return array;
        }
    }


    protected string RandomString(
        // ReSharper disable once ParameterHidesMember
        int? seed = null,
        int? length = null,
        int lenghtMin = 1,
        int lengthMax = 1_000,
        string characterPool = Constant.Chars_Alphanumeric_String
    )
    {
        var randomSeed = seed ?? Random.Next();
        var r = new Random(randomSeed);

        var len = length ?? r.Next(lenghtMin, lengthMax);
        var s = r.NextString(len, characterPool);
        Debug($"{nameof(RandomString)}(seed: {randomSeed}): " + s);
        return s;
    }

    protected string RandomText(
        // ReSharper disable once ParameterHidesMember
        int? seed = null,
        int numberOfLinesMin = 0,
        int numberOfLinesMax = 100,
        int lineLengthMin = 1,
        int lineLengthMax = 100,
        byte chanceOfBlankLine = 10,
        byte chanceOfEndWithNewLine = 75,
        byte chanceOfSpace = 20,
        string[] newLines = null
    )
    {
        lock (locker)
        {
            newLines ??= new[] { Constant.NewLine_Windows, Constant.NewLine_Unix };
            var randomSeed = seed ?? Random.Next();
            Debug($"{nameof(RandomText)}(seed: {randomSeed})");
            var r = new Random(randomSeed);

            var sb = new StringBuilder();
            var numberOfLines = r.Next(numberOfLinesMin, numberOfLinesMax);

            for (var i = 0; i < numberOfLines; i++)
            {
                if (random.NextBool(chanceOfBlankLine))
                {
                    sb.Append(""); // NOOP
                }
                else
                {
                    var lineLen = r.Next(lineLengthMin, lineLengthMax);
                    var str = r.NextString(lineLen, textChars).ToCharArray();
                    foreach (var c in str) sb.Append(r.NextBool(chanceOfSpace) ? ' ' : c);
                }

                if (i == numberOfLines - 1)
                {
                    // last line, chance to include new line rather than leaving off the newline
                    if (r.NextBool(chanceOfEndWithNewLine)) sb.Append(r.Pick(newLines));
                }
                else
                {
                    // Pick a random newline
                    sb.Append(r.Pick(newLines));
                }
            }

            Debug($"{nameof(RandomText)} generated random string[{sb.Length}] with seed {randomSeed}");
            return sb.ToString();
        }
    }
}
