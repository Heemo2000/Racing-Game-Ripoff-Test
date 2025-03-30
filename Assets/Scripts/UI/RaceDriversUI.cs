using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Game.TrackManagement;
using System.Text;

namespace Game.UI
{
    public class RaceDriversUI : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset inGameScreen;
        [SerializeField] private VisualTreeAsset raceDriverUI;
        private UIDocument uiDocument;
        private VisualElement inGameScreenInstance = null;
        private Label lapIndicator;
        private StringBuilder tempTextBuilder;
        private List<TemplateContainer> racingUIs;

        private Color transparentYellow = Color.white;
        private Color transparentGray = Color.white;

        public void LoadInGameUI(Dictionary<int, RaceDriverData>.ValueCollection datas)
        {
            inGameScreenInstance = inGameScreen.Instantiate();
            var root = inGameScreenInstance.Q<VisualElement>("Root");
            lapIndicator = root.Q<Label>("LapIndicator");
            var raceDriverStatuses = root.Q<VisualElement>("RaceDriverStatuses");

            racingUIs.Clear();

            foreach (var data in datas)
            {
                var ui = raceDriverUI.Instantiate();
                var uiRoot = ui.Q<VisualElement>("Root");
                var raceDriverName = ui.Q<Label>("RaceDriverName") as Label;
                var progress = ui.Q<Label>("Progress") as Label;

                raceDriverName.text = data.Name;
                
                tempTextBuilder.Clear();
                tempTextBuilder.Append(Mathf.RoundToInt(data.CompleteProgress).ToString());
                tempTextBuilder.Append("%");

                progress.text = tempTextBuilder.ToString();
                
                if(data.IsPlayer)
                {
                    uiRoot.style.backgroundColor =  transparentYellow;
                }
                else
                {
                    uiRoot.style.backgroundColor = transparentGray;
                }

                raceDriverStatuses.Add(ui);
                racingUIs.Add(ui);
                inGameScreenInstance.visible = true;
            }

            uiDocument.rootVisualElement.Clear();
            uiDocument.rootVisualElement.Add(inGameScreenInstance);
        }

        public void UpdateInGameUI(Dictionary<int, RaceDriverData>.ValueCollection datas)
        {
            var rankQuery = datas.OrderBy(data => data.Ranking);

            int i = 0;
            
            foreach(var value in rankQuery)
            {
                var ui = racingUIs[i];
                var uiRoot = ui.Q<VisualElement>("Root");
                var raceDriverName = ui.Q<Label>("RaceDriverName") as Label;
                var progress = ui.Q<Label>("Progress") as Label;

                if(value.IsPlayer)
                {
                    uiRoot.style.backgroundColor = transparentYellow;
                    tempTextBuilder.Clear();
                    tempTextBuilder.Append(value.CompletedLaps + 1);
                    tempTextBuilder.Append('/');
                    tempTextBuilder.Append(value.TotalLaps);
                    lapIndicator.text = tempTextBuilder.ToString();
                }
                else
                {
                    uiRoot.style.backgroundColor = transparentGray;
                }

                raceDriverName.text = value.Name;
                
                tempTextBuilder.Clear();
                tempTextBuilder.Append((value.CompleteProgress * 100.0f).ToString("F2"));
                tempTextBuilder.Append("%");

                progress.text = tempTextBuilder.ToString();
                i++;
            }
        }

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            tempTextBuilder = new StringBuilder();
            racingUIs = new List<TemplateContainer>();

            transparentYellow = Color.yellow;
            transparentYellow.a = 0.7f;

            transparentGray = Color.gray;
            transparentGray.a = 0.7f;
        }
    }
}
