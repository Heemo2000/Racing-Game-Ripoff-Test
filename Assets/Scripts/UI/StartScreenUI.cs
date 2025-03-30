using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI
{
    public class StartScreenUI : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement root;
        private Label timeText;

        public void SetTimeText(int seconds)
        {
            timeText.text = seconds.ToString();
        }

        public void SetStartScreenEnabled(bool state)
        {
            //Debug.Log("Setting start screen state to " + state);
            root.visible = state;
        }

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            root = uiDocument.rootVisualElement;
            timeText = root.Q<Label>("TimeText") as Label;
            
            SetStartScreenEnabled(false);
        }
    }
}
