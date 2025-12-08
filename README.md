# Shared Popup

Popup and toast message management system for UI in Paldrasil games.

## Features

### Popup System
- **PopupBase**: Base class for all popups with automatic fade in/out
- **PopupOverlayManager**: Manages popup stack with background dimming
- Supports modal/non-modal popups
- Automatic back button (Escape) handling
- Background click handling
- Stack-based popup management

### Toast System
- **ToastManager**: Manages toast messages
- **ToastItem**: Component for toast item
- **ToastData**: Data structure for toast
- Queue system support
- Positioning options (Top/Bottom, Left/Center/Right)
- Concurrent toast limit
- Sequential/parallel display modes

## Dependencies

- `com.paldrasil.shared.foundation`: Uses `ObjectsPool` from foundation package

## Installation

This package requires the `com.paldrasil.shared.foundation` package.

## Usage

### Popup System

#### Creating a Popup

```csharp
using Shared.Popup;
using UnityEngine;

public class MyPopup : PopupBase
{
    public override void OnBeforePresent(object payload)
    {
        // Setup data before displaying
        if (payload is MyData data)
        {
            // Initialize UI with data
        }
    }
    
    // Override properties if needed
    public override bool IsModal => true;
    public override float DimAlpha => 0.7f;
    public override bool CloseOnBackgroundTap => true;
    public override bool ConsumeBackButton => true;
}
```

#### Using PopupOverlayManager

```csharp
using Shared.Popup;

// Get manager instance
var popupManager = FindObjectOfType<PopupOverlayManager>();

// Open popup
var popup = popupManager.Present<MyPopup>(payload: myData);

// Or with prefab key
var popup = popupManager.Present<MyPopup>("MyPopupPrefab", payload: myData);

// Close popup
popupManager.Dismiss(popup);

// Close top popup
popupManager.DismissTop();

// Close all
popupManager.DismissAll();

// Check if popup is open
if (popupManager.Has<MyPopup>())
{
    // Popup is open
}
```

### Toast System

#### Setup ToastManager

1. Create a GameObject with `ToastManager` component
2. Setup `ObjectsPool` with "ToastItem" prefab
3. Configure position, spacing, and queue settings

#### Displaying Toast

```csharp
using Shared.Popup;

// Get ToastManager instance
var toastManager = FindObjectOfType<ToastManager>();

// Show simple toast
toastManager.Show("Hello World!", ToastType.Info, duration: 3f);

// Show toast with data
var toastData = new ToastData("Success!", ToastType.Success, 2f);
toastManager.Show(toastData);

// Dismiss all toasts
toastManager.DismissAll();
```

#### Toast Types

```csharp
public enum ToastType
{
    Info,
    Success,
    Warning,
    Error
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

## Structure

### Popup System
- **PopupBase**: Abstract base class for popups
- **PopupOverlayManager**: Manager component for popup stack

### Toast System
- **ToastManager**: Manager component for toasts
- **ToastItem**: Component for toast item UI
- **ToastData**: Data class for toast
- **ToastType**: Enum for toast type

## Requirements

- Unity 6000.2 or higher
- `com.paldrasil.shared.foundation` package

## License

Internal package for Paldrasil games.
