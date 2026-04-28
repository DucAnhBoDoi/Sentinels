using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode; // Để check quyền Server/Client
using Scripts.Floor3.AI;
using Scripts.Floor3.Network; // Gọi trạm phát sóng

namespace Scripts.Floor3.UI
{
    public class TopicSelectionUI : MonoBehaviour
    {
        public static TopicSelectionUI Instance;
        public static bool hasStartedMission = false;

        [SerializeField] private Button _technologyButton;
        [SerializeField] private Button _biologyButton;
        [SerializeField] private Button _ethicsButton;
        [SerializeField] private Button _startButton;
        
        [SerializeField] private TextMeshProUGUI _loadingText;
        [SerializeField] private TextMeshProUGUI _sourceText;
        [SerializeField] private TextMeshProUGUI _startButtonText;
        
        [SerializeField] private GameObject _loadingPanel;
        [SerializeField] private GeminiQuizGenerator _geminiGenerator;

        private bool _questionsReady = false;

        private void Awake()
        {
            Instance = this;
            hasStartedMission = false; 

            _technologyButton?.onClick.AddListener(() => OnTopicSelected("technology"));
            _biologyButton?.onClick.AddListener(() => OnTopicSelected("biology"));
            _ethicsButton?.onClick.AddListener(() => OnTopicSelected("ethics"));
            _startButton?.onClick.AddListener(OnStartClicked);

            if (_loadingPanel != null) _loadingPanel.SetActive(false);
            if (_startButton != null) _startButton.gameObject.SetActive(false);
        }

        private void OnEnable() { Time.timeScale = 0f; }
        private void OnDisable() { Time.timeScale = 1f; }

        private void Start()
        {
            // KHÓA TAY CLIENT: Nếu là Client thì làm mờ các nút, không cho bấm
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            {
                if (_technologyButton != null) _technologyButton.interactable = false;
                if (_biologyButton != null) _biologyButton.interactable = false;
                if (_ethicsButton != null) _ethicsButton.interactable = false;
                if (_startButton != null) _startButton.interactable = false;
            }
        }

        private void OnTopicSelected(string topic)
        {
            // Bảo vệ kép: Chỉ Host mới được gọi API
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer) return;
            if (_questionsReady) return; 
            _questionsReady = false;

            // Nhờ trạm phát sóng báo cho các máy khác hiện bảng Loading
            if (TopicNetworkSync.Instance != null) TopicNetworkSync.Instance.SyncLoadingStateClientRpc();

            _geminiGenerator.FetchAndPreload(topic, OnQuestionsReadyHost);
        }

        // Hàm này các máy khác sẽ chạy khi nhận được tín hiệu
        public void ApplyLoadingState()
        {
            if (_technologyButton != null) _technologyButton.interactable = false;
            if (_biologyButton != null) _biologyButton.interactable = false;
            if (_ethicsButton != null) _ethicsButton.interactable = false;

            if (_loadingPanel != null) _loadingPanel.SetActive(true);
            if (_loadingText != null) _loadingText.text = "Generating questions with AI...";
            if (_sourceText != null) _sourceText.text = "";
            if (_startButton != null) _startButton.gameObject.SetActive(false);
        }

        private void OnQuestionsReadyHost()
        {
            bool isFallback = _geminiGenerator.IsUsingFallback;
            // Nhờ trạm phát sóng báo cho các máy khác biết câu hỏi đã tải xong
            if (TopicNetworkSync.Instance != null) TopicNetworkSync.Instance.SyncReadyStateClientRpc(isFallback);
        }

        // Hàm này các máy khác sẽ chạy khi AI load xong
        public void ApplyReadyState(bool isFallback)
        {
            _questionsReady = true;
            if (_loadingText != null) _loadingText.text = "Questions ready!";
            if (_sourceText != null) _sourceText.text = isFallback ? "Backup active" : "AI Loaded";
            
            if (_startButton != null) 
            {
                _startButton.gameObject.SetActive(true);
                // Host thì hiện "Start Mission", Client thì hiện "Waiting for Host..."
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) 
                {
                    _startButton.interactable = true;
                    if (_startButtonText != null) _startButtonText.text = "Start Mission";
                }
                else 
                {
                    _startButton.interactable = false;
                    if (_startButtonText != null) _startButtonText.text = "Waiting for Host...";
                }
            }
        }

        private void OnStartClicked()
        {
            // Chỉ Host mới được bấm nút xuất phát
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer) return;
            if (!_questionsReady) return;
            
            // Nhờ trạm phát sóng hô to: "XUẤT PHÁT THÔI!!!"
            if (TopicNetworkSync.Instance != null) TopicNetworkSync.Instance.SyncStartMissionClientRpc();
        }

        // Hàm này cả Host và Client cùng chạy để vào game
        public void ApplyStartMission()
        {
            Time.timeScale = 1f; 
            hasStartedMission = true; 
            
            // LƯU Ý CHO VỤ KẸT DI CHUYỂN: Nhớ mở khóa bảng Quest nếu bạn có dùng nó nhé!
            QuestPopupManager.isGameStarted = true; 

            if (Floor3UIManager.Instance != null) Floor3UIManager.Instance.StartMission();
        }
    }
}