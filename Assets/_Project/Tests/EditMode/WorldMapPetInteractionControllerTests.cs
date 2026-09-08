using GeminiLab.Modules.WorldMap;
using NUnit.Framework;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    public sealed class WorldMapPetInteractionControllerTests
    {
        [TestCase(0f, WorldMapPetBaseAnimation.Idle)]
        [TestCase(0.00001f, WorldMapPetBaseAnimation.Idle)]
        [TestCase(0.5f, WorldMapPetBaseAnimation.Move)]
        [TestCase(-0.5f, WorldMapPetBaseAnimation.Move)]
        public void BaseAnimation_IsDeterminedByActualHorizontalMovement(
            float horizontalSpeed,
            WorldMapPetBaseAnimation expected)
        {
            Assert.That(
                WorldMapPetInteractionRules.ResolveBaseAnimation(horizontalSpeed),
                Is.EqualTo(expected));
        }

        [Test]
        public void RoleActions_AreStrictlySeparated()
        {
            Assert.That(WorldMapPetInteractionRules.IsActionAllowed(
                WorldMapPetRole.Angel,
                WorldMapPetSpecialAction.AngelPray), Is.True);
            Assert.That(WorldMapPetInteractionRules.IsActionAllowed(
                WorldMapPetRole.Angel,
                WorldMapPetSpecialAction.DevilCast), Is.False);
            Assert.That(WorldMapPetInteractionRules.IsActionAllowed(
                WorldMapPetRole.Devil,
                WorldMapPetSpecialAction.DevilSleep), Is.True);
            Assert.That(WorldMapPetInteractionRules.IsActionAllowed(
                WorldMapPetRole.Devil,
                WorldMapPetSpecialAction.AngelWater), Is.False);
        }

        [Test]
        public void Proximity_UsesColliderEdgeInsteadOfTreePivot()
        {
            var tree = new GameObject("Tree");
            try
            {
                var collider = tree.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(10f, 10f);
                Vector2 petPosition = new Vector2(5.5f, 0f);

                Assert.That(Vector2.Distance(petPosition, tree.transform.position), Is.GreaterThan(5f));
                Assert.That(
                    WorldMapPetInteractionRules.DistanceToCollider(petPosition, collider),
                    Is.EqualTo(0.5f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(tree);
            }
        }
    }
}
