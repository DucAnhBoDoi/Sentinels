// ============================================================
// FILE: Assets/Scripts/Floor3/UI/RobotAuraRenderer.cs
// Namespace: Scripts.Floor3.UI
// ============================================================
// Hiển thị vòng aura filled xung quanh robot dựa trên khoảng
// cách của player gần nhất đến robot.
//
// LOGIC HIỂN THỊ (theo player GẦN NHẤT):
//   dist <= escortDistance  → Aura XANH LÁ  (an toàn, robot đang đi)
//   dist <= warnDistance    → Aura VÀNG      (cảnh báo, hơi xa)
//   dist >  warnDistance    → Aura ĐỎ        (nguy hiểm, quá xa)
//
// CÁCH HOẠT ĐỘNG:
//   - Dùng SpriteRenderer với sprite hình tròn (Unity built-in)
//   - Scale sprite để khớp với bán kính vùng tương ứng
//   - Dùng alpha để tạo hiệu ứng filled mờ đẹp
//   - Pulse animation nhẹ khi ở vùng đỏ để thu hút sự chú ý
//   - Subscribe ProximityEventBus — không tham chiếu trực tiếp
//     ProximityDetector hay RobotController
//
// SETUP TRONG UNITY:
//   1. Tạo child GameObject trong Mechanical_Soul_Robot
//      đặt tên "RobotAura"
//   2. Gắn script này lên "RobotAura"
//   3. KHÔNG cần gắn Sprite thủ công —
//      script tự tạo sprite circle lúc runtime
//   4. Điền escortDistance / warnDistance / farDistance
//      khớp với giá trị trong ProximityDetector Inspector
// ============================================================

using UnityEngine;
using Scripts.Floor3.Core;

namespace Scripts.Floor3.UI
{
    public class RobotAuraRenderer : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────

        [Header("Ngưỡng khoảng cách (khớp với ProximityDetector)")]
        [Tooltip("Khớp với _escortDistance trong ProximityDetector")]
        [SerializeField] private float _escortDistance = 6f;
        [Tooltip("Khớp với _warnDistance trong ProximityDetector")]
        [SerializeField] private float _warnDistance = 5f;

        [Header("Màu sắc aura")]
        [SerializeField] private Color _safeColor = new Color(0.0f, 1.0f, 0.2f, 0.18f); // xanh lá
        [SerializeField] private Color _warnColor = new Color(1.0f, 0.9f, 0.0f, 0.20f); // vàng
        [SerializeField] private Color _dangerColor = new Color(1.0f, 0.15f, 0.1f, 0.22f); // đỏ

        [Header("Pulse khi nguy hiểm")]
        [Tooltip("Bật hiệu ứng nhấp nháy nhẹ khi ở vùng đỏ")]
        [SerializeField] private bool _pulseOnDanger = true;
        [SerializeField] private float _pulseSpeed = 2.5f;
        [SerializeField] private float _pulseAlphaMin = 0.08f;
        [SerializeField] private float _pulseAlphaMax = 0.30f;

        [Header("Transition")]
        [Tooltip("Tốc độ chuyển màu mượt giữa các vùng")]
        [SerializeField] private float _colorLerpSpeed = 6f;

        [Header("Render")]
        [SerializeField] private int _sortingOrder = -1; // dưới robot
        [SerializeField] private string _sortingLayerName = "Default";

        // ── Private State ─────────────────────────────────────────────────

        private SpriteRenderer _sr;

        // Trạng thái hiện tại
        private enum AuraZone { Safe, Warn, Danger }
        private AuraZone _currentZone = AuraZone.Safe;
        private Color _targetColor;
        private float _currentRadius;

        // ── Lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            SetupSpriteRenderer();
            _targetColor = _safeColor;
            _currentRadius = _escortDistance;
            ApplyScale(_escortDistance);
        }

        private void OnEnable()
        {
            ProximityEventBus.OnProximityUpdated += HandleProximityUpdated;
        }

        private void OnDisable()
        {
            ProximityEventBus.OnProximityUpdated -= HandleProximityUpdated;
        }

        private void Update()
        {
            // Smooth color transition
            Color current = _sr.color;
            Color next = Color.Lerp(current, _targetColor, Time.deltaTime * _colorLerpSpeed);

            // Pulse effect khi nguy hiểm
            if (_pulseOnDanger && _currentZone == AuraZone.Danger)
            {
                float alpha = Mathf.Lerp(
                    _pulseAlphaMin, _pulseAlphaMax,
                    (Mathf.Sin(Time.time * _pulseSpeed) + 1f) * 0.5f
                );
                next.a = alpha;
            }

            _sr.color = next;
        }

        // ── Event Handler ─────────────────────────────────────────────────

        private void HandleProximityUpdated(
            float distA, float distB,
            float warnThreshold, float farThreshold)
        {
            // Dùng khoảng cách của player GẦN NHẤT để xác định zone
            float closestDist = Mathf.Min(distA, distB);

            AuraZone newZone;
            float targetRadius;

            if (closestDist <= _escortDistance)
            {
                // Cả 2 player đều gần → an toàn → aura xanh lá (vòng escort)
                newZone = AuraZone.Safe;
                targetRadius = _escortDistance;
                _targetColor = _safeColor;
            }
            else if (closestDist <= _warnDistance + (_escortDistance - _warnDistance) + 1f)
            {
                // Player ở vùng warn → aura vàng
                newZone = AuraZone.Warn;
                targetRadius = _warnDistance;
                _targetColor = _warnColor;
            }
            else
            {
                // Player đã quá xa → aura đỏ + pulse
                newZone = AuraZone.Danger;
                targetRadius = farThreshold;
                _targetColor = _dangerColor;
            }

            _currentZone = newZone;
            _currentRadius = targetRadius;

            // Scale ngay lập tức khi đổi zone (không lerp scale để tránh lỗi size)
            ApplyScale(targetRadius);
        }

        // ── Helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Scale SpriteRenderer để đường kính sprite khớp với radius thực tế.
        /// Unity's default circle sprite có diameter = 1 unit →
        /// scale = radius * 2 cho cả X lẫn Y.
        /// </summary>
        private void ApplyScale(float radius)
        {
            float diameter = radius * 2f;
            transform.localScale = new Vector3(diameter, diameter, 1f);
        }

        /// <summary>
        /// Tự tạo SpriteRenderer với circle sprite built-in của Unity.
        /// Không cần import asset ngoài.
        /// </summary>
        private void SetupSpriteRenderer()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null)
                _sr = gameObject.AddComponent<SpriteRenderer>();

            // Tự tạo circle sprite bằng code — không phụ thuộc built-in resource path
            if (_sr.sprite == null)
                _sr.sprite = CreateCircleSprite(128);

            _sr.color = _safeColor;
            _sr.sortingOrder = _sortingOrder;
            _sr.sortingLayerName = _sortingLayerName;
            _sr.material = new Material(Shader.Find("Sprites/Default"));
        }

        /// <summary>
        /// Tạo sprite hình tròn filled bằng Texture2D.
        /// resolution: số pixel mỗi chiều (128 là đủ mượt cho aura).
        /// </summary>
        private Sprite CreateCircleSprite(int resolution)
        {
            var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            var pixels = new Color32[resolution * resolution];

            float center = resolution * 0.5f;
            float radius = center;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dist = Mathf.Sqrt((x - center) * (x - center) +
                                            (y - center) * (y - center));

                    if (dist <= radius)
                    {
                        // Anti-alias: pixel ở rìa mờ dần
                        float alpha = Mathf.Clamp01(radius - dist);
                        pixels[y * resolution + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                    }
                    else
                    {
                        pixels[y * resolution + x] = new Color32(0, 0, 0, 0);
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            return Sprite.Create(
                tex,
                new Rect(0, 0, resolution, resolution),
                new Vector2(0.5f, 0.5f), // pivot center
                resolution               // pixels per unit = resolution → sprite = 1 unit
            );
        }
    }
}