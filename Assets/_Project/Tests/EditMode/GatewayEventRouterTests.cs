#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core.Events;
using GeminiLab.Modules.Gateway;
using GeminiLab.Modules.Pet;
using NUnit.Framework;

namespace GeminiLab.Tests.EditMode
{
    public sealed class GatewayEventRouterTests
    {
        [Test]
        public void WorkDoneEvent_EnqueuesWorkCompletedCommand()
        {
            FakeGatewayClient client = new();
            PetCommandLinkService commandService = new();
            EventBus eventBus = new();
            using GatewayEventRouter router = new(client, commandService, eventBus);
            eventBus.Publish(new PetCommandAcceptedEvent("trace_done", false, PetCommandType.WorkRequest, PetCommandSource.PlayerMessage));

            client.Emit(new GatewayEventEnvelope
            {
                TraceId = "trace_done",
                EventType = GatewayEventType.WorkDone,
                Message = "ok"
            });
            router.ProcessPendingEvents();

            bool hasCommand = commandService.TryDequeue(out PetCommand command);
            Assert.IsTrue(hasCommand);
            Assert.AreEqual(PetCommandType.WorkCompleted, command.Request.CommandType);
            Assert.AreEqual("trace_done", command.TraceId);
        }

        [Test]
        public void WorkDoneEvent_ArrivesBeforeAccepted_IsDeferredAndReplayed()
        {
            FakeGatewayClient client = new();
            PetCommandLinkService commandService = new();
            EventBus eventBus = new();
            using GatewayEventRouter router = new(client, commandService, eventBus);

            client.Emit(new GatewayEventEnvelope
            {
                TraceId = "trace_deferred",
                EventType = GatewayEventType.WorkDone,
                Message = "done_first"
            });
            router.ProcessPendingEvents();
            Assert.IsFalse(commandService.TryDequeue(out PetCommand _));

            eventBus.Publish(new PetCommandAcceptedEvent("trace_deferred", false, PetCommandType.WorkRequest, PetCommandSource.PlayerMessage));

            bool hasCommand = commandService.TryDequeue(out PetCommand replayed);
            Assert.IsTrue(hasCommand);
            Assert.AreEqual(PetCommandType.WorkCompleted, replayed.Request.CommandType);
            Assert.AreEqual("trace_deferred", replayed.TraceId);
        }

        [Test]
        public void ChatChunkEvents_SameTrace_AreNotDroppedByDedup()
        {
            FakeGatewayClient client = new();
            PetCommandLinkService commandService = new();
            EventBus eventBus = new();
            int chunkCount = 0;
            using IDisposable _ = eventBus.Subscribe<GatewayChatChunkEvent>(_ => chunkCount++);
            using GatewayEventRouter router = new(client, commandService, eventBus);

            client.Emit(new GatewayEventEnvelope
            {
                TraceId = "trace_chat",
                EventType = GatewayEventType.ChatChunk,
                Message = "chunk_1"
            });
            client.Emit(new GatewayEventEnvelope
            {
                TraceId = "trace_chat",
                EventType = GatewayEventType.ChatChunk,
                Message = "chunk_2"
            });

            router.ProcessPendingEvents();
            Assert.AreEqual(2, chunkCount);
        }

        private sealed class FakeGatewayClient : IGatewayClient
        {
            private readonly HashSet<string> _acks = new(StringComparer.Ordinal);

            public event Action<GatewayEventEnvelope>? EventReceived;
            public GatewayEnvironment CurrentEnvironment => GatewayEnvironment.Mock;
            public bool IsOnline => true;
            public GatewayRetryPolicy RetryPolicy => new(1, 1, 1, 1f);
            public GatewayMetrics Metrics { get; } = new();

            public IReadOnlyCollection<string> GetAckedTraceIds() => _acks;

            public void MarkAcked(string traceId)
            {
                _acks.Add(traceId);
            }

            public Task<int> ReplayPendingAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(0);
            }

            public Task<GatewaySendResult> SendAsync(GatewayRequest request, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new GatewaySendResult(true, GatewayChannelType.Http, new GatewayResponse
                {
                    TraceId = request.TraceId,
                    Success = true
                }, GatewayErrorKind.None, string.Empty));
            }

            public void Emit(GatewayEventEnvelope envelope)
            {
                EventReceived?.Invoke(envelope);
            }
        }
    }
}
