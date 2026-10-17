using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class VolcanoCaveBuilder
{
    const string Art="Assets/Art/Environment/VolcanoCave";
    const string Source="ArtSource/VolcanoCave";
    const string RootName="VolcanoCaveEnvironment";
    static Transform[] SceneTransforms() => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
    static VolcanoCaveBridge.Layout Layout() => JsonUtility.FromJson<VolcanoCaveBridge.Layout>(File.ReadAllText(Source+"/layout.json"));
    static void RequireScene()
    {
        if(EditorApplication.isPlaying || SceneManager.GetActiveScene().path!="Assets/Scenes/FildScene.unity")
            throw new InvalidOperationException("Open FildScene in Edit Mode.");
    }
    [MenuItem("Tools/Volcano Cave/2 Build Environment")]
    public static void Build()
    {
        RequireScene();
        // Preserve any new in-editor work on each rebuild, including changes made between runs.
        Directory.CreateDirectory("Library/VolcanoCave/SceneBackups");
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),"Library/VolcanoCave/SceneBackups/FildScene-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var importer=(ModelImporter)AssetImporter.GetAtPath(Art+"/VolcanoCave.fbx");
        importer.globalScale=1;
        importer.useFileScale=true;
        importer.bakeAxisConversion=true;
        importer.importCameras=false; importer.importLights=false; importer.importAnimation=false;
        importer.addCollider=false; importer.isReadable=false;
        importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        importer.SaveAndReimport();
        Directory.CreateDirectory(Art+"/Materials");
        var rockShader=Shader.Find("VolcanoCave/Rock");
        var lavaShader=Shader.Find("VolcanoCave/Lava");
        if(!rockShader || !lavaShader) throw new InvalidOperationException("Volcano shaders are not imported.");
        var palette=new Dictionary<string,Color>();
        Color[] rocks={new Color(.095f,.115f,.145f),new Color(.14f,.165f,.19f),new Color(.20f,.215f,.235f),new Color(.12f,.115f,.13f),new Color(.24f,.245f,.25f)};
        Color[] ground={new Color(.105f,.092f,.09f),new Color(.125f,.114f,.109f),new Color(.145f,.131f,.122f),new Color(.115f,.105f,.104f)};
        for(int i=0;i<rocks.Length;i++) palette["VC_Basalt_"+i]=Color.Lerp(new Color(.25f,.27f,.30f),rocks[i]*2.0f,.5f);
        for(int i=0;i<ground.Length;i++) palette["VC_Ground_"+i]=new Color(.22f,.205f,.195f)+new Color(i*.003f,i*.003f,i*.003f);
        palette["VC_Lava"]=new Color(1,.11f,.009f);
        palette["VC_Ember"]=new Color(1,.43f,.03f);
        palette["VC_Crust"]=new Color(.055f,.043f,.047f);
        palette["VC_Rock"]=Color.white;
        var mats=new Dictionary<string,Material>();
        foreach(var pair in palette)
        {
            bool hot=pair.Key=="VC_Lava"||pair.Key=="VC_Ember";
            string path=Art+"/Materials/"+pair.Key+".mat";
            Material mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat) { mat=new Material(hot?lavaShader:rockShader); AssetDatabase.CreateAsset(mat,path); }
            mat.shader=hot?lavaShader:rockShader; mat.color=pair.Value;
            if(!hot) { mat.SetFloat("_Fade",1); mat.SetFloat("_VertexPalette",pair.Key=="VC_Rock"?1:0); }
            mat.enableInstancing=true; EditorUtility.SetDirty(mat); mats[pair.Key]=mat;
        }
        Transform old=SceneTransforms().FirstOrDefault(t=>t.name==RootName);
        if(old) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var root=new GameObject(RootName);
        var environment=SceneTransforms().First(t=>t.name=="Environment");
        root.transform.SetParent(environment,false);
        var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/VolcanoCave.fbx"));
        model.name="Geometry"; model.transform.SetParent(root.transform,false);
        foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>mats[m.name]).ToArray();
            renderer.gameObject.layer=LayerMask.NameToLayer("Environment");
            renderer.receiveShadows=true;
        }
        // Assert the complete coordinate conversion before changing the live whitebox.
        ValidateAlignment(model,Layout());
        var all=SceneTransforms();
        // A manually previewed instance of the same generated FBX would double the environment.
        // Keep the object (and its transform) but hide its duplicate renderers.
        foreach(var t in all.Where(t=>t.parent==null && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)==Art+"/VolcanoCave.fbx"))
            foreach(var r in t.GetComponentsInChildren<Renderer>()) r.enabled=false;
        var walls=all.First(t=>t.name=="Walls");
        foreach(var renderer in walls.GetComponentsInChildren<MeshRenderer>(true)) renderer.enabled=false;
        var groundPlane=all.First(t=>t.name=="Ground_Plane");
        groundPlane.GetComponent<MeshRenderer>().enabled=false;
        var fade=root.AddComponent<VolcanoCaveVisibility>();
        fade.occluders=model.GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("Wall_")||r.name.StartsWith("Rim_")||r.name.StartsWith("Arch_")||r.name.StartsWith("Tunnel_")||r.name.StartsWith("Overhang_")).ToArray();
        // Scene references bind at runtime, so the reusable prefab remains portable.
        RenderSettings.skybox=null;
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.48f,.52f,.60f);
        RenderSettings.ambientEquatorColor=new Color(.31f,.33f,.39f);
        RenderSettings.ambientGroundColor=new Color(.12f,.075f,.055f);
        RenderSettings.fog=true; RenderSettings.fogMode=FogMode.ExponentialSquared;
        RenderSettings.fogColor=new Color(.09f,.10f,.14f); RenderSettings.fogDensity=.012f;
        var main=Camera.main;
        if(main) { main.clearFlags=CameraClearFlags.SolidColor; main.backgroundColor=RenderSettings.fogColor; }
        foreach(var light in UnityEngine.Object.FindObjectsOfType<Light>())
            if(light.type==LightType.Directional) { light.color=new Color(.76f,.83f,1); light.intensity=.85f; light.transform.rotation=Quaternion.Euler(48,-35,0); light.shadows=LightShadows.Soft; }
        var lights=new GameObject("LavaLighting"); lights.transform.SetParent(root.transform,false);
        Vector3[] lightsAt={new Vector3(-13,1.6f,-10),new Vector3(7,1.6f,-4),new Vector3(-7,1.6f,12),new Vector3(14.5f,2.3f,15.5f)};
        foreach(var position in lightsAt)
        {
            var go=new GameObject("LavaBounce"); go.transform.SetParent(lights.transform,false); go.transform.position=position;
            var l=go.AddComponent<Light>(); l.type=LightType.Point; l.range=9; l.intensity=2.1f;
            l.color=new Color(1,.24f,.055f); l.shadows=LightShadows.None; l.renderMode=LightRenderMode.ForcePixel;
        }
        CreateParticles(root.transform);
        PrefabUtility.SaveAsPrefabAssetAndConnect(root,Art+"/VolcanoCaveEnvironment.prefab",InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Selection.activeGameObject=root;
        if(SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.LookAt(new Vector3(0,0,0),Quaternion.Euler(52,0,0),62);
        Verify(); Preview();
    }
    static void ValidateAlignment(GameObject model,VolcanoCaveBridge.Layout layout)
    {
        var transforms=model.GetComponentsInChildren<Transform>();
        // Exporter includes handedness conversion; never hide misalignment with a mirrored root.
        for(int i=0;i<layout.walls.Length;i++)
        {
            var w=layout.walls[i];
            Vector3 center=w.position+w.rotation*Vector3.Scale(w.center,w.scale);
            var t=transforms.First(x=>x.name=="Wall_"+i.ToString("00"));
            if(Vector3.Distance(t.position,center)>.02f) throw new InvalidOperationException("FBX misalignment: "+t.name+" "+t.position+" expected "+center);
        }
    }
    static void CreateParticles(Transform root)
    {
        string texPath=Art+"/Materials/SoftParticle.png";
        if(!File.Exists(texPath))
        {
            var tex=new Texture2D(32,32,TextureFormat.RGBA32,false);
            for(int y=0;y<32;y++) for(int x=0;x<32;x++)
            {
                float a=Mathf.Clamp01(1-new Vector2((x-15.5f)/15.5f,(y-15.5f)/15.5f).sqrMagnitude);
                tex.SetPixel(x,y,new Color(1,1,1,a*a));
            }
            tex.Apply(); File.WriteAllBytes(texPath,tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(texPath);
        }
        Material mat=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/EmberParticles.mat");
        if(!mat) { mat=new Material(Shader.Find("Legacy Shaders/Particles/Additive")); AssetDatabase.CreateAsset(mat,Art+"/Materials/EmberParticles.mat"); }
        mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        mat.SetColor("_TintColor",new Color(1,.25f,.035f,.6f));
        var go=new GameObject("DriftingEmbers"); go.transform.SetParent(root,false); go.transform.localPosition=new Vector3(0,.1f,0);
        var ps=go.AddComponent<ParticleSystem>(); var main=ps.main;
        main.startLifetime=new ParticleSystem.MinMaxCurve(3,6); main.startSpeed=.28f;
        main.startSize=new ParticleSystem.MinMaxCurve(.035f,.09f); main.maxParticles=100;
        main.simulationSpace=ParticleSystemSimulationSpace.World; main.startColor=new Color(1,.5f,.15f,.65f);
        var emission=ps.emission; emission.rateOverTime=12;
        var shape=ps.shape; shape.shapeType=ParticleSystemShapeType.Box; shape.scale=new Vector3(44,.1f,44);
        var velocity=ps.velocityOverLifetime; velocity.enabled=true; velocity.space=ParticleSystemSimulationSpace.World;
        velocity.x=.08f; velocity.y=.25f; velocity.z=.04f;
        var color=ps.colorOverLifetime; color.enabled=true; var gradient=new Gradient();
        gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(1,.3f,.1f),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.6f,.15f),new GradientAlphaKey(0,1)}); color.color=gradient;
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial=mat;
        // Sparse dark ash uses the same soft texture, with alpha blending instead of additive light.
        var ashMat=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/AshParticles.mat");
        if(!ashMat) { ashMat=new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")); AssetDatabase.CreateAsset(ashMat,Art+"/Materials/AshParticles.mat"); }
        ashMat.mainTexture=mat.mainTexture; ashMat.SetColor("_TintColor",new Color(.16f,.13f,.12f,.16f));
        var ash=UnityEngine.Object.Instantiate(go,root); ash.name="SparseAsh";
        var ap=ash.GetComponent<ParticleSystem>(); var am=ap.main; am.startSize=new ParticleSystem.MinMaxCurve(.4f,.8f); am.maxParticles=24;
        var ae=ap.emission; ae.rateOverTime=3; ash.GetComponent<ParticleSystemRenderer>().sharedMaterial=ashMat;
    }
    [MenuItem("Tools/Volcano Cave/3 Verify Alignment and Route")]
    public static void Verify()
    {
        RequireScene(); var layout=Layout(); var all=SceneTransforms();
        var root=all.First(t=>t.name==RootName);
        ValidateAlignment(root.Find("Geometry").gameObject,layout);
        var colliders=all.First(t=>t.name=="Walls").GetComponentsInChildren<BoxCollider>(true);
        if(colliders.Length!=layout.walls.Length) throw new InvalidOperationException("Wall collider count changed.");
        foreach(var entry in layout.walls)
        {
            var match=colliders.FirstOrDefault(c=>c.name==entry.name&&Vector3.Distance(c.transform.position,entry.position)<.001f);
            if(!match || !match.enabled || (match.size-entry.size).sqrMagnitude>.000001f || Quaternion.Angle(match.transform.rotation,entry.rotation)>.01f || (match.transform.lossyScale-entry.scale).sqrMagnitude>.000001f)
                throw new InvalidOperationException("Wall collision changed: "+entry.name);
        }
        if(root.GetComponentsInChildren<Collider>(true).Length!=0) throw new InvalidOperationException("Decorative environment must not introduce blockers.");
        var exit=all.First(t=>t.name=="BossFieldTransitionTrigger").GetComponent<BoxCollider>();
        if((exit.size-layout.exit.size).sqrMagnitude>.000001f || (exit.center-layout.exit.center).sqrMagnitude>.000001f) throw new InvalidOperationException("Exit trigger changed.");
        int tris=root.GetComponentsInChildren<MeshFilter>().Sum(f=>(int)Enumerable.Range(0,f.sharedMesh.subMeshCount).Sum(s=>(long)f.sharedMesh.GetIndexCount(s)/3));
        if(tris>150000) throw new InvalidOperationException("Environment exceeds triangle budget.");
        var errors=root.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Where(m=>!m||!m.shader||ShaderUtil.ShaderHasError(m.shader)).ToArray();
        if(errors.Length>0) throw new InvalidOperationException("Missing material or shader error.");
        Physics.SyncTransforms();
        // Grid reachability uses the original oriented collider geometry and a 0.3m clearance.
        const int cells=100; const float step=.5f; var blocked=new bool[cells,cells];
        for(int x=0;x<cells;x++) for(int z=0;z<cells;z++)
        {
            Vector3 p=new Vector3(-24.75f+x*step,.7f,-24.75f+z*step);
            blocked[x,z]=colliders.Any(c=>(c.ClosestPoint(p)-p).sqrMagnitude<.30f*.30f);
        }
        var spawn=all.First(t=>t.name=="PlayerSpawnPoint").position;
        int sx=Mathf.Clamp(Mathf.RoundToInt((spawn.x+24.75f)/step),0,99),sz=Mathf.Clamp(Mathf.RoundToInt((spawn.z+24.75f)/step),0,99);
        var seen=new bool[cells,cells]; var queue=new Queue<Vector2Int>(); queue.Enqueue(new Vector2Int(sx,sz)); seen[sx,sz]=true;
        int visited=0; bool reachesExit=false;
        int[] dx={1,-1,0,0},dz={0,0,1,-1};
        while(queue.Count>0)
        {
            var p=queue.Dequeue(); visited++;
            if(exit.bounds.Contains(new Vector3(-24.75f+p.x*step,.5f,-24.75f+p.y*step))) reachesExit=true;
            for(int i=0;i<4;i++) { int x=p.x+dx[i],z=p.y+dz[i]; if(x<0||z<0||x>=cells||z>=cells||seen[x,z]||blocked[x,z]) continue; seen[x,z]=true; queue.Enqueue(new Vector2Int(x,z)); }
        }
        if(!reachesExit) throw new InvalidOperationException("No path from spawn to exit at 0.3m clearance.");
        var renderers=root.GetComponentsInChildren<Renderer>();
        int slots=renderers.Sum(r=>r.sharedMaterials.Length);
        int materials=renderers.SelectMany(r=>r.sharedMaterials).Distinct().Count();
        string report="Alignment: 26/26 wall origins within 2cm\nOriginal wall colliders unchanged\nNew environment colliders: 0\nExit trigger unchanged\nReachable exit: true\nReachable grid cells: "+visited+"\nTriangles: "+tris+"\nRenderers: "+renderers.Length+"\nMaterial slots: "+slots+"\nShared materials: "+materials+"\nShader errors: 0\n";
        File.WriteAllText(Source+"/unity-validation.txt",report); Debug.Log("Volcano verification passed. "+report);
    }
    [MenuItem("Tools/Volcano Cave/4 Render Preview Images")]
    public static void Preview()
    {
        RequireScene(); Directory.CreateDirectory(Source+"/Previews");
        Capture("unity-overview",new Vector3(0,54,-45),new Vector3(0,0,0),55);
        Capture("unity-entrance",new Vector3(0,5.25f,-26.7f),new Vector3(0,1.2f,-20),60);
        Capture("unity-center",new Vector3(-8,5.25f,-4.5f),new Vector3(-5,1.2f,2),60);
        Capture("unity-exit",new Vector3(5.5f,4.5f,15.48f),new Vector3(13,2,15.48f),60);
    }
    static void Capture(string name,Vector3 from,Vector3 to,float fov)
    {
        var go=new GameObject("VolcanoPreviewCamera"){hideFlags=HideFlags.HideAndDontSave};
        var camera=go.AddComponent<Camera>(); camera.transform.position=from; camera.transform.LookAt(to); camera.fieldOfView=fov;
        camera.nearClipPlane=.1f; camera.farClipPlane=180; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=RenderSettings.fogColor;
        camera.cullingMask=~(1<<5); // UI belongs in gameplay captures, not environment reference images.
        var rt=RenderTexture.GetTemporary(1440,960,24,RenderTextureFormat.ARGB32);
        var previous=RenderTexture.active;
        try { camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
            var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false); texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); texture.Apply();
            File.WriteAllBytes(Source+"/Previews/"+name+".png",texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        } finally { camera.targetTexture=null; RenderTexture.active=previous; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(go); }
    }
}
