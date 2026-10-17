using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Opt-in, bounded cave traversal in Play Mode. Never calls the save system.</summary>
[InitializeOnLoad]
public static class CaveInteriorPlaytest
{
    const string Key="CaveInterior.Playtest";
    const string Report="ArtSource/CaveInterior/playtest.txt";
    static readonly List<Vector3> route=new List<Vector3>();
    static Rigidbody body;
    static int point,stage,errors;
    static double nextTick,deadline;
    static double lastWaypointTime,nextProgress;
    static float distance;
    static Vector3 last;
    static bool oldRunInBackground;

    static CaveInteriorPlaytest()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
            { stage=0; nextTick=EditorApplication.timeSinceStartup+2;deadline=EditorApplication.timeSinceStartup+180; }
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key,false))
                Finish(false,"Play Mode ended before verification completed.");
        };
        Application.logMessageReceived+=(message,stack,type)=>
        {
            if(SessionState.GetBool(Key,false) && (type==LogType.Error||type==LogType.Exception||type==LogType.Assert))
            { errors++; File.AppendAllText(Report,"ERROR "+message+"\n"); }
        };
    }

    [MenuItem("Tools/Cave Interior/4 Run Traversal Smoke Test")]
    public static void Begin()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Start the test in Edit Mode.");
        CaveInteriorBuilder.Verify();
        errors=0;
        File.WriteAllText(Report,"Runtime cave smoke test. Enemy encounters are disabled only in this temporary session. No save writes.\n");
        SessionState.SetBool(Key,true);
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling) return;
        double now=EditorApplication.timeSinceStartup;
        if(now<nextTick) return;
        nextTick=now+.025;
        try
        {
            if(now>deadline) throw new TimeoutException("Traversal timed out at waypoint "+point);
            if(SceneManager.GetActiveScene().name!="FildScene") throw new InvalidOperationException("Unexpected scene transition during traversal.");
            if(stage==0)
            {
                oldRunInBackground=Application.runInBackground;
                Application.runInBackground=true;
                if(TutorialPanelController.Current) TutorialPanelController.Current.ConfirmSkip();
                FieldPauseState.Clear();
                var player=GameObject.FindGameObjectWithTag("Player");
                if(!player) throw new InvalidOperationException("Player not spawned.");
                body=player.GetComponent<Rigidbody>();
                player.GetComponent<SimplePlayerMovement>().enabled=false;
                var enemies=UnityEngine.Object.FindObjectsOfType<EnemyFieldController>();
                File.AppendAllText(Report,"Spawned enemies: "+enemies.Length+"; chests: "+UnityEngine.Object.FindObjectsOfType<FieldChestController>().Length+"\n");
                foreach(var enemy in enemies)
                {
                    enemy.enabled=false;
                    foreach(var collider in enemy.GetComponentsInChildren<Collider>()) collider.enabled=false;
                }
                Physics.SyncTransforms();
                CreateRoute();
                body.position=new Vector3(0,.5f,-20);body.velocity=Vector3.zero;
                last=body.position;distance=0;point=0;stage=1;lastWaypointTime=now;nextProgress=now+10;
                ScreenCapture.CaptureScreenshot("ArtSource/CaveInterior/Previews/playmode-entrance.png");
                nextTick=now+.5;return;
            }
            if(stage==2) { Finish(errors==0,"Walked "+distance.ToString("F1")+"m through "+route.Count+" waypoints; grounded; camera inside; runtime errors: "+errors);return; }
            if(now>nextProgress)
            {
                nextProgress=now+10;
                File.AppendAllText(Report,"Progress "+point+"/"+route.Count+" at "+body.position+" rotation "+body.rotation.eulerAngles+"\n");
            }
            if(now-lastWaypointTime>12) throw new InvalidOperationException("Stuck at "+body.position+" toward "+route[point]+"; rotation "+body.rotation.eulerAngles);
            Vector3 position=body.position;
            distance+=Vector3.Distance(position,last);last=position;
            if(position.y<.25f || position.y>.85f) throw new InvalidOperationException("Player lost floor contact: "+position);
            var constraint=UnityEngine.Object.FindObjectOfType<CaveCameraConstraint>();
            if(!constraint || !constraint.target || !constraint.viewCamera) throw new InvalidOperationException("Runtime camera references not bound.");
            var focus=body.position+Vector3.up*1.2f;
            var deltaCamera=Camera.main.transform.position-focus;
            if(Physics.Raycast(focus,deltaCamera.normalized,deltaCamera.magnitude,1<<7,QueryTriggerInteraction.Ignore))
                throw new InvalidOperationException("Following camera crossed cave geometry.");
            Vector3 delta=route[point]-position;delta.y=0;
            if(delta.magnitude<.15f)
            {
                point++;
                lastWaypointTime=now;
                if(point==route.Count)
                {
                    body.velocity=Vector3.zero;
                    ScreenCapture.CaptureScreenshot("ArtSource/CaveInterior/Previews/playmode-exit.png");
                    stage=2;nextTick=now+.5;return;
                }
                if(point==route.Count/2)
                    ScreenCapture.CaptureScreenshot("ArtSource/CaveInterior/Previews/playmode-center.png");
                delta=route[point]-position;delta.y=0;
            }
            Vector3 velocity=delta.normalized*Mathf.Min(4,delta.magnitude*12);
            body.velocity=new Vector3(velocity.x,body.velocity.y,velocity.z);
            body.MoveRotation(Quaternion.Slerp(body.rotation,Quaternion.LookRotation(delta),.2f));
        }
        catch(Exception e) { Finish(false,e.Message); }
    }

    static void CreateRoute()
    {
        const int count=100;
        var blocked=new bool[count,count];
        var cave=new CaveTunnelGeometry();
        for(int x=0;x<count;x++) for(int z=0;z<count;z++)
        {
            var p=new Vector3(-24.75f+x*.5f,.85f,-24.75f+z*.5f);
            blocked[x,z]=cave.Air(p+Vector3.up*.6f)<1.1f || Physics.CheckBox(p-Vector3.up*.25f,new Vector3(.75f,.48f,.75f),Quaternion.identity,1<<7,QueryTriggerInteraction.Ignore);
        }
        Vector2Int Cell(Vector3 p)=>new Vector2Int(Mathf.RoundToInt((p.x+24.75f)*2),Mathf.RoundToInt((p.z+24.75f)*2));
        route.Clear();
        Vector3 from=new Vector3(0,.5f,-20);
        foreach(var goal in new[]{new Vector3(-10,.5f,-9.5f),new Vector3(7.5f,.5f,-2.48f),
            new Vector3(.8f,.5f,3.8f),new Vector3(6.1f,.5f,4.42f),new Vector3(8.4f,.5f,4.42f),
            new Vector3(-5.69f,.5f,9.46f),new Vector3(2.8f,.5f,10),new Vector3(-11,.5f,13),new Vector3(10.43f,.5f,15.48f)})
        {
            Vector2Int start=Cell(from),end=Cell(goal);
            var parents=new Dictionary<Vector2Int,Vector2Int>{{start,start}};
            var queue=new Queue<Vector2Int>();queue.Enqueue(start);
            Vector2Int[] moves={Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
            while(queue.Count>0&&!parents.ContainsKey(end))
            {
                var p=queue.Dequeue();
                foreach(var move in moves)
                {
                    var n=p+move;
                    if(n.x<0||n.y<0||n.x>=count||n.y>=count||blocked[n.x,n.y]||parents.ContainsKey(n)) continue;
                    parents[n]=p;queue.Enqueue(n);
                }
            }
            if(!parents.ContainsKey(end)) throw new InvalidOperationException("No physical route to "+goal);
            var leg=new List<Vector3>();
            for(var p=end;p!=start;p=parents[p]) leg.Add(new Vector3(-24.75f+p.x*.5f,.5f,-24.75f+p.y*.5f));
            leg.Reverse();route.AddRange(leg);from=goal;
            File.AppendAllText(Report,"Physical route available to "+goal+" ("+leg.Count+" cells)\n");
        }
    }

    static void Finish(bool passed,string message)
    {
        SessionState.SetBool(Key,false);
        File.AppendAllText(Report,(passed?"PASS ":"FAIL ")+message+"\n");
        Application.runInBackground=oldRunInBackground;
        if(EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
    }
}
