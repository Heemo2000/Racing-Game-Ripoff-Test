using Game.TrackManagement;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI
{
    public class EndGameUI : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset endGameScreen;
        private UIDocument uiDocument;
        private TemplateContainer endGameScreenInstance = null;
        private VisualElement root = null;
        private Label winStatus = null;

        public void ShowEndScreen(Dictionary<int, RaceDriverData>.ValueCollection datas)
        {
            endGameScreenInstance.visible = true;

            var rankQuery = datas.OrderBy(data => data.Ranking);

            RaceDriverData firstRacerData = null;

            for(int i = 0; i < rankQuery.Count(); i++)
            {
                var current = rankQuery.ElementAt(i);
                if (current.Ranking == 1)
                {
                    firstRacerData = current;
                    break;
                }
            }

            if (firstRacerData.IsPlayer)
            {
                winStatus.style.color = Color.green;
                winStatus.text = "You are first.\nYou win!";
            }
            else
            {
                winStatus.style.color = Color.red;
                winStatus.text = firstRacerData.Name + " is first.\n You lose.";
            }

            uiDocument.rootVisualElement.Clear();
            uiDocument.rootVisualElement.Add(endGameScreenInstance);
        }

        private void Initialize()
        {
            endGameScreenInstance = endGameScreen.Instantiate();
            root = endGameScreenInstance.Q<VisualElement>("Root");
            winStatus = root.Q<Label>("WinStatus");
        }

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            Initialize();
        }
    }
}
