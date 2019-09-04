using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpectrumDisplayer : MonoBehaviour {

    AudioSource player;
    public int columnCount = 10;

    public Color[] colors;
    public GameObject blockPrefab;

    List<List<GameObject>> blocks = new List<List<GameObject>>();

	// Use this for initialization
	void Start () {
        player = GetComponent<AudioSource>();
        for (int i=0;i<columnCount;i++)
        {
            blocks.Add(new List<GameObject>());
        }

        colors = new Color[columnCount];
        for (int i=0;i<columnCount;i++)
        {
            colors[i] = Color.HSVToRGB(1f/columnCount * i, 1, 1);
        }
        transform.localPosition = new Vector3(transform.position.x, transform.localScale.y * columnCount / 2, transform.position.z);
	}
	
	// Update is called once per frame
	void Update () {
        if (Time.frameCount % 1 == 0)
        {
            float[] data = new float[1024];
            player.GetOutputData(data, 0);
            //player.GetSpectrumData(data, 0, FFTWindow.Rectangular);

            float[] spectrum = FFT.forward(data);
            //float[] spectrum = data;

            float[] ordered = FFT.orderCoefficients(spectrum);

            int[] shouldBeHeights = new int[columnCount];
            int sliceSize = ordered.Length / columnCount;
            for (int i = 0; i < columnCount; i++)
            {
                shouldBeHeights[i] = (int)(arraySliceMean(ordered, i * sliceSize, (i + 1) * sliceSize) * columnCount / 10);
            }
   
            updateBlocks(shouldBeHeights);

            /*int[] targetHeights = new int[columnCount];
            for (int i=0;i<columnCount;i++)
            {
                targetHeights[i] = 10;
            }
            updateBlocks(targetHeights);*/
        }

	}

    private void updateBlocks(int[] targetBarHeights)
    {
        for (int i=0;i<targetBarHeights.Length;i++)
        {
            if (i > blocks.Count) Debug.LogWarning("i " + i + " is out of range of " + blocks.Count + " for blocks");
            if (i > targetBarHeights.Length) Debug.LogWarning("i " + i + " is out of range of " + targetBarHeights.Length + " for targetHeight");
            if (blocks[i].Count > targetBarHeights[i])
            {
                for (int o = blocks[i].Count - 1; o >= targetBarHeights[i]; o--)
                {
                    if (o < 0) Debug.LogWarning("O is below zero");
                    if (o > blocks[i].Count) Debug.LogWarning("o " + o + " is out of range at blocks of i with range " + blocks[i].Count);
                    Destroy(blocks[i][o]);
                    blocks[i].RemoveAt(o);
                }
            } else if (blocks[i].Count < targetBarHeights[i])
            {
                for (int o=blocks[i].Count;o<targetBarHeights[i];o++)
                {
                    addBlockAtPosition(i, o);
                }
            }
        }
    }

    private void addBlockAtPosition(int gridX, int gridY)
    {
        float blockSize = transform.localScale.x / columnCount;
        float trueX = (gridX - columnCount / 2);
        float trueY = (gridY - columnCount / 2) + 0.5f;
        Vector3 localPosition = new Vector3(trueX, trueY, 0);
        GameObject newBlock = Instantiate(blockPrefab, Vector3.zero, Quaternion.identity);
        newBlock.transform.SetParent(gameObject.transform);
        newBlock.transform.localPosition = localPosition;

        //Set the new blocks color
        newBlock.GetComponent<MeshRenderer>().material.SetColor("_Color", colors[gridX]);

        //Debug.Log("Old local scale: ")
        //Debug.Log("Should be block Size: "+blockSize);
        newBlock.transform.localScale = Vector3.Scale(newBlock.transform.localScale, transform.localScale);

        blocks[gridX].Add(newBlock);
    }

    private float arraySliceMean(float[] arr, int sliceStart, int sliceEnd)
    {
        float mean = 0;
        for (int i=sliceStart;i<sliceEnd;i++)
        {
            mean += arr[i];
        }
        return mean / (sliceEnd - sliceStart);
    }
}
