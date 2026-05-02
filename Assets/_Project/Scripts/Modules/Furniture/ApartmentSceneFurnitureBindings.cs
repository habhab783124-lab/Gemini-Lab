#nullable enable
using System;
using UnityEngine;

namespace GeminiLab.Modules.Furniture
{
    [DefaultExecutionOrder(-500)]
    public sealed class ApartmentSceneFurnitureBindings : MonoBehaviour
    {
        [SerializeField] private BindingEntry[] _bindings = Array.Empty<BindingEntry>();

        private void Awake()
        {
            ApplyBindings();
        }

        [ContextMenu("Apply Bindings")]
        public void ApplyBindings()
        {
            for (int i = 0; i < _bindings.Length; i++)
            {
                BindingEntry entry = _bindings[i];
                if (entry.Target is null)
                {
                    continue;
                }

                if (!entry.Target.TryGetComponent(out SpriteRenderer _))
                {
                    Debug.LogWarning($"[ApartmentSceneFurnitureBindings] Skip '{entry.Target.name}' because it has no SpriteRenderer.", entry.Target);
                    continue;
                }

                Furniture furniture = entry.Target.GetComponent<Furniture>() ?? entry.Target.AddComponent<Furniture>();
                InteractionAnchor anchor = entry.Target.GetComponent<InteractionAnchor>() ?? entry.Target.AddComponent<InteractionAnchor>();
                SceneFurnitureDefinitionHint hint = entry.Target.GetComponent<SceneFurnitureDefinitionHint>() ?? entry.Target.AddComponent<SceneFurnitureDefinitionHint>();

                hint.Configure(
                    entry.DefinitionId,
                    entry.Category,
                    entry.InteractionType,
                    entry.InteractionDurationSeconds,
                    entry.PlacementType,
                    entry.OccupiedCells,
                    entry.Buff,
                    entry.IncludeInBuildPalette);

                anchor.SetAvailable(entry.IsAvailable);

                if (furniture.isActiveAndEnabled)
                {
                    furniture.Initialize(furniture.InstanceId, furniture.Definition);
                }
            }
        }

        [Serializable]
        private sealed class BindingEntry
        {
            [SerializeField] private GameObject? _target;
            [SerializeField] private bool _isAvailable = true;
            [SerializeField] private bool _includeInBuildPalette;
            [SerializeField] private string _definitionId = string.Empty;
            [SerializeField] private FurnitureCategory _category = FurnitureCategory.Unknown;
            [SerializeField] private FurnitureInteractionType _interactionType = FurnitureInteractionType.Unknown;
            [SerializeField] private float _interactionDurationSeconds = -1f;
            [SerializeField] private FurniturePlacementType _placementType = FurniturePlacementType.Floor;
            [SerializeField] private Vector2Int _occupiedCells = Vector2Int.one;
            [SerializeField] private EnvironmentalBuff _buff;

            public GameObject? Target => _target;
            public bool IsAvailable => _isAvailable;
            public bool IncludeInBuildPalette => _includeInBuildPalette;
            public string DefinitionId => _definitionId;
            public FurnitureCategory Category => _category;
            public FurnitureInteractionType InteractionType => _interactionType;
            public float InteractionDurationSeconds => _interactionDurationSeconds;
            public FurniturePlacementType PlacementType => _placementType;
            public Vector2Int OccupiedCells => _occupiedCells;
            public EnvironmentalBuff Buff => _buff;
        }
    }
}
