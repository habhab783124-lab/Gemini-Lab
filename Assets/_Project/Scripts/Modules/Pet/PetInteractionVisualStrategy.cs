#nullable enable
using System;
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Per-pet policy for choosing whether an interaction animation should be
    /// shown on the main renderer or via a detached visual overlay.
    /// </summary>
    [Serializable]
    public sealed class PetInteractionVisualStrategy
    {
        [Serializable]
        private sealed class AnimatorStateRule
        {
            [SerializeField] private string _animatorStateName = string.Empty;
            [SerializeField] private bool _useDetachedVisual = true;

            public string AnimatorStateName => _animatorStateName;
            public bool UseDetachedVisual => _useDetachedVisual;
        }

        [SerializeField] private bool _useDetachedVisualByDefault;
        [SerializeField] private AnimatorStateRule[] _rules = Array.Empty<AnimatorStateRule>();

        public bool UsesDetachedVisual(string animatorStateName, PetId petId)
        {
            if (string.IsNullOrWhiteSpace(animatorStateName))
            {
                return false;
            }

            for (int i = 0; i < _rules.Length; i++)
            {
                AnimatorStateRule rule = _rules[i];
                if (!string.Equals(rule.AnimatorStateName, animatorStateName, StringComparison.Ordinal))
                {
                    continue;
                }

                return rule.UseDetachedVisual;
            }

            if (_rules.Length > 0 || _useDetachedVisualByDefault)
            {
                return _useDetachedVisualByDefault;
            }

            return UsesPetDefaults(animatorStateName, petId);
        }

        private static bool UsesPetDefaults(string animatorStateName, PetId petId)
        {
            if (petId != PetId.Angel)
            {
                return false;
            }

            return animatorStateName switch
            {
                "Sleep" => true,
                "Interact_Flower" => true,
                "Interact_PlayingMusic" => true,
                "Interact_Write" => true,
                _ => false
            };
        }
    }
}
