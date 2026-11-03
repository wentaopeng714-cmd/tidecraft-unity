using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
namespace Tidecraft
{
    public sealed class SandHUD : MonoBehaviour
    {
        SandApp app; TMP_FontAsset font; RectTransform safe;
        UnityEngine.UI.CanvasScaler scaler;
        RectTransform brand, actions, tray, opt, hintPanel;
        UnityEngine.UI.GridLayoutGroup actionGrid;
        TextMeshProUGUI titleText, subtitleText;
        float layoutWidth, layoutHeight;
        readonly List<UnityEngine.UI.Button> tools = new List<UnityEngine.UI.Button>();
        readonly List<RaycastResult> hits = new List<RaycastResult>();
        TextMeshProUGUI hint, optionLabel, counts, soundText;
        UnityEngine.UI.Button undo, redo;
        GameObject modal; Rect lastSafe; int lastWidth, lastHeight;
        public bool ModalOpen => modal != null && modal.activeSelf;
        public void Initialize(SandApp owner, TMP_FontAsset suppliedFont)
        {
            app = owner; font = suppliedFont ? suppliedFont : Resources.Load<TMP_FontAsset>("TidecraftFont");
            if (!font)
            {
                var source = Resources.Load<Font>("NotoSansSC-Subset");
                if (source) font = TMP_FontAsset.CreateFontAsset(source);
                else font = TMP_Settings.defaultFontAsset;
            }
            var canvasObject = new GameObject("Touch interface", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = app.View.Camera; canvas.planeDistance = 1;
            scaler = canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Screen.height > Screen.width ? new Vector2(720, 1280) : new Vector2(1440, 810); scaler.matchWidthOrHeight = Screen.height > Screen.width ? 0 : .5f;
            canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            foreach (var es in FindObjectsByType<EventSystem>(FindObjectsSortMode.None)) es.gameObject.SetActive(false);
            var events = new GameObject("Touch UI events"); events.AddComponent<EventSystem>(); events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            safe = Rect("Safe area", canvasObject.transform); ApplySafeArea();
            brand = Panel("Brand", safe, new Vector2(0, 1), new Vector2(22, -20), new Vector2(300, 82), Palette.Alpha(Palette.Cream, .94f));
            titleText = Label("沙岸工坊", brand, new Vector2(20, -6), new Vector2(260, 46), 27, Palette.Ink, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            subtitleText = Label("T I D E C R A F T   /   触控原型", brand, new Vector2(20, -53), new Vector2(270, 22), 12, Palette.Ink);
            actions = Rect("Actions", safe); actions.anchorMin = actions.anchorMax = actions.pivot = new Vector2(1, 1); actions.anchoredPosition = new Vector2(-22, -20); actions.sizeDelta = new Vector2(680, 56);
            actionGrid = actions.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>(); actionGrid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            undo = Button("撤销", actions, app.Undo); redo = Button("重做", actions, app.Redo);
            Button("保存", actions, app.Save); Button("读取", actions, app.Load); Button("视角", actions, app.View.Home);
            var sound = Button("声音 开", actions, () => { app.Audio.Toggle(); soundText.text = app.Audio.Muted ? "声音 关" : "声音 开"; }); soundText = sound.GetComponentInChildren<TextMeshProUGUI>();
            Button("重置", actions, ConfirmReset);
            tray = Panel("Tool tray", safe, new Vector2(.5f, 0), new Vector2(0, 18), new Vector2(844, 114), Palette.Alpha(Palette.Ink, .96f));
            var toolRow = Rect("Tools", tray); toolRow.anchorMin = Vector2.zero; toolRow.anchorMax = Vector2.one; toolRow.offsetMin = new Vector2(10, 10); toolRow.offsetMax = new Vector2(-10, -10);
            var layout = toolRow.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>(); layout.spacing = 8; layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            string[] labels = { "画城墙", "放塔楼", "挖水沟", "堆沙地", "移除" };
            for (int i = 0; i < 5; i++)
            {
                int index = i; var btn = Button(labels[i], toolRow, () => app.SetTool((SandTool)index));
                var text = btn.GetComponentInChildren<TextMeshProUGUI>(); text.rectTransform.anchorMin = new Vector2(0, 0); text.rectTransform.anchorMax = new Vector2(1, .36f); text.fontSize = 19;
                var icon = new GameObject("Tool icon", typeof(RectTransform)).AddComponent<UnityEngine.UI.Image>(); icon.transform.SetParent(btn.transform, false);
                var r = icon.rectTransform; r.anchorMin = r.anchorMax = new Vector2(.5f, .7f); r.sizeDelta = new Vector2(40, 40); icon.sprite = MakeIcon(i); icon.color = Palette.Ink; icon.raycastTarget = false;
                tools.Add(btn);
            }
            opt = Panel("Tool option", safe, new Vector2(1, 0), new Vector2(-22, 24), new Vector2(208, 96), Palette.Alpha(Palette.Cream, .96f));
            var optionButton = opt.gameObject.AddComponent<UnityEngine.UI.Button>(); optionButton.targetGraphic = opt.GetComponent<UnityEngine.UI.Image>(); optionButton.onClick.AddListener(() => { if (app.Tool == SandTool.Tower) app.CycleTower(); else app.CycleBrush(); });
            optionLabel = Label("", opt, new Vector2(12, -15), new Vector2(184, 70), 19, Palette.Ink, TextAlignmentOptions.Center);
            hintPanel = Panel("Hint", safe, new Vector2(.5f, 0), new Vector2(0, 146), new Vector2(860, 45), Palette.Alpha(Palette.Cream, .93f));
            hintPanel.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            hint = Label("", hintPanel, new Vector2(16, -7), new Vector2(828, 32), 18, Palette.Ink, TextAlignmentOptions.Center);
            counts = Label("", safe, new Vector2(22, -112), new Vector2(440, 34), 17, Palette.Ink);
            BuildModal(); ApplyLayout(); Refresh();
        }
        RectTransform Rect(string name, Transform parent)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false); r.localScale = Vector3.one; return r;
        }
        RectTransform Panel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var r = Rect(name, parent); r.anchorMin = r.anchorMax = r.pivot = anchor; r.anchoredPosition = position; r.sizeDelta = size;
            var image = r.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color;
            return r;
        }
        TextMeshProUGUI Label(string text, Transform parent, Vector2 position, Vector2 size, float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft, FontStyles style = FontStyles.Normal)
        {
            var r = Rect("Text", parent); r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = position; r.sizeDelta = size;
            var t = r.gameObject.AddComponent<TextMeshProUGUI>(); if (font) t.font = font; t.text = text; t.fontSize = fontSize; t.color = color; t.alignment = alignment; t.fontStyle = style; t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Ellipsis; return t;
        }
        UnityEngine.UI.Button Button(string text, Transform parent, UnityEngine.Events.UnityAction action)
        {
            var r = Rect(text, parent); r.sizeDelta = new Vector2(90, 56); var image = r.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = Palette.Cream;
            var b = r.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic = image;
            var colors = b.colors; colors.normalColor = Palette.Cream; colors.highlightedColor = Palette.SandLight; colors.pressedColor = Palette.Shallow; colors.selectedColor = Palette.Cream; colors.disabledColor = Palette.Alpha(Palette.Cream, .5f); b.colors = colors;
            b.onClick.AddListener(action); b.onClick.AddListener(() => app.Audio.Click());
            var l = Label(text, r, Vector2.zero, Vector2.zero, 18, Palette.Ink, TextAlignmentOptions.Center, FontStyles.Bold);
            l.rectTransform.anchorMin = Vector2.zero; l.rectTransform.anchorMax = Vector2.one; l.rectTransform.offsetMin = Vector2.zero; l.rectTransform.offsetMax = Vector2.zero;
            return b;
        }
        void BuildModal()
        {
            var r = Panel("Reset confirmation", safe, new Vector2(.5f, .5f), Vector2.zero, new Vector2(590, 300), Palette.Cream); modal = r.gameObject;
            Label("重新铺一片沙滩？", r, new Vector2(28, -26), new Vector2(530, 42), 28, Palette.Ink, TextAlignmentOptions.Center, FontStyles.Bold);
            Label("当前建造会清空。重置后仍可撤销。", r, new Vector2(28, -90), new Vector2(530, 40), 20, Palette.Ink, TextAlignmentOptions.Center);
            var row = Rect("Choices", r); row.anchorMin = new Vector2(0, 0); row.anchorMax = new Vector2(1, 0); row.pivot = new Vector2(.5f, 0); row.offsetMin = new Vector2(28, 26); row.offsetMax = new Vector2(-28, 122);
            var layout = row.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>(); layout.spacing = 14; layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            Button("继续建造", row, () => modal.SetActive(false)); Button("重置沙滩", row, () => { modal.SetActive(false); app.ResetBeach(); }); modal.SetActive(false);
        }
        void ConfirmReset() { app.PointerEnd(false); modal.SetActive(true); modal.transform.SetAsLastSibling(); }
        public void Message(string text) { if (hint) hint.text = text; }
        public void Refresh()
        {
            if (tools.Count == 0) return;
            for (int i = 0; i < tools.Count; i++)
            {
                bool selected = (int)app.Tool == i; var c = tools[i].colors; c.normalColor = selected ? Palette.Shallow : Palette.Cream; c.selectedColor = c.normalColor; tools[i].colors = c;
                tools[i].GetComponent<UnityEngine.UI.Image>().color = c.normalColor;
            }
            if (undo) undo.interactable = app.CanUndo; if (redo) redo.interactable = app.CanRedo;
            counts.text = "沙堡 " + app.TowerCount + "    /    墙段 " + app.WallCount + "    /    自由建造";
            string[] styles = { "圆形塔楼", "方形塔楼", "高塔" };
            optionLabel.text = app.Tool == SandTool.Tower ? styles[app.TowerStyle] + "\n点击切换模具" : "笔刷 " + (app.BrushRadius < .8f ? "小" : app.BrushRadius < 1.1f ? "中" : "大") + "\n点击切换大小";
        }
        public bool IsOverUI(Vector2 screen)
        {
            if (ModalOpen) return true;
            var es = EventSystem.current; if (!es) return false;
            hits.Clear(); es.RaycastAll(new PointerEventData(es) { position = screen }, hits); return hits.Count > 0;
        }
        void ApplySafeArea()
        {
            Rect area = Screen.safeArea; safe.anchorMin = new Vector2(area.xMin / Mathf.Max(1, Screen.width), area.yMin / Mathf.Max(1, Screen.height));
            safe.anchorMax = new Vector2(area.xMax / Mathf.Max(1, Screen.width), area.yMax / Mathf.Max(1, Screen.height)); safe.offsetMin = safe.offsetMax = Vector2.zero;
            lastSafe = area; lastWidth = Screen.width; lastHeight = Screen.height;
        }
        void Update()
        {
            if (safe && (Screen.safeArea != lastSafe || Screen.width != lastWidth || Screen.height != lastHeight)) { ApplySafeArea(); ApplyLayout(); }
        }
        void LateUpdate()
        {
            if (safe && actions && (Mathf.Abs(safe.rect.width - layoutWidth) > .5f || Mathf.Abs(safe.rect.height - layoutHeight) > .5f)) ApplyLayout();
        }
        static void Place(RectTransform r, Vector2 anchor, Vector2 position, Vector2 size)
        {
            r.anchorMin = r.anchorMax = r.pivot = anchor; r.anchoredPosition = position; r.sizeDelta = size;
        }
        static void StretchText(TextMeshProUGUI t, Vector2 inset)
        {
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = inset; t.rectTransform.offsetMax = -inset;
        }
        void ApplyLayout()
        {
            if (!actions || !hint || !counts) return;
            bool portrait = Screen.height > Screen.width;
            scaler.referenceResolution = portrait ? new Vector2(720, 1280) : new Vector2(1440, 810);
            scaler.matchWidthOrHeight = portrait ? 0 : .5f;
            float w = safe.rect.width > 0 ? safe.rect.width : (portrait ? 720 : 1440);
            layoutWidth = safe.rect.width; layoutHeight = safe.rect.height;
            if (portrait)
            {
                Place(brand, new Vector2(0,1), new Vector2(16,-18), new Vector2(w-32,90));
                Place(titleText.rectTransform, new Vector2(0,1), new Vector2(18,-7), new Vector2(w-68,48)); titleText.fontSize=32;
                Place(subtitleText.rectTransform, new Vector2(0,1), new Vector2(18,-58), new Vector2(w-68,24)); subtitleText.fontSize=14;
                Place(counts.rectTransform, new Vector2(0,1), new Vector2(20,-117), new Vector2(w-40,32)); counts.fontSize=22;
                Place(actions, new Vector2(0,1), new Vector2(20,-158), new Vector2(w-40,186));
                actionGrid.constraintCount=4; actionGrid.spacing=new Vector2(10,10); actionGrid.cellSize=new Vector2((w-70)/4,88);
                opt.SetParent(actions,false); opt.SetAsLastSibling();
                Place(tray, new Vector2(.5f,0), new Vector2(0,18), new Vector2(w-32,160));
                Place(hintPanel, new Vector2(.5f,0), new Vector2(0,194), new Vector2(w-40,100));
                hint.fontSize=22; hint.textWrappingMode=TextWrappingModes.Normal;
            }
            else
            {
                Place(brand,new Vector2(0,1),new Vector2(22,-20),new Vector2(300,82));
                Place(titleText.rectTransform,new Vector2(0,1),new Vector2(20,-6),new Vector2(260,46)); titleText.fontSize=27;
                Place(subtitleText.rectTransform,new Vector2(0,1),new Vector2(20,-53),new Vector2(270,22)); subtitleText.fontSize=12;
                Place(counts.rectTransform,new Vector2(0,1),new Vector2(22,-112),new Vector2(440,34)); counts.fontSize=17;
                Place(actions,new Vector2(1,1),new Vector2(-22,-20),new Vector2(680,56));
                opt.SetParent(safe,false); Place(opt,new Vector2(1,0),new Vector2(-22,24),new Vector2(208,96));
                actionGrid.constraintCount=7; actionGrid.spacing=new Vector2(8,8); actionGrid.cellSize=new Vector2((680-48)/7f,56);
                Place(tray,new Vector2(.5f,0),new Vector2(0,18),new Vector2(844,114));
                Place(hintPanel,new Vector2(.5f,0),new Vector2(0,146),new Vector2(860,45));
                hint.fontSize=18; hint.textWrappingMode=TextWrappingModes.NoWrap;
            }
            StretchText(hint,new Vector2(12,8)); StretchText(optionLabel,new Vector2(6,8));
            optionLabel.fontSize=portrait?20:19; optionLabel.textWrappingMode=TextWrappingModes.Normal;
            foreach (Transform child in actions) if(child.gameObject!=opt.gameObject) child.GetComponentInChildren<TextMeshProUGUI>().fontSize=portrait?24:18;
            foreach (var tool in tools)
            {
                tool.GetComponentInChildren<TextMeshProUGUI>().fontSize=portrait?23:19;
                tool.transform.Find("Tool icon").GetComponent<RectTransform>().sizeDelta=Vector2.one*(portrait?48:40);
            }
            Debug.Log("[Tidecraft] UI layout: "+(portrait?"portrait":"landscape")+" / "+Screen.width+"x"+Screen.height);
        }
        static Sprite MakeIcon(int type)
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false); var data = new Color32[64 * 64];
            System.Action<int, int> dot = (x, y) => { for (int dy = -2; dy <= 2; dy++) for (int dx = -2; dx <= 2; dx++) if (x + dx >= 0 && x + dx < 64 && y + dy >= 0 && y + dy < 64) data[(y + dy) * 64 + x + dx] = new Color32(255, 255, 255, 255); };
            System.Action<int, int, int, int> line = (a, b, c, d) => { int n = Mathf.Max(Mathf.Abs(c - a), Mathf.Abs(d - b)); for (int i = 0; i <= n; i++) dot(Mathf.RoundToInt(Mathf.Lerp(a, c, (float)i / Mathf.Max(1, n))), Mathf.RoundToInt(Mathf.Lerp(b, d, (float)i / Mathf.Max(1, n)))); };
            if (type == 0 || type == 1)
            {
                line(12, 12, 52, 12); line(12, 12, 12, 42); line(52, 12, 52, 42);
                line(12, 42, 20, 42); line(20, 42, 20, 33); line(20, 33, 28, 33); line(28, 33, 28, 42); line(28, 42, 36, 42); line(36, 42, 36, 33); line(36, 33, 44, 33); line(44, 33, 44, 42); line(44, 42, 52, 42);
                if (type == 1) { line(32, 44, 32, 58); line(32, 58, 45, 54); line(45, 54, 32, 50); }
            }
            else if (type == 2) { line(39, 43, 20, 21); line(32, 51, 44, 39); line(32, 51, 42, 59); line(42, 59, 52, 49); line(52, 49, 44, 39); line(12, 26, 26, 12); line(12, 26, 10, 12); line(10, 12, 26, 12); }
            else if (type == 3) { line(9, 13, 55, 13); line(9, 13, 26, 40); line(26, 40, 35, 24); line(35, 24, 42, 34); line(42, 34, 55, 13); line(32, 44, 32, 59); line(25, 52, 32, 59); line(32, 59, 39, 52); }
            else { line(14, 14, 50, 50); line(14, 50, 50, 14); }
            texture.SetPixels32(data); texture.Apply(); return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f));
        }
    }
}
