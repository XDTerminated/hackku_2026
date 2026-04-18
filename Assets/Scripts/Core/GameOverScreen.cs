using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HackKU.Core
{
    public class GameOverScreen : MonoBehaviour
    {
        public Canvas screenCanvas;
        public Text yearText;
        public Text causeText;
        public Text lessonText;
        public Button restartButton;

        void OnEnable()
        {
            StatsManager.OnGameOver += HandleGameOver;
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
            if (screenCanvas != null) screenCanvas.gameObject.SetActive(false);
        }

        void OnDisable()
        {
            StatsManager.OnGameOver -= HandleGameOver;
            if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
        }

        void HandleGameOver(GameOverInfo info)
        {
            if (screenCanvas != null) screenCanvas.gameObject.SetActive(true);
            if (yearText != null) yearText.text = "Year " + info.yearReached;
            if (causeText != null) causeText.text = FormatCause(info.cause);
            if (lessonText != null) lessonText.text = BuildLesson(info.cause);
        }

        static string FormatCause(string cause)
        {
            if (string.IsNullOrEmpty(cause)) return "Game Over";
            switch (cause)
            {
                case "broke": return "You went broke.";
                case "miserable": return "You became miserable.";
                default: return cause;
            }
        }

        static string BuildLesson(string cause)
        {
            switch (cause)
            {
                case "broke":
                    return "Every dollar you let slip adds up. Budget first, splurge second.";
                case "miserable":
                    return "Money means nothing if you never did what made you happy.";
                default:
                    return "Balance what you earn against what actually makes life worth living.";
            }
        }

        void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
