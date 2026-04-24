#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.FSM;
using GeminiLab.Modules.Pet;
using NUnit.Framework;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    public sealed class PetStateMachineBuilderTests
    {
        private static PetContext CreateContext(float energy = 100f)
        {
            PetStateValueSO config = ScriptableObject.CreateInstance<PetStateValueSO>();
            config.SleepEnterEnergyThreshold = 20f;
            config.SleepExitEnergyThreshold = 60f;
            PetRuntimeData data = new()
            {
                Energy = energy,
                Mood = 50f,
                Satiety = 50f
            };

            return new PetContext(data, config);
        }

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Reset();
        }

        [Test]
        public void Build_LowEnergy_TransitionsToSleeping()
        {
            PetContext context = CreateContext(energy: 10f);
            StateMachine<PetContext> machine = PetStateMachineBuilder.Build(context);

            machine.Tick(0.1f);

            Assert.AreEqual(SleepingState.StateName, context.RuntimeData.CurrentState);
        }

        [Test]
        public void Build_WorkRequested_PrioritizesWorkingOverRegularFlow()
        {
            PetContext context = CreateContext(energy: 80f);
            context.RuntimeData.WorkRequested = true;
            context.RuntimeData.TimeInCurrentState = 2f; // Also satisfies Idle -> Moving.
            StateMachine<PetContext> machine = PetStateMachineBuilder.Build(context);

            machine.Tick(0.1f);

            Assert.AreEqual(WorkingState.StateName, context.RuntimeData.CurrentState);
        }

        [Test]
        public void Build_LowEnergyAndWorkRequested_PrioritizesSleepingAnyTransition()
        {
            PetContext context = CreateContext(energy: 5f);
            context.RuntimeData.WorkRequested = true;
            StateMachine<PetContext> machine = PetStateMachineBuilder.Build(context);

            machine.Tick(0.1f);

            Assert.AreEqual(SleepingState.StateName, context.RuntimeData.CurrentState);
        }

        [Test]
        public void Build_SleepingRecoversToIdle_WhenEnergyHighEnough()
        {
            PetContext context = CreateContext(energy: 10f);
            StateMachine<PetContext> machine = PetStateMachineBuilder.Build(context);
            machine.Tick(0.1f); // Enter sleeping.

            context.RuntimeData.Energy = 80f;
            machine.Tick(0.6f);
            machine.Tick(0.01f);

            Assert.AreEqual(IdleState.StateName, context.RuntimeData.CurrentState);
        }

        [Test]
        public void PublishStateChanged_PublishesEventToEventBus()
        {
            EventBus eventBus = new();
            ServiceLocator.Register(eventBus);
            PetStateChangedEvent? received = null;
            eventBus.Subscribe<PetStateChangedEvent>(evt => received = evt);

            PetController.PublishStateChanged("Idle", "Moving");

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual("Idle", received.Value.FromState);
            Assert.AreEqual("Moving", received.Value.ToState);
        }
    }
}
