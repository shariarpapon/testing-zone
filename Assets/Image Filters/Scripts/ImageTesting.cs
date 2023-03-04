using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ImageTesting : MonoBehaviour
{
    public Texture2D inputTexture;
    public UnityEngine.UI.Image imageUI;
    public ComputeShader computer;

    [Header("Properties")]
    public bool applyFilter;
    public int kernel = 2;

    DataBuffer[] dataBuffer;

    public void Execute() 
    {
        Texture2D sampleTexture = applyFilter ? ApplyFilter(inputTexture) : inputTexture;
        imageUI.sprite = Sprite.Create(sampleTexture, new Rect(0, 0, inputTexture.width, inputTexture.height), Vector2.zero);
    }

    private Texture2D ApplyFilter(Texture2D texture) 
    {
        dataBuffer = new DataBuffer[texture.width * texture.height];
        int idx = 0;
        for (int x = 0; x < texture.width; x++)
            for (int y = 0; y < texture.height; y++)
            {
                dataBuffer[idx] = new DataBuffer(x, y, texture.GetPixel(x, y));
                idx++;
            }

        int kernelIndex = computer.FindKernel("ImageFilter");
        int bufferSize = texture.width * texture.height;
        computer.SetInt("size_x", texture.width);
        computer.SetInt("size_y", texture.height);
        computer.SetInt("k", kernel);

        ComputeBuffer buffer = new ComputeBuffer(bufferSize, sizeof(float) * 4 + sizeof(int) * 2);
        buffer.SetData(dataBuffer);

        computer.SetBuffer(kernelIndex, "dataBuffer", buffer);
        computer.Dispatch(kernelIndex, texture.width / 16, texture.height / 16, 1);

        buffer.GetData(dataBuffer);
        Texture2D sampleTexture = new Texture2D(texture.width, texture.height);
        for (int i = 0; i < dataBuffer.Length; i++) 
        {
            sampleTexture.SetPixel(dataBuffer[i].x, dataBuffer[i].y, dataBuffer[i].GetColor());
        }

        sampleTexture.Apply();
        buffer.Release();

        return sampleTexture;
    }

    private bool ValidIndex(int index, int min, int max) 
    {
        return index >= min || index <= max ? true : false;
    }

    [System.Serializable]
    public struct DataBuffer 
    {
        public int x, y;
        public float r, g, b, a;

        public DataBuffer(int x, int y, Color color)
        {
            this.x = x;
            this.y = y;
            r = color.r;
            b = color.b;
            a = color.a;
            g = color.g;
        }

        public Color GetColor() 
        {
            return new Color(r, g, b, a);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ImageTesting))]
public class ImageTestingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();

        EditorGUILayout.Space();
        if (GUILayout.Button("Execute")) 
        {
            ((ImageTesting)target).Execute();
        }

        EditorUtility.SetDirty(target);
    }
}
#endif
