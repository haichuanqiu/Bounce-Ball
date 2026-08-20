
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameMeta
{
    public class MainMenu : MonoBehaviour
    {
        public static MainMenu Instance { get; private set; }

        public enum CurrentPanel
        {
            GameResult,
            Lobby,
            InGame
        }

        [Header("Panels")]
        public LobbyPanel LobbyPanel;
        public InGamePanel InGamePanel;
        public GameResultPanel GameResultPanel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            QualitySettings.vSyncCount = 0;

            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            Goto(CurrentPanel.Lobby);
        }

        public void Goto(CurrentPanel panel)
        {
            // 先关闭所有 Panel
            LobbyPanel.Disable();
            InGamePanel.Disable();
            GameResultPanel.Disable();

            // 再打开目标 Panel
            switch (panel)
            {
                case CurrentPanel.Lobby:
                    LobbyPanel.Activate();
                    break;

                case CurrentPanel.InGame:
                    InGamePanel.Activate();
                    break;

                case CurrentPanel.GameResult:
                    GameResultPanel.Activate();
                    break;
            }
        }
    }


    public abstract class Panel : MonoBehaviour
    {
        public abstract void Activate();
        public abstract void Disable();
    }


}
