#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Modules.Furniture;
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Runtime mutable pet stats used by FSM decisions.
    /// </summary>
    [Serializable]
    public sealed class PetRuntimeData
    {
        [Range(0f, 100f)]
        public float Mood = 60f;

        [Range(0f, 100f)]
        public float Energy = 100f;

        [Range(0f, 100f)]
        public float Satiety = 75f;

        public float TimeInCurrentState;

        public float RuntimeTimeSeconds;

        public string CurrentState = "None";

        public bool WorkRequested;

        public Vector2 Position;

        public Vector2 TargetPosition;

        public string TargetFurnitureId = string.Empty;

        public FurnitureCategory TargetFurnitureCategory = FurnitureCategory.Unknown;

        public bool TargetReached;

        public int PathIndex;

        public List<Vector2> ActivePath = new();

        public float PreventSleepBeforeTime;

        public string LastTraceId = string.Empty;

        public string ActiveWorkTraceId = string.Empty;

        public string ActiveWorkMessage = string.Empty;

        public PetWorkTargetType RequiredWorkTargetType = PetWorkTargetType.Any;

        public bool IsAtRequiredWorkTarget;

        public bool IsTraveling;

        public string ActiveTravelTraceId = string.Empty;

        public string ActiveTravelTopic = string.Empty;

        public float TravelEndAtSeconds;

        public int TravelCompletedCount;

        public string LastInteractionFurnitureId = string.Empty;

        public string LastInteractionSummary = string.Empty;
    }
}
