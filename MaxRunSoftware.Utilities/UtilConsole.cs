/*
Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)

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

namespace MaxRunSoftware.Utilities;

public static partial class Util
{
    private readonly struct ConsoleColorChanger : IDisposable
    {
        private readonly ConsoleColor foreground;
        private readonly bool foregroundSwitched;
        private readonly ConsoleColor background;
        private readonly bool backgroundSwitched;

        public ConsoleColorChanger(ConsoleColor? foreground, ConsoleColor? background)
        {
            this.foreground = System.Console.ForegroundColor;
            this.background = System.Console.BackgroundColor;

            var fswitch = false;
            if (foreground != null)
            {
                if (this.foreground != foreground.Value)
                {
                    fswitch = true;
                    System.Console.ForegroundColor = foreground.Value;
                }
            }
            foregroundSwitched = fswitch;

            var bswitch = false;
            if (background != null)
            {
                if (this.background != background.Value)
                {
                    bswitch = true;
                    System.Console.BackgroundColor = background.Value;
                }
            }
            backgroundSwitched = bswitch;
        }

        public void Dispose()
        {
            if (foregroundSwitched) System.Console.ForegroundColor = foreground;
            if (backgroundSwitched) System.Console.BackgroundColor = background;
        }
    }

    /// <summary>
    /// Changes the console color, and then changes it back when it is disposed
    /// </summary>
    /// <param name="foreground">The foreground color</param>
    /// <param name="background">The background color</param>
    /// <returns>A disposable that will change the colors back once disposed</returns>
    public static IDisposable ChangeConsoleColor(ConsoleColor? foreground = null, ConsoleColor? background = null) => new ConsoleColorChanger(foreground, background);

}
