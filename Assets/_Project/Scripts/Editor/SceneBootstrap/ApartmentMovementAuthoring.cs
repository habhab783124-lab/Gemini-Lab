#nullable enable
using GeminiLab.Modules.Pet;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    public static class ApartmentMovementAuthoring
    {
        [MenuItem("Tools/Gemini-Lab/Apartment/Upgrade Pet Movement")]
        public static void Upgrade()
        {
            if (Application.isPlaying) return;
            foreach (PetController pet in Object.FindObjectsByType<PetController>(FindObjectsSortMode.None))
            {
                if (pet.gameObject.scene.path != "Assets/_Project/Scenes/Apartment/Apartment_Main.unity") continue;
                var petObject = new SerializedObject(pet);
                ApartmentPetMovement motor = pet.GetComponent<ApartmentPetMovement>();
                if (motor == null) motor = Undo.AddComponent<ApartmentPetMovement>(pet.gameObject);
                var motorObject = new SerializedObject(motor);
                motorObject.FindProperty("_body").objectReferenceValue = pet.GetComponent<Rigidbody2D>();
                motorObject.FindProperty("_footprint").objectReferenceValue = pet.GetComponent<CapsuleCollider2D>();
                motorObject.FindProperty("_room").objectReferenceValue = petObject.FindProperty("_movementBounds").objectReferenceValue;
                motorObject.ApplyModifiedProperties();
                petObject.FindProperty("_apartmentMovement").objectReferenceValue = motor;
                petObject.ApplyModifiedProperties();

                var interactions = pet.GetComponent<PetPlayerFurnitureInteractionController>();
                if (interactions == null) continue;
                var bindingsObject = new SerializedObject(interactions);
                SerializedProperty bindings = bindingsObject.FindProperty("_bindings");
                for (int i = 0; i < bindings.arraySize; i++)
                {
                    SerializedProperty binding = bindings.GetArrayElementAtIndex(i);
                    SerializedProperty approach = binding.FindPropertyRelative("_approachPoint");
                    if (approach.objectReferenceValue != null) continue; // 保留手调的站立点。
                    var target = binding.FindPropertyRelative("_target").objectReferenceValue as GameObject;
                    bool fallback = binding.FindPropertyRelative("_useFallbackWorldPoint").boolValue;
                    if (target == null && !fallback) continue;
                    Vector2 requested = fallback ? binding.FindPropertyRelative("_fallbackWorldPoint").vector2Value : (Vector2)target!.transform.position;
                    float radius = binding.FindPropertyRelative("_activationDistance").floatValue;
                    if (!motor.TryFindStandPoint(requested, radius, out Vector2 point))
                    {
                        Debug.LogError($"[ApartmentMovement] {pet.name}/{binding.FindPropertyRelative("_label").stringValue} 没有可达站立点。", pet);
                        continue;
                    }
                    var anchor = new GameObject($"Approach_{pet.PetId}_{binding.FindPropertyRelative("_label").stringValue}");
                    Undo.RegisterCreatedObjectUndo(anchor, "Author pet approach point");
                    anchor.transform.SetParent(target != null ? target.transform : pet.transform.parent, false);
                    anchor.transform.position = new Vector3(point.x, point.y, pet.transform.position.z);
                    approach.objectReferenceValue = anchor.transform;
                }
                bindingsObject.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(pet.gameObject.scene);
            }
            Debug.Log("[ApartmentMovement] 已绑定公寓移动与站立点；请核对并保存 Scene。");
        }
    }
}
