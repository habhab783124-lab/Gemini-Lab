#nullable enable
using GeminiLab.Modules.WorldMap;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    public sealed class WorldMapFlowerPlacementRegionTests
    {
        [Test]
        public void Region_NormalizesAndMatchesOwner()
        {
            GameObject gameObject = new("PlacementRegionTest");
            try
            {
                WorldMapFlowerPlacementRegion region = gameObject.AddComponent<WorldMapFlowerPlacementRegion>();
                SetOwner(region, "devil");

                Assert.That(region.Owner, Is.EqualTo("demon"));
                Assert.That(region.MatchesOwner("demon"), Is.True);
                Assert.That(region.MatchesOwner("angel"), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Region_ContainsWholePlacementRectOnlyInsideBounds()
        {
            GameObject gameObject = new("PlacementRegionTest");
            try
            {
                WorldMapFlowerPlacementRegion region = gameObject.AddComponent<WorldMapFlowerPlacementRegion>();
                BoxCollider2D collider = gameObject.GetComponent<BoxCollider2D>()!;
                gameObject.transform.position = new Vector3(5f, 2f, 0f);
                collider.size = new Vector2(10f, 6f);

                Assert.That(region.Contains(new Rect(1f, -1f, 8f, 4f)), Is.True);
                Assert.That(region.Contains(new Rect(1f, -1f, 10f, 6f)), Is.True);
                Assert.That(region.Contains(new Rect(0.9f, -1f, 8f, 4f)), Is.False);
                Assert.That(region.Contains(new Rect(1f, -1f, 10.1f, 6f)), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Controller_EntersAuthoredRegionMode_WhenAnyRegionBindingExists()
        {
            GameObject controllerObject = new("PlacementControllerTest");
            controllerObject.SetActive(false);
            try
            {
                WorldMapFlowerPlacementController controller =
                    controllerObject.AddComponent<WorldMapFlowerPlacementController>();
                Assert.That(controller.UsesAuthoredPlacementRegions, Is.False);

                GameObject regionObject = new("PlacementRegionTest");
                regionObject.transform.SetParent(controllerObject.transform);
                WorldMapFlowerPlacementRegion region =
                    regionObject.AddComponent<WorldMapFlowerPlacementRegion>();

                var serialized = new SerializedObject(controller);
                var regions = serialized.FindProperty("_placementRegions")!;
                regions.arraySize = 1;
                regions.GetArrayElementAtIndex(0)!.objectReferenceValue = region;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(controller.UsesAuthoredPlacementRegions, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
            }
        }

        private static void SetOwner(WorldMapFlowerPlacementRegion region, string owner)
        {
            var serialized = new SerializedObject(region);
            serialized.FindProperty("_owner")!.stringValue = owner;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
