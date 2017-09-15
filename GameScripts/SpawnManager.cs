using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameScripts
{
	public class SpawnManager : MonoBehaviour {

		private struct ObjectTransform
		{
			public Vector3 position;
			public Quaternion rotation;
			public Vector3 scale;
		}
	
		#region fields 
		// Use this for initialization
		private int seed;
		private List<GameObject> treeList;
		private List<GameObject> buildingList;
		private List<GameObject> rocksList;
		private List<GameObject> enemiesList;
		private Dictionary<GameObject, TransformData> currentTrees;
		private Dictionary<GameObject, TransformData> currentGrass;
		private Dictionary<GameObject, TransformData> currentBuilding;
		private Dictionary<GameObject, TransformData> currentEnemies;
		private Dictionary<GameObject, TransformData> currentRocks;
		private Camera cam;
		private GameObject player;
		private GameObject enemies;
		private GameObject grassObj;
		private GameObject trees;
		private GameObject plants;
		private GameObject buildings;
		private GameObject rocks;
		private List<TransformData> treeTransforms;
		private List<TransformData> grassTransforms;
		private List<TransformData> buildingTransforms;
		private List<TransformData> enemiesTransforms;
		private List<TransformData> rocksTransforms;
		private TransformData? playerTransform;
		private GameObject myPlayer;
		private float min;
		private float spawnDistance;
		#endregion
	
		void LoadData ()
		{
			treeList = new List<GameObject>();
			currentTrees = new Dictionary<GameObject, TransformData>();
			currentGrass = new Dictionary<GameObject, TransformData>();
			currentBuilding = new Dictionary<GameObject, TransformData>();
			currentRocks = new Dictionary<GameObject, TransformData>();
			currentEnemies = new Dictionary<GameObject, TransformData>();
			min = transform.parent.lossyScale.x * 0.975f;
			BinaryFormatter bf = new BinaryFormatter();
			FileStream file = File.Open(Application.streamingAssetsPath + "/Planets/Planet_" + seed + "/PlanetData/TreesData.dat", FileMode.Open);
			treeTransforms = bf.Deserialize(file) as List<TransformData>;
			file.Close();
				
			FileStream file2 = File.Open(Application.streamingAssetsPath + "/Planets/Planet_" + seed + "/PlanetData/PlayerData.dat", FileMode.Open);
			playerTransform = bf.Deserialize(file2) as TransformData?;
			file2.Close();
		
			FileStream file3 = File.Open(Application.streamingAssetsPath + "/Planets/Planet_" + seed + "/PlanetData/PlantsData.dat", FileMode.Open);
			grassTransforms = bf.Deserialize(file3) as List<TransformData>;
			file3.Close();
		
			FileStream file4 = File.Open(Application.streamingAssetsPath + "/Planets/Planet_" + seed + "/PlanetData/BuildingsData.dat", FileMode.Open);
			buildingTransforms = bf.Deserialize(file4) as List<TransformData>;
			file4.Close();
		
			FileStream file5 = File.Open(Application.streamingAssetsPath + "/Planets/Planet_" + seed + "/PlanetData/EnemiesData.dat", FileMode.Open);
			enemiesTransforms = bf.Deserialize(file5) as List<TransformData>;
			file5.Close();
			
			FileStream file6 = File.Open(Application.streamingAssetsPath + "/Planets/Planet_" + seed + "/PlanetData/RocksData.dat", FileMode.Open);
			rocksTransforms = bf.Deserialize(file6) as List<TransformData>;
			file6.Close();

			player = Resources.Load<GameObject>("Prefabs/Player");
		
			grassObj = Resources.Load<GameObject>("Prefabs/Vegetation/Plants/Plants");
		
			//fill a list of tree gameobjects		        
			//Conifer
			treeList.Add(Resources.Load<GameObject>("Prefabs/Vegetation/SpeedTrees/Conifer_Desktop"));
        
			//BroadLeaf
			treeList.Add(Resources.Load<GameObject>("Prefabs/Vegetation/SpeedTrees/Broadleaf_Desktop"));
        
			//Dead tree
			treeList.Add(Resources.Load<GameObject>("Prefabs/Vegetation/Trees/Tree 2"));
        
			//Bush
			treeList.Add(Resources.Load<GameObject>("Prefabs/Vegetation/Trees/Tree 3"));
		
			//Palms
			treeList.Add(Resources.Load<GameObject>("Prefabs/Vegetation/SpeedTrees/Palm_Desktop"));

			//Load all buildings
			buildingList = Resources.LoadAll<GameObject>("Prefabs/Buildings").ToList();

			//Load all rocks
			rocksList = Resources.LoadAll<GameObject>("Prefabs/Rocks/_prefabs").ToList();

			//Load all enemies
			enemiesList = Resources.LoadAll<GameObject>("Prefabs/Enemies").ToList();

			trees = new GameObject();
			trees.name = "Trees";   
			trees.transform.parent = transform;
			trees.transform.localScale = Vector3.one;
			buildings = new GameObject();
			buildings.name = "Buildings";   
			buildings.transform.parent = transform;
			buildings.transform.localScale = Vector3.one;
			plants = new GameObject();
			plants.name = "Plants";   
			plants.transform.parent = transform;
			plants.transform.localScale = Vector3.one;
			enemies = new GameObject();
			enemies.name = "Enemies";   
			enemies.transform.parent = transform;
			enemies.transform.localScale = Vector3.one;
			rocks = new GameObject();
			rocks.name = "Rocks";   
			rocks.transform.parent = transform;
			rocks.transform.localScale = Vector3.one;
			SpawnPlayer();
			cam = Camera.main;
			if (cam.depthTextureMode == DepthTextureMode.None)
				cam.depthTextureMode = DepthTextureMode.Depth;
			spawnDistance = cam.farClipPlane / 8;
		}

		private void Awake()
		{
			var parentName = transform.parent.name;
			var lastChar = Regex.Match(parentName, @"\d+").Value;
			var seedNum = Int32.Parse(lastChar);
			SetSeed(seedNum);		
		}

		private void Start()
		{	
			LoadData();
			//DebugTrees();
			//DebugGrass();
			//DebugBuildings();	
			StartCoroutine(Manage());
			//DebugEnemies();
			//DebugRocks();
		}

		public void SetSeed(int num)
		{
			seed = num;
		}

		private void SpawnPlayer()
		{	
			var playerClone = Instantiate(player);		
			playerClone.name = "Player";
			var gravity = playerClone.GetComponent<PlayerGravity>();	
			if (playerTransform != null) AssignTransform(playerClone, transform.gameObject, GetTransform(playerTransform.Value));
			playerClone.transform.position += transform.up * 1.5f;
			myPlayer = GameObject.Find("Player");	
		}

		private void DebugTrees()
		{
			foreach (var trans in treeTransforms)
			{
				var objTransform = GetTransform(trans);
			
				/*var triCentre = objTransform.position;
			var originV = (triCentre - transform.position).normalized;
			var originRot = Quaternion.FromToRotation(Vector3.up, originV);
			var normalRot = objTransform.rotation;									
			var angle = Quaternion.Angle(originRot, normalRot);	
			
			if (angle <= 10)
			{*/					
				var radius = Mathf.Pow(objTransform.position.x - transform.parent.position.x, 2) +
				             Mathf.Pow(objTransform.position.y - transform.parent.position.y, 2) +
				             Mathf.Pow(objTransform.position.z - transform.parent.position.z, 2);
				if (radius > min * min)
				{
					int[] indexInputs = {2, 0, 1, 3};
					//Choose different probability for each tree. 5% dead trees, 35% broadleafs, 35% conifer and 20% bushes
					int randomTree = RandomProbabilityNumber(0.1f, 0.8f, indexInputs, false);
					GameObject tree = Instantiate(treeList[randomTree]);					
					AssignTransform(tree, trees, objTransform);
					switch (randomTree)
					{
						case 0:
							tree.name = "Conifer";
							break;
						case 1:
							tree.name = "BroadLeaf";
							break;
						case 2:
							tree.name = "Dead Tree";
							break;
						case 3:
							tree.name = "Bush";
							tree.transform.localScale /= 1.3f;
							break;
						default:
							break;
					}
				}
				else
				{
					GameObject tree = Instantiate(treeList[4]);
					AssignTransform(tree, trees, objTransform);
					tree.name = "Palm";
					tree.transform.localScale *= 3;
				}
				//}
			}
		}

		private void ManageTrees(ObjectTransform objTransform, TransformData trans, Dictionary<GameObject, TransformData> currentObjects, float radius)
		{					
			if (radius > min * min)
			{
				int[] indexInputs = {2, 0, 1, 3};
				//Choose different probability for each tree. 5% dead trees, 35% broadleafs, 35% conifer and 20% bushes
				int randomTree = RandomProbabilityNumber(0.1f, 0.8f, indexInputs, false);
				GameObject tree = Instantiate(treeList[randomTree]);
				AssignTransform(tree, trees, objTransform);
				switch (randomTree)
				{
					case 0:
						tree.name = "Conifer";
						tree.transform.localScale /= 1.5f;
						break;
					case 1:
						tree.name = "BroadLeaf";
						break;
					case 2:
						tree.name = "Dead Tree";
						break;
					case 3:
						tree.name = "Bush";
						tree.transform.localScale /= 1.3f;
						break;
					default:
						break;
				}
				currentObjects[tree] = trans;
			}
			else
			{
				GameObject tree = Instantiate(treeList[4]);
				AssignTransform(tree, trees, objTransform);
				tree.name = "Palm";
				tree.transform.localScale *= 3;
				currentObjects[tree] = trans;
			}					
		}

		private void DebugGrass()
		{
			foreach (var trans in grassTransforms)
			{			
				var objTransform = GetTransform(trans);
				var radius = Mathf.Pow(objTransform.position.x - transform.parent.position.x, 2) +
				             Mathf.Pow(objTransform.position.y - transform.parent.position.y, 2) +
				             Mathf.Pow(objTransform.position.z - transform.parent.position.z, 2);

				if (radius > min * min)
				{						
					GameObject grass = Instantiate(grassObj);
					AssignTransform(grass, plants, objTransform);					
				}			
			}
		}

		private void DebugBuildings()
		{
			foreach (var trans in buildingTransforms)
			{			
				var objTransform = GetTransform(trans);
				var radius = Mathf.Pow(objTransform.position.x - transform.parent.position.x, 2) +
				             Mathf.Pow(objTransform.position.y - transform.parent.position.y, 2) +
				             Mathf.Pow(objTransform.position.z - transform.parent.position.z, 2);

				if (radius > min * min)
				{
					int[] indexes = {0, 1, 2, 3, 4};
					var randomBuilding = RandomProbabilityNumber(0.1f, 0.8f, indexes, true);
					GameObject building;
					if (playerTransform != null && GetTransform(playerTransform.Value).position.Equals(objTransform.position))
					{
						building = Instantiate(buildingList[1]);
					}
					else
					{
						building = Instantiate(buildingList[randomBuilding]);
					}
					AssignTransform(building, buildings, objTransform);					
				}			
			}
		}

		private void DebugEnemies()
		{			
			foreach (var trans in enemiesTransforms)
			{			
				var objTransform = GetTransform(trans);
				var radius = Mathf.Pow(objTransform.position.x - transform.parent.position.x, 2) +
				             Mathf.Pow(objTransform.position.y - transform.parent.position.y, 2) +
				             Mathf.Pow(objTransform.position.z - transform.parent.position.z, 2);
				
				if (radius > min * min)
				{	
					GameObject enemy = Instantiate(enemiesList[0]);				
					AssignTransform(enemy, enemies, objTransform);					
				}			
			}
			
		}

		private void DebugRocks()
		{
			Debug.Log("rocks: " + rocksList.Count);
			foreach (var trans in rocksTransforms)
			{			
				var objTransform = GetTransform(trans);
				var radius = Mathf.Pow(objTransform.position.x - transform.parent.position.x, 2) +
				             Mathf.Pow(objTransform.position.y - transform.parent.position.y, 2) +
				             Mathf.Pow(objTransform.position.z - transform.parent.position.z, 2);
				
				if (radius > min * min)
				{	
					GameObject rock = Instantiate(rocksList[Random.Range(0, 3)]);				
					AssignTransform(rock, rocks, objTransform);					
				}			
			}
		}

		private void ManageBuildings(ObjectTransform objTransform, TransformData trans, Dictionary<GameObject, TransformData> currentObjects, float radius)
		{
			if (radius > min * min)
			{
				//if(currentObjects.Any(e => Vector3.Distance(objTransform.position, GetTransform(e.Value).position) > 2)) {
					int[] indexes = {0, 1, 2, 3, 4};
					var randomBuilding = RandomProbabilityNumber(0.1f, 0.8f, indexes, true);
					GameObject building;
					if (playerTransform != null && GetTransform(playerTransform.Value).position == objTransform.position)
					{
						building = Instantiate(buildingList[1]);
					}
					else
					{
						building = Instantiate(buildingList[randomBuilding]);
					}
					AssignTransform(building, buildings, objTransform);
					currentObjects[building] = trans;
				//}
			}
		}

		private void ManageObjects(List<TransformData> objTransforms, Dictionary<GameObject, TransformData> currentObjects, float distance, int objType)
		{
			var spawnPoints = objTransforms
				.Where(e => Vector3.Distance(GetTransform(e).position,
					            myPlayer.transform.position) <= distance)
				.ToList();
			if (spawnPoints.Count != 0)
			{
				foreach (var trans in spawnPoints)
				{
					if (!currentObjects.Values.Contains(trans))
					{
						var objTransform = GetTransform(trans);
						var radius = Mathf.Pow(objTransform.position.x - transform.parent.position.x, 2) +
						             Mathf.Pow(objTransform.position.y - transform.parent.position.y, 2) +
						             Mathf.Pow(objTransform.position.z - transform.parent.position.z, 2);
						switch (objType)
						{
							case 0:
								ManageGrass(objTransform, trans, currentObjects, radius);
								break;
							case 1:
								ManageTrees(objTransform, trans, currentObjects, radius);
								break;
							case 2:
								ManageBuildings(objTransform, trans, currentObjects, radius);
								break;
							case 3:
								ManageRocks(objTransform, trans, currentObjects);
								break;
							case 4:
								ManageEnemies(objTransform, trans, currentObjects, radius);
								break;
						}
					}
				}
			}
			if (currentObjects.Count != 0)
			{
				List<GameObject> temp = new List<GameObject>();
				foreach (var obj in currentObjects.Keys)
				{
					if (Vector3.Distance(obj.transform.position, myPlayer.transform.position) > distance)
					{
						temp.Add(obj);
						Destroy(obj);
					}				
				}
				temp.ForEach(e => currentObjects.Remove(e));
			}	
		}

		private void ManageRocks(ObjectTransform objTransform, TransformData trans, Dictionary<GameObject, TransformData> currentObjects)
		{
			GameObject rock = Instantiate(rocksList[Random.Range(0, 3)]);
			AssignTransform(rock, rocks, objTransform);
			currentObjects[rock] = trans;
		}

		private void ManageEnemies(ObjectTransform objTransform, TransformData trans, Dictionary<GameObject, TransformData> currentObjects, float radius)
		{
			if (radius > min * min)
			{
				GameObject enemy = Instantiate(enemiesList[Random.Range(0, 1)]);
				AssignTransform(enemy, enemies, objTransform);
				currentObjects[enemy] = trans;
			}
		}
		
		private void ManageGrass(ObjectTransform objTransform, TransformData trans, Dictionary<GameObject, TransformData> currentObjects, float radius)
		{
			if (radius > min * min)
			{
				GameObject grass = Instantiate(grassObj);
				AssignTransform(grass, plants, objTransform);
				currentObjects[grass] = trans;
			}
		}		

		private int RandomProbabilityNumber(float minValue, float maxValue, int[] values, bool isBuilding)
		{
			float rand = Random.value;

			if (rand <= minValue)
			{
				return values[0];
			} 
			if (rand > minValue && rand <= maxValue)
			{
				int pick1 = Random.Range(1, 3);
				return values[pick1]; 
			}
			int pick2 = isBuilding ? Random.Range(3, 5) : 3;
			return values[pick2];     
		}

		private ObjectTransform GetTransform(TransformData data)
		{		
			//Add the current position and rotation of the planet to the original transform data	
			return new ObjectTransform
			{
				position = new Vector3(data.X, data.Y, data.Z) + transform.parent.position,
				rotation = new Quaternion(data.rotX, data.rotY, data.rotZ, data.rotW),
				scale =  new Vector3(data.scaleX, data.scaleY, data.scaleZ)
			};
		}

		private void AssignTransform(GameObject obj, GameObject parent, ObjectTransform trans)
		{
			obj.transform.parent = parent.transform;
			obj.transform.position = trans.position;
			obj.transform.rotation = trans.rotation;
			obj.transform.localScale = trans.scale;
		}

		private IEnumerator Manage()
		{
			while (Application.isPlaying)
			{				
				for (int i = 0; i < 5; i++)
				{					
					switch (i)
					{
						case 0:
							ManageObjects(grassTransforms, currentGrass, 100, i);
							break;
						case 1:
							ManageObjects(treeTransforms, currentTrees, spawnDistance, i);
							break;
						case 2:
							ManageObjects(buildingTransforms, currentBuilding, spawnDistance, i);
							break;
						case 3:
							ManageObjects(rocksTransforms, currentRocks, 100, i);
							break;
						case 4:
							ManageObjects(enemiesTransforms, currentEnemies, spawnDistance, i);
							break;
					}					
				}				
				yield return new WaitForSeconds(0.05f);			
			} 
		}
	}
}
