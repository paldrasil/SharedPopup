using System.Collections;
using UnityEngine;

namespace Shared.Popup
{
    /// <summary>Base cho mọi popup: có sẵn fade in/out, hook background/back.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class PopupBase : MonoBehaviour
    {
        [Header("Popup Base")]
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] float fadeIn = 0.15f;
        [SerializeField] float fadeOut = 0.12f;

        protected PopupOverlayManager Manager { get; private set; }
        public virtual bool IsModal => true;         // ảnh hưởng dim/background block
        public virtual float DimAlpha => 0.6f;       // alpha background khi popup là modal
        public virtual bool CloseOnBackgroundTap => true;
        public virtual bool ConsumeBackButton => true;

        internal void __Bind(PopupOverlayManager mgr) => Manager = mgr;

        /// <summary>Được gọi trước khi Present để set dữ liệu.</summary>
        public virtual void OnBeforePresent(object payload) { }

        /// <summary>Hiệu ứng xuất hiện.</summary>
        public virtual IEnumerator PlayIn()
        {
            if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
            var animator = GetComponent<Animator>();
            if (animator)
            {
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                yield return new WaitForSecondsRealtime(fadeIn);
            }
            else
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = true;
                float t = 0f;
                while (t < fadeIn)
                {
                    t += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Clamp01(t / fadeIn);
                    yield return null;
                }
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
            }
        }

        /// <summary>Hiệu ứng biến mất.</summary>
        public virtual IEnumerator PlayOut()
        {
            if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            float t = 0f;
            float start = canvasGroup.alpha;
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, 0f, t / fadeOut);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }

        /// <summary>Click background. Trả true nếu đã xử lý (consume).</summary>
        public virtual bool OnBackgroundClick()
        {
            if (CloseOnBackgroundTap)
            {
                Manager.Dismiss(this);
                return true;
            }
            return false;
        }

        /// <summary>Back/Escape. Trả true nếu đã xử lý.</summary>
        public virtual bool OnBack()
        {
            if (ConsumeBackButton)
            {
                Manager.Dismiss(this);
                return true;
            }
            return false;
        }
    }
}
