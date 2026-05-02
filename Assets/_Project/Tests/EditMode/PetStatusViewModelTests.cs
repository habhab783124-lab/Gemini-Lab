#nullable enable
using GeminiLab.Core.Events;
using GeminiLab.Modules.Furniture;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.UI.ViewModels;
using NUnit.Framework;

namespace GeminiLab.Tests.EditMode
{
    public sealed class PetStatusViewModelTests
    {
        [Test]
        public void ReceivesSnapshotAndWorkEvents_UpdatesViewState()
        {
            EventBus eventBus = new();
            using PetStatusViewModel viewModel = new(eventBus);

            eventBus.Publish(new PetRuntimeSnapshotChangedEvent(
                currentState: "Moving",
                mood: 66f,
                energy: 82f,
                satiety: 71f,
                workRequested: false,
                targetFurnitureId: "bed_01",
                targetFurnitureCategory: FurnitureCategory.Bed,
                targetFurnitureInteractionType: FurnitureInteractionType.SleepRest,
                isTraveling: false,
                lastInteractionFurnitureId: "harp_01",
                lastInteractionSummary: "休闲交互 / Leisure (Mood +5, Energy +0)"));
            eventBus.Publish(new PetWorkStartedEvent("trace_work", "desk_01"));

            Assert.AreEqual("Moving", viewModel.CurrentState);
            Assert.AreEqual(66f, viewModel.Mood, 0.01f);
            Assert.AreEqual(82f, viewModel.Energy, 0.01f);
            Assert.AreEqual(71f, viewModel.Satiety, 0.01f);
            Assert.AreEqual("Bed/睡眠交互 (bed_01)", viewModel.TargetLabel);
            Assert.AreEqual("休闲交互 / Leisure (Mood +5, Energy +0)", viewModel.LastInteractionSummary);
            Assert.AreEqual("Working", viewModel.WorkStatus);
            Assert.AreEqual("At desk_01", viewModel.LastWorkMessage);
        }
    }
}
