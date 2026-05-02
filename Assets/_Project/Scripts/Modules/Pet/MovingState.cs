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
            if (context.FurnitureService is null)
            {
                context.RuntimeData.TargetReached = true;
                context.RuntimeData.TargetFurnitureId = string.Empty;
                context.RuntimeData.TargetFurnitureCategory = FurnitureCategory.Unknown;
                context.RuntimeData.TargetFurnitureInteractionType = FurnitureInteractionType.Unknown;
                context.RuntimeData.TargetInteractionDurationSeconds = 1f;
                context.RuntimeData.ActivePath.Clear();
                context.RuntimeData.PathIndex = 0;
                return;
            }

            bool foundTarget;
            FurnitureInteractionTarget target;
            if (context.RuntimeData.WorkRequested && context.RuntimeData.RequiredWorkTargetType == PetWorkTargetType.WorkDesk)
            {
                foundTarget = context.FurnitureService.TryGetBestInteractionTarget(
                    context.RuntimeData.Position,
                    FurnitureInteractionQuery.WorkDeskOnly,
                    out target);
            }
            else if (ShouldPreferBedTarget(context))
            {
                foundTarget = context.FurnitureService.TryGetBestInteractionTarget(
                    context.RuntimeData.Position,
                    FurnitureInteractionQuery.BedOnly,
                    out target)
                    || TryGetAutonomousAwakeTarget(context, out target);
            }
            else
            {
                foundTarget = TryGetAutonomousAwakeTarget(context, out target)
                    || TryGetAutonomousBedFallback(context, out target);
            }

            if (!foundTarget)
            {
                context.RuntimeData.TargetReached = true;
                context.RuntimeData.TargetFurnitureId = string.Empty;
                context.RuntimeData.TargetFurnitureCategory = FurnitureCategory.Unknown;
                context.RuntimeData.TargetFurnitureInteractionType = FurnitureInteractionType.Unknown;
                context.RuntimeData.TargetInteractionDurationSeconds = 1f;
                context.RuntimeData.ActivePath.Clear();
                context.RuntimeData.PathIndex = 0;
                if (context.RuntimeData.WorkRequested)
                {
                    context.RuntimeData.IsAtRequiredWorkTarget = false;
                }

                return;
            }

            context.RuntimeData.TargetFurnitureId = target.FurnitureId;
            context.RuntimeData.TargetFurnitureCategory = target.Category;
            context.RuntimeData.TargetFurnitureInteractionType = target.InteractionType;
            context.RuntimeData.TargetInteractionDurationSeconds = target.InteractionDurationSeconds;
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

        private static bool TryGetAutonomousAwakeTarget(PetContext context, out FurnitureInteractionTarget target)
        {
            // Autonomous roaming: never pick WorkDesk without an explicit work request.
            bool shouldPreferLeisure = context.RuntimeData.Mood <= context.Config.LeisureSeekMoodThreshold ||
                                       context.RuntimeData.Energy >= context.Config.NighttimeBedSeekEnergyThreshold;

            if (shouldPreferLeisure &&
                context.FurnitureService!.TryGetBestInteractionTarget(
                    context.RuntimeData.Position,
                    new FurnitureInteractionQuery(FurnitureCategory.Leisure, hasRequiredCategory: true),
                    out target))
            {
                return true;
            }

            if (context.FurnitureService!.TryGetBestInteractionTarget(
                context.RuntimeData.Position,
                new FurnitureInteractionQuery(FurnitureCategory.Decoration, hasRequiredCategory: true),
                out target))
            {
                return true;
            }

            if (!shouldPreferLeisure &&
                context.FurnitureService!.TryGetBestInteractionTarget(
                    context.RuntimeData.Position,
                    new FurnitureInteractionQuery(FurnitureCategory.Leisure, hasRequiredCategory: true),
                    out target))
            {
                return true;
            }

            target = default;
            return false;
        }

        private static bool TryGetAutonomousBedFallback(PetContext context, out FurnitureInteractionTarget target)
        {
            if (!context.IsRealWorldNight() && context.RuntimeData.Energy > context.Config.DaytimeBedSeekEnergyThreshold)
            {
                target = default;
                return false;
            }

            return context.FurnitureService!.TryGetBestInteractionTarget(
                context.RuntimeData.Position,
                FurnitureInteractionQuery.BedOnly,
                out target);
        }

        private static bool ShouldPreferBedTarget(PetContext context)
        {
            if (context.RuntimeData.Energy <= context.Config.SleepEnterEnergyThreshold)
            {
                return true;
            }

            float threshold = context.IsRealWorldNight()
                ? context.Config.NighttimeBedSeekEnergyThreshold
                : context.Config.DaytimeBedSeekEnergyThreshold;

            return context.RuntimeData.Energy <= threshold;
        }
    }
}
