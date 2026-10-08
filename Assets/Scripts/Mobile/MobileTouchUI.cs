using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MafiaUnity
{
    /// <summary>
    /// Runtime-created Android touch controls. No scene/prefab changes are
    /// required, which makes the first Android port easy to merge and test.
    /// </summary>
    public sealed class MobileTouchUI : MonoBehaviour
    {
        private RectTransform canvasRect;
        private VirtualJoystick moveJoystick;
        private LookPad lookPad;

        private void Awake()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            BuildUI();
#endif
        }

        private void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            MobileInputState.Move = moveJoystick != null ? moveJoystick.Value : Vector2.zero;
            if (lookPad != null)
                MobileInputState.AddLook(lookPad.ConsumeDelta());
            MobileInputState.RunHeld = RunButton.Held;
            MobileInputState.CrouchHeld = CrouchButton.Held;
#endif
        }

        private void LateUpdate()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            MobileInputState.ResetFrame();
#endif
        }

        private void BuildUI()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("MobileEventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }

            var canvasObject = new GameObject("MobileTouchCanvas");
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            canvasRect = canvasObject.GetComponent<RectTransform>();

            moveJoystick = CreateJoystick("MoveJoystick", new Vector2(0f, 0f), new Vector2(300f, 300f));
            lookPad = CreateLookPad("LookPad");

            RunButton = CreateButton("WALK", new Vector2(1f, 0f), new Vector2(-280f, 220f), 150f);
            CrouchButton = CreateButton("CROUCH", new Vector2(1f, 0f), new Vector2(-500f, 140f), 150f);

            var use = CreateButton("USE", new Vector2(1f, 1f), new Vector2(-240f, -190f), 170f);
            use.OnDown = () => MobileInputState.PressUse();
        }

        private static VirtualJoystick CreateJoystick(string name, Vector2 anchor, Vector2 size)
        {
            var root = CreatePanel(name, anchor, new Vector2(35f, 35f), size, 0.20f);
            var joystick = root.gameObject.AddComponent<VirtualJoystick>();
            joystick.Handle = CreatePanel("Handle", new Vector2(0.5f, 0.5f), Vector2.zero, size * 0.38f, 0.38f);
            joystick.Handle.SetParent(root, false);
            joystick.Handle.anchoredPosition = Vector2.zero;
            return joystick;
        }

        private static LookPad CreateLookPad(string name)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.42f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.001f);
            var pad = go.AddComponent<LookPad>();
            return pad;
        }

        private static HoldButton CreateButton(string label, Vector2 anchor, Vector2 position, float size)
        {
            var panel = CreatePanel(label, anchor, position, new Vector2(size, size), 0.30f);
            var button = panel.gameObject.AddComponent<HoldButton>();

            var textObject = new GameObject("Label");
            textObject.transform.SetParent(panel, false);
            var text = textObject.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 28;
            var tr = text.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;

            return button;
        }

        private static RectTransform CreatePanel(string name, Vector2 anchor, Vector2 position, Vector2 size, float alpha)
        {
            var go = new GameObject(name);
            go.transform.SetParent(GameObject.Find("MobileTouchCanvas").transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            var image = go.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, alpha);
            return rt;
        }

        private static HoldButton RunButton;
        private static HoldButton CrouchButton;

        private sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
        {
            public RectTransform Handle;
            public Vector2 Value { get; private set; }

            public void OnPointerDown(PointerEventData eventData) { UpdateValue(eventData); }
            public void OnDrag(PointerEventData eventData) { UpdateValue(eventData); }

            public void OnPointerUp(PointerEventData eventData)
            {
                Value = Vector2.zero;
                if (Handle != null) Handle.anchoredPosition = Vector2.zero;
            }

            private void UpdateValue(PointerEventData eventData)
            {
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    transform as RectTransform, eventData.position, eventData.pressEventCamera, out local);

                var rect = (transform as RectTransform).rect;
                var radius = Mathf.Min(rect.width, rect.height) * 0.5f;
                Value = Vector2.ClampMagnitude(local / Mathf.Max(radius, 1f), 1f);

                if (Handle != null)
                    Handle.anchoredPosition = Value * radius * 0.62f;
            }
        }

        private sealed class LookPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
        {
            private Vector2 Delta { get; set; }
            private Vector2 lastPosition;
            private bool active;

            public void OnPointerDown(PointerEventData eventData)
            {
                active = true;
                lastPosition = eventData.position;
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (!active) return;
                Delta += (eventData.position - lastPosition) * 0.12f;
                lastPosition = eventData.position;
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                active = false;
            }

            public Vector2 ConsumeDelta()
            {
                var value = Delta;
                Delta = Vector2.zero;
                return value;
            }
        }

        private sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            public bool Held { get; private set; }
            public System.Action OnDown;

            public void OnPointerDown(PointerEventData eventData)
            {
                Held = true;
                if (OnDown != null) OnDown();
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                Held = false;
            }
        }
    }
}
