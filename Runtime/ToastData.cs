using UnityEngine;

namespace Shared.Popup
{
    /// <summary>Data class để truyền thông tin cho toast message.</summary>
    public class ToastData
    {
        public string message;
        public ToastType type = ToastType.Info;
        public float duration = 3f;
        public Sprite icon;           // Optional: icon hiển thị
        public string actionText;     // Optional: text cho action button
        public System.Action onAction; // Optional: callback khi click action button

        public ToastData()
        {
        }

        public ToastData(string message, ToastType type = ToastType.Info, float duration = 3f)
        {
            this.message = message;
            this.type = type;
            this.duration = duration;
        }
    }
}

