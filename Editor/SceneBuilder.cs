using GameScripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneBuilder : EditorWindow
{

	public static int PlanetNum = 10;
	private static GameObject sun;
	private static GameObject menu;
	private static GameObject[] planets;
	private static HeightmapGenerator mapGenerator;
	private static PlanetGenerator planetGenerator;
	private const string spacePath = "Assets/Scenes/SpaceScene.unity";
	private const string menuPath = "Assets/Scenes/Menu.unity";

	[MenuItem("Window/Planet Generator")]
	private static void Init()
	{
		SceneBuilder window = (SceneBuilder)GetWindow(typeof(SceneBuilder));
		window.Show();
	}

	private static void LoadData()
	{
		RenderSettings.skybox = Resources.Load<Material>("Materials/SkyBox/StarSkyBox");
		sun = Instantiate(Resources.Load<GameObject>("Prefabs/Sun"));
		sun.transform.position = new Vector3(0, 0 ,0);
		sun.name = "Sun";
		menu = Instantiate(Resources.Load<GameObject>("Prefabs/IngameMenu"));
		menu.name = "Menu";
		planets = Resources.LoadAll<GameObject>("Prefabs/Planets");
	}
	
	public static void PreProcess()
	{	
		mapGenerator = new HeightmapGenerator();
		planetGenerator = new PlanetGenerator();
		mapGenerator.Generate(PlanetNum);
		planetGenerator.GeneratePlanets(PlanetNum);		
		BuildScene();
	}

	private static void BuildScene()
	{	
		Scene spaceScene = EditorSceneManager.OpenScene(spacePath, OpenSceneMode.Single);
		LoadData();			
		var randomPlanet = Random.Range(0, planets.Length);
		for (int i = 0; i < planets.Length; i++)
		{
			var planet = Instantiate(planets[i]);
			planet.GetComponent<Atmosphere>().m_sun = sun;			
			planet.transform.position = new Vector3(0, 0, (i + 1) * 3000);			
			if (i == randomPlanet)
			{
				var sm = planet.transform.GetChild(3).gameObject;
				planet.SetActive(true);
				sm.SetActive(true);
			}
		}		
		EditorSceneManager.SaveScene(spaceScene, spacePath, true);	
		EditorSceneManager.CloseScene(spaceScene, true);
	}

	private void OnGUI()
	{
		planetGenerator = new PlanetGenerator();
		mapGenerator = new HeightmapGenerator();
		if (GUILayout.Button("Generate Planet"))
		{
			mapGenerator.Generate(1);
			planetGenerator.GeneratePlanets(1);
			BuildScene();
			BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
			buildPlayerOptions.scenes = new[] {menuPath, spacePath};
			buildPlayerOptions.locationPathName = "D:/Uni 2016/Project/Builds/Coelestium.exe";
			buildPlayerOptions.target = BuildTarget.StandaloneWindows;
			buildPlayerOptions.options = BuildOptions.None;
			BuildPipeline.BuildPlayer(buildPlayerOptions);
		}		
	}
}

