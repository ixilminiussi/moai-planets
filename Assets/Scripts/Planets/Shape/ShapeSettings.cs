using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Planet Body/Shape Settings")]
public class ShapeSettings : ScriptableObject
{
	public bool randomize;
	public ComputeShader heightMapCompute;

	public float seed;
	public event System.Action OnSettingsChanged;

	[Header("Planet Parameters")]
	public PlanetSettings planet;
	public OceanSettings ocean;
	public ContinentSettings continent;

	ComputeBuffer heightBuffer;

	[HideInInspector]
	Vector4 planesNoiseOffset;
	Vector4 mountainNoiseOffset;
	Vector4 continentNoiseOffset;

	public float[] CalculateHeights(ComputeBuffer vertexBuffer)
	{
		Random.InitState((int)seed);
		continentNoiseOffset = RandomVector(-1000, 1000);
		mountainNoiseOffset = RandomVector(-1000, 1000);
		planesNoiseOffset = continentNoiseOffset + RandomVector(-10, 10);

		Vector4 mountainNoiseParams = new Vector4(planet.detail, continent.mountainSize, planet.ruggedness, continent.mountainSharpness);
		Vector4 planesNoiseParams = new Vector4(1, 1 / continent.planeSize, 0, 0);
		Vector4 continentNoiseParams = new Vector4(planet.detail, 1 / planet.continentSize, planet.ruggedness, 0);
		// Set data
		heightMapCompute.SetFloat("seed", seed);
		heightMapCompute.SetInt("numVertices", vertexBuffer.count);
		heightMapCompute.SetBuffer(0, "vertices", vertexBuffer);

		heightMapCompute.SetFloat("planetSize", planet.size);
		heightMapCompute.SetVector("continentNoiseOffset", continentNoiseOffset);
		heightMapCompute.SetVector("continentNoiseParams", continentNoiseParams);
		heightMapCompute.SetFloat("mountainElevation", continent.mountainElevation);
		heightMapCompute.SetFloat("mountainMask", 1 - continent.mountainFrequency);
		heightMapCompute.SetVector("mountainNoiseOffset", mountainNoiseOffset);
		heightMapCompute.SetVector("mountainNoiseParams", mountainNoiseParams);
		heightMapCompute.SetFloat("planesSmoothing", continent.transitionSmoothing);
		heightMapCompute.SetVector("planesNoiseOffset", planesNoiseOffset);
		heightMapCompute.SetVector("planesNoiseParams", planesNoiseParams);
		heightMapCompute.SetFloat("oceanBed", ocean.bed);
		heightMapCompute.SetFloat("oceanDepth", ocean.depth);
		heightMapCompute.SetFloat("oceanSmoothing", ocean.smoothing);

		ComputeHelper.CreateAndSetBuffer<float>(ref heightBuffer, vertexBuffer.count, heightMapCompute, "heights");

		// Run
		ComputeHelper.Run(heightMapCompute, vertexBuffer.count);

		// Get heights
		var heights = new float[vertexBuffer.count];
		heightBuffer.GetData(heights);
		return heights;
	}

	public void ReleaseBuffers()
	{
		ComputeHelper.Release(heightBuffer);
	}

	protected void OnValidate()
	{
		if (OnSettingsChanged != null)
		{
			OnSettingsChanged();
		}
	}

	Vector4 RandomVector(float min, float max)
	{
		return new Vector4(Random.Range(min, max), Random.Range(min, max), Random.Range(min, max), Random.Range(min, max));
	}

	[System.Serializable]
	public struct PlanetSettings
	{
		public int size;
		[Range(1, 10)] public int detail;
		[Range(0, 1)] public float ruggedness;
		[Range(0, 2)] public float continentSize;
	}

	[System.Serializable]
	public struct OceanSettings
	{
		[Range(0, 50)] public int depth;
		[Range(0, 1)] public float bed;
		[Range(0, 1)] public float smoothing;
	}

	[System.Serializable]
	public struct ContinentSettings
	{
		[Range(1, 50)] public int mountainElevation;
		[Range(0, 2)] public float mountainSize;
		[Range(0, 5)] public float mountainSharpness;
		[Range(0, 1)] public float mountainFrequency;
		[Range(0, 2)] public float planeSize;
		[Range(0, 4)] public float transitionSmoothing;
	}
}