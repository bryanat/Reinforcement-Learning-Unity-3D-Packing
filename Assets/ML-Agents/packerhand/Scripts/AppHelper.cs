 using UnityEngine;
 public static class AppHelper
 {
     public static string webplayerQuitURL = "http://google.com";

     public static void Quit()
     {
        if (Application.isEditor)
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
         else if (Application.platform == RuntimePlatform.WebGLPlayer)
         {
            Application.OpenURL(webplayerQuitURL);
         }
         else
         {
            Application.Quit();
         }
     }
 }