using System.Collections;
using System.Collections.Generic;
using Shared.Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace Shared.Popup
{
    /// <summary>Quản lý overlay popup theo stack + background dim.</summary>
    public class PopupOverlayManager : MonoBehaviour
    {
        [Header("Hierarchy Refs")]
        [SerializeField] Canvas overlayCanvas;            // sorting order cao
        [SerializeField] RectTransform container;         // nơi chứa popup
        [SerializeField] Image backgroundDim;             // Image full-screen
        [SerializeField] CanvasGroup backgroundGroup;     // để fade/almost-block
        [SerializeField] ObjectsPool uiPool;

        [Header("Background")]
        [SerializeField] float dimFade = 0.15f;           // thời gian fade background
        [SerializeField] bool blockInputWhenDimmed = true;

        class Entry
        {
            public PopupBase popup;
            public bool modal;
            public float dim;
            public string key;     // optional: ngăn mở trùng
        }

        readonly List<Entry> _stack = new();

        void Awake()
        {
            if (!overlayCanvas) overlayCanvas = GetComponentInChildren<Canvas>(true);
            if (!container) container = overlayCanvas.transform.Find("Container") as RectTransform;
            if (!backgroundDim) backgroundDim = overlayCanvas.transform.Find("Background").GetComponent<Image>();
            if (!backgroundGroup) backgroundGroup = backgroundDim.GetComponent<CanvasGroup>();

            // Lắng nghe click background
            var btn = backgroundDim.GetComponent<Button>();
            if (!btn) btn = backgroundDim.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(OnBackgroundClicked);

            // Start state
            backgroundGroup.alpha = 0f;
            backgroundDim.raycastTarget = false;
        }

        void Update()
        {
            // Back/Escape → gửi cho popup top
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_stack.Count > 0)
                {
                    var top = _stack[_stack.Count - 1].popup;
                    if (!top.OnBack())
                    {
                        // nếu top không consume: có thể chuyển cho popup dưới (tuỳ ý)
                        for (int i = _stack.Count - 2; i >= 0; --i)
                        {
                            if (_stack[i].popup.OnBack()) break;
                        }
                    }
                }
            }
        }

        public T Present<T>(object payload = null) where T : PopupBase
        {
            var type = typeof(T);
            string prefabKey = type.Name;
            var prefabGO = uiPool.FindPrefab(prefabKey);
            var prefab = prefabGO.GetComponent<T>();
            if (prefab == null) return default(T);
            return Present(prefab, payload, prefabKey);
        }

        public T Present<T>(string prefabKey, object payload = null) where T : PopupBase
        {
            var prefabGO = uiPool.FindPrefab(prefabKey);
            var prefab = prefabGO.GetComponent<T>();
            if (prefab == null) return default(T);
            return Present(prefab, payload, prefabKey);
        }

        /// <summary>Mở popup prefab. payload tùy ý, key để chống mở trùng.</summary>
        public T Present<T>(T prefab, object payload = null, string key = null) where T : PopupBase
        {
            if (!container) container = overlayCanvas.transform as RectTransform;

            if (!string.IsNullOrEmpty(key))
            {
                for (int i = 0; i < _stack.Count; i++)
                {
                    if (_stack[i].key == key) return _stack[i].popup as T;
                }
            }

            var inst = Instantiate(prefab, container);
            inst.transform.SetAsLastSibling();
            inst.__Bind(this);
            inst.OnBeforePresent(payload);
            StartCoroutine(Co_Open(inst));

            var e = new Entry
            {
                popup = inst,
                modal = inst.IsModal,
                dim = Mathf.Clamp01(inst.DimAlpha),
                key = key
            };
            _stack.Add(e);
            UpdateBackground();
            return inst;
        }

        /// <summary>Đóng popup cụ thể.</summary>
        public void Dismiss(PopupBase popup)
        {
            if (popup == null) return;
            int idx = _stack.FindIndex(e => e.popup == popup);
            if (idx < 0) return;
            StartCoroutine(Co_CloseAt(idx));
        }

        /// <summary>Đóng popup top nếu có.</summary>
        public void DismissTop()
        {
            if (_stack.Count == 0) return;
            StartCoroutine(Co_CloseAt(_stack.Count - 1));
        }

        /// <summary>Đóng tất cả popup.</summary>
        public void DismissAll()
        {
            // đóng từ top xuống để hiệu ứng mượt
            StartCoroutine(Co_CloseAll());
        }

        /// <summary>Kiểm tra đã có popup kiểu T trong stack.</summary>
        public bool Has<T>() where T : PopupBase
        {
            for (int i = 0; i < _stack.Count; i++)
                if (_stack[i].popup is T) return true;
            return false;
        }

        IEnumerator Co_Open(PopupBase popup)
        {
            yield return popup.PlayIn();
        }

        IEnumerator Co_CloseAt(int index)
        {
            var entry = _stack[index];
            // Pop và re-evaluate background ngay trước khi hiệu ứng out (cho cảm giác responsive)
            _stack.RemoveAt(index);
            UpdateBackground();

            yield return entry.popup.PlayOut();
            if (entry.popup)
            {
                uiPool.Despawn(entry.popup.gameObject);
                //Destroy(entry.popup.gameObject);
            }
        }

        IEnumerator Co_CloseAll()
        {
            for (int i = _stack.Count - 1; i >= 0; --i)
            {
                var p = _stack[i].popup;
                yield return p.PlayOut();
                if (p)
                {
                    uiPool.Despawn(p.gameObject);
                    //Destroy(p.gameObject);
                }
            }
            _stack.Clear();
            UpdateBackground();
        }

        void OnBackgroundClicked()
        {
            if (_stack.Count == 0) return;

            // Ưu tiên popup top
            for (int i = _stack.Count - 1; i >= 0; --i)
            {
                var p = _stack[i].popup;
                if (p.OnBackgroundClick()) break; // đã consume
            }
        }

        void UpdateBackground()
        {
            // Lấy dim của popup modal gần top nhất (nếu có)
            float targetAlpha = 0f;
            bool shouldBlock = false;

            if(_stack.Count > 0)
            {
                backgroundGroup.transform.SetSiblingIndex(_stack.Count - 1);
            }
           
            for (int i = _stack.Count - 1; i >= 0; --i)
            {
                if (_stack[i].modal)
                {
                    targetAlpha = _stack[i].dim;
                    shouldBlock = true;
                    break;
                }
            }

            StopCoroutine(nameof(Co_FadeBackground));
            StartCoroutine(Co_FadeBackground(targetAlpha, shouldBlock && blockInputWhenDimmed));
        }

        IEnumerator Co_FadeBackground(float target, bool block)
        {
            float start = backgroundGroup.alpha;
            float t = 0f;
            while (t < dimFade)
            {
                t += Time.unscaledDeltaTime;
                backgroundGroup.alpha = Mathf.Lerp(start, target, t / dimFade);
                yield return null;
            }
            backgroundGroup.alpha = target;
            backgroundDim.raycastTarget = block && target > 0.001f;
        }
    }
}
