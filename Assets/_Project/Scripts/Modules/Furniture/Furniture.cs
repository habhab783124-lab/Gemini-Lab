#nullable enable
using System;
using UnityEngine;

namespace GeminiLab.Modules.Furniture
{
    /// <summary>
    /// Runtime furniture component.
    /// </summary>
    public sealed class Furniture : MonoBehaviour, IFurniture
    {
        [SerializeField] private string _instanceId = Guid.NewGuid().ToString("N");
        [SerializeField] private FurnitureDefinitionSO? _definition;
        [SerializeField] private InteractionAnchor? _anchor;

        public string InstanceId => _instanceId;

        public FurnitureDefinitionSO Definition => _definition!;

        public InteractionAnchor Anchor => _anchor!;

        private void Awake()
        {
            if (_anchor is null)
            {
                _anchor = gameObject.GetComponent<InteractionAnchor>() ?? gameObject.AddComponent<InteractionAnchor>();
            }

            if (_definition is null)
            {
                FurnitureDefinitionSO fallback = ScriptableObject.CreateInstance<FurnitureDefinitionSO>();
                fallback.ConfigureRuntime(
                    "Furniture.Fallback",
                    FurnitureCategory.Decoration,
                    FurniturePlacementType.Floor,
                    Vector2Int.one,
                    default);
                _definition = fallback;
            }
        }

        public void Initialize(string instanceId, FurnitureDefinitionSO definition)
        {
            _instanceId = instanceId;
            _definition = definition;

            if (_anchor is null)
            {
                _anchor = gameObject.GetComponent<InteractionAnchor>() ?? gameObject.AddComponent<InteractionAnchor>();
            }
        }
    }
}
