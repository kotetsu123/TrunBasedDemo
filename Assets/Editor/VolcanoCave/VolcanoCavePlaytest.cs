using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Opt-in Play Mode smoke test. Uses runtime interactions without writing player saves.</summary>
[InitializeOnLoad]
public static class VolcanoCavePlaytest
{
    const string Active="VolcanoCave.TestActive";
    const string Stage="VolcanoCave.TestStage";
    const string Report="ArtSource/VolcanoCave/playtest.txt";
    static double nextTick,deadline;
    static int sweep;
    static bool sawFade,sawRestored;
    static readonly HashSet<int> previouslyFaded=new HashSet<int>();
    static readonly List<Vector3> route=new List<Vector3>();
    static int routePoint;
    static float walked;
    static Vector3 lastPosition;
    static VolcanoCavePlaytest()
    {
        EditorApplication.update+=Tick;
        Application.logMessageReceived+=Log;
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Active,false)) deadline=EditorApplication.timeSinceStartup+90;
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Active,false)) Finish(false,"Play Mode ended before smoke test completion");
        };
    }
    [MenuItem("Tools/Volcano Cave/5 Run Play Mode Smoke Test")]
    public static void Begin()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Start in Edit Mode.");
        VolcanoCaveBuilder.Verify();
        File.WriteAllText(Report,"Runtime environment smoke test; no player save files are written.\nDEFERRED: battle / battle-return tests per user request (forced battle Tutorial). Enemy colliders are disabled only in this temporary test session.\n");
        SessionState.SetBool(Active,true); SessionState.SetInt(Stage,0);
        EditorApplication.EnterPlaymode();
    }
    static void Log(string message,string stack,LogType type)
    {
        if(SessionState.GetBool(Active,false) && (type==LogType.Error || type==LogType.Exception || type==LogType.Assert))
            File.AppendAllText(Report,"RUNTIME ERROR: "+message+"\n"+stack+"\n");
    }
    static void Note(string s) { File.AppendAllText(Report,s+"\n"); }
    static void Require(bool ok,string s) { if(!ok) throw new InvalidOperationException(s); Note("PASS "+s); }
    static GameObject Player() => GameObject.FindGameObjectWithTag("Player");
    static void Place(Vector3 p)
    {
        var player=Player(); player.transform.position=p;
        var body=player.GetComponent<Rigidbody>(); if(body) { if(!body.isKinematic) body.velocity=Vector3.zero; body.position=p; }
        Physics.SyncTransforms();
    }
    static void Advance(int stage,float wait=.5f)
    {
        SessionState.SetInt(Stage,stage); nextTick=EditorApplication.timeSinceStartup+wait;
        deadline=EditorApplication.timeSinceStartup+60;
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Active,false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        double now=EditorApplication.timeSinceStartup;
        if(now<nextTick) return;
        nextTick=now+.15;
        try
        {
            if(deadline==0) deadline=now+90;
            if(now>deadline) throw new TimeoutException("Smoke test timed out at stage "+SessionState.GetInt(Stage,0));
            int stage=SessionState.GetInt(Stage,0);
            string scene=SceneManager.GetActiveScene().name;
            if(stage==0)
            {
                if(Time.timeSinceLevelLoad<1.5f) return;
                Application.runInBackground=true;
                Require(scene=="FildScene","FildScene enters Play Mode");
                if(TutorialPanelController.Current) TutorialPanelController.Current.ConfirmSkip();
                FieldPauseState.Clear();
                Require(UnityEngine.Object.FindObjectsOfType<EnemyFieldController>().Length>=5,"field enemies spawned");
                Require(UnityEngine.Object.FindObjectsOfType<FieldChestController>().Length==3,"three chests spawned");
                Require(UnityEngine.Object.FindObjectOfType<FieldRecruitController>()!=null,"recruit point spawned");
                var player=Player(); Require(player!=null,"player spawned");
                Require(Vector3.Distance(player.transform.position,new Vector3(0,.5f,-20))<1,"player start position preserved");
                player.GetComponent<SimplePlayerMovement>().enabled=false;
                player.GetComponent<Rigidbody>().isKinematic=true;
                foreach(var enemy in UnityEngine.Object.FindObjectsOfType<EnemyFieldController>())
                {
                    enemy.enabled=false;
                    foreach(var collider in enemy.GetComponentsInChildren<Collider>()) collider.enabled=false;
                }
                Camera.main.GetComponent<FieldCameraController>().enabled=false;
                sweep=0; sawFade=false; sawRestored=false; previouslyFaded.Clear(); Advance(1);
            }
            else if(stage==1)
            {
                var fade=UnityEngine.Object.FindObjectOfType<VolcanoCaveVisibility>();
                var block=new MaterialPropertyBlock();
                foreach(var r in fade.occluders)
                {
                    r.GetPropertyBlock(block); float f=block.GetFloat("_Fade");
                    if(f<.5f) { sawFade=true; previouslyFaded.Add(r.GetInstanceID()); }
                    if(f>.99f && previouslyFaded.Contains(r.GetInstanceID())) sawRestored=true;
                }
                if(sweep>=36)
                {
                    Require(sawFade,"occluding rock segments fade during camera sweep");
                    Require(sawRestored,"previously faded segments restore full visibility");
                    Note("PASS 36 camera poses: 3 locations x 4 azimuths x 3 pitches (-15,25,60)");
                    BeginRoute(); return;
                }
                Vector3[] points={new Vector3(0,.5f,-20),new Vector3(-5,.5f,2),new Vector3(10,.5f,15.48f)};
                float[] pitches={-15,25,60};
                Vector3 p=points[sweep/12]; Place(p);
                Quaternion rotation=Quaternion.Euler(pitches[(sweep%12)/4],(sweep%4)*90,0);
                Camera.main.transform.position=p+rotation*new Vector3(0,3,-6);
                Camera.main.transform.LookAt(p+Vector3.up*1.2f);
                if(sweep==5||sweep==17||sweep==29)
                    ScreenCapture.CaptureScreenshot("ArtSource/VolcanoCave/Previews/playmode-"+(sweep/12)+".png");
                sweep++; nextTick=now+.28;
            }
            else if(stage==7)
            {
                var body=Player().GetComponent<Rigidbody>();
                Vector3 p=body.position;
                walked+=Vector3.Distance(p,lastPosition); lastPosition=p;
                if(p.y<.3f || p.y>.8f) throw new InvalidOperationException("Player left the flat floor during route traversal: "+p);
                Vector3 delta=route[routePoint]-p; delta.y=0;
                if(delta.magnitude<.18f)
                {
                    routePoint++;
                    if(routePoint==route.Count)
                    {
                        body.velocity=Vector3.zero;
                        Note("PASS physics traversal from spawn through center to exit approach: "+walked.ToString("F1")+"m, "+route.Count+" waypoints, player remains grounded");
                        Advance(2); return;
                    }
                    delta=route[routePoint]-p; delta.y=0;
                }
                Vector3 velocity=delta.normalized*3;
                body.velocity=new Vector3(velocity.x,body.velocity.y,velocity.z);
                nextTick=now+.025;
            }
            else if(stage==2)
            {
                var chest=UnityEngine.Object.FindObjectsOfType<FieldChestController>().First(c=>c.ChestId=="chest_001");
                chest.TryOpen(); Require(FieldBattleContext.IsChestOpened("chest_001"),"chest interaction updates runtime state");
                UnityEngine.Object.FindObjectOfType<FieldRecruitController>().TryRecruit();
                Require(PartyRuntimeState.HasMember("Argo_002"),"Argo recruitment updates party");
                var player=Player(); player.GetComponent<Rigidbody>().isKinematic=false;
                Camera.main.GetComponent<FieldCameraController>().enabled=true;
                FieldPauseState.Clear();
                foreach(var enemy in UnityEngine.Object.FindObjectsOfType<EnemyFieldController>()) foreach(var collider in enemy.GetComponentsInChildren<Collider>()) collider.enabled=false;
                Place(new Vector3(12.4f,.5f,15.48f)); Advance(5,2);
            }
            else if(stage==5)
            {
                if(scene!="Boss_FieldScene" || Time.timeSinceLevelLoad<.4f) return;
                var entry=UnityEngine.Object.FindObjectsOfType<FieldSceneSpawnPoint>().First(s=>s.SpawnPointId=="BossFieldEntry");
                Require(Vector3.Distance(Player().transform.position,entry.transform.position)<1,"exit trigger loads Boss_FieldScene at BossFieldEntry");
                foreach(var enemy in UnityEngine.Object.FindObjectsOfType<EnemyFieldController>()) foreach(var collider in enemy.GetComponentsInChildren<Collider>()) collider.enabled=false;
                var returnTrigger=UnityEngine.Object.FindObjectsOfType<FieldSceneTransitionTrigger>().First(t=>
                    new SerializedObject(t).FindProperty("targetSceneName").stringValue=="FildScene");
                Place(returnTrigger.GetComponent<BoxCollider>().bounds.center); Advance(6,2);
            }
            else if(stage==6)
            {
                if(scene!="FildScene" || Time.timeSinceLevelLoad<.4f) return;
                Require(Vector3.Distance(Player().transform.position,new Vector3(10.43f,0,15.48f))<1,"Boss return spawn preserved");
                Require(GameObject.Find("VolcanoCaveEnvironment")!=null,"environment survives scene round trips");
                Require(FieldBattleContext.IsChestOpened("chest_001"),"opened chest survives scene round trip");
                Require(PartyRuntimeState.HasMember("Argo_002"),"recruited party survives scene round trip");
                Note("Editor frame draw calls (Game + open editor views, including shadows/light passes): "+UnityEditor.UnityStats.drawCalls+"; rendered triangles: "+UnityEditor.UnityStats.triangles+". Not a standalone player benchmark.");
                Finish(!File.ReadAllText(Report).Contains("RUNTIME ERROR:"),"Runtime smoke sequence complete");
            }
        }
        catch(Exception e) { Finish(false,e.ToString()); }
    }
    static void BeginRoute()
    {
        // Test-only path lookup, never saved as a game navigation system.
        // Drive the existing dynamic Rigidbody so walls and floor are actually exercised.
        var walls=GameObject.Find("Walls").GetComponentsInChildren<BoxCollider>(true);
        const int n=100;
        var blocked=new bool[n,n];
        for(int x=0;x<n;x++) for(int z=0;z<n;z++)
        {
            Vector3 p=new Vector3(-24.75f+x*.5f,.7f,-24.75f+z*.5f);
            blocked[x,z]=walls.Any(c=>(c.ClosestPoint(p)-p).sqrMagnitude<.65f*.65f);
        }
        Vector2Int Cell(Vector3 p) => new Vector2Int(Mathf.Clamp(Mathf.RoundToInt((p.x+24.75f)*2),0,99),Mathf.Clamp(Mathf.RoundToInt((p.z+24.75f)*2),0,99));
        Vector3 Position(Vector2Int p) => new Vector3(-24.75f+p.x*.5f,.5f,-24.75f+p.y*.5f);
        route.Clear(); routePoint=0; walked=0;
        var start=new Vector3(0,.5f,-20);
        Vector3[] goals={new Vector3(-5,.5f,2),new Vector3(10.43f,.5f,15.48f)};
        foreach(var goal in goals)
        {
            var from=Cell(start); var to=Cell(goal);
            var parents=new Dictionary<Vector2Int,Vector2Int>();
            var queue=new Queue<Vector2Int>(); queue.Enqueue(from); parents[from]=from;
            Vector2Int[] moves={Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
            while(queue.Count>0 && !parents.ContainsKey(to))
            {
                var p=queue.Dequeue();
                foreach(var move in moves)
                {
                    var next=p+move;
                    if(next.x<0||next.y<0||next.x>=n||next.y>=n||blocked[next.x,next.y]||parents.ContainsKey(next)) continue;
                    parents[next]=p; queue.Enqueue(next);
                }
            }
            if(!parents.ContainsKey(to)) throw new InvalidOperationException("No route with player clearance to "+goal);
            var leg=new List<Vector3>();
            for(var p=to;p!=from;p=parents[p]) leg.Add(Position(p));
            leg.Reverse(); route.AddRange(leg); start=goal;
        }
        Place(new Vector3(0,.5f,-20));
        Player().GetComponent<Rigidbody>().isKinematic=false;
        Camera.main.GetComponent<FieldCameraController>().enabled=true;
        lastPosition=Player().transform.position;
        Advance(7,.1f); deadline=EditorApplication.timeSinceStartup+180;
    }
    static void Finish(bool passed,string detail)
    {
        SessionState.SetBool(Active,false);
        Note((passed?"PASS ":"FAIL ")+detail);
        if(EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
    }
}
