using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using LibNoise.Generator;
using LibNoise.Operator;
using LibNoise;

public class HeightmapGenerator {

    //Generator Modules
    #region
    private LibNoise.Generator.Perlin perlinGenerator;
    private Billow billowGenerator;
    private RidgedMultifractal ridgedGenerator;
    #endregion

    //Modifier Modules
    #region
    private ScaleBias scaleBias;
    private Turbulence turbulence;
    #endregion

    //Selector Modules
    #region
    private Select select;
    #endregion

    //Map Builder
    #region
    private Noise2D mapBuilder;
    private Texture2D finalMap;
    private Texture2D normalMap;
    #endregion

    public void Generate(int mapNum)
    {       
        for (int i = 0; i < mapNum; i++)
        {
              GenerateMap(i);             
        }  
    }

    // Use this for initialization
    private void GenerateMap(int seed)
    {     
        perlinGenerator = new LibNoise.Generator.Perlin();
        billowGenerator = new Billow();
        ridgedGenerator = new RidgedMultifractal();

        perlinGenerator.Frequency = 1f;
        perlinGenerator.Persistence = 0.25f;
        perlinGenerator.Seed = seed;
        billowGenerator = new Billow();
        billowGenerator.Frequency = 2.0f;
        scaleBias = new ScaleBias(0.125, -0.75, billowGenerator);
        select = new Select(ridgedGenerator, scaleBias, perlinGenerator);
        select.SetBounds(0.0, 1000.0);
        select.FallOff = 0.125;       

        turbulence = new Turbulence(select);
        turbulence.Frequency = 5.0;
        turbulence.Power = 0.125;

        mapBuilder = new Noise2D(2048, 2048, turbulence);
        mapBuilder.GenerateSpherical(-90, 90, -180, 180);
        finalMap = mapBuilder.GetTexture(GradientPresets.Grayscale);
        
        normalMap = mapBuilder.GetNormalMap(5.0f);

        byte[] bytes = finalMap.EncodeToPNG();
        byte[] bytesN = normalMap.EncodeToPNG();
        BinaryFormatter bf = new BinaryFormatter();
        Directory.CreateDirectory(Application.streamingAssetsPath + "/Heightmaps");
        var file = File.Create(Application.streamingAssetsPath + "/Heightmaps/map_" + seed + ".dat");
        var file2 = File.Create(Application.streamingAssetsPath + "/Heightmaps/map_" + seed + "N.dat");
        bf.Serialize(file, bytes);
        bf.Serialize(file2, bytesN);
        file.Close();
        file2.Close();  
        
    }
} 
