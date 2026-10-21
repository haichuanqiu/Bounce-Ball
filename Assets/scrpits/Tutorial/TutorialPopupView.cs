using UnityEngine;
using UnityEngine.UI;

namespace BallzGame.Tutorial
{
    public sealed class TutorialPopupView : MonoBehaviour
    {
        [SerializeField] private string popupId;
        [SerializeField] private Button confirmButton;

        public string PopupId => popupId;
        public bool Confirmed { get; private set; }

        private void OnEnable()
        {
            if (confirmButton)
                confirmButton.onClick.AddListener(Confirm);
        }

        private void OnDisable()
        {
            if (confirmButton)
                confirmButton.onClick.RemoveListener(Confirm);
        }

        public void Configure(string id, Button button)
        {
            popupId = id;
            confirmButton = button;
        }

        public void Present()
        {
            Confirmed = false;
            if (!confirmButton)
            {
                Debug.LogError($"Tutorial popup '{popupId}' is missing its confirm button reference.", this);
                Confirmed = true;
                return;
            }
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        private void Confirm()
        {
            Confirmed = true;
        }

    }
}