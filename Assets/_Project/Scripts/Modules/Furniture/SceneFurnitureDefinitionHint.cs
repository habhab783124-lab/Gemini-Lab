#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Furniture
{
    public sealed class SceneFurnitureDefinitionHint : MonoBehaviour
    {
        [SerializeField] private bool _enabledHint = true;
        [SerializeField] private bool _includeInBuildPalette;
        [SerializeField] private string _definitionId = string.Empty;
        [SerializeField] private FurnitureCategory _category = FurnitureCategory.Unknown;
        [SerializeField] private FurnitureInteractionType _interactionType = FurnitureInteractionType.Unknown;
        [SerializeField] private float _interactionDurationSeconds = -1f;
        [SerializeField] private FurniturePlacementType _placementType = FurniturePlacementType.Floor;
        [SerializeField] private Vector2Int _occupiedCells = Vector2Int.one;
        [SerializeField] private EnvironmentalBuff _buff;

        public bool EnabledHint => _enabledHint;

        public bool IncludeInBuildPalette => _includeInBuildPalette;

        public string DefinitionId => _definitionId;

        public FurnitureCategory Category => _category;

        public FurnitureInteractionType InteractionType => _interactionType;

        public float InteractionDurationSeconds => _interactionDurationSeconds;

        public FurniturePlacementType PlacementType => _placementType;

        public Vector2Int OccupiedCells => _occupiedCells;

        public EnvironmentalBuff Buff => _buff;

        public void Configure(
            string definitionId,
            FurnitureCategory category,
            FurnitureInteractionType interactionType,
            float interactionDurationSeconds,
            FurniturePlacementType placementType,
            Vector2Int occupiedCells,
            EnvironmentalBuff buff,
            bool includeInBuildPalette = false,
            bool enabledHint = true)
        {
            _definitionId = definitionId;
            _category = category;
            _interactionType = interactionType;
            _interactionDurationSeconds = interactionDurationSeconds;
            _placementType = placementType;
            _occupiedCells = occupiedCells;
            _buff = buff;
            _includeInBuildPalette = includeInBuildPalette;
            _enabledHint = enabledHint;
        }
    }
}
