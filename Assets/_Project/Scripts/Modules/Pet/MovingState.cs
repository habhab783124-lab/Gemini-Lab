#nullable enable
using GeminiLab.Core.FSM;
using GeminiLab.Modules.Furniture;
using GeminiLab.Modules.Navigation;
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Moves pet to the selected furniture interaction point.
    /// </summary>
    public sealed class MovingState : IState<PetContext>
    {
        public const string StateName = "Moving";

        public string Name => StateName;

        public void Enter(PetContext context)
        {
            context.EnterState(StateName);
            context.RuntimeData.TargetReached = false;
            context.RuntimeData.IsAtRequiredWorkTarget = false;
            AcquireTargetAndPath(context);
        }

        public void Tick(PetContext context, float deltaTime)
        {
            context.Advance(deltaTime);
            if (context.RuntimeData.TargetReached)
            {
                return;
            }

            if (context.RuntimeData.ActivePath.Count == 0)
            {
                context.RuntimeData.TargetReached = true;
                return;
            }

            int index = Mathf.Clamp(context.RuntimeData.PathIndex, 0, context.RuntimeData.ActivePath.Count - 1);
            Vector2 current = context.RuntimeData.Position;
            Vector2 waypoint = context.RuntimeData.ActivePath[index];
            Vector2 next = Vector2.MoveTowards(current, waypoint, context.MoveSpeed * deltaTime);
            context.RuntimeData.Position = next;

            if (Vector2.Distance(next, waypoint) <= 0.01f)
            {
                context.RuntimeData.PathIndex++;
                if (context.RuntimeData.PathIndex >= context.RuntimeData.ActivePath.Count)
                {
                    context.RuntimeData.TargetReached = true;
                }
            }
        }

        public void FixedTick(PetContext context, float fixedDeltaTime)
        {
        }

        public void Exit(PetContext context)
        {
        }

        private static void AcquireTargetAndPath(PetContext context)
        {
            FurnitureInteractionQuery query = context.RuntimeData.WorkRequested && context.RuntimeData.RequiredWorkTargetType == PetWorkTargetType.WorkDesk
                ? FurnitureInteractionQuery.WorkDeskOnly
                : FurnitureInteractionQuery.Any;
            if (context.FurnitureService is null || !context.FurnitureService.TryGetBestInteractionTarget(context.RuntimeData.Position, query, out FurnitureInteractionTarget target))
            {
                context.RuntimeData.TargetReached = true;
                if (context.RuntimeData.WorkRequested)
                {
                    context.RuntimeData.WorkRequested = false;
                    if (!string.IsNullOrWhiteSpace(context.RuntimeData.ActiveWorkTraceId))
                    {
                        context.EventBus?.Publish(new PetWorkFailedEvent(context.RuntimeData.ActiveWorkTraceId, "No reachable work target."));
                    }

                    context.RuntimeData.ActiveWorkTraceId = string.Empty;
                    context.RuntimeData.ActiveWorkMessage = string.Empty;
                    context.RuntimeData.RequiredWorkTargetType = PetWorkTargetType.Any;
                }

                return;
            }

            context.RuntimeData.TargetFurnitureId = target.FurnitureId;
            context.RuntimeData.TargetPosition = target.InteractionPoint;
            context.RuntimeData.IsAtRequiredWorkTarget =
                !context.RuntimeData.WorkRequested ||
                context.RuntimeData.RequiredWorkTargetType == PetWorkTargetType.Any ||
                target.Category == FurnitureCategory.WorkDesk;

            if (context.NavigationService is null ||
                !context.NavigationService.TryRequestPath(context.RuntimeData.Position, target.InteractionPoint, out NavigationPath path))
            {
                context.RuntimeData.ActivePath.Clear();
                context.RuntimeData.ActivePath.Add(target.InteractionPoint);
            }
            else
            {
                context.RuntimeData.ActivePath.Clear();
                for (int i = 0; i < path.Points.Count; i++)
                {
                    context.RuntimeData.ActivePath.Add(path.Points[i]);
                }
            }

            context.RuntimeData.PathIndex = 1;
            if (context.RuntimeData.ActivePath.Count <= 1)
            {
                context.RuntimeData.PathIndex = 0;
            }
        }
    }
}
