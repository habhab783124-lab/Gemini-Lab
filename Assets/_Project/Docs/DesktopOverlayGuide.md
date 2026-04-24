# Desktop Overlay Guide

## Overview
- `DesktopOverlayManager` controls Apartment/Overlay mode switching.
- `WindowModeAdapter` abstracts window behavior (mode + click-through).
- `ForegroundWindowProbe` polls active process context.
- `AppContextRuleEngine` converts foreground app context into pet command suggestions.

## Runtime Controls
- Press `F10` to toggle between apartment mode and desktop overlay mode.
- Overlay mode enables click-through behavior via `WindowModeAdapter`.

## Integration Points
- Foreground change event: `ForegroundApplicationChangedEvent`
- Overlay mode event: `OverlayModeChangedEvent`
- Rule engine emits work command requests through `IPetCommandLinkService`.

## Platform Notes
- Current implementation is platform-safe and does not require native plugin bindings.
- Windows-native behavior (transparent window / z-order / extended frame) can be added behind `IWindowModeAdapter` without affecting game logic.
