#nullable enable
using GeminiLab.Core.Events;
using NUnit.Framework;

namespace GeminiLab.Tests.EditMode
{
    public sealed class CommandDispatcherTests
    {
        private sealed class CounterCommand : ICommand
        {
            private readonly System.Action _action;

            public CounterCommand(string name, System.Action action)
            {
                Name = name;
                _action = action;
            }

            public string Name { get; }

            public void Execute()
            {
                _action.Invoke();
            }
        }

        [Test]
        public void Flush_ExecutesCommandsInFifoOrder()
        {
            CommandDispatcher dispatcher = new();
            string trace = string.Empty;

            dispatcher.Enqueue(new CounterCommand("A", () => trace += "A"));
            dispatcher.Enqueue(new CounterCommand("B", () => trace += "B"));
            dispatcher.Flush();

            Assert.AreEqual("AB", trace);
        }

        [Test]
        public void Dispatch_InvokesExecutedEvent()
        {
            CommandDispatcher dispatcher = new();
            string executedName = string.Empty;
            dispatcher.CommandExecuted += command => executedName = command.Name;

            dispatcher.Dispatch(new CounterCommand("TestCommand", () => { }));

            Assert.AreEqual("TestCommand", executedName);
        }
    }
}
