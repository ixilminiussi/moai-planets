using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Planet Body/Shading Settings")]
public class ShadingSettings : ScriptableObject
{
	public bool randomize;
	public ComputeShader shadingDataCompute;

	public Material terrainMaterial = null;
	public float seed;
	public event System.Action OnSettingsChanged;

	[Header("Settings")]
	public float shoreHeight;
	public float planesHeight;
	public float mountainsHeight;
	public float peaksHeight;
	public BiomeSettings biome;
	public SteepnessSettings steepness;
	public PlanetColors colors;

	protected Vector4[] cachedShadingData;
	ComputeBuffer shadingBuffer;


	// Generate Vector4[] of shading data. This is stored in mesh uvs and used to help shade the body
	public Vector4[] GenerateShadingData(ComputeBuffer vertexBuffer)
	{
		int numVertices = vertexBuffer.count;
		Vector4[] shadingData = new Vector4[numVertices];

		shadingDataCompute.SetInt("numVertices", numVertices);
		shadingDataCompute.SetBuffer(0, "vertices", vertexBuffer);
		ComputeHelper.CreateAndSetBuffer<Vector4>(ref shadingBuffer, numVertices, shadingDataCompute, "shadingData");

		// Run
		ComputeHelper.Run(shadingDataCompute, numVertices);

		// Get data
		shadingBuffer.GetData(shadingData);

		return shadingData;
	}

	// Set shading properties on terrain
	public void SetTerrainProperties(Material material, Vector3 heightMinMax)
	{
		if (randomize)
		{
			RandomizeColors();
		}
		Debug.Log(heightMinMax.x + ": " + heightMinMax.y + ": " + heightMinMax.z);
		material.SetVector("_HeightMinMax", heightMinMax);
		material.SetFloat("_ShoreHeight", shoreHeight);
		material.SetFloat("_PlanesHeight", planesHeight);
		material.SetFloat("_PeaksHeight", peaksHeight);
		material.SetFloat("_SteepnessThreshold", steepness.threshold);
		material.SetFloat("_SteepnessSharpness", steepness.sharpness);
		material.SetFloat("_SteepnessDropoff", steepness.dropoff);
		material.SetFloat("_BiomeSize", biome.size);
		material.SetFloat("_BiomeSharpness", biome.sharpness);
		material.SetFloat("_NoiseStrength", biome.randomizeStrength);

		material.SetColor("_FlatUnderwater", colors.flatUnderwater);
		material.SetColor("_SteepUnderwater", colors.steepUnderwater);
		material.SetColor("_Shores", colors.shores);
		material.SetColor("_GrassA", colors.grassA);
		material.SetColor("_GrassB", colors.grassB);
		material.SetColor("_SteepA", colors.steepA);
		material.SetColor("_SteepB", colors.steepB);
		material.SetColor("_Snow", colors.snow);
	}

	void ApplyColors(Material material, PlanetColors color)
	{
	}

	/* public virtual void SetOceanProperties(Material oceanMaterial)
	{
		if (oceanSettings)
		{
			oceanSettings.SetProperties(oceanMaterial, seed, randomize);
		}
	}*/

	public void ReleaseBuffers()
	{
		ComputeHelper.Release(shadingBuffer);
	}

	public void RandomizeColors()
	{
		return;
	}

	public static void TextureFromGradient(ref Texture2D texture, int width, Gradient gradient, FilterMode filterMode = FilterMode.Bilinear)
	{
		if (texture == null)
		{
			texture = new Texture2D(width, 1);
		}
		else if (texture.width != width)
		{
			texture.Reinitialize(width, 1);
		}
		if (gradient == null)
		{
			gradient = new Gradient();
			gradient.SetKeys(
				new GradientColorKey[] { new GradientColorKey(Color.black, 0), new GradientColorKey(Color.black, 1) },
				new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) }
			);
		}
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.filterMode = filterMode;

		Color[] cols = new Color[width];
		for (int i = 0; i < cols.Length; i++)
		{
			float t = i / (cols.Length - 1f);
			cols[i] = gradient.Evaluate(t);
		}
		texture.SetPixels(cols);
		texture.Apply();
	}

	protected void OnValidate()
	{
		if (OnSettingsChanged != null)
		{
			OnSettingsChanged();
		}
	}

	Vector4 RandomVector()
	{
		float min = -1000;
		float max = 1000;
		return new Vector4(Random.Range(min, max), Random.Range(min, max), Random.Range(min, max), Random.Range(min, max));
	}

	[System.Serializable]
	public struct PlanetColors
	{
		public Color flatUnderwater;
		public Color steepUnderwater;
		public Color shores;
		public Color grassA;
		public Color grassB;
		public Color steepA;
		public Color steepB;
		public Color snow;
	}

	[System.Serializable]
	public struct BiomeSettings
	{
		[Range(0, 2)]public float size;
		[Range(1, 8)]public float sharpness;
		[Range(0, 2)]public float randomizeStrength;
		[Range(1, 8)]public float randomizeSharpness;
	}

	[System.Serializable]
	public struct SteepnessSettings
	{
		[Range(0, 2)]public float threshold;
		[Range(0, 1)]public float dropoff;
		[Range(1, 8)]public float sharpness;
	}
}
