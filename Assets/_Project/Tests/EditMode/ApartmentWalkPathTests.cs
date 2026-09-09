using System.Collections.Generic;
using GeminiLab.Modules.Pet;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace GeminiLab.Tests.EditMode
{
    public sealed class ApartmentWalkPathTests
    {
        private static readonly Rect Area = Rect.MinMaxRect(-5, -5, 5, 5);

        [Test]
        public void RouteAroundFurniture_AllSegmentsStayClear()
        {
            var obstacles = new[] { Rect.MinMaxRect(-1, -2, 1, 2) };
            var path = new List<Vector2>();
            var start = new Vector2(-4, 0);
            Assert.IsTrue(ApartmentWalkPath.TryPlan(start, new Vector2(4, 0), Area, obstacles, path));
            Assert.Greater(path.Count, 1);
            foreach (var point in path)
            {
                Assert.IsTrue(ApartmentWalkPath.IsFree(point, Area, obstacles));
                Assert.IsTrue(ApartmentWalkPath.IsClear(start, point, obstacles));
                start = point;
            }
            Assert.AreEqual(new Vector2(4, 0), start);
        }

        [Test]
        public void WallAcrossRoom_ReturnsUnreachable()
        {
            var obstacles = new[] { Rect.MinMaxRect(-1, -6, 1, 6) };
            var path = new List<Vector2>();
            Assert.IsFalse(ApartmentWalkPath.TryPlan(new Vector2(-4, 0), new Vector2(4, 0), Area, obstacles, path));
            Assert.IsEmpty(path);
        }

        [Test]
        public void TargetInsideFurnitureOrOutsideRoom_IsRejected()
        {
            var obstacles = new[] { Rect.MinMaxRect(-1, -1, 1, 1) };
            var path = new List<Vector2>();
            Assert.IsFalse(ApartmentWalkPath.TryPlan(new Vector2(-4, 0), Vector2.zero, Area, obstacles, path));
            Assert.IsFalse(ApartmentWalkPath.TryPlan(new Vector2(-4, 0), new Vector2(7, 0), Area, obstacles, path));
        }

        [Test]
        public void FastMovementCannotTunnelThroughThinFurniture()
        {
            var obstacles = new[] { Rect.MinMaxRect(0, -4, 0.05f, 4) };
            Vector2 next = ApartmentWalkPath.Slide(new Vector2(-3, 0), new Vector2(7, 0), Area, obstacles);
            Assert.Less(next.x, 0);
            Assert.Greater(next.x, -0.01f);
        }

        [Test]
        public void DiagonalMovementSlidesAlongFurniture()
        {
            var obstacles = new[] { Rect.MinMaxRect(0, -4, 1, 4) };
            Vector2 next = ApartmentWalkPath.Slide(new Vector2(-1, -1), new Vector2(2, 2), Area, obstacles);
            Assert.Less(next.x, 0);
            Assert.Greater(next.y, 0.9f);
        }

        [Test]
        public void EveryStepStaysInsideRoom()
        {
            Vector2 next = ApartmentWalkPath.Slide(Vector2.zero, new Vector2(100, -100), Area, new Rect[0]);
            Assert.AreEqual(new Vector2(5, -5), next);
        }

        [Test]
        public void FootOffsetAndMirroredScale_KeepTheSameWalkArea()
        {
            var roomObject = new GameObject("test-room");
            var petObject = new GameObject("test-pet");
            try
            {
                roomObject.transform.position = new Vector3(100, 100);
                var room = roomObject.AddComponent<BoxCollider2D>();
                room.size = new Vector2(10, 10); room.isTrigger = true;
                petObject.transform.position = new Vector3(100, 100);
                petObject.transform.localScale = Vector3.one * 0.5f;
                var body = petObject.AddComponent<Rigidbody2D>();
                var footprint = petObject.AddComponent<CapsuleCollider2D>();
                footprint.size = Vector2.one;
                footprint.offset = new Vector2(-0.1f, -3);
                var motor = petObject.AddComponent<ApartmentPetMovement>();
                var so = new SerializedObject(motor);
                so.FindProperty("_body").objectReferenceValue = body;
                so.FindProperty("_footprint").objectReferenceValue = footprint;
                so.FindProperty("_room").objectReferenceValue = room;
                so.ApplyModifiedPropertiesWithoutUndo();
                Physics2D.SyncTransforms();
                Vector2 bottom = motor.Clamp(new Vector2(100, 0));
                // 房间下沿 95；脚底偏移 -1.5、半高 .25、skin .04。
                Assert.That(bottom.y, Is.EqualTo(96.79f).Within(0.001f));
                Rect before = motor.WalkArea;
                petObject.transform.localScale = new Vector3(-0.5f, 0.5f, 0.5f);
                motor.Clamp(bottom);
                Assert.AreEqual(before, motor.WalkArea);
                petObject.transform.position = new Vector3(101.2345f, 99.8765f);
                motor.Clamp(bottom);
                Assert.AreEqual(before, motor.WalkArea, "移动位置不应使脚底偏移和导航边界抖动");
            }
            finally { Object.DestroyImmediate(petObject); Object.DestroyImmediate(roomObject); }
        }

        [Test]
        public void OpenPassageIsOnlyRouteThroughDivider_ClosingBlocksBothDirections()
        {
            var walls=new List<Rect>{Rect.MinMaxRect(-.8f,-6,.8f,-1),Rect.MinMaxRect(-.8f,1,.8f,6)};
            var path=new List<Vector2>();
            var start=new Vector2(-4,3); var target=new Vector2(4,3);
            Assert.IsTrue(ApartmentWalkPath.TryPlan(start,target,Area,walls,path));
            Assert.Greater(path.Count,1); Assert.IsTrue(path.Exists(p=>Mathf.Abs(p.y)<1));
            var direct=ApartmentWalkPath.Slide(start,new Vector2(8,0),Area,walls);
            Assert.Less(direct.x,0,"开门也不能穿过门口以外的墙面");
            walls.Add(Rect.MinMaxRect(-.8f,-1,.8f,1));
            Assert.IsFalse(ApartmentWalkPath.TryPlan(start,target,Area,walls,path));
            Assert.IsFalse(ApartmentWalkPath.TryPlan(target,start,Area,walls,path));
        }
    }
}
