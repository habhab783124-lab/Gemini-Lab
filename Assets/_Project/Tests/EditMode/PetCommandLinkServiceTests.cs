#nullable enable
using GeminiLab.Modules.Pet;
using NUnit.Framework;

namespace GeminiLab.Tests.EditMode
{
    public sealed class PetCommandLinkServiceTests
    {
        [Test]
        public void Enqueue_HigherPriorityDequeuedFirst()
        {
            PetCommandLinkService service = new();
            _ = service.Enqueue(new PetCommandRequest("low", PetCommandType.WorkRequest, PetCommandSource.System, forceWake: false, priority: 1, PetWorkTargetType.WorkDesk, "low"));
            _ = service.Enqueue(new PetCommandRequest("high", PetCommandType.WorkRequest, PetCommandSource.System, forceWake: false, priority: 10, PetWorkTargetType.WorkDesk, "high"));

            bool hasCommand = service.TryDequeue(out PetCommand command);

            Assert.IsTrue(hasCommand);
            Assert.AreEqual("high", command.TraceId);
        }

        [Test]
        public void RequestWork_CreatesWorkRequestCommand()
        {
            PetCommandLinkService service = new();

            _ = service.RequestWork(forceWake: true);
            bool hasCommand = service.TryDequeue(out PetCommand command);

            Assert.IsTrue(hasCommand);
            Assert.AreEqual(PetCommandType.WorkRequest, command.Request.CommandType);
            Assert.IsTrue(command.Request.ForceWake);
            Assert.AreEqual(PetWorkTargetType.WorkDesk, command.Request.TargetType);
        }
    }
}
