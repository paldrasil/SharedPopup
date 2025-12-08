using System.Collections.Generic;
using Shared.Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace Shared.Popup
{
    /// <summary>Quản lý toast messages: queue, positioning, stacking.</summary>
    public class ToastManager : MonoBehaviour
    {
        [Header("Hierarchy Refs")]
        [SerializeField] Canvas toastCanvas;          // Canvas riêng cho toast (hoặc dùng chung)
        [SerializeField] RectTransform container;     // Container để spawn toasts
        [SerializeField] ObjectsPool uiPool;

        [Header("Positioning")]
        [SerializeField] ToastPosition position = ToastPosition.TopCenter;
        [SerializeField] float spacing = 10f;         // Khoảng cách giữa các toast
        [SerializeField] float maxWidth = 400f;      // Max width của toast
        [SerializeField] float paddingTop = 50f;     // Padding từ edge
        [SerializeField] float paddingSide = 20f;

        [Header("Queue Settings")]
        [SerializeField] int maxConcurrentToasts = 5; // Số toast tối đa hiển thị cùng lúc
        [SerializeField] bool sequentialDisplay = false; // Hiển thị tuần tự hay cùng lúc

        private readonly Queue<ToastData> _queue = new Queue<ToastData>();
        private readonly List<ToastItem> _activeToasts = new List<ToastItem>();

        const string TOAST_PREFAB_KEY = "ToastItem";

        void Awake()
        {
            if (!toastCanvas)
            {
                // Try to find existing canvas
                toastCanvas = GetComponentInChildren<Canvas>(true);
                if (!toastCanvas)
                {
                    // Create canvas if not found
                    var canvasGO = new GameObject("ToastCanvas");
                    canvasGO.transform.SetParent(transform);
                    toastCanvas = canvasGO.AddComponent<Canvas>();
                    toastCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    toastCanvas.sortingOrder = 1000; // High sorting order
                    canvasGO.AddComponent<CanvasScaler>();
                    canvasGO.AddComponent<GraphicRaycaster>();
                }
            }

            if (!container)
            {
                var containerGO = new GameObject("Container");
                containerGO.transform.SetParent(toastCanvas.transform, false);
                container = containerGO.AddComponent<RectTransform>();
                SetupContainer();
            }
            else
            {
                SetupContainer();
            }
        }

        void SetupContainer()
        {
            // Setup container anchors based on position
            switch (position)
            {
                case ToastPosition.TopCenter:
                    container.anchorMin = new Vector2(0.5f, 1f);
                    container.anchorMax = new Vector2(0.5f, 1f);
                    container.pivot = new Vector2(0.5f, 1f);
                    container.anchoredPosition = new Vector2(0f, -paddingTop);
                    break;
                case ToastPosition.TopLeft:
                    container.anchorMin = new Vector2(0f, 1f);
                    container.anchorMax = new Vector2(0f, 1f);
                    container.pivot = new Vector2(0f, 1f);
                    container.anchoredPosition = new Vector2(paddingSide, -paddingTop);
                    break;
                case ToastPosition.TopRight:
                    container.anchorMin = new Vector2(1f, 1f);
                    container.anchorMax = new Vector2(1f, 1f);
                    container.pivot = new Vector2(1f, 1f);
                    container.anchoredPosition = new Vector2(-paddingSide, -paddingTop);
                    break;
                case ToastPosition.BottomCenter:
                    container.anchorMin = new Vector2(0.5f, 0f);
                    container.anchorMax = new Vector2(0.5f, 0f);
                    container.pivot = new Vector2(0.5f, 0f);
                    container.anchoredPosition = new Vector2(0f, paddingTop);
                    break;
                case ToastPosition.BottomLeft:
                    container.anchorMin = new Vector2(0f, 0f);
                    container.anchorMax = new Vector2(0f, 0f);
                    container.pivot = new Vector2(0f, 0f);
                    container.anchoredPosition = new Vector2(paddingSide, paddingTop);
                    break;
                case ToastPosition.BottomRight:
                    container.anchorMin = new Vector2(1f, 0f);
                    container.anchorMax = new Vector2(1f, 0f);
                    container.pivot = new Vector2(1f, 0f);
                    container.anchoredPosition = new Vector2(-paddingSide, paddingTop);
                    break;
            }
        }

        /// <summary>Hiển thị toast message.</summary>
        public void Show(string message, ToastType type = ToastType.Info, float duration = 3f)
        {
            Show(new ToastData(message, type, duration));
        }

        /// <summary>Hiển thị toast với data.</summary>
        public void Show(ToastData data)
        {
            if (data == null || string.IsNullOrEmpty(data.message)) return;

            if (sequentialDisplay && _activeToasts.Count > 0)
            {
                // Thêm vào queue nếu đang hiển thị tuần tự
                _queue.Enqueue(data);
            }
            else if (_activeToasts.Count >= maxConcurrentToasts)
            {
                // Thêm vào queue nếu đã đạt max concurrent
                _queue.Enqueue(data);
            }
            else
            {
                // Spawn ngay lập tức
                SpawnToast(data);
            }
        }

        void SpawnToast(ToastData data)
        {
            if (!uiPool) return;

            var prefabGO = uiPool.FindPrefab(TOAST_PREFAB_KEY);
            if (!prefabGO)
            {
                Debug.LogWarning($"[ToastManager] Prefab '{TOAST_PREFAB_KEY}' not found in ObjectsPool!");
                return;
            }

            var prefab = prefabGO.GetComponent<ToastItem>();
            if (!prefab)
            {
                Debug.LogWarning($"[ToastManager] Prefab '{TOAST_PREFAB_KEY}' doesn't have ToastItem component!");
                return;
            }

            // Spawn từ pool
            var toastGO = uiPool.Spawn(TOAST_PREFAB_KEY, Vector3.zero, Quaternion.identity, -1f, container);
            var toast = toastGO.GetComponent<ToastItem>();
            if (!toast)
            {
                Debug.LogWarning($"[ToastManager] Spawned object doesn't have ToastItem component!");
                uiPool.Despawn(toastGO);
                return;
            }

            // Setup toast item
            _activeToasts.Add(toast);
            toast.Setup(data, () => OnToastDismissed(toast));

            // Update positions
            UpdateToastPositions();
        }

        void OnToastDismissed(ToastItem toast)
        {
            _activeToasts.Remove(toast);
            if (uiPool && toast)
            {
                uiPool.Despawn(toast.gameObject);
            }

            // Process queue
            if (_queue.Count > 0 && _activeToasts.Count < maxConcurrentToasts)
            {
                var next = _queue.Dequeue();
                SpawnToast(next);
            }
            else
            {
                UpdateToastPositions();
            }
        }

        void UpdateToastPositions()
        {
            float currentOffset = 0f;
            bool isTop = position == ToastPosition.TopCenter || 
                         position == ToastPosition.TopLeft || 
                         position == ToastPosition.TopRight;

            for (int i = 0; i < _activeToasts.Count; i++)
            {
                var toast = _activeToasts[i];
                if (!toast) continue;

                var rt = toast.GetComponent<RectTransform>();
                if (!rt) continue;

                // Setup toast item layout based on container anchor
                float anchorY = isTop ? 1f : 0f;
                rt.anchorMin = new Vector2(0.5f, anchorY);
                rt.anchorMax = new Vector2(0.5f, anchorY);
                rt.pivot = new Vector2(0.5f, anchorY);
                rt.sizeDelta = new Vector2(maxWidth, rt.sizeDelta.y);

                // Position based on anchor (for top: negative offset, for bottom: positive offset)
                float yOffset = isTop ? -currentOffset : currentOffset;
                rt.anchoredPosition = new Vector2(0f, yOffset);

                // Update offset for next toast (consider height + spacing)
                float height = rt.sizeDelta.y > 0 ? rt.sizeDelta.y : 60f; // Default height fallback
                currentOffset += height + spacing;
            }
        }

        /// <summary>Dismiss tất cả toasts.</summary>
        public void DismissAll()
        {
            var toasts = new List<ToastItem>(_activeToasts);
            foreach (var toast in toasts)
            {
                toast.Dismiss();
            }
            _activeToasts.Clear();
            _queue.Clear();
        }

        void OnDestroy()
        {
            DismissAll();
        }
    }

    /// <summary>Vị trí hiển thị toast trên màn hình.</summary>
    public enum ToastPosition
    {
        TopCenter,
        TopLeft,
        TopRight,
        BottomCenter,
        BottomLeft,
        BottomRight
    }
}

