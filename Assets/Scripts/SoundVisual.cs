using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundVisual : MonoBehaviour
{
    private const int SAMPLE_SIZE = 1024;

    public float rmsValue;
    public float dbValue;
    public float pitchValue;

    public float backgroundIntensity;
    public Material backgroundMaterial;
    public Color minColor;
    public Color maxColor;

    public float maxVisualScale = 25.0f;
    public float visualModifier = 50.0f;
    public float visualiserSmoothSpeed = 10.0f;
    public float backgroundSmoothSpeed = 0.5f;
    public float keepPercentage = 0.5f;
    public int amtVisual = 64;
    public int dbCap = 40;

    private AudioSource source;
    private float[] samples;
    private float[] spectrum;
    private float sampleRate;

    private Transform[] visualList;
    private float[] visualScale;

    private void Start()
    {
        source = GetComponent<AudioSource>();
        samples = new float[SAMPLE_SIZE];
        spectrum = new float[SAMPLE_SIZE];
        sampleRate = AudioSettings.outputSampleRate;

        // SpawnLine();
        SpawnCircle();
    }

    private void SpawnLine()
    {
        visualScale = new float[amtVisual];
        visualList = new Transform[amtVisual];

        for (int i = 0; i < amtVisual; i += 1)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube) as GameObject;
            visualList[i] = go.transform;
            visualList[i].position = Vector3.right * i;
        }
    }

    private void SpawnCircle()
    {
        visualScale = new float[amtVisual];
        visualList = new Transform[amtVisual];
        Vector3 center = Vector3.zero;
        float radius = 10.0f;

        for (int i = 0; i < amtVisual; i += 1)
        {
            float ang = i * 1.0f / amtVisual;
            ang = ang * Mathf.PI * 2;

            float x = center.x + Mathf.Cos(ang) * radius;
            float y = center.y + Mathf.Sin(ang) * radius;

            Vector3 pos = center + new Vector3(x, y, 0);
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube) as GameObject;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(Vector3.forward, pos);
            visualList[i] = go.transform;
        }

    }

    private void Update()
    {
        AnalyseSound();
        UpdateVisual();
        UpdateBackground();
    }

    private void UpdateVisual()
    {
        int visualIndex = 0;
        int spectrumIndex = 0;
        int averageSize = (int) ((SAMPLE_SIZE * keepPercentage) / amtVisual);

        while (visualIndex < amtVisual)
        {
            int j = 0;
            float sum = 0;
            while (j < averageSize)
            {
                sum += spectrum[spectrumIndex];
                spectrumIndex++;
                j++;
            }

            float scaleY = sum / averageSize * visualModifier;
            visualScale[visualIndex] -= Time.deltaTime * visualiserSmoothSpeed;
            if (visualScale[visualIndex] < scaleY)
                visualScale[visualIndex] = scaleY;

            if (visualScale[visualIndex] > maxVisualScale)
                visualScale[visualIndex] = maxVisualScale;

            visualList[visualIndex].localScale = Vector3.one + Vector3.up * visualScale[visualIndex];
            visualIndex++;
        }
    }

    private void UpdateBackground()
    {
        backgroundIntensity -= Time.deltaTime * backgroundSmoothSpeed;
        if (backgroundIntensity < dbValue / dbCap)
            backgroundIntensity = dbValue / dbCap;

        backgroundMaterial.color = Color.Lerp(maxColor, minColor, -backgroundIntensity);
    }

    private void AnalyseSound()
    {
        source.GetOutputData(samples, 0);

        // Get the RMS
        float sum = 0;
        for (int i = 0; i < SAMPLE_SIZE; i += 1)
        {
            sum = samples[i] * samples[i];
        }
        rmsValue = Mathf.Sqrt(sum / SAMPLE_SIZE);

        // Get the DB Value
        dbValue = 20 * Mathf.Log10(rmsValue / 0.1f);

        // Get sound spectrum
        source.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        // Find pitch
        float maxV = 0;
        var maxN = 0;
        for (int i = 0; i < SAMPLE_SIZE; i += 1)
        {
            if (!(spectrum[i] > maxV) || !(spectrum[i] > 0.0f))
                continue;

            maxV = spectrum[i];
            maxN = i;
        }

        float freqN = maxN;
        if (maxN > 0 && maxN < SAMPLE_SIZE - 1)
        {
            var dL = spectrum[maxN - 1] / spectrum[maxN];
            var dR = spectrum[maxN - 1] / spectrum[maxN];
            freqN += 0.5f * (dR * dR - dL * dL);
        }
        pitchValue = freqN * (sampleRate / 2) / SAMPLE_SIZE;
    }
}
