/*----------------------------------------------------------
This Source Code Form is subject to the terms of the
Mozilla Public License, v.2.0. If a copy of the MPL
was not distributed with this file, You can obtain one
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using FluentAssertions;
using OneScript.DebugServices;
using Xunit;

namespace OneScript.DebugProtocol.Test
{
    public class BreakpointManagerTests
    {
        [Fact]
        public void ConditionOfRemovedBreakpointIsNull()
        {
            var manager = new DefaultBreakpointManager();
            manager.SetBreakpoints("module.os", new[] { (10, "А = 1") });
            manager.FindBreakpoint("module.os", 10).Should().BeTrue();

            // Отладчик снял точку между проверкой строки и чтением условия
            manager.Clear();

            manager.GetCondition("module.os", 10).Should().BeNull();
        }

        [Fact]
        public void BreakpointsCanBeCheckedWhileDebuggerChangesThem()
        {
            const string module = "module.os";
            var manager = new DefaultBreakpointManager();
            var errors = new ConcurrentQueue<Exception>();
            var stop = false;

            // Потоки скриптов проверяют точки на каждой строке
            var scriptThreads = Enumerable.Range(0, 4).Select(_ => new Thread(() =>
            {
                try
                {
                    while (!Volatile.Read(ref stop))
                    {
                        for (var line = 1; line <= 20; line++)
                        {
                            if (manager.FindBreakpoint(module, line))
                                manager.GetCondition(module, line);
                        }
                        manager.StopOnAnyException("ошибка");
                    }
                }
                catch (Exception e)
                {
                    errors.Enqueue(e);
                }
            }) { IsBackground = true }).ToArray();

            foreach (var thread in scriptThreads)
            {
                thread.Start();
            }

            // Поток отладчика в это время меняет точки
            var breakpoints = Enumerable.Range(1, 20).Select(line => (line, "")).ToArray();
            for (var i = 0; i < 20000; i++)
            {
                manager.SetBreakpoints(module, breakpoints);
                manager.SetExceptionBreakpoints(new[] { ("all", "ошибка") });
                manager.Clear();
            }

            Volatile.Write(ref stop, true);
            foreach (var thread in scriptThreads)
            {
                thread.Join();
            }

            errors.Should().BeEmpty();
        }
    }
}
