using System;
using System.Linq;
using GameMeta;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class BuildPageNavigation
{
    static TMP_FontAsset font;
    static Transform canvas;

    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit Play mode before building pages.");
        canvas = GameObject.Find("Canvas").transform;
        if (canvas.Find("NavigationPages")) throw new Exception("Navigation pages already exist.");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        var root = Rect("NavigationPages", canvas);
        Stretch(root);
        var settings = Page("Settings", "SETTINGS\nSettings page placeholder", root);
        var help = Page("HowToPlay", "HOW TO PLAY\nGameplay instructions placeholder", root);
        var upgrade = Page("Upgrade", "UPGRADE\nPermanent progression page placeholder", root);
        var codex = Page("Collection", "COLLECTION\nBalls / bricks / items reference placeholder", root);
        var ball = Page("BallDetails", "BALL DETAILS\nBall description placeholder", root);
        var brick = Page("BrickDetails", "BRICK DETAILS\nSpecial brick description placeholder", root);
        var item = Page("ItemDetails", "ITEM DETAILS\nItem description placeholder", root);
        var reset = Page("ResetDataConfirmation", "RESET DATA\nConfirmation placeholder - no data is changed", root);
        var abandon = Page("AbandonRunConfirmation", "ABANDON RUN\nConfirmation placeholder - no run is discarded", root);

        // Overlay pages retain their source beneath them. Closing returns to that source.
        foreach (var page in new[] { settings, help, upgrade, codex, ball, brick, item, reset, abandon })
        {
            Button(page.transform, "Back", "Back", new Vector2(0, -360), new[] { page }, new GameObject[0]);
            page.SetActive(false);
        }
        Button(settings.transform, "ResetData", "Reset data...", new Vector2(0, -100), null, new[] { reset });
        Button(reset.transform, "Confirm", "Confirm (preview)", new Vector2(0, -200), new[] { reset }, null);
        Button(abandon.transform, "Confirm", "Confirm (preview)", new Vector2(0, -200), new[] { abandon }, null);
        Button(codex.transform, "Balls", "Balls", new Vector2(0, 80), null, new[] { ball });
        Button(codex.transform, "Bricks", "Special bricks", new Vector2(0, -30), null, new[] { brick });
        Button(codex.transform, "Items", "Items", new Vector2(0, -140), null, new[] { item });

        Entrances(canvas.Find("Lobby"), "LobbyNavigation", new[] { "Settings", "How to play", "Collection" }, new[] { settings, help, codex });
        var upgradeButton = canvas.Find("Lobby/up Grade").GetComponent<UnityEngine.UI.Button>();
        var upgradeLink = upgradeButton.gameObject.AddComponent<PageNavigationLink>();
        upgradeLink.ShowPages = new[] { upgrade };
        UnityEventTools.AddPersistentListener(upgradeButton.onClick, upgradeLink.Navigate);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);
        Selection.activeGameObject = root.gameObject;
        return "Created 9 placeholder overlays, 3 lobby navigation entrances and connected the existing Upgrade button. Existing game pages preserved.";
    }

    static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create navigation UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    static GameObject Page(string name, string title, Transform parent)
    {
        var rect = Rect(name, parent);
        Stretch(rect);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.06f, 0.09f, 0.15f, 0.98f);
        Text(rect, "PageLabel", title, new Vector2(0, 260), new Vector2(680, 200), 34);
        return rect.gameObject;
    }

    static void Text(Transform parent, string name, string text, Vector2 position, Vector2 size, int fontSize)
    {
        var rect = Rect(name, parent);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    static void Button(Transform parent, string name, string label, Vector2 position, GameObject[] hide, GameObject[] show)
    {
        var rect = Rect(name, parent);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(310, 82);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.17f, 0.29f, 0.43f);
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        var link = rect.gameObject.AddComponent<PageNavigationLink>();
        link.HidePages = hide ?? new GameObject[0];
        link.ShowPages = show ?? new GameObject[0];
        UnityEventTools.AddPersistentListener(button.onClick, link.Navigate);
        Text(rect, "Label", label, Vector2.zero, new Vector2(300, 76), 27);
    }

    static void Entrances(Transform parent, string name, string[] labels, GameObject[] targets)
    {
        if (!parent) throw new Exception("Missing parent for " + name);
        var strip = Rect(name, parent);
        strip.anchorMin = strip.anchorMax = new Vector2(0.5f, 0.5f);
        strip.sizeDelta = new Vector2(340, labels.Length * 92);
        // Keep added controls away from the existing start / resume controls.
        strip.anchoredPosition = name == "LobbyNavigation" ? new Vector2(0, -260) :
            name == "PauseNavigation" ? new Vector2(0, -360) :
            name == "DetailsNavigation" ? new Vector2(-210, -480) : new Vector2(0, -550);
        for (int i = 0; i < labels.Length; i++)
            Button(strip, "Open" + targets[i].name, labels[i], new Vector2(0, (labels.Length - 1) * 46 - i * 92), null, new[] { targets[i] });
    }
}
