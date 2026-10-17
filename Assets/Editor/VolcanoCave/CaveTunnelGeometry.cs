using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Editor-only negative-space tunnel surface. Junctions share one continuous density field.</summary>
public sealed class CaveTunnelGeometry
{
    public const string LayoutPath = "ArtSource/CaveInterior/tunnel-layout.json";
    [Serializable] public class Point { public float x, z, radius, height; public Vector2 Position => new Vector2(x,z); }
    [Serializable] public class Route { public string name; public Point[] points; }
    [Serializable] public class Layout { public Route[] routes; }
    struct Segment { public Vector2 a, b; public float r0, r1, h0, h1; }
    readonly List<Segment> segments = new List<Segment>();
    public readonly Layout layout;

    public CaveTunnelGeometry()
    {
        layout = JsonUtility.FromJson<Layout>(File.ReadAllText(LayoutPath));
        if (layout?.routes == null || layout.routes.Length == 0) throw new InvalidDataException("No cave routes.");
        foreach (var route in layout.routes)
        {
            if (route.points == null || route.points.Length < 2) throw new InvalidDataException("Route needs two points.");
            foreach (var p in route.points)
                if (p.radius < 1.5f || p.height < 3.5f || Mathf.Abs(p.x)>16 || Mathf.Abs(p.z)>22)
                    throw new InvalidDataException("Cave point outside supported bounds: " + route.name);
            for (int i=0; i<route.points.Length-1; i++)
            {
                var a=route.points[i]; var b=route.points[i+1];
                Vector2 before=route.points[Mathf.Max(0,i-1)].Position;
                Vector2 after=route.points[Mathf.Min(route.points.Length-1,i+2)].Position;
                int count=Mathf.CeilToInt(Vector2.Distance(a.Position,b.Position)/.7f);
                Vector2 last=a.Position;
                for(int j=1;j<=count;j++)
                {
                    float t=j/(float)count, previous=(j-1)/(float)count;
                    Vector2 next=.5f*((2*a.Position)+(-before+b.Position)*t+
                        (2*before-5*a.Position+4*b.Position-after)*t*t+
                        (-before+3*a.Position-3*b.Position+after)*t*t*t);
                    segments.Add(new Segment {a=last,b=next,r0=Mathf.Lerp(a.radius,b.radius,previous),
                        r1=Mathf.Lerp(a.radius,b.radius,t),h0=Mathf.Lerp(a.height,b.height,previous),h1=Mathf.Lerp(a.height,b.height,t)});
                    last=next;
                }
            }
        }
    }

    // Positive is the hollow interior, negative is solid rock. The floor stays level for gameplay.
    public float Air(Vector3 p)
    {
        Vector2 q=new Vector2(p.x,p.z);
        float best=-20;
        foreach(var s in segments)
        {
            if(Mathf.Abs(q.x-s.a.x)>4.5f || Mathf.Abs(q.y-s.a.y)>4.5f) continue;
            Vector2 ab=s.b-s.a;
            float t=Mathf.Clamp01(Vector2.Dot(q-s.a,ab)/ab.sqrMagnitude);
            float r=Mathf.Lerp(s.r0,s.r1,t), h=Mathf.Lerp(s.h0,s.h1,t);
            float horizontal=Vector2.Distance(q,s.a+t*ab)/r;
            float vertical=(p.y-1.45f)/(h-1.45f);
            float air=(1-Mathf.Sqrt(horizontal*horizontal+vertical*vertical))*r;
            best=Mathf.Max(best,air);
        }
        float broad=(Mathf.PerlinNoise(p.x*.43f+p.y*.12f+17,p.z*.43f+31)-.5f)*.42f;
        float strata=Mathf.Sin(p.y*3.3f+p.x*.35f+p.z*.46f)*.055f;
        return Mathf.Min(best+broad+strata,p.y-.012f);
    }

    public Vector3 Normal(Vector3 p)
    {
        const float e=.025f;
        return new Vector3(Air(p+Vector3.right*e)-Air(p-Vector3.right*e),
            Air(p+Vector3.up*e)-Air(p-Vector3.up*e),
            Air(p+Vector3.forward*e)-Air(p-Vector3.forward*e)).normalized;
    }

    public IEnumerable<Vector3> Centerline(float spacing=1)
    {
        foreach(var s in segments)
        {
            int n=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(s.a,s.b)/spacing));
            for(int i=0;i<n;i++)
            {
                Vector2 p=Vector2.Lerp(s.a,s.b,i/(float)n);
                yield return new Vector3(p.x,.5f,p.y);
            }
        }
    }

    static readonly Vector3Int[] Corners={new Vector3Int(0,0,0),new Vector3Int(1,0,0),new Vector3Int(1,0,1),new Vector3Int(0,0,1),
        new Vector3Int(0,1,0),new Vector3Int(1,1,0),new Vector3Int(1,1,1),new Vector3Int(0,1,1)};
    static readonly int[,] Tetrahedra={{0,5,1,6},{0,1,2,6},{0,2,3,6},{0,3,7,6},{0,7,4,6},{0,4,5,6}};

    public Mesh Chunk(int chunkX,int chunkZ)
    {
        const int nx=20,ny=16,nz=20;
        const float step=.5f;
        Vector3 origin=new Vector3(-20+chunkX*10,-.5f,-25+chunkZ*10);
        var field=new float[nx+1,ny+1,nz+1];
        for(int x=0;x<=nx;x++) for(int y=0;y<=ny;y++) for(int z=0;z<=nz;z++)
            field[x,y,z]=Air(origin+new Vector3(x,y,z)*step);
        var vertices=new List<Vector3>(); var normals=new List<Vector3>(); var triangles=new List<int>();
        var indices=new Dictionary<Vector3Int,int>();
        var cube=new Vector3[8]; var values=new float[8]; var positive=new int[4];var negative=new int[4];
        int Vertex(int a,int b)
        {
            Vector3 v=Vector3.Lerp(cube[a],cube[b],values[a]/(values[a]-values[b]));
            var key=new Vector3Int(Mathf.RoundToInt(v.x*100000),Mathf.RoundToInt(v.y*100000),Mathf.RoundToInt(v.z*100000));
            if(indices.TryGetValue(key,out int index)) return index;
            index=vertices.Count;indices.Add(key,index);vertices.Add(v);normals.Add(Normal(v));return index;
        }
        void Triangle(int a,int b,int c)
        {
            if(a==b||b==c||a==c) return;
            Vector3 cross=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]);
            if(cross.sqrMagnitude<1e-12f) return;
            if(Vector3.Dot(cross,normals[a]+normals[b]+normals[c])<0) { int swap=b;b=c;c=swap; }
            triangles.Add(a);triangles.Add(b);triangles.Add(c);
        }
        for(int x=0;x<nx;x++) for(int y=0;y<ny;y++) for(int z=0;z<nz;z++)
        {
            int filled=0;
            for(int c=0;c<8;c++)
            {
                var k=new Vector3Int(x,y,z)+Corners[c];
                cube[c]=origin+(Vector3)k*step;values[c]=field[k.x,k.y,k.z];if(values[c]>0) filled++;
            }
            if(filled==0||filled==8) continue;
            for(int t=0;t<6;t++)
            {
                int ni=0,no=0;
                for(int c=0;c<4;c++) { int k=Tetrahedra[t,c];if(values[k]>0) positive[ni++]=k;else negative[no++]=k; }
                if(ni==0||ni==4) continue;
                if(ni==1) Triangle(Vertex(positive[0],negative[0]),Vertex(positive[0],negative[1]),Vertex(positive[0],negative[2]));
                else if(ni==3) Triangle(Vertex(negative[0],positive[0]),Vertex(negative[0],positive[1]),Vertex(negative[0],positive[2]));
                else
                {
                    int a=Vertex(positive[0],negative[0]),b=Vertex(positive[0],negative[1]);
                    int c=Vertex(positive[1],negative[1]),d=Vertex(positive[1],negative[0]);
                    Triangle(a,b,c);Triangle(a,c,d);
                }
            }
        }
        if(triangles.Count==0) return null;
        var mesh=new Mesh {name="Tunnel_"+chunkX+"_"+chunkZ,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
        mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
        return mesh;
    }
}
