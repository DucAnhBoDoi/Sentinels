using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scripts.Floor3.AI;

namespace Scripts.Floor3.UI
{
    public class TopicSelectionUI : MonoBehaviour
    {
        // ── THÊM BIẾN NÀY ĐỂ BÁO CHO QUÁI BIẾT ĐÃ VÀO GAME CHƯA ──
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

        // ── ĐÓNG BĂNG THỜI GIAN KHI BẢNG CHỌN CHỦ ĐỀ HIỆN LÊN ──
        private void OnEnable()
        {
            Time.timeScale = 0f; // Bọn quái, Robot, và mọi thứ dùng vật lý sẽ đứng im như tượng!
        }

        // ── MỞ KHÓA THỜI GIAN DỰ PHÒNG KHI BẢNG TẮT ĐI ──
        private void OnDisable()
        {
            Time.timeScale = 1f; 
        }

        private void Awake()
        {
            // Reset lại còi báo hiệu mỗi khi chơi lại game hoặc load lại scene
            hasStartedMission = false; 

            _technologyButton?.onClick.AddListener(() => OnTopicSelected("technology"));
            _biologyButton?.onClick.AddListener(() => OnTopicSelected("biology"));
            _ethicsButton?.onClick.AddListener(() => OnTopicSelected("ethics"));
            
            // Khi bấm Start Mission -> Gọi Floor3UIManager xử lý việc bật HUD
            _startButton?.onClick.AddListener(() => {
                if (_questionsReady && Floor3UIManager.Instance != null) {
                    
                    // ── MỞ KHÓA THỜI GIAN ĐỂ GAME CHẠY TRỞ LẠI ──
                    Time.timeScale = 1f; 

                    // ── BẬT CÒI BÁO HIỆU: ĐÃ BẮT ĐẦU SỨ MỆNH HỘ TỐNG! ──
                    hasStartedMission = true; 
                    
                    Floor3UIManager.Instance.StartMission();
                }
            });

            // Ẩn chữ loading và nút start đi (vì bảng Topic đang hiện)
            if (_loadingPanel != null) _loadingPanel.SetActive(false);
            if (_startButton != null) _startButton.gameObject.SetActive(false);
        }

        private void OnTopicSelected(string topic)
        {
            if (_questionsReady) return; 
            _questionsReady = false;

            // Tạm khóa các nút
            if (_technologyButton != null) _technologyButton.interactable = false;
            if (_biologyButton != null) _biologyButton.interactable = false;
            if (_ethicsButton != null) _ethicsButton.interactable = false;

            if (_loadingPanel != null) _loadingPanel.SetActive(true);
            if (_loadingText != null) _loadingText.text = "Generating questions with AI...";
            if (_sourceText != null) _sourceText.text = "";
            if (_startButton != null) _startButton.gameObject.SetActive(false);

            _geminiGenerator.FetchAndPreload(topic, OnQuestionsReady);
        }

        private void OnQuestionsReady()
        {
            _questionsReady = true;
            if (_loadingText != null) _loadingText.text = "Questions ready!";
            if (_sourceText != null) _sourceText.text = _geminiGenerator.IsUsingFallback ? "Backup active" : "AI Loaded";
            
            if (_startButton != null) _startButton.gameObject.SetActive(true);
            if (_startButtonText != null) _startButtonText.text = "Start Mission";
        }
    }
}