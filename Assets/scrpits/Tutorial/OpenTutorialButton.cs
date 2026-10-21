using UnityEngine;
using UnityEngine.UI;

namespace BallzGame.Tutorial
{
    [RequireComponent(typeof(Button))]
    public sealed class OpenTutorialButton : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OpenTutorial);
        }

        private static void OpenTutorial()
        {
            if (TutorialDirector.Instance)
                TutorialDirector.Instance.StartTutorial();
        }
    }
}
