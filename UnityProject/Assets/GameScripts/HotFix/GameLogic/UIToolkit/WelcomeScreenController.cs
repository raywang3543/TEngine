using UnityEngine;
using UnityEngine.UIElements;

namespace UIToolkitTest
{
    /// <summary>
    /// Drives the MBTI welcome screen (欢迎页).
    /// Attach to the same GameObject as UIDocument; set the
    /// UIDocument's visualTreeAsset to WelcomeScreen.uxml.
    ///
    /// Listen to OnStartTest / OnViewHistory to navigate to
    /// the next screen (e.g. via UIFactory.CreateUIPrefab).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class WelcomeScreenController : MonoBehaviour
    {
        // ── Marquee config ────────────────────────────────
        [Tooltip("Pixels per second the marquee scrolls left.")]
        [SerializeField] private float marqueeSpeed = 70f;

        // ── Star pulse config ─────────────────────────────
        [Tooltip("Scale amplitude for the star badge pulse (0 = no pulse).")]
        [SerializeField] private float pulseAmplitude = 0.05f;
        [SerializeField] private float pulseFrequency = 1.6f;   // cycles / second

        // ── Private state ─────────────────────────────────
        private UIDocument _doc;
        private VisualElement _marqueeTrack;
        private VisualElement _starBadge;

        private float _marqueeOffset;
        private float _singleLabelWidth;   // width of one copy of the marquee text
        private bool  _marqueeReady;

        // ─────────────────────────────────────────────────
        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var root = _doc.rootVisualElement;
            // Marquee: wait for geometry to resolve before reading widths
            _marqueeTrack = root.Q<VisualElement>("marquee-track");
            if (_marqueeTrack != null)
            {
                _marqueeReady  = false;
                _marqueeOffset = 0f;
                _marqueeTrack.RegisterCallback<GeometryChangedEvent>(OnMarqueeReady);
            }
            _starBadge = root.Q<VisualElement>("star-badge");
        }

        private void OnDisable()
        {
            _marqueeTrack?.UnregisterCallback<GeometryChangedEvent>(OnMarqueeReady);
            _marqueeReady = false;
        }

        // Called once when the marquee track has a resolved width
        private void OnMarqueeReady(GeometryChangedEvent evt)
        {
            // Each half of the track == one copy of the label
            float trackWidth = _marqueeTrack.resolvedStyle.width;
            if (trackWidth <= 0f) return;

            // Track contains two identical labels, so half = one label width
            _singleLabelWidth = trackWidth * 0.5f;
            if (_singleLabelWidth > 0f)
                _marqueeReady = true;

            _marqueeTrack.UnregisterCallback<GeometryChangedEvent>(OnMarqueeReady);
        }

        private void Update()
        {
            UpdateMarquee();
            UpdateStarPulse();
        }

        private void UpdateMarquee()
        {
            if (!_marqueeReady || _marqueeTrack == null) return;

            _marqueeOffset -= marqueeSpeed * Time.deltaTime;

            // Snap back by exactly one label-width for seamless looping
            if (_marqueeOffset <= -_singleLabelWidth)
                _marqueeOffset += _singleLabelWidth;

            _marqueeTrack.style.translate =
                new StyleTranslate(new Translate(_marqueeOffset, 0));
        }

        private void UpdateStarPulse()
        {
            if (_starBadge == null || pulseAmplitude <= 0f) return;

            float t = 1f + pulseAmplitude *
                      Mathf.Sin(Time.time * pulseFrequency * 2f * Mathf.PI);
            _starBadge.style.scale = new StyleScale(new Scale(new Vector2(t, t)));
        }
    }
}
