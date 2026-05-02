#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Modules.Navigation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GeminiLab.Modules.Furniture
{
    /// <summary>
    /// Runtime furniture service for placement and interaction querying.
    /// </summary>
    public sealed class FurnitureService : MonoBehaviour, IFurnitureService
    {
        [SerializeField] private LayerMask _obstacleMask;
        [SerializeField] private FurnitureDefinitionSO[] _defaultDefinitions = Array.Empty<FurnitureDefinitionSO>();

        private readonly List<Furniture> _placedFurniture = new();
        private readonly Dictionary<string, FurnitureDefinitionSO> _definitions = new(StringComparer.Ordinal);
        private readonly List<FurnitureDefinitionSO> _buildPalette = new();
        private readonly Grid2DSnapper _snapper = new();
        private readonly PlacementValidator2D _validator = new();
        private readonly HabitTagService _habitService = new();

        private INavigationService? _navigationService;
        private EventBus? _eventBus;

        public IReadOnlyList<Furniture> GetPlacedFurniture() => _placedFurniture;

        public IReadOnlyList<FurnitureDefinitionSO> GetBuildPalette() => _buildPalette;

        public bool HasPlacedFurniture => _placedFurniture.Count > 0;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Awake()
        {
            for (int i = 0; i < _defaultDefinitions.Length; i++)
            {
                FurnitureDefinitionSO definition = _defaultDefinitions[i];
                if (definition is not null && !string.IsNullOrWhiteSpace(definition.Id))
                {
                    _definitions[definition.Id] = definition;
                    _buildPalette.Add(definition);
                }
            }

            EnsureFallbackDefinitions();

            if (ServiceLocator.TryResolve(out INavigationService? navigationService))
            {
                _navigationService = navigationService;
            }

            if (ServiceLocator.TryResolve(out EventBus? eventBus))
            {
                _eventBus = eventBus;
            }

            RegisterSceneFurniture();
        }

        public bool TryPlaceFurniture(FurnitureDefinitionSO definition, Vector2 position, float rotationZ, out Furniture? furniture, out string failureReason)
        {
            string instanceId = Guid.NewGuid().ToString("N");
            return TryPlaceFurnitureWithId(definition, position, rotationZ, instanceId, out furniture, out failureReason);
        }

        public bool TryRemoveNearestFurniture(Vector2 position, float maxDistance, out string removedFurnitureId)
        {
            EnsureDependencies();
            RemoveDestroyedFurnitureEntries();
            removedFurnitureId = string.Empty;
            int nearestIndex = -1;
            float nearestDistance = maxDistance;

            for (int i = 0; i < _placedFurniture.Count; i++)
            {
                float distance = Vector2.Distance(position, _placedFurniture[i].transform.position);
                if (distance <= nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = i;
                }
            }

            if (nearestIndex < 0)
            {
                return false;
            }

            Furniture target = _placedFurniture[nearestIndex];
            removedFurnitureId = target.InstanceId;
            _placedFurniture.RemoveAt(nearestIndex);
            Destroy(target.gameObject);
            _eventBus?.Publish(new FurnitureRemovedEvent(removedFurnitureId));
            _ = _navigationService?.RebuildAsync("Furniture removed");
            return true;
        }

        public bool TryGetBestInteractionTarget(Vector2 origin, out FurnitureInteractionTarget target)
        {
            return TryGetBestInteractionTarget(origin, FurnitureInteractionQuery.Any, out target);
        }

        public bool TryGetBestInteractionTarget(Vector2 origin, FurnitureInteractionQuery query, out FurnitureInteractionTarget target)
        {
            RemoveDestroyedFurnitureEntries();
            target = default;
            if (_placedFurniture.Count == 0)
            {
                return false;
            }

            float bestScore = float.MinValue;
            Furniture? bestFurniture = null;
            for (int i = 0; i < _placedFurniture.Count; i++)
            {
                Furniture furniture = _placedFurniture[i];
                if (!furniture.Anchor.IsAvailable)
                {
                    continue;
                }

                FurnitureCategory category = ResolveCategory(furniture.Definition);
                if (query.HasRequiredCategory && category != query.RequiredCategory)
                {
                    continue;
                }

                Vector2 point = furniture.Anchor.WorldPosition;
                float distance = Vector2.Distance(origin, point);
                float preference = _habitService.GetPreferenceScore(furniture.InstanceId);
                float score = preference + furniture.Definition.Buff.EnergyDelta + furniture.Definition.Buff.MoodDelta - distance * 0.2f;
                if (category == FurnitureCategory.WorkDesk)
                {
                    score += 1f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestFurniture = furniture;
                }
            }

            if (bestFurniture is null)
            {
                return false;
            }

            Vector2 interactionPoint = bestFurniture.Anchor.WorldPosition;
            FurnitureCategory bestCategory = ResolveCategory(bestFurniture.Definition);
            target = new FurnitureInteractionTarget(
                bestFurniture.InstanceId,
                bestFurniture.Definition.Id,
                bestCategory,
                bestFurniture.Definition.InteractionType,
                bestFurniture.Definition.InteractionDurationSeconds,
                interactionPoint,
                bestScore);
            return true;
        }

        public bool TryConsumeInteractionBuff(string furnitureId, out EnvironmentalBuff buff)
        {
            EnsureDependencies();
            for (int i = 0; i < _placedFurniture.Count; i++)
            {
                Furniture furniture = _placedFurniture[i];
                if (furniture.InstanceId != furnitureId)
                {
                    continue;
                }

                buff = furniture.Definition.Buff;
                _habitService.RecordInteraction(furnitureId);
                _eventBus?.Publish(new FurnitureInteractionEvent(furnitureId, buff));
                return true;
            }

            buff = default;
            return false;
        }

        public FurnitureLayoutSnapshot CaptureLayout()
        {
            RemoveDestroyedFurnitureEntries();
            FurnitureLayoutEntry[] entries = new FurnitureLayoutEntry[_placedFurniture.Count];
            for (int i = 0; i < _placedFurniture.Count; i++)
            {
                Furniture furniture = _placedFurniture[i];
                entries[i] = new FurnitureLayoutEntry
                {
                    FurnitureId = furniture.InstanceId,
                    DefinitionId = furniture.Definition.Id,
                    Position = furniture.transform.position,
                    RotationZ = furniture.transform.eulerAngles.z
                };
            }

            return new FurnitureLayoutSnapshot
            {
                SchemaVersion = 1,
                Entries = entries
            };
        }

        public void RestoreLayout(FurnitureLayoutSnapshot snapshot)
        {
            for (int i = _placedFurniture.Count - 1; i >= 0; i--)
            {
                Destroy(_placedFurniture[i].gameObject);
            }
            _placedFurniture.Clear();

            for (int i = 0; i < snapshot.Entries.Length; i++)
            {
                FurnitureLayoutEntry entry = snapshot.Entries[i];
                if (!_definitions.TryGetValue(entry.DefinitionId, out FurnitureDefinitionSO? definition))
                {
                    continue;
                }

                _ = TryPlaceFurnitureWithId(definition, entry.Position, entry.RotationZ, entry.FurnitureId, out Furniture? _, out string _);
            }
        }

        private void EnsureFallbackDefinitions()
        {
            if (_buildPalette.Count >= 5)
            {
                return;
            }

            AddFallbackDefinition("Furniture_Bed_Angel_001", FurnitureCategory.Bed, FurniturePlacementType.Floor, new Vector2Int(2, 1), moodDelta: 2f, energyDelta: 6f);
            AddFallbackDefinition("Furniture_Nightstand_Angel_001", FurnitureCategory.Decoration, FurniturePlacementType.Floor, new Vector2Int(1, 1), moodDelta: 1f, energyDelta: 1f);
            AddFallbackDefinition("Furniture_Harp_Angel_001", FurnitureCategory.Leisure, FurniturePlacementType.Floor, new Vector2Int(1, 1), moodDelta: 5f, energyDelta: 0f);
            AddFallbackDefinition("Furniture_WorkDesk_Angel_01", FurnitureCategory.WorkDesk, FurniturePlacementType.Floor, new Vector2Int(2, 1), moodDelta: 1f, energyDelta: 2f);
            AddFallbackDefinition("Furniture_WorkDesk_Devil_01", FurnitureCategory.WorkDesk, FurniturePlacementType.Wall, new Vector2Int(1, 1), moodDelta: 3f, energyDelta: -1f);
        }

        private void AddFallbackDefinition(string id, FurnitureCategory category, FurniturePlacementType placementType, Vector2Int occupiedCells, float moodDelta, float energyDelta)
        {
            if (_definitions.ContainsKey(id))
            {
                return;
            }

            FurnitureDefinitionSO definition = ScriptableObject.CreateInstance<FurnitureDefinitionSO>();
            definition.ConfigureRuntime(
                id,
                category,
                placementType,
                occupiedCells,
                new EnvironmentalBuff
                {
                    MoodDelta = moodDelta,
                    EnergyDelta = energyDelta
                },
                sprite: null);
            _definitions[id] = definition;
            _buildPalette.Add(definition);
        }

        private void EnsureDependencies()
        {
            if (_navigationService is null && ServiceLocator.TryResolve(out INavigationService? navigationService))
            {
                _navigationService = navigationService;
            }

            if (_eventBus is null && ServiceLocator.TryResolve(out EventBus? eventBus))
            {
                _eventBus = eventBus;
            }
        }

        private bool TryPlaceFurnitureWithId(
            FurnitureDefinitionSO definition,
            Vector2 position,
            float rotationZ,
            string instanceId,
            out Furniture? furniture,
            out string failureReason)
        {
            EnsureDependencies();
            Vector2 snappedPosition = _snapper.Snap(position);
            if (!_validator.IsPlacementValid(definition, snappedPosition, _obstacleMask, out failureReason))
            {
                furniture = null;
                return false;
            }

            GameObject go = new($"Furniture_{definition.Id}");
            go.transform.position = new Vector3(snappedPosition.x, snappedPosition.y, 0f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = definition.Sprite;
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(
                Mathf.Max(0.5f, definition.OccupiedCells.x),
                Mathf.Max(0.5f, definition.OccupiedCells.y));

            Furniture runtimeFurniture = CreateFurnitureRuntime(go, definition, instanceId);
            ApplyFurniturePresentation(runtimeFurniture, renderer, definition);
            _placedFurniture.Add(runtimeFurniture);
            _definitions[definition.Id] = definition;

            _eventBus?.Publish(new FurniturePlacedEvent(instanceId, definition.Id, snappedPosition));
            _ = _navigationService?.RebuildAsync("Furniture placed");
            furniture = runtimeFurniture;
            return true;
        }

        private static Furniture CreateFurnitureRuntime(GameObject go, FurnitureDefinitionSO definition, string instanceId)
        {
            Furniture runtimeFurniture = go.AddComponent<Furniture>();
            runtimeFurniture.Initialize(instanceId, definition);
            return runtimeFurniture;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _ = scene;
            _ = mode;
            RegisterSceneFurniture();
        }

        private void RegisterSceneFurniture()
        {
            RemoveDestroyedFurnitureEntries();

            Furniture[] sceneFurniture = FindObjectsByType<Furniture>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < sceneFurniture.Length; i++)
            {
                Furniture furniture = sceneFurniture[i];
                if (furniture is null || _placedFurniture.Contains(furniture))
                {
                    continue;
                }

                FurnitureDefinitionSO resolvedDefinition = ResolveSceneFurnitureDefinition(furniture);
                furniture.Initialize(furniture.InstanceId, resolvedDefinition);

                if (furniture.TryGetComponent(out SpriteRenderer renderer) && renderer is not null)
                {
                    if (resolvedDefinition.Sprite is null)
                    {
                        resolvedDefinition.ConfigureRuntime(
                            resolvedDefinition.Id,
                            resolvedDefinition.Category,
                            resolvedDefinition.PlacementType,
                            resolvedDefinition.OccupiedCells,
                            resolvedDefinition.Buff,
                            renderer.sprite,
                            resolvedDefinition.InteractionType,
                            resolvedDefinition.InteractionDurationSeconds);
                    }

                    ApplyFurniturePresentation(furniture, renderer, resolvedDefinition);
                }

                _placedFurniture.Add(furniture);
            }
        }

        private FurnitureDefinitionSO ResolveSceneFurnitureDefinition(Furniture furniture)
        {
            SpriteRenderer? renderer = furniture.GetComponent<SpriteRenderer>();
            string spriteName = renderer?.sprite?.name ?? string.Empty;
            string objectName = furniture.gameObject.name;

            if (furniture.TryGetComponent(out SceneFurnitureDefinitionHint hint) && hint.EnabledHint)
            {
                string hintedDefinitionId = !string.IsNullOrWhiteSpace(hint.DefinitionId)
                    ? hint.DefinitionId
                    : (!string.IsNullOrWhiteSpace(spriteName) ? spriteName : $"Furniture_{objectName}");

                if (_definitions.TryGetValue(hintedDefinitionId, out FurnitureDefinitionSO? hintedExisting))
                {
                    return hintedExisting;
                }

                FurnitureCategory hintedCategory = hint.Category != FurnitureCategory.Unknown
                    ? hint.Category
                    : InferCategory(hintedDefinitionId, objectName);
                FurnitureInteractionType hintedInteractionType = hint.InteractionType != FurnitureInteractionType.Unknown
                    ? hint.InteractionType
                    : InferInteractionType(hintedDefinitionId, hintedCategory);
                float hintedInteractionDuration = hint.InteractionDurationSeconds > 0f
                    ? hint.InteractionDurationSeconds
                    : InferInteractionDuration(hintedInteractionType);

                Vector2Int hintedOccupiedCells = hint.OccupiedCells.x > 0 && hint.OccupiedCells.y > 0
                    ? hint.OccupiedCells
                    : InferOccupiedCells(hintedCategory);

                FurnitureDefinitionSO hintedDefinition = ScriptableObject.CreateInstance<FurnitureDefinitionSO>();
                hintedDefinition.ConfigureRuntime(
                    hintedDefinitionId,
                    hintedCategory,
                    hint.PlacementType,
                    hintedOccupiedCells,
                    hint.Buff,
                    renderer?.sprite,
                    hintedInteractionType,
                    hintedInteractionDuration);

                _definitions[hintedDefinitionId] = hintedDefinition;
                if (hint.IncludeInBuildPalette)
                {
                    _buildPalette.Add(hintedDefinition);
                }

                return hintedDefinition;
            }

            if (!string.IsNullOrWhiteSpace(spriteName) && _definitions.TryGetValue(spriteName, out FurnitureDefinitionSO? bySprite))
            {
                return bySprite;
            }

            string definitionId = !string.IsNullOrWhiteSpace(spriteName) ? spriteName : $"Furniture_{objectName}";
            FurnitureCategory category = InferCategory(definitionId, objectName);
            FurnitureInteractionType interactionType = InferInteractionType(definitionId, category);
            float interactionDuration = InferInteractionDuration(interactionType);
            FurniturePlacementType placementType = InferPlacementType(definitionId, objectName);
            Vector2Int occupiedCells = InferOccupiedCells(category);
            EnvironmentalBuff buff = InferBuff(definitionId, category);

            FurnitureDefinitionSO definition = ScriptableObject.CreateInstance<FurnitureDefinitionSO>();
            definition.ConfigureRuntime(definitionId, category, placementType, occupiedCells, buff, renderer?.sprite, interactionType, interactionDuration);
            _definitions[definitionId] = definition;
            _buildPalette.Add(definition);
            return definition;
        }

        private static void ApplyFurniturePresentation(Furniture furniture, SpriteRenderer renderer, FurnitureDefinitionSO definition)
        {
            SortingGroup sortingGroup = furniture.gameObject.GetComponent<SortingGroup>() ?? furniture.gameObject.AddComponent<SortingGroup>();
            renderer.sortingLayerName = "Furniture";
            renderer.sortingOrder = CalculateSortingOrder(furniture.transform.position.y, definition.PlacementType);
            sortingGroup.sortingLayerName = renderer.sortingLayerName;
            sortingGroup.sortingOrder = renderer.sortingOrder;
        }

        private void RemoveDestroyedFurnitureEntries()
        {
            for (int i = _placedFurniture.Count - 1; i >= 0; i--)
            {
                if (_placedFurniture[i] is null)
                {
                    _placedFurniture.RemoveAt(i);
                }
            }
        }

        private static FurnitureCategory InferCategory(string definitionId, string objectName)
        {
            string hint = $"{definitionId} {objectName}";
            if (ContainsAnyKeyword(hint, "WorkDesk", "工作桌", "工作台", "书桌"))
            {
                return FurnitureCategory.WorkDesk;
            }

            if (ContainsAnyKeyword(hint, "Bed", "床"))
            {
                return FurnitureCategory.Bed;
            }

            if (ContainsAnyKeyword(hint, "Leisure", "Harp", "休闲", "娱乐", "竖琴"))
            {
                return FurnitureCategory.Leisure;
            }

            if (ContainsAnyKeyword(hint, "Decoration", "Nightstand", "装饰", "摆件", "植物", "镜子", "床头柜", "书柜", "高柜"))
            {
                return FurnitureCategory.Decoration;
            }

            return FurnitureCategory.Decoration;
        }

        private static FurniturePlacementType InferPlacementType(string definitionId, string objectName)
        {
            string hint = $"{definitionId} {objectName}";
            return ContainsAnyKeyword(hint, "Wall", "Devil", "墙", "壁")
                ? FurniturePlacementType.Wall
                : FurniturePlacementType.Floor;
        }

        private static FurnitureInteractionType InferInteractionType(string definitionId, FurnitureCategory category)
        {
            if (ContainsAnyKeyword(definitionId, "床"))
            {
                return FurnitureInteractionType.SleepInBed;
            }

            if (ContainsAnyKeyword(definitionId, "工作桌", "工作台", "书桌", "公告板"))
            {
                return FurnitureInteractionType.WorkFocus;
            }

            if (ContainsAnyKeyword(definitionId, "高书柜", "书柜"))
            {
                return FurnitureInteractionType.InspectBookshelf;
            }

            if (ContainsAnyKeyword(definitionId, "镜子", "小圆镜"))
            {
                return FurnitureInteractionType.InspectMirror;
            }

            if (ContainsAnyKeyword(definitionId, "床头柜"))
            {
                return FurnitureInteractionType.InspectNightstand;
            }

            if (ContainsAnyKeyword(definitionId, "竖琴"))
            {
                return FurnitureInteractionType.PlayHarp;
            }

            if (ContainsAnyKeyword(definitionId, "吉他"))
            {
                return FurnitureInteractionType.PlayGuitar;
            }

            if (ContainsAnyKeyword(definitionId, "画架"))
            {
                return FurnitureInteractionType.PaintAtEasel;
            }

            if (ContainsAnyKeyword(definitionId, "照片板"))
            {
                return FurnitureInteractionType.ViewPhotoBoard;
            }

            if (ContainsAnyKeyword(definitionId, "盆栽", "窗台上的盆栽"))
            {
                return FurnitureInteractionType.ObservePlant;
            }

            if (ContainsAnyKeyword(definitionId, "窗台"))
            {
                return FurnitureInteractionType.ObserveWindow;
            }

            if (ContainsAnyKeyword(definitionId, "玩偶"))
            {
                return FurnitureInteractionType.InspectToy;
            }

            if (ContainsAnyKeyword(definitionId, "枕头"))
            {
                return FurnitureInteractionType.ArrangePillow;
            }

            if (ContainsAnyKeyword(definitionId, "纸张"))
            {
                return FurnitureInteractionType.InspectPapers;
            }

            if (ContainsAnyKeyword(definitionId, "音响", "耳机", "乐器"))
            {
                return FurnitureInteractionType.ListenToAudio;
            }

            if (ContainsAnyKeyword(definitionId, "左下小家具", "左下窄家具", "柜子", "储物", "边柜"))
            {
                return FurnitureInteractionType.OrganizeStorage;
            }

            if (ContainsAnyKeyword(definitionId, "地毯", "园地毯"))
            {
                return FurnitureInteractionType.RestOnRug;
            }

            if (ContainsAnyKeyword(definitionId, "凳子", "椅子"))
            {
                return FurnitureInteractionType.SitOnSeat;
            }

            if (ContainsAnyKeyword(definitionId, "沙发"))
            {
                return FurnitureInteractionType.LoungeOnSofa;
            }

            if (ContainsAnyKeyword(definitionId, "休闲"))
            {
                return FurnitureInteractionType.LeisureEngage;
            }

            if (category == FurnitureCategory.Decoration)
            {
                return FurnitureInteractionType.DecorInspect;
            }

            return category switch
            {
                FurnitureCategory.Bed => FurnitureInteractionType.SleepRest,
                FurnitureCategory.WorkDesk => FurnitureInteractionType.WorkFocus,
                FurnitureCategory.Leisure => FurnitureInteractionType.LeisureEngage,
                FurnitureCategory.Decoration => FurnitureInteractionType.DecorInspect,
                _ => FurnitureInteractionType.Unknown
            };
        }

        private static float InferInteractionDuration(FurnitureInteractionType interactionType)
        {
            return interactionType switch
            {
                FurnitureInteractionType.SleepRest => 2.5f,
                FurnitureInteractionType.SleepInBed => 3.2f,
                FurnitureInteractionType.WorkFocus => 1.4f,
                FurnitureInteractionType.DecorInspect => 1.6f,
                FurnitureInteractionType.LeisureEngage => 2.0f,
                FurnitureInteractionType.InspectBookshelf => 2.2f,
                FurnitureInteractionType.InspectMirror => 1.5f,
                FurnitureInteractionType.InspectNightstand => 1.8f,
                FurnitureInteractionType.PlayHarp => 2.4f,
                FurnitureInteractionType.PlayGuitar => 2.3f,
                FurnitureInteractionType.PaintAtEasel => 2.2f,
                FurnitureInteractionType.ViewPhotoBoard => 1.8f,
                FurnitureInteractionType.ObservePlant => 1.5f,
                FurnitureInteractionType.ObserveWindow => 1.8f,
                FurnitureInteractionType.InspectToy => 1.6f,
                FurnitureInteractionType.ArrangePillow => 1.4f,
                FurnitureInteractionType.InspectPapers => 1.4f,
                FurnitureInteractionType.ListenToAudio => 1.9f,
                FurnitureInteractionType.OrganizeStorage => 1.7f,
                FurnitureInteractionType.RestOnRug => 2.1f,
                FurnitureInteractionType.SitOnSeat => 1.6f,
                FurnitureInteractionType.LoungeOnSofa => 2.4f,
                _ => 1.0f
            };
        }

        private static Vector2Int InferOccupiedCells(FurnitureCategory category)
        {
            return category switch
            {
                FurnitureCategory.Bed => new Vector2Int(2, 1),
                FurnitureCategory.WorkDesk => new Vector2Int(2, 1),
                _ => Vector2Int.one
            };
        }

        private static EnvironmentalBuff InferBuff(string definitionId, FurnitureCategory category)
        {
            if (ContainsAnyKeyword(definitionId, "Nightstand", "床头柜"))
            {
                return new EnvironmentalBuff { MoodDelta = 1f, EnergyDelta = 1f };
            }

            if (ContainsAnyKeyword(definitionId, "Harp", "竖琴"))
            {
                return new EnvironmentalBuff { MoodDelta = 5f, EnergyDelta = 0f };
            }

            if (ContainsAnyKeyword(definitionId, "Devil", "恶魔"))
            {
                return new EnvironmentalBuff { MoodDelta = 3f, EnergyDelta = -1f };
            }

            return category switch
            {
                FurnitureCategory.Bed => new EnvironmentalBuff { MoodDelta = 2f, EnergyDelta = 6f },
                FurnitureCategory.WorkDesk => new EnvironmentalBuff { MoodDelta = 1f, EnergyDelta = 2f },
                FurnitureCategory.Leisure => new EnvironmentalBuff { MoodDelta = 4f, EnergyDelta = 0f },
                FurnitureCategory.Decoration => new EnvironmentalBuff { MoodDelta = 1f, EnergyDelta = 0f },
                _ => default
            };
        }

        private static int CalculateSortingOrder(float y, FurniturePlacementType placementType)
        {
            int sortOrder = -(int)(y * 100f);
            if (placementType == FurniturePlacementType.Wall)
            {
                sortOrder += 500;
            }

            return sortOrder;
        }

        private static FurnitureCategory ResolveCategory(FurnitureDefinitionSO definition)
        {
            if (definition.Category != FurnitureCategory.Unknown)
            {
                return definition.Category;
            }

            return InferCategory(definition.Id, definition.Id);
        }

        private static bool ContainsAnyKeyword(string source, params string[] keywords)
        {
            foreach (string keyword in keywords)
            {
                if (source.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
