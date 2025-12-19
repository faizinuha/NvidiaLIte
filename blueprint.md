# Blueprint: Tray Icon WPF App

## Overview

This application is a .NET 9 WPF utility that runs in the background. It provides a system tray icon for primary interaction and can display a non-intrusive, always-on-top sidebar overlay on the right side of the screen. The application is designed to be lightweight and starts without a main window.

## Features & Design

### Core
- **Framework:** WPF on .NET 9.
- **Main Window:** None. The application starts and runs in the background.
- **Shutdown Mode:** The application only shuts down when the "Exit" context menu item is clicked from the tray icon.

### Tray Icon
- **Library:** Uses `System.Windows.Forms.NotifyIcon` for the tray icon.
- **Icon:** A custom icon is loaded from the `Assets/` directory.
- **Interactions:**
    - **Double-Click:** Toggles the visibility of the overlay window.
    - **Right-Click (Context Menu):**
        - "Show/Hide Overlay": Toggles the visibility of the overlay window.
        - "Exit": Shuts down the application.

### Overlay Window (`OverlayWindow.xaml`)
- **Appearance:**
    - **Borderless:** `WindowStyle="None"`.
    - **Transparent:** `AllowsTransparency="True"` with a semi-transparent `Background`.
    - **Always-on-Top:** `Topmost="True"`.
    - **No Taskbar Icon:** `ShowInTaskbar="False"`.
- **Positioning:**
    - Automatically docks to the right side of the primary screen.
    - Fills the entire height of the screen's working area.
- **Behavior:**
    - Hidden on application startup.
    - Visibility is controlled via methods in `App.xaml.cs`.
    - Contains a "Close" button that hides the window rather than closing it.

## Current Plan

This is the initial setup of the application. The plan is to establish the foundational structure as described above.

**Steps:**
1.  **Project Scaffolding:** Create the file structure for a new WPF application.
2.  **Tray Icon Logic:** Implement `App.xaml.cs` to manage the tray icon and its context menu.
3.  **Overlay Window UI:** Define the visual structure of the overlay in `OverlayWindow.xaml`.
4.  **Overlay Window Logic:** Implement the code-behind (`OverlayWindow.xaml.cs`) to handle positioning and basic interactions.
5.  **Application Entry Point:** Configure `App.xaml` to start the application without a primary window.
