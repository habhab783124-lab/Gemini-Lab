#nullable enable
using GeminiLab.Modules.Furniture;
using NUnit.Framework;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    public sealed class FurnitureServiceSceneRegistrationTests
    {
        [Test]
        public void Awake_RegistersExistingSceneFurniture_FromSpriteName()
        {
            Sprite sprite = CreateNamedSprite("Furniture_Bed_Angel_001");
            GameObject furnitureGo = new("Bed_Angel");
            furnitureGo.AddComponent<InteractionAnchor>();
            furnitureGo.AddComponent<BoxCollider2D>();
            SpriteRenderer renderer = furnitureGo.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            Furniture furniture = furnitureGo.AddComponent<Furniture>();

            GameObject serviceHost = new("FurnitureServiceHost");
            FurnitureService service = serviceHost.AddComponent<FurnitureService>();

            bool found = service.TryGetBestInteractionTarget(Vector2.zero, FurnitureInteractionQuery.BedOnly, out FurnitureInteractionTarget target);

            Assert.IsTrue(found);
            Assert.AreEqual(FurnitureCategory.Bed, target.Category);
            Assert.AreEqual(furniture.InstanceId, target.FurnitureId);

            Object.DestroyImmediate(serviceHost);
            Object.DestroyImmediate(furnitureGo);
            Object.DestroyImmediate(sprite.texture);
            Object.DestroyImmediate(sprite);
        }

        private static Sprite CreateNamedSprite(string name)
        {
            Texture2D texture = new(8, 8);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 16f);
            sprite.name = name;
            return sprite;
        }
    }
}
