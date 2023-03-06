using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TestingZone.ImageFilter
{
    [ExecuteInEditMode]
    public class ImageFilter : MonoBehaviour
    {
        private const int NUM_THREADS = 32;

        public Texture2D inputTexture;
        public UnityEngine.UI.Image imageUI;
        public ComputeShader computer;
        public bool realtimeUpdate;

        [Header("Properties")]
        public bool applyFilter = true;
        public string filterKernel = "Blur";
        public uint sampleSize = 8;

        private ColorData[] colorDataArray;

        private void Update()
        {
            if (realtimeUpdate)
                Execute();
        }

        public void Execute() 
        {
            Texture2D sampleTexture = applyFilter ? ApplyFilter(inputTexture) : inputTexture;
            imageUI.sprite = Sprite.Create(sampleTexture, new Rect(0, 0, inputTexture.width, inputTexture.height), Vector2.zero);
        }

        private Texture2D ApplyFilter(Texture2D texture) 
        {
            colorDataArray = new ColorData[texture.width * texture.height];
            for (int x = 0; x < texture.width; x++)
                for (int y = 0; y < texture.height; y++)
                {
                    int idx = Index1D(x, y, texture.width);
                    colorDataArray[idx] = new ColorData(texture.GetPixel(x, y));
                }

            int kernelIndex = computer.FindKernel(GetValidatedKernel());
            int bufferSize = texture.width * texture.height;
            computer.SetInt("width", texture.width);
            computer.SetInt("height", texture.height);
            computer.SetInt("sample_size", (int)sampleSize);

            ComputeBuffer buffer = new ComputeBuffer(bufferSize, ColorData.GetByteSize());
            buffer.SetData(colorDataArray);

            computer.SetBuffer(kernelIndex, "colorDataBuffer", buffer);
            computer.Dispatch(kernelIndex, texture.width / NUM_THREADS, texture.height / NUM_THREADS, 1);

            buffer.GetData(colorDataArray);
            buffer.Release();

            Texture2D outputTexture = new(texture.width, texture.height);
            UpdateTexturePixelsWithReleasedData(outputTexture);

            return outputTexture;
        }

        private void UpdateTexturePixelsWithReleasedData (Texture2D texture) 
        {
            Color32[] colors = new Color32[texture.width * texture.height];
            for (int x = 0; x < texture.width; x++)
                for (int y = 0; y < texture.height; y++)
                {
                    int idx = Index1D(x, y, texture.width);
                    colors[idx] = colorDataArray[idx].GetColor();
                }
            texture.SetPixels32(colors);
            texture.Apply();
        }

        private int Index1D(int x, int y, int sizeX) 
        {
            return y * sizeX + x;
        }

        private string GetValidatedKernel() 
        {
            return string.IsNullOrEmpty(filterKernel) ? "Blur" : filterKernel;
        }

        [System.Serializable]
        private struct ColorData 
        {
            public float r, g, b;

            public ColorData(Color color)
            {
                r = color.r;
                g = color.g;
                b = color.b;
            }

            public Color GetColor() 
            {
                return new Color(r, g, b, 1);
            }

            public static int GetByteSize()
            {
                return sizeof(float) * 3;
            }
        }
    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(ImageFilter))]
    public class ImageTestingEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();
            ImageFilter imf = (ImageFilter)target;

            EditorGUILayout.Space();
            if (GUILayout.Button("Execute")) 
            {
                imf.Execute();
            }

            if (GUILayout.Button("Save Image") && (imf.imageUI.sprite != null))
            {
                byte[] bytes = imf.imageUI.sprite.texture.EncodeToPNG();
                File.WriteAllBytes(Application.dataPath + $"/Image Filters/Results/filtered_{System.DateTime.Now.ToFileTime()}.png", bytes);
                AssetDatabase.Refresh();
            }

            EditorUtility.SetDirty(target);
        }
    }
    #endif
}
