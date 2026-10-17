using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Carves connected cave passages along the graybox route without moving gameplay points.</summary>
public static class CaveInteriorBuilder
{
    const string ScenePath = "Assets/Scenes/FildScene.unity";
    const string Art = "Assets/Art/Environment/CaveInterior";
    const string Source = "ArtSource/CaveInterior";
    const string RootName = "CaveInterior";
    static BoxCollider[] walls;
    static CaveTunnelGeometry tunnel;
    static readonly HashSet<string> usedMeshes = new HashSet<string>();
    static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();

    static Transform[] All() => SceneManager.GetActiveScene().GetRootGameObjects()
        .SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();

    public static void OpenField()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before authoring.");
        if (SceneManager.GetActiveScene().path == ScenePath) return;
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("An open scene has unsaved changes. Save it before opening FildScene.");
        EditorSceneManager.OpenScene(ScenePath);
    }

    static void RequireScene()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != ScenePath)
            throw new InvalidOperationException("Open FildScene in Edit Mode.");
    }

    [MenuItem("Tools/Cave Interior/1 Build Enclosed Cave")]
    public static void Build()
    {
        RequireScene();
        Directory.CreateDirectory("Library/CaveInterior/Backups");
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),
            "Library/CaveInterior/Backups/FildScene-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity", true);
        Directory.CreateDirectory(Art);
        Directory.CreateDirectory(Source);
        AssetDatabase.Refresh();
        var all = All();
        walls = all.First(t => t.name == "Walls").GetComponentsInChildren<BoxCollider>(true);
        tunnel = new CaveTunnelGeometry();
        File.WriteAllText("Library/CaveInterior/gameplay-before.json", GameplaySignature());
        var shader = Shader.Find("VolcanoCave/Interior Rock");
        if (!shader || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Interior shader is not ready.");
        Material rock = Material("Cave_Stone", shader, new Color(.45f,.445f,.405f), new Color(.235f,.25f,.235f), 1);
        meshes.Clear();
        usedMeshes.Clear();
        foreach (var mesh in AssetDatabase.LoadAllAssetsAtPath(Art + "/CaveInteriorMeshes.asset").OfType<Mesh>())
            meshes[mesh.name] = mesh;
        var old = all.FirstOrDefault(t => t.name == RootName);
        if (old) Object.DestroyImmediate(old.gameObject);
        all = all.Where(t => t).ToArray();
        var root = new GameObject(RootName);
        root.transform.SetParent(all.First(t => t.name == "Environment"), false);
        var shell = Group(root.transform, "CarvedPassages");
        for (int z = 0; z < 5; z++) for (int x = 0; x < 4; x++)
        {
            var mesh = tunnel.Chunk(x,z);
            if (mesh) InstallMesh(shell,mesh,rock,true);
        }
        BuildHangingRocks(root.transform, rock);
        BuildLavaCracks(root.transform);
        BuildLighting(root.transform);
        root.AddComponent<CaveCameraConstraint>();
        root.AddComponent<CaveSpawnRecovery>().Configure(tunnel.Centerline().ToArray());

        // Keep the old authored environment in the scene for comparison and rollback.
        var previous = all.FirstOrDefault(t => t.name == "VolcanoCaveEnvironment");
        if (previous) { previous.gameObject.SetActive(false); PrefabUtility.RecordPrefabInstancePropertyModifications(previous.gameObject); }
        var testRoof = all.FirstOrDefault(t => t.name == "CaveCeiling");
        if (testRoof) testRoof.gameObject.SetActive(false);
        foreach (var t in all.Where(t => t.parent == null &&
            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == "Assets/Art/Environment/VolcanoCave/VolcanoCave.fbx"))
            foreach (var r in t.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        foreach (var wall in walls)
        {
            var renderer = wall.GetComponent<Renderer>();
            if (renderer) renderer.enabled = false;
        }
        var ground = all.First(t => t.name == "Ground_Plane").GetComponent<Renderer>();
        if (ground) ground.enabled = false;
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.34f,.35f,.34f);
        RenderSettings.ambientEquatorColor = new Color(.30f,.305f,.29f);
        RenderSettings.ambientGroundColor = new Color(.14f,.13f,.11f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(.045f,.044f,.043f);
        RenderSettings.fogDensity = .038f;
        foreach (var light in Object.FindObjectsOfType<Light>())
            if (light.type == LightType.Directional) { light.intensity = .15f; light.color = new Color(.9f,.85f,.76f); }
        if (Camera.main)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = RenderSettings.fogColor;
        }
        Physics.SyncTransforms();
        Verify();
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, Art + "/CaveInterior.prefab", InteractionMode.AutomatedAction);
        foreach (var unused in meshes.Where(pair=>!usedMeshes.Contains(pair.Key)).Select(pair=>pair.Value))
        {
            // Keep the library's main object/file ID; obsolete submeshes belonged to the replaced shell.
            if (AssetDatabase.IsMainAsset(unused)) { unused.Clear(); unused.name="CaveMeshLibrary"; EditorUtility.SetDirty(unused); }
            else Object.DestroyImmediate(unused,true);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Selection.activeGameObject = root;
        Preview();
    }

    static Transform Group(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static Material Material(string name, Shader shader, Color color, Color dark, float scale)
    {
        string path = Art + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
        mat.shader = shader;
        mat.SetColor("_Color", color);
        mat.SetColor("_DarkColor", dark);
        mat.SetFloat("_Scale", scale);
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static float Noise(float x, float z) => Mathf.PerlinNoise(x + 81.67f, z + 37.21f);

    static float RoofHeight(float x, float z)
    {
        float y=1.7f;
        if(tunnel.Air(new Vector3(x,y,z))<=0) return 0;
        while(y<7.5f && tunnel.Air(new Vector3(x,y,z))>0) y+=.05f;
        return y;
    }

    static Color VertexColor(Vector3 p, bool ceiling = false)
    {
        float mineral = .87f + .2f * Noise(p.x*.9f + p.y, p.z*.9f-p.y);
        float ao = ceiling ? .84f : Mathf.Lerp(.65f, 1, Mathf.Clamp01(p.y / 1.2f));
        return new Color(mineral, mineral, mineral, ao);
    }

    static void BuildHangingRocks(Transform root, Material material)
    {
        var random = new System.Random(20261017);
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        for (int i=0;i<200;i++)
        {
            float x=(float)random.NextDouble()*30-15,z=(float)random.NextDouble()*43-22;
            float d=tunnel.Air(new Vector3(x,1.7f,z));
            if(d<.35f || d>1.7f) continue;
            float top=RoofHeight(x,z)+.18f;
            float height=Mathf.Min(top-3.0f,.55f+(float)random.NextDouble()*.85f);
            if(height<.3f) continue;
            float radius=.2f+(float)random.NextDouble()*.25f;
            int start=vertices.Count;
            for(int ring=0;ring<4;ring++) for(int j=0;j<7;j++)
            {
                float t=ring/3f, a=j*Mathf.PI*2/7;
                float r=radius*Mathf.Pow(1-t,1.1f)+.015f;
                vertices.Add(new Vector3(x+Mathf.Cos(a)*r+t*.13f,top-t*height,z+Mathf.Sin(a)*r));
            }
            for(int ring=0;ring<3;ring++) for(int j=0;j<7;j++)
                Quad(triangles,start+ring*7+j,start+ring*7+(j+1)%7,start+(ring+1)*7+(j+1)%7,start+(ring+1)*7+j,false);
        }
        CreateMesh(root,"HangingStone",vertices,triangles,material,true,true);
    }

    static void Quad(List<int> triangles,int a,int b,int c,int d,bool reverse)
    {
        if(reverse) triangles.AddRange(new[]{a,c,b,a,d,c});
        else triangles.AddRange(new[]{a,b,c,a,c,d});
    }

    static void CreateMesh(Transform parent,string name,List<Vector3> vertices,List<int> triangles,Material material,bool collider,bool ceiling=false)
    {
        var generated=new Mesh {name=name};
        generated.SetVertices(vertices); generated.SetTriangles(triangles,0);
        generated.RecalculateNormals(); generated.RecalculateBounds();
        InstallMesh(parent,generated,material,collider,ceiling);
    }

    static void InstallMesh(Transform parent,Mesh generated,Material material,bool collider,bool ceiling=false)
    {
        string name=generated.name;
        usedMeshes.Add(name);
        generated.SetColors(generated.vertices.Select(v=>VertexColor(v,ceiling)).ToList());
        generated.tangents=generated.normals.Select(n=>
        {
            Vector3 t=Vector3.Cross(Mathf.Abs(n.y)>.95f?Vector3.forward:Vector3.up,n).normalized;
            return new Vector4(t.x,t.y,t.z,1);
        }).ToArray();
        if(meshes.TryGetValue(name,out var existing))
        {
            EditorUtility.CopySerialized(generated,existing); Object.DestroyImmediate(generated); generated=existing;
        }
        else
        {
            string path=Art+"/CaveInteriorMeshes.asset";
            if(!File.Exists(path)) AssetDatabase.CreateAsset(generated,path);
            else AssetDatabase.AddObjectToAsset(generated,path);
            meshes.Add(name,generated);
        }
        EditorUtility.SetDirty(generated);
        var go=new GameObject(name) {layer=LayerMask.NameToLayer("Environment")};
        go.transform.SetParent(parent,false);
        go.AddComponent<MeshFilter>().sharedMesh=generated;
        go.AddComponent<MeshRenderer>().sharedMaterial=material;
        if(collider) go.AddComponent<MeshCollider>().sharedMesh=generated;
    }

    static void BuildLavaCracks(Transform root)
    {
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/VolcanoCave/Materials/VC_Lava.mat");
        if(!material) throw new InvalidOperationException("Missing lava material.");
        var detail=Group(root,"MineralFissures");
        Vector3[] centers={new Vector3(-7.8f,0,-8.7f),new Vector3(7.8f,0,-3.65f),
            new Vector3(8.9f,0,5.8f),new Vector3(-6.1f,0,8.2f),new Vector3(-1,0,17.5f)};
        for(int k=0;k<centers.Length;k++)
        {
            var vertices=new List<Vector3>(); var triangles=new List<int>();
            for(int i=0;i<=16;i++)
            {
                float z=(i/16f-.5f)*2.8f;
                var p=centers[k]+new Vector3(Mathf.Sin(i*1.3f+k)*.055f,.023f,z);
                vertices.Add(p-Vector3.right*.025f);vertices.Add(p+Vector3.right*.025f);
                if(i>0) Quad(triangles,(i-1)*2,(i-1)*2+1,i*2+1,i*2,true);
            }
            CreateMesh(detail,"Fissure_"+k,vertices,triangles,material,false);
        }
    }

    static void BuildLighting(Transform root)
    {
        var lights=Group(root,"CaveBounceLights");
        Vector3[] positions={new Vector3(0,2.8f,-20),new Vector3(-4.5f,2.3f,-13.4f),new Vector3(-9,2.7f,-8),
            new Vector3(-2.8f,2.6f,-2.7f),new Vector3(8,2.4f,-2.5f),new Vector3(.8f,3,3.8f),
            new Vector3(8,3,4.4f),new Vector3(-5.2f,2.8f,9.5f),new Vector3(3,2.6f,11),
            new Vector3(-11,2.5f,13),new Vector3(-2,3,16.5f),new Vector3(10,3,16)};
        for(int i=0;i<positions.Length;i++)
        {
            var light=Group(lights,"LavaBounce_"+i).gameObject.AddComponent<Light>();
            light.transform.position=positions[i];
            light.type=LightType.Point; light.color=i%3==1?new Color(1,.71f,.48f):new Color(.86f,.91f,.85f);
            light.range=8; light.intensity=i%3==1?1.6f:1.3f;
            light.shadows=LightShadows.Soft;light.shadowStrength=.8f;light.shadowBias=.03f;
            light.renderMode=LightRenderMode.ForcePixel;
        }
    }

    static string GameplaySignature()
    {
        return string.Join("\n",All().Where(t=>t.name=="Walls" || t.name=="Ground_Plane" ||
            t.name.Contains("SpawnPoint") || t.name.Contains("TransitionTrigger"))
            .SelectMany(t=>t.GetComponentsInChildren<Transform>(true)).Distinct()
            .OrderBy(t=>t.name).ThenBy(t=>t.position.x).ThenBy(t=>t.position.z).Select(t=>t.name+"|"+t.position.ToString("F5")+"|"+
                t.rotation.ToString("F5")+"|"+t.lossyScale.ToString("F5")+"|"+
                string.Join(";",t.GetComponents<Collider>().Select(c=>EditorJsonUtility.ToJson(c)))));
    }

    [MenuItem("Tools/Cave Interior/2 Verify Route and Camera")]
    public static void Verify()
    {
        RequireScene(); Physics.SyncTransforms();
        tunnel=new CaveTunnelGeometry();
        var all=All();
        walls=all.First(t=>t.name=="Walls").GetComponentsInChildren<BoxCollider>(true);
        var root=all.First(t=>t.name==RootName);
        var shell=root.GetComponentsInChildren<MeshCollider>();
        var errors=root.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials)
            .Where(m=>!m || !m.shader || ShaderUtil.ShaderHasError(m.shader)).ToArray();
        if(errors.Length>0) throw new InvalidOperationException("Interior has missing materials or shader errors.");
        string signaturePath="Library/CaveInterior/gameplay-before.json";
        if(File.Exists(signaturePath) && !File.ReadAllLines(signaturePath).OrderBy(s=>s)
            .SequenceEqual(GameplaySignature().Split('\n').OrderBy(s=>s)))
            throw new InvalidOperationException("Gameplay transforms or colliders changed.");
        // The old empty outside area is now solid rock. Validate the intended routes and all objectives.
        const int count=100; const float step=.5f;
        var blocked=new bool[count,count]; var seen=new bool[count,count];
        for(int x=0;x<count;x++) for(int z=0;z<count;z++)
        {
            var p=new Vector3(-24.75f+x*step,.85f,-24.75f+z*step);
            blocked[x,z]=tunnel.Air(p)<.7f || Physics.CheckBox(p,new Vector3(.56f,.4f,.56f),Quaternion.identity,1<<7,QueryTriggerInteraction.Ignore);
        }
        var start=all.First(t=>t.name=="PlayerSpawnPoint").position;
        int sx=Mathf.RoundToInt((start.x+24.75f)/step),sz=Mathf.RoundToInt((start.z+24.75f)/step);
        var queue=new Queue<Vector2Int>(); queue.Enqueue(new Vector2Int(sx,sz)); seen[sx,sz]=true;
        int visited=0; bool exitReached=false;
        var exit=all.First(t=>t.name=="BossFieldTransitionTrigger").GetComponent<BoxCollider>();
        int[] dx={1,-1,0,0},dz={0,0,1,-1};
        while(queue.Count>0)
        {
            var p=queue.Dequeue(); visited++;
            var world=new Vector3(-24.75f+p.x*step,.5f,-24.75f+p.y*step);
            if(BoxDistance(exit,world)<.001f) exitReached=true;
            for(int i=0;i<4;i++)
            {
                int x=p.x+dx[i],z=p.y+dz[i];
                if(x<0||z<0||x>=count||z>=count||seen[x,z]||blocked[x,z]) continue;
                seen[x,z]=true;queue.Enqueue(new Vector2Int(x,z));
            }
        }
        if(!exitReached) throw new InvalidOperationException("Exit is not reachable.");
        var objectives=Objectives();
        var recovery=root.GetComponent<CaveSpawnRecovery>();
        foreach(var objective in objectives)
        {
            Vector3 p=objective.Value;
            int x=Mathf.RoundToInt((p.x+24.75f)/step),z=Mathf.RoundToInt((p.z+24.75f)/step);
            if(x<0||z<0||x>=count||z>=count||blocked[x,z]||!seen[x,z])
                throw new InvalidOperationException("Objective not reachable: "+objective.Key+" at "+p);
            if(!recovery.HasFloor(p)) throw new InvalidOperationException("Objective has no cave floor: "+objective.Key);
        }
        foreach(var obsolete in new[]{new Vector3(-20,.5f,0),new Vector3(20,.5f,-20),new Vector3(-5,.5f,2)})
            if(recovery.HasFloor(obsolete)||!recovery.TryFindRecovery(obsolete,out _))
                throw new InvalidOperationException("Old-save cave recovery failed: "+obsolete);
        int roofHits=0;
        for(int x=0;x<count;x++) for(int z=0;z<count;z++)
        {
            if(!seen[x,z]) continue;
            var p=new Vector3(-24.75f+x*step,1.7f,-24.75f+z*step);
            if(shell.Any(c=>c.Raycast(new Ray(p,Vector3.up),out _,8))) roofHits++;
            else throw new InvalidOperationException("Open ceiling above "+p);
        }
        int poses=0,adjusted=0;
        var probe=new GameObject("CaveCameraVerification") {hideFlags=HideFlags.HideAndDontSave};
        var constraint=probe.AddComponent<CaveCameraConstraint>();
        EditorUtility.CopySerialized(root.GetComponent<CaveCameraConstraint>(),constraint);
        constraint.viewCamera=Camera.main;
        try
        {
        foreach(var position in objectives.Values.Concat(new[]{new Vector3(.8f,.5f,3.8f),new Vector3(-11,.5f,13)}))
            foreach(float pitch in new[]{-15f,-12.5f,25f,60f}) for(int yaw=0;yaw<360;yaw+=30)
            {
                Vector3 focus=position+Vector3.up*1.2f;
                Vector3 desired=position+Quaternion.Euler(pitch,yaw,0)*new Vector3(0,3,-6);
                Vector3 resolved=constraint.ResolvePosition(focus,desired);
                if((resolved-desired).sqrMagnitude>.0001f) adjusted++;
                Vector3 direction=resolved-focus;
                if(Physics.Raycast(focus,direction.normalized,direction.magnitude,1<<7,QueryTriggerInteraction.Ignore))
                    throw new InvalidOperationException("Camera crosses a wall or ceiling.");
                poses++;
            }
        }
        finally { Object.DestroyImmediate(probe); }
        int triangles=root.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3);
        Directory.CreateDirectory(Source);
        File.WriteAllText(Source+"/validation.txt","Carved tunnel layout v2\nGameplay transforms/colliders unchanged\nOriginal graybox retained, inactive walls: "+walls.Count(w=>!w.gameObject.activeInHierarchy)+"/"+walls.Length+
            "\nExit reachable: "+exitReached+"\nAll objectives reachable: "+objectives.Count+"\nReachable half-meter cells: "+visited+"\nCeiling coverage rays: "+roofHits+
            "\nCamera poses: "+poses+"\nConstrained camera poses: "+adjusted+"\nTriangles: "+triangles+
            "\nOld-save recovery cases: 3\nShader errors: 0\nEdit-mode physics checks; not a full playthrough.\n");
        Debug.Log("Cave Interior: route, roof coverage and "+poses+" camera poses verified.");
    }

    public static Dictionary<string,Vector3> Objectives()
    {
        var creator=Object.FindObjectOfType<FieldCreator>();
        var data=(FieldData)new SerializedObject(creator).FindProperty("fieldData").objectReferenceValue;
        var result=new Dictionary<string,Vector3>();
        foreach(var spawn in data.SpawnPoints) result.Add(spawn.SpawnId,spawn.Position);
        foreach(var item in data.FieldObjects.Where(o=>o.Prefab&&o.Prefab.GetComponent<FieldChestController>())) result.Add(item.ObjectId,item.Position);
        foreach(var recruit in data.RecruitPoints) result.Add(recruit.RecruitId,recruit.Position);
        foreach(var point in All().Where(t=>t.name=="PlayerSpawnPoint"||t.name=="BossExitReturnSpawnPoint"))
        { Vector3 p=point.position;p.y=.5f;result.Add(point.name,p); }
        return result;
    }

    [MenuItem("Tools/Cave Interior/5 Reload and Verify Saved Scene")]
    public static void ReloadAndVerify()
    {
        RequireScene();
        if(SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save your scene changes before the reload check.");
        EditorSceneManager.OpenScene(ScenePath);
        Verify();
        Preview();
        File.AppendAllText(Source+"/validation.txt","Saved scene reload: PASS\n");
    }

    static float BoxDistance(BoxCollider box, Vector3 point)
    {
        Vector3 local=box.transform.InverseTransformPoint(point)-box.center;
        Vector3 outside=new Vector3(Mathf.Max(0,Mathf.Abs(local.x)-box.size.x*.5f),
            Mathf.Max(0,Mathf.Abs(local.y)-box.size.y*.5f),Mathf.Max(0,Mathf.Abs(local.z)-box.size.z*.5f));
        return Vector3.Scale(outside,box.transform.lossyScale).magnitude;
    }

    [MenuItem("Tools/Cave Interior/3 Render Previews")]
    public static void Preview()
    {
        RequireScene();
        Directory.CreateDirectory(Source+"/Previews");
        Capture("entrance",new Vector3(0,.5f,-20),0,-12.5f);
        Capture("first-turn",new Vector3(-9.2f,.5f,-7.5f),55,-12.5f);
        Capture("central-corridor",new Vector3(.8f,.5f,3.8f),0,-12.5f);
        Capture("treasure-branch",new Vector3(5,.5f,-2.5f),90,-12.5f);
        Capture("upper-turn",new Vector3(-9.3f,.5f,9.5f),-55,-12.5f);
        Capture("exit",new Vector3(10,.5f,15.48f),90,-12.5f);
        Capture("high-angle",new Vector3(.8f,.5f,3.8f),65,60);
        MapPreview();
    }

    static void MapPreview()
    {
        tunnel=new CaveTunnelGeometry();
        const int size=700;
        var texture=new Texture2D(size,size,TextureFormat.RGB24,false);
        var pixels=new Color[size*size];
        for(int y=0;y<size;y++) for(int x=0;x<size;x++)
        {
            var p=new Vector3((x/(float)size-.5f)*50,.85f,(y/(float)size-.5f)*50);
            float d=tunnel.Air(p);
            pixels[y*size+x]=d>0?Color.Lerp(new Color(.23f,.29f,.29f),new Color(.65f,.70f,.65f),Mathf.Clamp01(d)):
                new Color(.055f,.065f,.07f);
        }
        foreach(var p in Objectives().Values)
        {
            int cx=Mathf.RoundToInt((p.x/50+.5f)*size),cy=Mathf.RoundToInt((p.z/50+.5f)*size);
            for(int y=-4;y<=4;y++) for(int x=-4;x<=4;x++) pixels[(cy+y)*size+cx+x]=new Color(1,.55f,.15f);
        }
        texture.SetPixels(pixels);texture.Apply();File.WriteAllBytes(Source+"/Previews/tunnel-map.png",texture.EncodeToPNG());Object.DestroyImmediate(texture);
    }

    static void Capture(string name,Vector3 player,float yaw,float pitch)
    {
        var go=new GameObject("CavePreviewCamera") {hideFlags=HideFlags.HideAndDontSave};
        var camera=go.AddComponent<Camera>();
        camera.fieldOfView=60;camera.nearClipPlane=.1f;camera.farClipPlane=80;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=RenderSettings.fogColor;
        camera.cullingMask=~(1<<5);camera.aspect=16f/9;
        var constraint=go.AddComponent<CaveCameraConstraint>();constraint.viewCamera=camera;
        Vector3 focus=player+Vector3.up*1.2f;
        camera.transform.position=constraint.ResolvePosition(focus,player+Quaternion.Euler(pitch,yaw,0)*new Vector3(0,3,-6));
        camera.transform.LookAt(focus);
        var rt=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32);
        var previous=RenderTexture.active;
        var playerObject=GameObject.FindGameObjectWithTag("Player");
        var originalRenderer=playerObject?playerObject.GetComponent<MeshRenderer>():null;
        bool wasHidden=originalRenderer&&originalRenderer.forceRenderingOff;
        var previewPlayer=new GameObject("PreviewPlayer") {hideFlags=HideFlags.HideAndDontSave};
        if(originalRenderer)
        {
            previewPlayer.transform.SetPositionAndRotation(player,playerObject.transform.rotation);
            previewPlayer.transform.localScale=playerObject.transform.lossyScale;
            previewPlayer.AddComponent<MeshFilter>().sharedMesh=playerObject.GetComponent<MeshFilter>().sharedMesh;
            previewPlayer.AddComponent<MeshRenderer>().sharedMaterials=originalRenderer.sharedMaterials;
        }
        try
        {
            if(originalRenderer) originalRenderer.forceRenderingOff=true;
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
            File.WriteAllBytes(Source+"/Previews/"+name+".png",texture.EncodeToPNG());Object.DestroyImmediate(texture);
        }
        finally
        {
            if(originalRenderer) originalRenderer.forceRenderingOff=wasHidden;
            Object.DestroyImmediate(previewPlayer);
            camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(go);
        }
    }
}
