using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shared.Popup
{
    /// <summary>UI component cho một toast message. Tự động dismiss sau duration.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(RectTransform))]
    public class ToastItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] Image backgroundImage;
        [SerializeField] Image iconImage;
        [SerializeField] TextMeshProUGUI messageText;
        [SerializeField] Button actionButton;
        [SerializeField] TextMeshProUGUI actionButtonText;

        [Header("Animation")]
        [SerializeField] float fadeInDuration = 0.2f;
        [SerializeField] float fadeOutDuration = 0.15f;
        [SerializeField] bool slideAnimation = true;
        [SerializeField] float slideDistance = 50f;

        [Header("Type Colors")]
        [SerializeField] Color infoColor = new Color(0.2f, 0.4f, 0.8f, 0.9f);
        [SerializeField] Color successColor = new Color(0.2f, 0.8f, 0.3f, 0.9f);
        [SerializeField] Color warningColor = new Color(1f, 0.8f, 0.2f, 0.9f);
        [SerializeField] Color errorColor = new Color(0.9f, 0.2f, 0.2f, 0.9f);

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private Coroutine _dismissCoroutine;
        private System.Action _onDismissed;
        private System.Action _onAction;

        void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();

            if (actionButton)
            {
                actionButton.onClick.AddListener(OnActionClicked);
            }
        }

        /// <summary>Setup toast với data và callback khi dismissed.</summary>
        public void Setup(ToastData data, System.Action onDismissed = null)
        {
            _onDismissed = onDismissed;

            // Set message
            if (messageText)
            {
                messageText.text = data.message ?? "";
            }

            // Set icon
            if (iconImage)
            {
                iconImage.sprite = data.icon;
                iconImage.gameObject.SetActive(data.icon != null);
            }

            // Set background color theo type
            if (backgroundImage)
            {
                backgroundImage.color = GetColorForType(data.type);
            }

            // Setup action button
            _onAction = data.onAction;
            if (actionButton)
            {
                bool hasAction = !string.IsNullOrEmpty(data.actionText) && data.onAction != null;
                actionButton.gameObject.SetActive(hasAction);
                if (hasAction && actionButtonText)
                {
                    actionButtonText.text = data.actionText;
                }
            }

            // Start auto-dismiss
            if (data.duration > 0f)
            {
                if (_dismissCoroutine != null)
                {
                    StopCoroutine(_dismissCoroutine);
                }
                _dismissCoroutine = StartCoroutine(Co_AutoDismiss(data.duration));
            }

            // Play fade in animation
            StartCoroutine(Co_FadeIn());
        }

        /// <summary>Manually dismiss toast.</summary>
        public void Dismiss()
        {
            if (_dismissCoroutine != null)
            {
                StopCoroutine(_dismissCoroutine);
                _dismissCoroutine = null;
            }
            StartCoroutine(Co_FadeOutAndDestroy());
        }

        void OnActionClicked()
        {
            _onAction?.Invoke();
            // Action button click không tự động dismiss toast
            // Có thể gọi Dismiss() từ callback nếu cần
        }

        Color GetColorForType(ToastType type)
        {
            return type switch
            {
                ToastType.Success => successColor,
                ToastType.Warning => warningColor,
                ToastType.Error => errorColor,
                _ => infoColor
            };
        }

        IEnumerator Co_AutoDismiss(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            yield return Co_FadeOutAndDestroy();
        }

        IEnumerator Co_FadeIn()
        {
            if (!_canvasGroup) yield break;

            // Get final target position (current position)
            Vector2 targetPos = Vector2.zero;// _rectTransform.anchoredPosition;
            
            // Calculate start position (offset from target)
            Vector2 startPos = targetPos;
            if (slideAnimation)
            {
                Vector2 slideOffset = GetSlideDirection() * slideDistance;
                startPos += slideOffset;
            }

            // Set initial state
            _rectTransform.anchoredPosition = startPos;
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = true;

            // Animate to target
            float t = 0f;
            while (t < fadeInDuration)
            {
                t += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(t / fadeInDuration);

                _canvasGroup.alpha = progress;
                if (slideAnimation)
                {
                    _rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, progress);
                }

                yield return null;
            }

            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            if (slideAnimation)
            {
                _rectTransform.anchoredPosition = targetPos;
            }
        }

        IEnumerator Co_FadeOutAndDestroy()
        {
            if (!_canvasGroup) yield break;

            _canvasGroup.interactable = false;
            float startAlpha = _canvasGroup.alpha;
            Vector2 startPos = _rectTransform.anchoredPosition;
            Vector2 targetPos = startPos;
            if (slideAnimation)
            {
                targetPos += GetSlideDirection() * slideDistance;
            }

            float t = 0f;
            while (t < fadeOutDuration)
            {
                t += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(t / fadeOutDuration);

                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
                if (slideAnimation)
                {
                    _rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, progress);
                }

                yield return null;
            }

            _canvasGroup.alpha = 0f;

            _onDismissed?.Invoke();
        }

        Vector2 GetSlideDirection()
        {
            // Determine slide direction based on anchor position
            Vector2 anchor = _rectTransform.anchorMin;
            if (anchor.y >= 0.5f) // Top
            {
                return Vector2.up;
            }
            else // Bottom
            {
                return Vector2.down;
            }
        }

        void OnDestroy()
        {
            if (_dismissCoroutine != null)
            {
                StopCoroutine(_dismissCoroutine);
            }
        }
    }
}

