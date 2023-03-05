using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TestingZone.ImageFilter
{ 
    public class ImageFilter : MonoBehaviour
    {
        public Texture2D inputTexture;
        public UnityEngine.UI.Image imageUI;
        public ComputeShader computer;

        [Header("Properties")]
        public bool applyFilter = true;
        public int sampleSize = 8;

        ColorData[] colorDataArray;

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

            int kernelIndex = computer.FindKernel("FilterComputer");
            int bufferSize = texture.width * texture.height;
            computer.SetInt("width", texture.width);
            computer.SetInt("height", texture.height);
            computer.SetInt("sample_size", sampleSize);

            ComputeBuffer buffer = new ComputeBuffer(bufferSize, ColorData.GetByteSize());
            buffer.SetData(colorDataArray);

            computer.SetBuffer(kernelIndex, "colorDataBuffer", buffer);
            computer.Dispatch(kernelIndex, texture.width / 16, texture.height / 16, 1);

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

            EditorGUILayout.Space();
            if (GUILayout.Button("Execute")) 
            {
                ((ImageFilter)target).Execute();
            }

            EditorUtility.SetDirty(target);
        }
    }
    #endif
}
