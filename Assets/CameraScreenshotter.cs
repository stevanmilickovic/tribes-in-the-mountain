using UnityEngine;
using System.IO;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class CameraScreenshotter : MonoBehaviour
{
    public int width = 3840;
    public int height = 2160;

    private void Start()
    {
        StartCoroutine(Capture());
    }

    private IEnumerator Capture()
    {
        yield return new WaitForEndOfFrame();

        Camera cam = GetComponent<Camera>();
        RenderTexture rt = new RenderTexture(width, height, 24);
        cam.targetTexture = rt;

        Texture2D img = new Texture2D(width, height, TextureFormat.RGB24, false);

        cam.Render();
        RenderTexture.active = rt;
        img.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        img.Apply();

        cam.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        string path = TryGetAssetsPath();
        string filename = Path.Combine(path, "MapCapture_4K.png");

        File.WriteAllBytes(filename, img.EncodeToPNG());
        Debug.Log("Saved screenshot to: " + filename);
    }

    private string TryGetAssetsPath()
    {
        try
        {
            string p = Application.dataPath + "/Screenshots";
            if (!Directory.Exists(p)) Directory.CreateDirectory(p);
            return p;
        }
        catch
        {
            string downloads = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "/Downloads";
            return downloads;
        }
    }
}
