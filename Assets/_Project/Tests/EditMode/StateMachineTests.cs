#nullable enable
using GeminiLab.Core.FSM;
using NUnit.Framework;

namespace GeminiLab.Tests.EditMode
{
    public sealed class StateMachineTests
    {
        private sealed class TestContext
        {
            public bool Flag;
            public int TickCount;
        }

        private sealed class IdleState : IState<TestContext>
        {
            public string Name => "Idle";

            public void Enter(TestContext context) { }

            public void Tick(TestContext context, float deltaTime)
            {
                context.TickCount++;
            }

            public void FixedTick(TestContext context, float fixedDeltaTime) { }

            public void Exit(TestContext context) { }
        }

        private sealed class ActiveState : IState<TestContext>
        {
            public string Name => "Active";

            public void Enter(TestContext context) { }

            public void Tick(TestContext context, float deltaTime) { }

            public void FixedTick(TestContext context, float fixedDeltaTime) { }

            public void Exit(TestContext context) { }
        }

        [Test]
        public void Tick_WhenTransitionConditionTrue_ChangesState()
        {
            TestContext context = new();
            StateMachine<TestContext> machine = new(context);
            machine
                .AddState(new IdleState())
                .AddState(new ActiveState())
                .AddTransition<IdleState, ActiveState>(c => c.Flag);
            machine.SetInitialState<IdleState>();

            context.Flag = true;
            machine.Tick(0.1f);

            Assert.AreEqual("Active", machine.CurrentState?.Name);
        }

        [Test]
        public void Tick_WhenNoTransition_TicksCurrentState()
        {
            TestContext context = new();
            StateMachine<TestContext> machine = new(context);
            machine.AddState(new IdleState());
            machine.SetInitialState<IdleState>();

            machine.Tick(0.1f);

            Assert.AreEqual(1, context.TickCount);
            Assert.AreEqual("Idle", machine.CurrentState?.Name);
        }
    }
}
