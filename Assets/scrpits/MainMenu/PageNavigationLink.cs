using UnityEngine;

namespace GameMeta
{
    // Navigation only: no game, settings, save or reward operations.
    public class PageNavigationLink : MonoBehaviour
    {
        public GameObject[] HidePages = new GameObject[0];
        public GameObject[] ShowPages = new GameObject[0];

        public void Navigate()
        {
            foreach (var page in HidePages)
                if (page) page.SetActive(false);
            foreach (var page in ShowPages)
                if (page)
                {
                    page.SetActive(true);
                    page.transform.SetAsLastSibling();
                }
        }
    }
}
