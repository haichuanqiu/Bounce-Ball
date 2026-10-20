using GameMeta;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class LayoutLobby
{
    static TMP_FontAsset font;
    static Color panel = new Color(0.08f, 0.12f, 0.22f);
    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new System.Exception("Exit Play mode first");
        var lobby = GameObject.Find("Canvas").transform.Find("Lobby");
        if (lobby.Find("LobbyLayout")) throw new System.Exception("Layout already exists");
        Undo.RegisterFullObjectHierarchyUndo(lobby.gameObject, "Layout lobby");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        Place((RectTransform)lobby, 0, 0, 1, 1);
        lobby.Find("Image").gameObject.SetActive(false);
        var root = Box("LobbyLayout", lobby, 0, 0, 1, 1, new Color(0.025f, 0.04f, 0.08f));
        root.SetAsFirstSibling();
        Card("TopScore", root, .03f, .935f, .33f, .985f, "BEST SCORE\n--", 24);
        Card("TopWave", root, .36f, .935f, .66f, .985f, "BEST WAVE\n--", 24);
        Label("Version", root, .84f, .94f, .99f, .98f, "v" + Application.version, 20);
        Label("Title", root, .08f, .76f, .92f, .89f, "BOUNCE BALL", 78);
        Label("ArtPlaceholder", root, .15f, .57f, .85f, .73f, "KEY VISUAL AREA", 24, new Color(.35f,.43f,.56f));

        var start = lobby.Find("New Game");
        MoveButton(start, lobby, .16f, .475f, .84f, .55f, "START GAME", 48, new Color(.75f,.49f,.12f));
        var resume = Card("ContinuePlaceholder", root, .16f, .38f, .84f, .46f, "CONTINUE GAME\nNo saved run", 30);
        var resumeButton = resume.gameObject.AddComponent<Button>();
        resumeButton.targetGraphic = resume.GetComponent<Image>();
        resumeButton.interactable = false;

        var records = Box("PersonalRecords", root, .12f, .265f, .88f, .365f, panel);
        Label("Title", records, .03f, .71f, .97f, .98f, "PERSONAL RECORDS", 25);
        Card("Score", records, .025f, .08f, .49f, .69f, "BEST SCORE\n--", 28);
        Card("Wave", records, .51f, .08f, .975f, .69f, "BEST WAVE\n--", 28);

        var nav = lobby.Find("LobbyNavigation");
        var settings = nav.Find("OpenSettings");
        var help = nav.Find("OpenHowToPlay");
        var collection = nav.Find("OpenCollection");
        var upgrade = lobby.Find("up Grade");
        MoveButton(collection, lobby, .06f, .125f, .265f, .245f, "COLLECTION\nBalls & items", 25, panel);
        MoveButton(upgrade, lobby, .285f, .125f, .49f, .245f, "UPGRADE\nProgression", 25, panel);
        MoveButton(help, lobby, .51f, .125f, .715f, .245f, "HOW TO PLAY\nRules", 25, panel);
        MoveButton(settings, lobby, .735f, .125f, .94f, .245f, "SETTINGS\nAudio & more", 25, panel);
        var topSettings = Object.Instantiate(settings.gameObject, lobby);
        Undo.RegisterCreatedObjectUndo(topSettings, "Create top settings shortcut");
        topSettings.name = "TopSettings";
        MoveButton(topSettings.transform, lobby, .70f, .935f, .81f, .985f, "SET", 25, panel);
        nav.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(lobby.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        Selection.activeGameObject = lobby.gameObject;
        return "Lobby layout saved; existing actions retained. Continue and records are placeholders.";
    }

    static void Place(RectTransform r, float x0,float y0,float x1,float y1)
    {
        r.anchorMin = new Vector2(x0,y0); r.anchorMax = new Vector2(x1,y1);
        r.pivot = new Vector2(.5f,.5f); r.offsetMin = r.offsetMax = Vector2.zero;
        r.localScale = Vector3.one;
    }
    static RectTransform Box(string name,Transform parent,float x0,float y0,float x1,float y1,Color color)
    {
        var go = new GameObject(name,typeof(RectTransform),typeof(Image));
        Undo.RegisterCreatedObjectUndo(go,"Create lobby layout"); go.transform.SetParent(parent,false);
        var r = (RectTransform)go.transform; Place(r,x0,y0,x1,y1);
        go.GetComponent<Image>().color = color;
        return r;
    }
    static RectTransform Card(string name,Transform parent,float x0,float y0,float x1,float y1,string text,int size)
    {
        var r = Box(name,parent,x0,y0,x1,y1,panel);
        Label("Label",r,.035f,.05f,.965f,.95f,text,size); return r;
    }
    static void Label(string name,Transform parent,float x0,float y0,float x1,float y1,string text,int size,Color? color=null)
    {
        var go = new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(go,"Create lobby label"); go.transform.SetParent(parent,false);
        Place((RectTransform)go.transform,x0,y0,x1,y1);
        var t = go.GetComponent<TextMeshProUGUI>(); t.font=font;t.text=text;t.fontSize=size;
        t.enableAutoSizing=true;t.fontSizeMin=16;t.fontSizeMax=size;
        t.alignment=TextAlignmentOptions.Center;t.color=color??Color.white;t.raycastTarget=false;
    }
    static void MoveButton(Transform t,Transform parent,float x0,float y0,float x1,float y1,string text,int size,Color color)
    {
        Undo.SetTransformParent(t,parent,"Move lobby button"); Place((RectTransform)t,x0,y0,x1,y1);
        var image=t.GetComponent<Image>(); image.sprite=null;image.color=color;
        var label=t.GetComponentInChildren<TMP_Text>(true);Place(label.rectTransform,.03f,.05f,.97f,.95f);
        label.font=font;label.text=text;label.fontSize=size;label.enableAutoSizing=true;
        label.fontSizeMin=16;label.fontSizeMax=size;label.alignment=TextAlignmentOptions.Center;label.color=Color.white;
    }
}
