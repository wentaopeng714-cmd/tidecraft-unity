using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
namespace Tidecraft
{
    public sealed class SandInput : MonoBehaviour
    {
        SandApp app;
        bool gesture, waitRelease, pointerDown, lastMouseCamera;
        Vector2 lastMid, lastVector, lastMouse;
        public void Initialize(SandApp owner) { app = owner; EnhancedTouchSupport.Enable(); }
        void Update()
        {
            if (!app || !app.Ready) return;
            var touches = Touch.activeTouches;
            if (touches.Count > 0)
            {
                if (touches.Count >= 2)
                {
                    if (pointerDown) { app.PointerEnd(true); pointerDown = false; }
                    Vector2 a = touches[0].screenPosition, b = touches[1].screenPosition, mid = (a + b) / 2, v = b - a;
                    if (!gesture)
                    {
                        waitRelease = app.Hud.IsOverUI(a) || app.Hud.IsOverUI(b); gesture = true;
                    }
                    else if (!waitRelease) app.View.Gesture(mid - lastMid, v.magnitude / Mathf.Max(10, lastVector.magnitude), Vector2.SignedAngle(lastVector, v));
                    lastMid = mid; lastVector = v; return;
                }
                if (gesture || waitRelease) return; // Lifting one finger never paints accidentally.
                var touch = touches[0]; Vector2 pos = touch.screenPosition;
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    if (!app.Hud.IsOverUI(pos)) { pointerDown = true; app.PointerBegin(pos, true); }
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended || touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    if (pointerDown && touch.phase == UnityEngine.InputSystem.TouchPhase.Ended) app.PointerMove(pos, true);
                    if (pointerDown) app.PointerEnd(touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled);
                    pointerDown = false;
                }
                else if (pointerDown) app.PointerMove(pos, true);
                return;
            }
            if (gesture || waitRelease) { gesture = waitRelease = false; return; }
            if (Touchscreen.current != null && pointerDown && Mouse.current == null) { app.PointerEnd(false); pointerDown = false; }
            var mouse = Mouse.current; if (mouse == null) return;
            Vector2 p = mouse.position.ReadValue();
            bool cameraDrag = mouse.rightButton.isPressed || mouse.middleButton.isPressed;
            if (cameraDrag)
            {
                if (pointerDown) { app.PointerEnd(true); pointerDown = false; }
                if (lastMouseCamera) { if (mouse.rightButton.isPressed) app.View.Rotate((p.x - lastMouse.x) * -.25f); else app.View.Gesture(p - lastMouse, 1, 0); }
            }
            else
            {
                if (mouse.leftButton.wasPressedThisFrame && !app.Hud.IsOverUI(p)) { pointerDown = true; app.PointerBegin(p, false); }
                if (pointerDown && mouse.leftButton.isPressed) app.PointerMove(p, false);
                if (pointerDown && mouse.leftButton.wasReleasedThisFrame) { app.PointerEnd(false); pointerDown = false; }
                if (!pointerDown) app.Hover(p);
            }
            if (!app.Hud.IsOverUI(p)) app.View.Zoom(mouse.scroll.ReadValue().y);
            lastMouse = p; lastMouseCamera = cameraDrag;
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.digit1Key.wasPressedThisFrame) app.SetTool(SandTool.Wall);
                if (k.digit2Key.wasPressedThisFrame) app.SetTool(SandTool.Tower);
                if (k.digit3Key.wasPressedThisFrame) app.SetTool(SandTool.Dig);
                if (k.digit4Key.wasPressedThisFrame) app.SetTool(SandTool.Raise);
                if (k.zKey.wasPressedThisFrame && (k.ctrlKey.isPressed || k.leftMetaKey.isPressed)) app.Undo();
            }
        }
        void OnApplicationFocus(bool focused) { if (!focused && app && pointerDown) { app.PointerEnd(true); pointerDown = false; } }
        void OnDestroy() { if (EnhancedTouchSupport.enabled) EnhancedTouchSupport.Disable(); }
    }
}
