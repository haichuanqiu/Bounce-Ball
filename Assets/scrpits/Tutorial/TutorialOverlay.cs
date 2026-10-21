using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BallzGame.Tutorial
{
    public sealed class TutorialOverlay : MonoBehaviour
    {
        private readonly Dictionary<string, TutorialPopupView> popupById = new Dictionary<string, TutorialPopupView>();
        private TutorialPopupView current;

        private void Awake()
        {
            RebuildLookup();
            Hide();
        }

        private void RebuildLookup()
        {
            popupById.Clear();
            foreach (TutorialPopupView popup in GetComponentsInChildren<TutorialPopupView>(true))
            {
                popup.gameObject.SetActive(false);
                if (string.IsNullOrWhiteSpace(popup.PopupId))
                    continue;
                if (!popupById.TryAdd(popup.PopupId, popup))
                    Debug.LogWarning($"Tutorial: duplicate popup id '{popup.PopupId}'.", popup);
            }
        }

        public IEnumerator Show(string popupId)
        {
            Hide();
            if (popupById.Count == 0)
                RebuildLookup();
            if (!popupById.TryGetValue(popupId, out current))
            {
                Debug.LogError($"Tutorial: popup '{popupId}' was not found under Canvas/NavigationPages/HowToPlay.");
                yield break;
            }
            current.Present();
            yield return new WaitUntil(() => current.Confirmed);
            Hide();
        }

        public void Hide()
        {
            if (current)
            {
                current.gameObject.SetActive(false);
                current = null;
            }
        }
    }
}
