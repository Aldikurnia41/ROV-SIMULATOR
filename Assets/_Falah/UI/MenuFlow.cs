using UnityEngine;
using UnityEngine.SceneManagement;

namespace Falah.RovSim.UI
{
    /// <summary>Shows one <see cref="MenuScreen"/> at a time: Login, ROV, Scenario, Briefing, then loads the simulation.</summary>
    public sealed class MenuFlow : MonoBehaviour
    {
        [SerializeField] MenuScreen[] screens;
        [SerializeField] string simulationScene = "RoVGameplay";

        int index;

        void Awake()
        {
            for (int i = 0; i < screens.Length; i++)
            {
                int captured = i;
                if (screens[i].NextButton != null) screens[i].NextButton.onClick.AddListener(() => Next(captured));
                if (screens[i].BackButton != null) screens[i].BackButton.onClick.AddListener(() => Show(captured - 1));
            }
        }

        void Start() => Show(0);

        void Next(int from)
        {
            if (from != index || !screens[from].TryAdvance()) return;
            if (from + 1 < screens.Length) Show(from + 1);
            else SceneManager.LoadScene(simulationScene);
        }

        void Show(int target)
        {
            if (target < 0 || target >= screens.Length) return;
            index = target;
            for (int i = 0; i < screens.Length; i++) screens[i].gameObject.SetActive(i == target);
            screens[target].OnShow();
        }
    }
}
