using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine.Pool;
using System.Threading.Tasks;
using System;
using System.Linq;

public class ProceduralGeneration : MonoBehaviour
{
    public static ProceduralGeneration instance;

    // hierarchy
    public GameObject prefab_chunk;
    public Transform transform_chunks;
    public float chunkSize;
    public int chunkWidthCubes;
    public int chunkHeightCubes;
    public float groundThreshhold;
    public float groundScale;
    public float offset;
    public byte chunksPerFrame;
    public Texture2D rainTempMap;
    public BiomeData[] biomesData;

    const int RESOLUTION = 64;
    static float cubeWidth { get { return instance.chunkSize/instance.chunkWidthCubes; } }
    static float chunkHeight { get { return cubeWidth * instance.chunkHeightCubes; } }
    // float vertexSpacing { get { return (float)chunkSize/(float)(chunkWidthVertices-1); } }

    static int diameter { get { return instance.renderDistance*2+1; } }
    static int maxChunks { get { return (int)Math.Sqr(diameter); } }
    private int renderDistance;
    public int RenderDistance {
        set {
            renderDistance = value;
            
        }
        get { return renderDistance; }
    }

    const int MAX_DECORS = 20;

    public static long seed;
    public static ushort seed_ll { get { return (ushort)((seed >> 48) & 0xFFFF); } }
    public static ushort seed_lm { get { return (ushort)((seed >> 32) & 0xFFFF); } }
    public static ushort seed_rm { get { return (ushort)((seed >> 16) & 0xFFFF); } }
    public static ushort seed_rr { get { return (ushort)((seed >>  0) & 0xFFFF); } } // TODO double check
    public static float seed_temp { get { return seed_ll+((float)seed_ll/100000.0f); } }
    public static float seed_rain { get { return seed_lm+((float)seed_lm/100000.0f); } }
    public static float seed_grnd { get { return seed_rm+((float)seed_rm/100000.0f); } }
    public static float seed_     { get { return seed_rr+((float)seed_rr/100000.0f); } }

    static LinkedList<Chunk> loadingChunks;
    public static ObjectPool<Chunk> pool_chunks;
    // public static ObjectPool<GameObject>[] pool_decor;
    public static Dictionary<(int x, int z), Chunk> loadedChunks;
    public static Vector3Int prevPos, currPos;
    static int scrollX { get { return currPos.x; } }
    static int scrollZ { get { return currPos.z; } }
    static byte[,] rain_temp_map;
    static int rain_temp_map_width;
    static Biome[] biomes;

    public void Init()
    {
        RenderDistance = 8;

        instance = this;

        rain_temp_map_width = rainTempMap.width;
        rain_temp_map = new byte[rain_temp_map_width,rain_temp_map_width];
        biomes = new Biome[biomesData.Length];

        for(int i=0; i<biomes.Length; i++)
        {
            biomes[i] = new Biome(biomesData[i]);
            if(ColorUtility.TryParseHtmlString(biomesData[i].rain_temp_map_color, out Color color))
            {
                var color32 = (Color32)color;
                for(int x=0; x<rain_temp_map_width; x++)
                {
                    for(int y=0; y<rain_temp_map_width; y++)
                    {
                        Color32 c = rainTempMap.GetPixel(x, y);

                        if(c.r == color32.r && c.g == color32.g && c.b == color32.b)
                        {
                            rain_temp_map[x,y] = (byte)i;
                        }
                    }
                }
            }
        }
        biomesData = null;

        // pool_decor = new ObjectPool<GameObject>[Biome.s_decorations.Length];
        // foreach(var pair in Biome.s_indexes)
        // {
        //     pool_decor[pair.Value] = new ObjectPool<GameObject>(
        //         () => {
        //             // on create
        //             var decor = Instantiate(Biome.s_decorations[pair.Value], transform_decor);

        //             return decor;
        //         },
        //         (decor) => {
        //             // on get
        //             decor.SetActive(true);
        //         },
        //         (decor) => {
        //             // on return
        //             decor.SetActive(false);
        //         },
        //         (decor) => {
        //             // on destroy
        //             Destroy(decor);
        //         },
        //         false, maxChunks, maxChunks*MAX_DECORS
        //     );
        // }

        loadingChunks = new LinkedList<Chunk>();

        pool_chunks = new ObjectPool<Chunk>(
            () => {
                // on create
                var go = Instantiate(prefab_chunk, transform_chunks);
                var chunk = go.GetComponent<Chunk>();
                var mesh = new Mesh();
                mesh.MarkDynamic();
                // mesh.vertices = new Vector3[(int)Math.Sqr(chunkWidthVertices)*(chunkHeightVertices+1)];
                // mesh.triangles = new int[(int)Math.Sqr(chunkWidthVertices-1)*2*3]; // 2 triangles per 4 vertices, 3 vertices per triangle
                mesh.bounds = new Bounds(new Vector3(chunkSize/2, chunkHeight/2, chunkSize/2), new Vector3(chunkSize, chunkHeight, chunkSize));
                chunk.vertices = new List<Vector3>(100);
                chunk.triangles = new List<int>(100);
                chunk.meshFilter.mesh = mesh;
                // chunk.meshRenderer.materials = new Material[biomes.Length];
                // for(int i=0; i<biomes.Length; i++) {
                //     chunk.meshRenderer.materials[i] = biomes[i].material;
                // }
                // mesh.subMeshCount = biomes.Length;

                // chunk.decorPositions = new Vector3[MAX_DECORS];
                // chunk.decors = new int[MAX_DECORS];
                // chunk.decorRefs = new GameObject[MAX_DECORS];

                return chunk;
            },
            (chunk) => {
                // on get
                chunk.gameObject.SetActive(true);
            },
            (chunk) => {
                // on return
                chunk.gameObject.SetActive(false);
            },
            (chunk) => {
                // on destroy
                Destroy(chunk.gameObject);
            },
            false, maxChunks, maxChunks
        );
        loadedChunks = new Dictionary<(int x, int z), Chunk>();
        prevPos = new Vector3Int(0, 0, 0);
        currPos = new Vector3Int(0, 0, 0);
    }

    public static long RandomSeed()
    {
        seed = Math.RandLong();
        return seed;
    }

    static float Perlin(float seed, float x, float z, float chunkX, float chunkZ, float min=0, float max=1, float scale=1)
    {
        const int perlinOffset = 34546; // prevents mirroring
        return Math.Remap(Mathf.PerlinNoise((perlinOffset+seed+x+instance.chunkSize*chunkX)*scale, (perlinOffset+seed+z+instance.chunkSize*chunkZ)*scale), 0, 1, min, max);
    }

    // public static float Height(float x, float z, float chunkX=0, float chunkZ=0, int biome=-1)
    // {
    //     if(biome == -1)
    //     {
    //         biome = PerlinBiome(x, z, chunkX, chunkZ);
    //     }
    //     var b = biomes[biome];
    //     return Perlin(seed_height, x, z, chunkX, chunkZ, 0, 0.5f, 0.2f)
    //             +Perlin(seed_height, x, z, chunkX, chunkZ, b.minHeight, b.maxHeight, 0.04f);
    // }

    public static bool IsGround(float x, float y, float z, float chunkX=0, float chunkZ=0) {
        return Math.Perlin3D(seed_grnd, x+chunkX*instance.chunkSize, y, z+chunkZ*instance.chunkSize, instance.groundScale) > instance.groundThreshhold;
    }

    public static byte MapClamped(byte[,] map, int x, int y)
    {
        return map[Mathf.Clamp(x, 0, map.GetLength(0)-1), Mathf.Clamp(y, 0, map.GetLength(1)-1)];
    }

    public static int PerlinBiome(float x, float z, float chunkX=0, float chunkZ=0)
    {
        const float perlinScaleRain = 0.003f;
        const float perlinScaleTemp = 0.003f;

        float perlinValRain = Perlin(seed_rain, x, z, chunkX, chunkZ, 0, 1, perlinScaleRain);
        float perlinValTemp = Perlin(seed_temp, x, z, chunkX, chunkZ, 0, 1, perlinScaleTemp);

        float perlinScaleFine = 0.1f;
        float fineNoise = Perlin(seed_, x, z, chunkX, chunkZ, 0, 0.05f, perlinScaleFine);

        perlinValTemp -= fineNoise;
        perlinValTemp = Mathf.Round(perlinValTemp * rain_temp_map_width);
        perlinValRain -= fineNoise;
        perlinValRain = Mathf.Round(perlinValRain * rain_temp_map_width);

        return MapClamped(rain_temp_map, (int)perlinValTemp, (int)perlinValRain);
    }

    void SetColor(ref Color c, float r, float g, float b, float a=1)
    {
        c.r = r;
        c.g = g;
        c.b = b;
        c.a = a;
    }

    async void Load(int chunkX, int chunkZ)
    {
        if(loadedChunks.ContainsKey((chunkX, chunkZ))) return;

        var chunk = pool_chunks.Get();
        loadedChunks.Add((chunkX, chunkZ), chunk);
        chunk.transform.SetPositionAndRotation(new Vector3(chunkX*chunkSize, 0, chunkZ*chunkSize), Quaternion.identity);

        // Vector3[] decorPositions = chunk.decorPositions;
        // int[] decors = chunk.decors;
        // int numOfDecors = 0;

        var v = new Vector3(0, 0, 0);
        int AddVertex(int x, int y, int z) {
            v.Set(x*cubeWidth, y*cubeWidth, z*cubeWidth);
            chunk.vertices.Add(v);
            return chunk.vertices.Count-1;
        }

        void AddTriangle(int x1, int y1, int z1, int x2, int y2, int z2, int x3, int y3, int z3) {
            chunk.triangles.Add(AddVertex(x1, y1, z1));
            chunk.triangles.Add(AddVertex(x2, y2, z2));
            chunk.triangles.Add(AddVertex(x3, y3, z3));
        }

        // void AddSquare(float x1, float y1, float z1, float x2, float y2, float z2, float x3, float y3, float z3, float x4, float y4, float z4) {
        //     AddTriangle();
        //     AddTriangle();
        // }

        bool IsGround_(int x, int y, int z) {
            return IsGround(x*cubeWidth, y*cubeWidth, z*cubeWidth, chunkX, chunkZ);
        }

        await Task.Run(() => {
            chunk.vertices.Clear();
            chunk.triangles.Clear();

            for(int x=0; x<chunkWidthCubes; ++x) for(int y=0; y<chunkHeightCubes; ++y) for(int z=0; z<chunkWidthCubes; ++z)
            {
                // int biome = PerlinBiome(x*vertexSpacing, z*vertexSpacing, chunkX, chunkZ);

                if(IsGround_(x, y, z)) {
                    if(!IsGround_(x+1, y, z)) {
                        AddTriangle(x+1, y, z, x+1, y+1, z, x+1, y, z+1);
                        AddTriangle(x+1, y+1, z, x+1, y+1, z+1, x+1, y, z+1);
                    }
                    if(!IsGround_(x-1, y, z)) {
                        AddTriangle(x, y, z, x, y, z+1, x, y+1, z);
                        AddTriangle(x, y+1, z, x, y, z+1, x, y+1, z+1);
                    }
                    if(!IsGround_(x, y+1, z)) {
                        AddTriangle(x, y+1, z, x, y+1, z+1, x+1, y+1, z);
                        AddTriangle(x+1, y+1, z, x, y+1, z+1, x+1, y+1, z+1);
                    }
                    if(!IsGround_(x, y-1, z)) {
                        AddTriangle(x, y, z, x+1, y, z, x, y, z+1);
                        AddTriangle(x+1, y, z, x+1, y, z+1, x, y, z+1);
                    }
                    if(!IsGround_(x, y, z+1)) {
                        AddTriangle(x, y, z+1, x+1, y, z+1, x, y+1, z+1);
                        AddTriangle(x, y+1, z+1, x+1, y, z+1, x+1, y+1, z+1);
                    }
                    if(!IsGround_(x, y, z-1)) {
                        AddTriangle(x, y, z, x, y+1, z, x+1, y, z);
                        AddTriangle(x, y+1, z, x+1, y+1, z, x+1, y, z);
                    }
                }

                // v.Set(
                //     (float)x*vertexSpacing+Perlin(154.2643f, x*vertexSpacing, z*vertexSpacing, chunkX, chunkZ, -offset, offset), 
                //     Height((float)x*vertexSpacing, (float)z*vertexSpacing, chunkX, chunkZ, biome), 
                //     (float)z*vertexSpacing+Perlin(56743.2534525f, x*vertexSpacing, z*vertexSpacing, chunkX, chunkZ, -offset, offset)
                // );
                // chunk.vertices.Add(v);
                
                // if(x<chunkWidthVertices-1 && z<chunkWidthVertices-1)
                // {
                //     chunk.triangles.Add(chunkWidthVertices*x+z);
                //     chunk.triangles.Add(chunkWidthVertices*x+z+1);
                //     chunk.triangles.Add(chunkWidthVertices*(x+1)+z);
                //     chunk.triangles.Add(chunkWidthVertices*(x+1)+z);
                //     chunk.triangles.Add(chunkWidthVertices*x+z+1);
                //     chunk.triangles.Add(chunkWidthVertices*(x+1)+z+1);
                // }
            }
        });

        // numOfDecors = 1;
        // decors[0] = 0;
        // decorPositions[0].Set(chunkX*chunkSize, Height(0, 0, chunkX, chunkZ), chunkZ*chunkSize);

        chunk.meshFilter.sharedMesh.Clear();
        chunk.meshFilter.sharedMesh.SetVertices(chunk.vertices);
        chunk.meshFilter.sharedMesh.SetTriangles(chunk.triangles, 0, false);
        chunk.meshFilter.sharedMesh.UploadMeshData(false);

        Debug.Log(chunk.meshFilter.sharedMesh.isReadable);

        // chunk.decorPositions = decorPositions;
        // chunk.decors = decors;
        // chunk.numOfDecors = numOfDecors;

        float dist = Math.Dist(currPos.x, currPos.z, chunkX, chunkZ);

        loadingChunks.AddLast(chunk);
    }

    static void Unload(int x, int z)
    {
        if(!loadedChunks.ContainsKey((x, z))) return;

        var chunk = loadedChunks[(x, z)];
        loadedChunks.Remove((x, z));
        // for(int i=0; i<chunk.numOfDecors; i++)
        // {
        //     if(chunk.decorRefs[i] != null) pool_decor[chunk.decors[i]].Release(chunk.decorRefs[i]);
        // }
        pool_chunks.Release(chunk);
    }

    void UnloadTooFar()
    {
        var toUnload = new LinkedList<(int x, int z)>();
        foreach(var chunk in loadedChunks)
        {
            if(Mathf.Abs(currPos.x - chunk.Key.x) > renderDistance+1 || Mathf.Abs(currPos.z - chunk.Key.z) > renderDistance+1)
            {
                toUnload.AddLast((chunk.Key.x, chunk.Key.z));
            }
        }
        foreach(var pos in toUnload)
        {
            Unload(pos.x, pos.z);
        }
    }

    void AlternatingLoop(Action<int> func, int max)
    {
        for(int i=0; i<max; i++)
        {
            func(i);
            if(i != 0) func(-i);
        }
    }

    public static void UnloadAll()
    {
        loadingChunks.Clear();
        var toUnload = new (int x, int z)[loadedChunks.Count];
        int i=0;
        foreach(var pair in loadedChunks)
        {
            toUnload[i] = pair.Key;
            i++;
        }
        foreach(var key in toUnload)
        {
            Unload(key.x, key.z);
        }
    }

    void Update()
    {
        Vector3 p = PlayerMovement.rb.position;
        currPos.Set((int)Mathf.Floor(p.x/chunkSize), 0, (int)Mathf.Floor(p.z/chunkSize));

        if(currPos != prevPos || loadedChunks.Count == 0)
        {
            UnloadTooFar();
            AlternatingLoop((x) => {
                AlternatingLoop((z) => {
                    Load(currPos.x+x, currPos.z+z);
                }, renderDistance);
            }, renderDistance);
        }

        prevPos.Set(currPos.x, 0, currPos.z);

        var offset = new Vector3(chunkSize/2, 0, chunkSize/2);
        for(int i=0; loadingChunks.Count > 0 && i < chunksPerFrame; i++)
        {
            var closest = loadingChunks.First;
            var closestDist = Mathf.Infinity;
            for(var node = loadingChunks.First; node != null; node = node.Next)
            {
                var dist = Vector3.Distance(PlayerMovement.rb.position, node.Value.transform.position+offset);
                if(dist < closestDist)
                {
                    closestDist = dist;
                    closest = node;
                }
            }

            var chunk = closest.Value;
            loadingChunks.Remove(closest);

            chunk.meshFilter.mesh.RecalculateNormals();
            chunk.meshCollider.sharedMesh = chunk.meshFilter.sharedMesh;

            // for(int di=0; di<chunk.numOfDecors; di++)
            // {
            //     var decor = pool_decor[chunk.decors[di]].Get();
            //     decor.transform.position = chunk.decorPositions[di];
            //     chunk.decorRefs[di] = decor;
            // }
        }
    }

    void FixedUpdate()
    {
        
    }
}