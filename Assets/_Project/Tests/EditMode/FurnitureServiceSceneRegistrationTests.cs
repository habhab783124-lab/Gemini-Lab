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
            Assert.AreEqual(FurnitureInteractionType.SleepInBed, target.InteractionType);
            Assert.AreEqual(furniture.InstanceId, target.FurnitureId);

            Object.DestroyImmediate(serviceHost);
            Object.DestroyImmediate(furnitureGo);
            Object.DestroyImmediate(sprite.texture);
            Object.DestroyImmediate(sprite);
        }

        [Test]
        public void Awake_InfersObjectLevelInteractionType_FromChineseSpriteName()
        {
            Sprite sprite = CreateNamedSprite("家具_休闲_吉他_恶魔_01");
            GameObject furnitureGo = new("家具_休闲_吉他_恶魔_01");
            furnitureGo.AddComponent<InteractionAnchor>();
            furnitureGo.AddComponent<BoxCollider2D>();
            SpriteRenderer renderer = furnitureGo.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            _ = furnitureGo.AddComponent<Furniture>();

            GameObject serviceHost = new("FurnitureServiceHost");
            FurnitureService service = serviceHost.AddComponent<FurnitureService>();

            bool found = service.TryGetBestInteractionTarget(Vector2.zero, FurnitureInteractionQuery.Any, out FurnitureInteractionTarget target);

            Assert.IsTrue(found);
            Assert.AreEqual(FurnitureCategory.Leisure, target.Category);
            Assert.AreEqual(FurnitureInteractionType.PlayGuitar, target.InteractionType);
            Assert.Greater(target.InteractionDurationSeconds, 2f);

            Object.DestroyImmediate(serviceHost);
            Object.DestroyImmediate(furnitureGo);
            Object.DestroyImmediate(sprite.texture);
            Object.DestroyImmediate(sprite);
        }

        [TestCase("家具_装饰_镜子_恶魔_01", FurnitureCategory.Decoration, FurnitureInteractionType.InspectMirror)]
        [TestCase("家具_装饰_小圆镜_天使_01", FurnitureCategory.Decoration, FurnitureInteractionType.InspectMirror)]
        [TestCase("家具_装饰_园地毯_天使_01", FurnitureCategory.Decoration, FurnitureInteractionType.RestOnRug)]
        [TestCase("家具_休闲_画架_恶魔_01", FurnitureCategory.Leisure, FurnitureInteractionType.PaintAtEasel)]
        [TestCase("家具_休闲_照片板_恶魔_01", FurnitureCategory.Leisure, FurnitureInteractionType.ViewPhotoBoard)]
        [TestCase("家具_工作桌_花盆方桌_天使_01", FurnitureCategory.WorkDesk, FurnitureInteractionType.WorkFocus)]
        [TestCase("家具_装饰_窗台_天使_01", FurnitureCategory.Decoration, FurnitureInteractionType.ObserveWindow)]
        [TestCase("家具_装饰_床上的玩偶_恶魔_01", FurnitureCategory.Decoration, FurnitureInteractionType.InspectToy)]
        [TestCase("家具_装饰_沙发上枕头_恶魔_01", FurnitureCategory.Decoration, FurnitureInteractionType.ArrangePillow)]
        [TestCase("家具_装饰_左下小家具_恶魔_01", FurnitureCategory.Decoration, FurnitureInteractionType.OrganizeStorage)]
        [TestCase("家具_装饰_左下窄家具_恶魔_01", FurnitureCategory.Decoration, FurnitureInteractionType.OrganizeStorage)]
        public void Awake_InfersSpecificInteractionType_FromChineseSpriteName(string spriteName, FurnitureCategory expectedCategory, FurnitureInteractionType expectedInteractionType)
        {
            Sprite sprite = CreateNamedSprite(spriteName);
            GameObject furnitureGo = new(spriteName);
            furnitureGo.AddComponent<InteractionAnchor>();
            furnitureGo.AddComponent<BoxCollider2D>();
            SpriteRenderer renderer = furnitureGo.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            _ = furnitureGo.AddComponent<Furniture>();

            GameObject serviceHost = new("FurnitureServiceHost");
            FurnitureService service = serviceHost.AddComponent<FurnitureService>();

            bool found = service.TryGetBestInteractionTarget(Vector2.zero, FurnitureInteractionQuery.Any, out FurnitureInteractionTarget target);

            Assert.IsTrue(found);
            Assert.AreEqual(expectedCategory, target.Category);
            Assert.AreEqual(expectedInteractionType, target.InteractionType);

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
