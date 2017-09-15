using System;
using UnityEngine;
using System.IO;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using GameScripts;
using Random = UnityEngine.Random;

public class PlanetGenerator {

    private Texture2D[] maps;
    private Mesh[] cubeSpheres;
    private CubeSphere cubeSphereGenerator;
    private List<int> distinctNumbers;
    private List<int> treeTriangles;
    private List<int> buildingTriangles;   

    private void LoadData(int planetNum)
    {
        maps = new Texture2D[planetNum];
        distinctNumbers = new List<int>();
        
        cubeSphereGenerator = new CubeSphere();
        cubeSpheres = new Mesh[maps.Length];
        
        treeTriangles = new List<int>();
        buildingTriangles = new List<int>();
        //Load all the maps and create cube sphere for each
        BinaryFormatter bf = new BinaryFormatter();
        for (int i = 0; i < planetNum; i++)
        {            
            var file = File.Open(Application.streamingAssetsPath + "/Heightmaps/map_" + i + ".dat", FileMode.Open);
            var mapBytes = bf.Deserialize(file) as byte[];
            var tex = new Texture2D(2048, 2048);
            tex.LoadImage(mapBytes);
            maps[i] = tex;
            file.Close();
            cubeSpheres[i] = cubeSphereGenerator.Generate(i);
        }            
    }

    public void GeneratePlanets(int planetNum)
    {
        LoadData(planetNum);
        //Creates a list of distinct random numbers
        DistinctRandomNumbers(planetNum);
        for (int n = 0; n < planetNum; n++)
        {            
            int seed = distinctNumbers[n];            
            //Create new gameobject
            GameObject workingObject = new GameObject();
            workingObject.layer = 11;
            workingObject.tag = "Planet";
            
            //Applies the provided heightmap to all vertices of the mesh
            ApplyHeightmap(maps[seed], seed, workingObject);

            //Divides the main mesh into submeshes depending on elevation.
            Material[] tempMats = CalculateSubmeshes(workingObject, seed);

            //Attach atmosphere component
            AttachAtmosphere(workingObject);
            
            //Attach water component
            AttachWater(workingObject);

            //Attach clouds component
            AttachClouds(workingObject);

            //Attach gravity script
            AttachGravity(workingObject);
            
            //Attach Wind zone, which affects trees
            AttachWindZone(workingObject);           
            
            //Add spawn manager component which manages spawning of objects
            AddSpawnManager(workingObject, seed);
            
            //Calculate the transform of each tree to be spawned
            CalculateObjectsData(workingObject, seed, true, false, false);
            
            //Calculate the transform of each building to be spawned
            CalculateObjectsData(workingObject, seed, false, true, false);
        
            //Calculate the transform of each rock to be spawned
            CalculateObjectsData(workingObject, seed, false, false, true);
            
            //Calculate the transform of each plants patch to be spawned
            CalculateObjectsData(workingObject, seed, false, false, false);
            
            //Calculate the transform of the player to be spawn at             
            CalculateCharacterSpawnPoint(workingObject, seed, true);
        
            //Calculate the transform of enemies to be spawned
            CalculateCharacterSpawnPoint(workingObject, seed, false);
            workingObject.SetActive(false);
            //Saves the planet with the attached water, clouds, atmosphere and player objects as a prefab
            SavePrefab(seed, workingObject.gameObject, tempMats);                                  
        }
        Debug.Log("Planets Generation Finished Successfully!");
    }

    //Generates distinct random numbers in the range [0, maxNum] corresponding to seed
    private void DistinctRandomNumbers(int maxNum)
    {
        for (int n = 0; n < maxNum; n++)
        {
            int seed = Random.Range(0, maxNum);
            while (distinctNumbers.Contains(seed))
            {
                seed = Random.Range(0, maxNum);
            }
            distinctNumbers.Add(seed);
        }     
    }

    private void AttachAtmosphere(GameObject obj)
    {
        //Attach the atmosphere script to the parent object
        obj.AddComponent<Atmosphere>();

        GameObject atmosphere = new GameObject();
        atmosphere.name = "Atmosphere";
        atmosphere.transform.parent = obj.transform; //Make this object child of the parent object
        atmosphere.transform.localScale = new Vector3(1.08f, 1.08f, 1.08f); //set the scale as 1.05 times the scale of the parent
        atmosphere.AddComponent<MeshFilter>();
        atmosphere.AddComponent<MeshRenderer>();
        atmosphere.AddComponent<SphereCollider>();
        atmosphere.GetComponent<SphereCollider>().radius = 1f;
        atmosphere.GetComponent<SphereCollider>().center = atmosphere.transform.position;
        atmosphere.GetComponent<SphereCollider>().isTrigger = true; //set the collider to trigger for future use
        //load the mesh and materials
        atmosphere.GetComponent<MeshFilter>().mesh = Resources.Load("Meshes/CubeSphere") as Mesh;
        atmosphere.GetComponent<MeshRenderer>().material = Resources.Load("Materials/Atmospheric Scattering/Shaders/Atmosphere_SkyFromAtmosphere") as Material;
         
    }

    private void AttachWater(GameObject obj)
    {
        GameObject water = new GameObject();
        water.name = "Water";
        water.transform.parent = obj.transform;
        water.transform.localScale = new Vector3(0.973f, 0.973f, 0.973f);
        water.AddComponent<MeshFilter>();
        water.AddComponent<MeshRenderer>();
        water.GetComponent<MeshFilter>().mesh = Resources.Load("Meshes/CubeSphere") as Mesh;
        water.GetComponent<MeshRenderer>().material = Resources.Load("Materials/Water Shaders/Water") as Material;
        
        GameObject underwater = new GameObject();
        underwater.name = "Underwater";
        underwater.transform.parent = water.transform;
        underwater.transform.localScale = Vector3.one;
        underwater.AddComponent<MeshFilter>();
        underwater.AddComponent<MeshRenderer>();
        underwater.AddComponent<SphericalFog>();
        underwater.GetComponent<MeshFilter>().mesh = Resources.Load("Meshes/CubeSphere") as Mesh;
        underwater.GetComponent<MeshRenderer>().material = Resources.Load("Materials/Water Shaders/Underwater/FX_Spherical Fog") as Material;

    }

    private void AttachClouds(GameObject obj)
    {
        GameObject clouds = new GameObject();
        clouds.name = "Clouds";
        clouds.transform.parent = obj.transform;
        clouds.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        clouds.AddComponent<CloudToy>();
    }

    private void AttachGravity(GameObject obj)
    {
        obj.AddComponent<PlanetGravity>();       
    }

    private void AttachWindZone(GameObject obj)
    {
        obj.AddComponent<WindZone>();
        var wind = obj.GetComponent<WindZone>();
        wind.windMain = 0.15f;
        wind.windTurbulence = 0.2f;
        wind.windPulseMagnitude = 0.2f;
        wind.windPulseFrequency = 0.1f;
    } 

    //Used to calculate the position for grass, trees, buildings and rocks
    private void CalculateObjectsData(GameObject obj, int seedNum, bool isTree, bool isBuilding, bool isRock)
    {     
        List<TransformData> transforms = new List<TransformData>();        
        Mesh workingMesh = obj.GetComponent<MeshFilter>().sharedMesh;
        List<int> trianglesL;
        if (isRock)
        {
            trianglesL = workingMesh.GetTriangles(2).ToList();
        }
        else
        {
            trianglesL = workingMesh.GetTriangles(1).ToList();
        }
        //For buildings remove all used triangles by trees
        if (isBuilding)
        {
            trianglesL.RemoveRange(0, trianglesL.Count / 3); // The whole subset used for forest
            trianglesL.RemoveAll(e => treeTriangles.Contains(e)); //and each individual random tree
        }       
        Vector3[] vertices = workingMesh.vertices;       
        int triNUm = trianglesL.Count / 3;
        //Use difference subset of the triangles for forest and buildings so they don't fully match
        //Use hashset for a subset of triangles to speed up Contains method
        HashSet<int> triSet;
        if (isBuilding)
        {
            triSet = new HashSet<int>(trianglesL.GetRange(0, trianglesL.Count / 2));
        }
        else
        {
            triSet = new HashSet<int>(trianglesL.GetRange(0, triNUm));
        }
        var objectScale = isTree ? obj.transform.lossyScale.x * 0.0005f : obj.transform.lossyScale.x * 0.001f;
        
        bool isGrass = !isTree && !isBuilding && !isRock;
        for (int i = 0; i < triNUm; i++)
        {            
            bool distribute = isRock ? i % 10 == 0 : i % Distribution(i, triSet, isBuilding) == 0;
            //Distribute objects in clusters or at random seperated by N number of triangles distance              
            if (isGrass || distribute)
            {
                int temp = i;
                int index1 = trianglesL[temp * 3];
                int index2 = trianglesL[temp * 3 + 1];
                int index3 = trianglesL[temp * 3 + 2];
                
                Vector3 p1 = obj.transform.TransformPoint(vertices[index1]);
                Vector3 p2 = obj.transform.TransformPoint(vertices[index2]);
                Vector3 p3 = obj.transform.TransformPoint(vertices[index3]);
                //Get the position of each triangle's mid point in world space coordinates
                Vector3 centre = (p1 + p2 + p3) / 3;
                             
                //Calculate the normal vector of the mid point                
                Vector3 v1 = centre - p1;
                Vector3 v2 = centre - p2;                
                Vector3 normal = Vector3.Cross(v1, v2);
                //Calculate a vector poiting from origin to the normal vector
                Vector3 origin = (centre - obj.transform.position).normalized;
                //Compare the triangle normal direction with that of origin outward direction
                //in order to avoid steep terrain
                float angle = Vector3.Angle(origin, normal.normalized);
                
                bool angleCheck = (isTree || isRock) && angle <= 10 || isBuilding && angle <= 5;
                if (isGrass || angleCheck)
                {
                    //Get the rotation of the normal vector
                    Quaternion rot = Quaternion.FromToRotation(Vector3.up, normal.normalized);
                    //Assign the values to the transfrom struct                    
                    transforms.Add(NewTransformData(centre, rot, Vector3.one * objectScale));                    
                    if (isTree)
                    {
                        treeTriangles.Add(index1);
                        treeTriangles.Add(index2);
                        treeTriangles.Add(index3);                        
                    } 
                    else if (isBuilding)
                    {
                        buildingTriangles.Add(index1);
                        buildingTriangles.Add(index2);
                        buildingTriangles.Add(index3);
                    }
                }
            }
        }
        //Save the lists of transform data into binary files
        BinaryFormatter bf = new BinaryFormatter();
        Directory.CreateDirectory(Application.streamingAssetsPath + "/Planets/Planet_" + seedNum + "/PlanetData");
        FileStream file;
        if (isTree)
        {
            file = File.Create(Application.streamingAssetsPath + "/Planets/Planet_" + seedNum + "/PlanetData/TreesData.dat");
        } 
        else if (isBuilding)
        {
            file = File.Create(Application.streamingAssetsPath + "/Planets/Planet_" + seedNum + "/PlanetData/BuildingsData.dat");
        }
        else if (isRock)
        {
            file = File.Create(Application.streamingAssetsPath + "/Planets/Planet_" + seedNum + "/PlanetData/RocksData.dat");
        }
        else
        {
            file = File.Create(Application.streamingAssetsPath + "/Planets/Planet_" + seedNum + "/PlanetData/PlantsData.dat");
        }
        bf.Serialize(file, transforms);       
        file.Close();
    }

    private TransformData NewTransformData(Vector3 pos, Quaternion rot, Vector3 scale)
    {
        return new TransformData
        {
            X = pos.x,
            Y = pos.y,
            Z = pos.z,
            rotX = rot.x,
            rotY = rot.y,
            rotZ = rot.z,
            rotW = rot.w,
            scaleX = scale.x,
            scaleY = scale.y,
            scaleZ = scale.z
        };
    }
    
    private void AddSpawnManager(GameObject obj, int seedNum)
    {
        GameObject spawn = new GameObject();
        spawn.transform.parent = obj.transform;
        spawn.name = "Spawn Manager";
        spawn.AddComponent<SpawnManager>(); 
        spawn.SetActive(false);
    }

    private void CalculateCharacterSpawnPoint(GameObject obj, int seedNum, bool isPlayer)
    {
        List<TransformData> enemies = new List<TransformData>(); 
        var min = obj.transform.lossyScale.x * 0.975f;
        var mesh = obj.GetComponent<MeshFilter>().sharedMesh;
        List<int> triangles;
        if (isPlayer)
        {
            triangles = buildingTriangles;
        }
        else
        {
            triangles = mesh.GetTriangles(1).ToList();
            triangles.RemoveRange(0, triangles.Count / 3); // The whole subset used for forest
            triangles.RemoveAll(e => treeTriangles.Contains(e)); //and each individual random tree
            triangles.RemoveAll(e => buildingTriangles.Contains(e));
        }
        var vertices = mesh.vertices;
        var charScale = obj.transform.lossyScale.x * 0.0007f;
        for (int i = 0; i < triangles.Count / 3; i++)
        {
            var temp = i;
            var index1 = triangles[temp * 3];
            var index2 = triangles[temp * 3 + 1];
            var index3 = triangles[temp * 3 + 2];
                
            var p1 = obj.transform.TransformPoint(vertices[index1]);
            var p2 = obj.transform.TransformPoint(vertices[index2]);
            var p3 = obj.transform.TransformPoint(vertices[index3]);
            //Get the position of each triangle's mid point in world space coordinates
            var centre = (p1 + p2 + p3) / 3;                        
            //Calculate the normal vector of the mid point                
            Vector3 v1 = centre - p1;
            Vector3 v2 = centre - p2;                
            var normal = Vector3.Cross(v1, v2);
            //Calculate a vector poiting from origin to the normal vector
            var origin = (centre - obj.transform.position).normalized;
            var angle = Vector3.Angle(origin, normal.normalized);            
            //Compare the direction of the triangle normal to that of planet centre outward direction
            //in order to avoid steep terrain
            if (angle <= 5)
            {
                var rot = Quaternion.FromToRotation(Vector3.up, normal.normalized);
                if (isPlayer)
                {
                    charScale = obj.transform.lossyScale.x * 0.0005f;
                    var cRadius = Mathf.Pow(centre.x - obj.transform.position.x, 2) +
                                  Mathf.Pow(centre.y - obj.transform.position.y, 2) +
                                  Mathf.Pow(centre.z - obj.transform.position.z, 2);
                    if (cRadius > min * min)
                    {
                        TransformData t = NewTransformData(centre, rot, Vector3.one * charScale);
                        BinaryFormatter bf = new BinaryFormatter();
                        FileStream file = File.Create(Application.streamingAssetsPath + "/Planets/Planet_" + seedNum +
                                                      "/PlanetData/PlayerData.dat");
                        bf.Serialize(file, t);
                        file.Close();
                        break;
                    }
                }
                if (i % 5 == 0)
                {
                    enemies.Add(NewTransformData(centre, rot, Vector3.one * charScale));
                }
            }        
        }
        if (!isPlayer)
        {
            BinaryFormatter bf2 = new BinaryFormatter();
            FileStream file2 = File.Create(Application.streamingAssetsPath + "/Planets/Planet_" + seedNum +
                                           "/PlanetData/EnemiesData.dat");
            bf2.Serialize(file2, enemies);
            file2.Close();
        }
    }

    private int Distribution(int i, HashSet<int> range, bool isBuilding)
    {
        if(range.Contains(i))
        {
            return isBuilding ? Random.Range(5, 8) : Random.Range(3, 21);
        }
        return !isBuilding ? Random.Range(200, 500) : i + 1; // otherwise spread the trees randomly
    }

    private void ApplyHeightmap(Texture2D mapPar, int seedNum, GameObject workingObject) {

        //Pick a random scale for the planet, so that each one differs in size
        var radius = Random.Range(1000, 1500);
        workingObject.transform.localScale = new Vector3(radius, radius, radius);
        workingObject.AddComponent<MeshFilter>();
        workingObject.AddComponent<MeshRenderer>();
        workingObject.AddComponent<MeshCollider>();
        workingObject.name = "Planet_Object_" + seedNum;
        workingObject.tag = "Planet";

        //Create new mesh for the object       
        var workingMesh = cubeSphereGenerator.Generate(seedNum);
        workingMesh.name = "Planet_Mesh_" + seedNum;
        var vertices = workingMesh.vertices; //gets the vertices of the mesh        
        var uvs = workingMesh.uv; //get the uvs of the mesh

        float pixelHeight;
        Texture2D map = new Texture2D(mapPar.width, mapPar.height, mapPar.format, mapPar.mipmapCount > 1);
        map.LoadRawTextureData(mapPar.GetRawTextureData());
        map.Apply();        
        
        for (int i = 0; i < vertices.Length; i++) {
         
            //get the pixel color value at the uv position. Returns in range [0,1]
            pixelHeight = map.GetPixelBilinear(uvs[i].x, uvs[i].y).grayscale;

            //remap in the range [-1, 1]
            pixelHeight = (pixelHeight * 2f) - 1;

            //Amplify deep water regions
            if(pixelHeight < -0.8f)
            {
                pixelHeight *= 2;
            }
            //Cut out shalower water regions
            else  if (pixelHeight > -0.8f && pixelHeight < -0.7f)
            {
                pixelHeight *= 1.4f;
            }
            //Define where beaches start at
            else if (pixelHeight > 0 && pixelHeight < 0.5f)
            {
                pixelHeight *= -0.5f;
            }
            //Define flat regions and mountains
            else if (pixelHeight > 0.95 && pixelHeight < 0.97)
            {
                pixelHeight *= 1.3f;
            }
            //Amplify mountain peeks
            else if (pixelHeight > 0.97)
            {
                pixelHeight *= 2;
            }

            //Clamp the value only in the range [-1.5, 1.1]
            float clampedPixel = Mathf.Clamp(pixelHeight, -1.5f, 1.1f);

            //Scale down the whole terrain so it doesn't reach the sky
            clampedPixel = clampedPixel / 25;

            //apply pixelValue to every vertex and add 1 to account for each vertex original scale 
            vertices[i] *= clampedPixel + 1; 
        }

        workingMesh.vertices = vertices; //assign the new vertices values
        workingMesh.RecalculateBounds();
        workingMesh.RecalculateNormals(); // recalculate normals
        workingMesh.RecalculateTangents();
        workingObject.GetComponent<MeshFilter>().mesh = workingMesh;
        workingObject.GetComponent<MeshCollider>().sharedMesh = workingMesh;

    }

    private Material[] CalculateSubmeshes(GameObject workingObject, int seedNum)
    {
        //Load and apply all the ground materials
        Material[] materials = new Material[3];       
        for (int n = 0; n < materials.Length; n++) {
            //Needs new instance of material so we don't modify the same materials for each planet
            Material mat = new Material(Resources.Load("Materials/Ground Shaders/Level " + (n + 1) + " Material") as Material);
            materials[n] = mat;                  
        }
        //Load and apply the planet normal map to all materials
        BinaryFormatter bf = new BinaryFormatter();
        var file = File.Open(Application.streamingAssetsPath + "/Heightmaps/map_" + seedNum + "N.dat", FileMode.Open);
        var mapBytes = bf.Deserialize(file) as byte[];
        Texture2D normalMap = new Texture2D(2048, 2048);
        normalMap.LoadImage(mapBytes);
        file.Close();
        for (int k = 0; k < materials.Length; k++)
        {
            materials[k].SetTexture("_PlanetNormal", normalMap);
        }
        workingObject.GetComponent<MeshRenderer>().sharedMaterials = materials;
        //Set the submesh count to 3 to define 3 regions
        workingObject.GetComponent<MeshFilter>().sharedMesh.subMeshCount = 3;

        var workingMesh = workingObject.GetComponent<MeshFilter>().sharedMesh;        
        var triangles = workingMesh.triangles;
        var vertices = workingMesh.vertices;

        List<int> submesh1 = new List<int>();
        List<int> submesh2 = new List<int>();
        List<int> submesh3 = new List<int>();
              
        float min = workingObject.transform.lossyScale.x * 0.967f; // About 97% of object scale (world radius) ;
        float max = workingObject.transform.lossyScale.x; // at 100% object scale the maximum is defined;
        
        for (int j = 0; j < triangles.Length / 3; j++)
        {
            var temp = j;
            var index1 = triangles[temp * 3];
            var index2 = triangles[temp * 3 + 1];
            var index3 = triangles[temp * 3 + 2];
                
            var p1 = workingObject.transform.TransformPoint(vertices[index1]);
            var p2 = workingObject.transform.TransformPoint(vertices[index2]);
            var p3 = workingObject.transform.TransformPoint(vertices[index3]);
            //Get the position of each triangle's mid point in world space coordinates
            var centre = (p1 + p2 + p3) / 3;                            
            
            //Calculate the radius of the mid point            
            float cRadius = Mathf.Sqrt(Mathf.Pow((centre.x), 2) +
                                       Mathf.Pow((centre.y), 2) +
                                       Mathf.Pow((centre.z), 2));         
                       
            //Define the regions for submeshes
            //Water regions
            if (cRadius < min) {
                submesh1.Add(index1);
                submesh1.Add(index2);
                submesh1.Add(index3);            

              // Grass regions
            } else if (cRadius >= min && cRadius < max) {

                submesh2.Add(index1);
                submesh2.Add(index2);
                submesh2.Add(index3);
                
                //Mountain regions
            } else {
                submesh3.Add(index1);
                submesh3.Add(index2);
                submesh3.Add(index3);
            }
        }

        //Assign the submeshes to the mesh
        workingMesh.SetTriangles(submesh1, 0);
        workingMesh.SetTriangles(submesh2, 1);
        workingMesh.SetTriangles(submesh3, 2);
        workingMesh.RecalculateBounds();
        workingMesh.RecalculateNormals();
        workingMesh.RecalculateTangents();
        workingObject.GetComponent<MeshFilter>().mesh = workingMesh;
        workingObject.GetComponent<MeshCollider>().sharedMesh = workingMesh;

        return materials;
    }

    private void SavePrefab(int seedNum, GameObject workingObject, Material[] materials) {

        Mesh workingMesh = workingObject.GetComponent<MeshFilter>().sharedMesh;
        Directory.CreateDirectory("Assets/Resources/Prefabs/Planets/Planet_" + seedNum);
        Directory.CreateDirectory("Assets/Resources/Prefabs/Planets/Planet_" + seedNum + "/Mesh");
        Directory.CreateDirectory("Assets/Resources/Prefabs/Planets/Planet_" + seedNum + "/Materials");
        var prefabPath = "Assets/Resources/Prefabs/Planets/Planet_Prefab_" + seedNum + ".prefab";
        var meshPath = "Assets/Resources/Prefabs/Planets/Planet_" + seedNum + "/Mesh/Planet_Mesh_" + seedNum + ".asset";
        
        workingMesh.RecalculateBounds();
        workingMesh.RecalculateNormals();
        workingMesh.RecalculateTangents();
        
        AssetDatabase.CreateAsset(workingMesh, meshPath);
        for (int n = 0; n < materials.Length; n++) {
            string materialPath = "Assets/Resources/Prefabs/Planets/Planet_" + seedNum + "/Materials/Planet_" + seedNum + "_Material_" + n + ".mat";
            AssetDatabase.CreateAsset(materials[n], materialPath);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var emptyPrefab = PrefabUtility.CreateEmptyPrefab(prefabPath);

        PrefabUtility.ReplacePrefab(workingObject, emptyPrefab);
        
    }
}

