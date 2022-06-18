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

namespace MaxRunSoftware.Utilities.Tests;

public class ExecutablePoolTests
{
    public class Executable : IExecutable
    {
        public int ExecutedCount { get; private set; }

        public void Execute()
        {
            Thread.Sleep(10);
            ExecutedCount++; 
            
        }
    }
    
    public class OnComplete
    {
        public int CompletedCount { get; private set; }
        public void Complete(ExecutablePool pool) => CompletedCount++;

    }
    
    [Fact]
    public void AllTasksExecuted()
    {
        var list = new List<Executable>();
        var executableCount = 1000;
        for (var i = 0; i < executableCount; i++) list.Add(new Executable());

        var oc = new OnComplete();

        
        
        var c = new ExecutablePoolConfig
        {
            Enumerator = list.GetEnumerator(),
            NumberOfThreads = 12,
            OnComplete    = oc.Complete,
        };

        using (var ep = ExecutablePool.Execute(c))
        {
            while (!ep.GetState(false).IsComplete)
            {
                Thread.Sleep(100);
            }

            var state = ep.GetState(true);
            Assert.True(state.ExecutingItems.IsEmpty());
            Assert.True(state.ThreadsActive == 0);
            Assert.True(state.ThreadsInactive == 12);
            Assert.True(state.ThreadsTotal == 12);
        }

        
        Assert.True(list.All(o => o.ExecutedCount == 1));
        Assert.True(oc.CompletedCount == 1);
        
    }
}
