#nullable enable
using GeminiLab.Core;
using GeminiLab.Modules.Pet;
using System.Collections.Generic;
using UnityEngine;

namespace GeminiLab.Modules.RoomRelic
{
    /// <summary>
    /// 挂在现有 PetMovementBounds / PetMovementBounds_Devil 上的逻辑触发器。
    /// 只负责检测对应宠物进入房间并调用服务，不创建视觉节点。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomRelicEntryTrigger : MonoBehaviour
    {
        [SerializeField] private RoomId _roomId = RoomId.AngelRoom;
        [SerializeField] private PetId _expectedPetId = PetId.Angel;
        private readonly HashSet<Collider2D> _occupants = new();

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryEnter(other);
        }

        // 启动时已经位于区域内，或服务稍晚注册时，都能补齐进入事件。
        private void OnTriggerStay2D(Collider2D other) => TryEnter(other);

        private void TryEnter(Collider2D other)
        {
            if (_occupants.Contains(other)) return;
            PetController? pet = other.GetComponentInParent<PetController>();
            if (pet == null || pet.PetId != _expectedPetId)
            {
                return;
            }

            if (ServiceLocator.TryResolve(out IRoomRelicService? service) && service is not null)
            {
                _occupants.Add(other);
                service.ProcessRoomEntry(_roomId);
                service.SetCurrentRoom(_roomId);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!_occupants.Remove(other) || _occupants.Count > 0) return;

            if (ServiceLocator.TryResolve(out IRoomRelicService? service) && service is not null)
            {
                service.ClearCurrentRoom(_roomId);
            }
        }

        private void OnDisable()
        {
            _occupants.Clear();
            if (ServiceLocator.TryResolve(out IRoomRelicService? service) && service is not null)
                service.ClearCurrentRoom(_roomId);
        }
    }
}
