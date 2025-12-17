# Shared Popup

Popup and toast message management system for UI in Paldrasil games, providing stack-based popup management with background dimming and flexible toast notifications.

## Features

### Popup System
- **PopupBase**: Abstract base class for all popups with automatic fade in/out animations
- **PopupOverlayManager**: Manages popup stack with background dimming and input blocking
- **Stack-based Management**: Multiple popups can be stacked, with proper z-ordering
- **Modal/Non-modal Support**: Control background dimming and input blocking per popup
- **Automatic Back Button Handling**: Escape key support with consumption control
- **Background Click Handling**: Configurable background tap to close
- **Key-based Duplicate Prevention**: Prevent opening duplicate popups using keys
- **ObjectsPool Integration**: Uses `ObjectsPool` from foundation package for efficient instantiation

### Toast System
- **ToastManager**: Manages toast message queue and display
- **ToastItem**: UI component for individual toast messages
- **ToastData**: Data structure for toast configuration
- **Queue System**: Automatic queuing when max concurrent toasts reached
- **Positioning Options**: 6 positions (Top/Bottom × Left/Center/Right)
- **Concurrent Limit**: Configurable maximum concurrent toasts
- **Sequential/Parallel Display**: Choose between sequential or parallel toast display
- **Auto-dismiss**: Automatic dismissal after duration
- **Slide Animation**: Optional slide-in/slide-out animations
- **Action Button Support**: Optional action button with callback

## Dependencies

- `com.paldrasil.shared.foundation`: Uses `ObjectsPool` for popup and toast instantiation

## Installation

This package requires the `com.paldrasil.shared.foundation` package to be installed.

## Usage

### Popup System

#### Setup PopupOverlayManager

1. Create a GameObject with `PopupOverlayManager` component
2. Assign references:
   - `overlayCanvas`: Canvas with high sorting order
   - `container`: RectTransform where popups will be instantiated
   - `backgroundDim`: Image component for background dimming
   - `backgroundGroup`: CanvasGroup for fade animation
   - `uiPool`: ObjectsPool instance with popup prefabs registered

#### Creating a Popup

```csharp
using Shared.Popup;
using UnityEngine;

public class MyPopup : PopupBase
{
    // Override to setup data before displaying
    public override void OnBeforePresent(object payload)
    {
        if (payload is MyData data)
        {
            // Initialize UI with data
            // e.g., SetText(data.message);
        }
    }
    
    // Configure popup behavior
    public override bool IsModal => true;              // Affects background dimming
    public override float DimAlpha => 0.7f;           // Background dim alpha (0-1)
    public override bool CloseOnBackgroundTap => true; // Close when background clicked
    public override bool ConsumeBackButton => true;    // Handle Escape key
    
    // Optional: Customize animations
    // Override PlayIn() and PlayOut() for custom animations
}
```

#### Using PopupOverlayManager

```csharp
using Shared.Popup;

// Get manager instance
var popupManager = FindObjectOfType<PopupOverlayManager>();

// Open popup (auto-finds prefab by type name)
var popup = popupManager.Present<MyPopup>(payload: myData);

// Open popup with specific prefab key
var popup = popupManager.Present<MyPopup>("MyPopupPrefab", payload: myData);

// Open popup with duplicate prevention key
var popup = popupManager.Present<MyPopup>(prefab, payload: myData, key: "unique_key");
// If popup with same key exists, returns existing popup instead of creating new one

// Close specific popup
popupManager.Dismiss(popup);

// Close top popup
popupManager.DismissTop();

// Close all popups
popupManager.DismissAll();

// Check if popup is open
if (popupManager.Has<MyPopup>())
{
    // Popup is open
}

// Get top popup
var topPopup = popupManager.GetTopPopup();
```

#### PopupBase Properties

```csharp
public abstract class PopupBase : MonoBehaviour
{
    // Override these properties to customize behavior
    public virtual bool IsModal => true;              // Modal popups dim background
    public virtual float DimAlpha => 0.6f;           // Background dim alpha
    public virtual bool CloseOnBackgroundTap => true; // Close on background click
    public virtual bool ConsumeBackButton => true;    // Handle Escape key
    
    // Methods to override
    public virtual void OnBeforePresent(object payload) { }
    public virtual IEnumerator PlayIn() { /* fade in */ }
    public virtual IEnumerator PlayOut() { /* fade out */ }
    public virtual bool OnBackgroundClick() { /* return true if handled */ }
    public virtual bool OnBack() { /* return true if handled */ }
}
```

### Toast System

#### Setup ToastManager

1. Create a GameObject with `ToastManager` component
2. Assign references:
   - `toastCanvas`: Canvas for toast display (auto-created if not assigned)
   - `container`: RectTransform container for toasts (auto-created if not assigned)
   - `uiPool`: ObjectsPool instance with "ToastItem" prefab registered
3. Configure settings:
   - `position`: Toast position (TopCenter, TopLeft, TopRight, BottomCenter, BottomLeft, BottomRight)
   - `spacing`: Spacing between toasts
   - `maxWidth`: Maximum width of toast
   - `paddingTop`: Padding from screen edge
   - `paddingSide`: Side padding
   - `maxConcurrentToasts`: Maximum toasts displayed simultaneously
   - `sequentialDisplay`: Display toasts sequentially or in parallel

#### Displaying Toast

```csharp
using Shared.Popup;

// Get ToastManager instance
var toastManager = FindObjectOfType<ToastManager>();

// Show simple toast
toastManager.Show("Hello World!", ToastType.Info, duration: 3f);

// Show toast with different types
toastManager.Show("Operation successful!", ToastType.Success, duration: 2f);
toastManager.Show("Warning message", ToastType.Warning, duration: 4f);
toastManager.Show("Error occurred!", ToastType.Error, duration: 5f);

// Show toast with ToastData
var toastData = new ToastData
{
    message = "Custom toast message",
    type = ToastType.Success,
    duration = 3f,
    icon = myIconSprite,
    actionText = "Undo",
    onAction = () => { /* Handle action */ }
};
toastManager.Show(toastData);

// Dismiss all toasts
toastManager.DismissAll();
```

#### Toast Types

```csharp
public enum ToastType
{
    Info,       // General information (blue)
    Success,    // Success message (green)
    Warning,    // Warning message (yellow)
    Error       // Error message (red)
}
```

#### Toast Positions

```csharp
public enum ToastPosition
{
    TopCenter,
    TopLeft,
    TopRight,
    BottomCenter,
    BottomLeft,
    BottomRight
}
```

#### ToastItem Customization

ToastItem component supports:
- Custom background colors per type
- Icon display
- Action button with callback
- Fade in/out animations
- Slide animations (optional)
- Auto-dismiss after duration

## Structure

### Popup System

- **PopupBase**: Abstract base class for popups
  - Handles fade in/out animations
  - Manages background click and back button events
  - Provides hooks for customization
  - Requires `CanvasGroup` component

- **PopupOverlayManager**: Manager component for popup stack
  - Manages popup stack with proper z-ordering
  - Handles background dimming and input blocking
  - Processes Escape key input
  - Prevents duplicate popups using keys
  - Integrates with ObjectsPool for instantiation

### Toast System

- **ToastManager**: Manager component for toasts
  - Manages toast queue and active toasts
  - Handles positioning and spacing
  - Supports sequential and parallel display modes
  - Auto-creates canvas and container if not assigned

- **ToastItem**: UI component for toast message
  - Displays message, icon, and action button
  - Handles fade and slide animations
  - Auto-dismisses after duration
  - Requires `CanvasGroup` and `RectTransform` components

- **ToastData**: Data class for toast configuration
  - Contains message, type, duration
  - Optional icon, action text, and callback

- **ToastType**: Enum for toast type (Info, Success, Warning, Error)

- **ToastPosition**: Enum for toast position (6 positions)

## Requirements

- Unity 6000.2 or higher
- `com.paldrasil.shared.foundation` package

## License

Internal package for Paldrasil games.
