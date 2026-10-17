using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Bounded editor commands for reproducible authoring in an already-open Unity editor.
// No network listener or arbitrary code execution. Commands live in ignored Library/.
[InitializeOnLoad]
public static class VolcanoCaveBridge
{
    const string Work = "Library/VolcanoCave";
    static double nextPoll;
    static VolcanoCaveBridge() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        nextPoll = EditorApplication.timeSinceStartup + 1;
        string command = Work + "/command.txt";
        if (!File.Exists(command)) return;
        // Asset refresh can briefly report idle before newly written scripts start compiling.
        // Do not run a request against an older assembly during that window.
        DateTime editorBuild=File.GetLastWriteTimeUtc("Library/ScriptAssemblies/Assembly-CSharp-Editor.dll");
        if(Directory.GetFiles("Assets/Editor/VolcanoCave","*.cs").Any(p=>File.GetLastWriteTimeUtc(p)>editorBuild)) return;
        DateTime runtimeBuild=File.GetLastWriteTimeUtc("Library/ScriptAssemblies/Assembly-CSharp.dll");
        if(Directory.GetFiles("Assets/Scripts/Environment","*.cs").Any(p=>File.GetLastWriteTimeUtc(p)>runtimeBuild)) return;
        string action = File.ReadAllText(command).Trim();
        File.Delete(command);
        try
        {
            if (action == "export") ExportLayout();
            else if (action == "build") Invoke("VolcanoCaveBuilder", "Build");
            else if (action == "verify") Invoke("VolcanoCaveBuilder", "Verify");
            else if (action == "preview") Invoke("VolcanoCaveBuilder", "Preview");
            else if (action == "playtest") Invoke("VolcanoCavePlaytest", "Begin");
            else if (action == "interior-open") Invoke("CaveInteriorBuilder", "OpenField");
            else if (action == "interior-build") Invoke("CaveInteriorBuilder", "Build");
            else if (action == "interior-preview") Invoke("CaveInteriorBuilder", "Preview");
            else if (action == "interior-verify") Invoke("CaveInteriorBuilder", "Verify");
            else if (action == "interior-playtest") Invoke("CaveInteriorPlaytest", "Begin");
            else if (action == "interior-reload") Invoke("CaveInteriorBuilder", "ReloadAndVerify");
            else throw new InvalidOperationException("Unknown Volcano command: " + action);
            File.WriteAllText(Work + "/result.txt", "OK " + action + " " + DateTime.Now.ToString("s"));
        }
        catch (Exception e) { File.WriteAllText(Work + "/result.txt", e.ToString()); Debug.LogException(e); }
    }
    static void Invoke(string type, string method)
    {
        typeof(VolcanoCaveBridge).Assembly.GetType(type, true).GetMethod(method).Invoke(null, null);
    }

    [Serializable] public class Entry
    {
        public string name;
        public Vector3 position, scale, center, size;
        public Quaternion rotation;
        public int instanceId;
    }
    [Serializable] public class Layout
    {
        public string sourceScene;
        public Entry[] walls, markers;
        public Entry exit;
    }
    static Entry Capture(Transform t)
    {
        var box = t.GetComponent<BoxCollider>();
        return new Entry { name=t.name, position=t.position, rotation=t.rotation, scale=t.lossyScale,
            center=box ? box.center : Vector3.zero, size=box ? box.size : Vector3.one, instanceId=t.GetInstanceID() };
    }
    [MenuItem("Tools/Volcano Cave/1 Export Current Layout")]
    public static void ExportLayout()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/FildScene.unity") throw new InvalidOperationException("Open FildScene first.");
        Directory.CreateDirectory(Work);
        Directory.CreateDirectory("ArtSource/VolcanoCave");
        string backup = "ArtSource/VolcanoCave/FildScene.before-volcano.unity";
        if (!File.Exists(backup)) EditorSceneManager.SaveScene(scene, backup, true);
        var all = scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        var walls = all.First(t=>t.name=="Walls");
        var exit = all.First(t=>t.name=="BossFieldTransitionTrigger");
        var data = new Layout { sourceScene=scene.path,
            walls=walls.GetComponentsInChildren<BoxCollider>(true).Where(c=>c.gameObject.activeInHierarchy).Select(c=>Capture(c.transform)).ToArray(),
            markers=all.Where(t=>t.name.Contains("SpawnPoint") || t.name=="Player" || t.name.Contains("TransitionTrigger")).Select(Capture).ToArray(), exit=Capture(exit) };
        File.WriteAllText("ArtSource/VolcanoCave/layout.json", JsonUtility.ToJson(data,true));
        Debug.Log("Volcano: exported " + data.walls.Length + " wall colliders from the live scene.");
    }
}
