#nullable enable
using System.Collections.Generic;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Modules.Furniture;
using TMPro;
using UnityEngine;

namespace GeminiLab.Modules.UI
{
    /// <summary>
    /// Exposes build palette data for inventory UI binding.
    /// </summary>
    public sealed class FurnitureInventoryPanelController : MonoBehaviour
    {
        [SerializeField] private TMP_Text? _label;

        private readonly List<string> _items = new();
        private EventBus? _eventBus;
        private System.IDisposable? _placedSub;
        private System.IDisposable? _removedSub;

        public IReadOnlyList<string> Items => _items;

        private void Awake()
        {
            _label ??= GetComponentInChildren<TMP_Text>();
            TryBindEvents();
            RefreshInventory();
        }

        private void Start()
        {
            TryBindEvents();
            RefreshInventory();
        }

        private void OnDestroy()
        {
            _placedSub?.Dispose();
            _removedSub?.Dispose();
        }

        public void RefreshInventory()
        {
            _items.Clear();
            if (!ServiceLocator.TryResolve(out IFurnitureService? furnitureService) || furnitureService is null)
            {
                if (_label is not null)
                {
                    _label.text = "家具库存面板\nFurnitureService unavailable";
                }

                return;
            }

            IReadOnlyList<FurnitureDefinitionSO> palette = furnitureService.GetBuildPalette();
            for (int i = 0; i < palette.Count; i++)
            {
                FurnitureDefinitionSO definition = palette[i];
                _items.Add($"{definition.Category}:{definition.Id}");
            }

            if (_label is not null)
            {
                System.Text.StringBuilder builder = new();
                builder.AppendLine("家具库存面板");
                builder.AppendLine($"Placed: {furnitureService.GetPlacedFurniture().Count}");
                builder.AppendLine("Palette:");
                for (int i = 0; i < _items.Count; i++)
                {
                    builder.AppendLine($"- {_items[i]}");
                }

                builder.Append("Controls: V toggle, LeftClick place, RightClick remove, Tab cycle");
                _label.text = builder.ToString();
            }
        }

        private void TryBindEvents()
        {
            if (_placedSub is not null || _removedSub is not null)
            {
                return;
            }

            if (ServiceLocator.TryResolve(out EventBus? eventBus) && eventBus is not null)
            {
                _eventBus = eventBus;
                _placedSub = _eventBus.Subscribe<FurniturePlacedEvent>(_ => RefreshInventory());
                _removedSub = _eventBus.Subscribe<FurnitureRemovedEvent>(_ => RefreshInventory());
            }
        }
    }
}
