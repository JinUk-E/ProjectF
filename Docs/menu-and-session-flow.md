# Main menu and session flow

Actor: main-codex / editor-tooling-codex. Requested 2026-10-20.

The main menu provides entry to the existing AI match and local two-player match, controls, and a confirmed application exit. Online matchmaking is outside this change.

During a match, Escape or the menu button opens pause. Continue resumes the current shot/AI thinking state. Returning to the main menu or quitting requires confirmation; cancelling keeps the pause menu open. Leaving discards the current match and releases its shot, terrain freeze, input suspension and paused time/audio state. Match results also provide a route back to the main menu.

The menu uses the existing original military uGUI style. Run `Fortress.Editor.MenuFlowBuilder.Upgrade()` in edit mode to generate the main scene and install match overlays through Unity. `Fortress.Editor.MenuFlowBuilder.Play()` starts at the main menu. The main scene is first in build settings; existing scenes are retained.

In the Unity Editor, confirmed application exit stops Play mode. There is no saved mid-match continuation.

Validation: the initial 74-case run exposed an obsolete single-Canvas assertion and AI replanning during scene destruction. Both were corrected and the full 74-case run passed (146.076 seconds). A subsequent late-turn/pause race fix and its new test were validated with all 12 SessionMenu/AI tests passing (43.127 seconds). The final total suite contains 75 cases; it was not rerun as one 75-case job.

Actual renders checked the main screen, manual, pause, confirmation and result panels. Result-to-menu button navigation and confirmed Editor quit were exercised. Screenshots and test outputs are in `Docs/Validation/menu-*`. Standalone Windows executable exit was not tested. Wide-window inspection caught and corrected initial CanvasScaler double scaling; the main composition now fits the available window.
