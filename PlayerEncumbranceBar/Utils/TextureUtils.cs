using System.IO;
using UnityEngine;

namespace PlayerEncumbranceBar.Utils
{
    public static class TextureUtils
    {
        public static Texture2D LoadTexture2DFromPath(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(path));

            return tex;
        }
    }
}
