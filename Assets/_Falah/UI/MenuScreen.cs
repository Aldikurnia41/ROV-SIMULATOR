using UnityEngine;
using UnityEngine.UI;

namespace Falah.RovSim.UI
{
    /// <summary>Base class of one step in the menu flow.</summary>
    public abstract class MenuScreen : MonoBehaviour
    {
        [SerializeField] protected Button nextButton;
        [SerializeField] protected Button backButton;
        [SerializeField] protected ScreenHeader header;

        public Button NextButton => nextButton;
        public Button BackButton => backButton;

        /// <summary>Called each time the screen becomes visible.</summary>
        public virtual void OnShow()
        {
            if (header != null) header.Refresh();
        }

        /// <summary>Validate and store the user's input; return false to stay on this screen.</summary>
        public virtual bool TryAdvance() => true;
    }
}
