 using UnityEngine;
 using System;
 public static class AppHelper
 {
    public static string webplayerQuitURL = "http://google.com";
    public static DateTime exporting_end_time = DateTime.MinValue;
    public static TimeSpan exporting_remaining_time;
    public static DateTime training_end_time=DateTime.MinValue;
     public static TimeSpan training_remaining_time;
     public static float training_time;
    public static float threshold_volume=75f;
    public static string early_stopping;
    public static string file_path;

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

     public static bool StartTimer(string flag)
     {
        // for stopping environment exporting fbx
        if (flag == "exporting")
        {
            // shut down environment 3 minutes from now
            if (exporting_end_time==DateTime.MinValue)
            {
                exporting_end_time = DateTime.Now.AddSeconds(30);
            }
            exporting_remaining_time = exporting_end_time - DateTime.Now;
            if  (exporting_remaining_time.TotalSeconds<=0)
            {
                return true;
            }
            else 
            {
                return false;
            }
        }
        // for training with a time limit
        else
        {   // Set the end time to 3 minutes from now
            if (training_end_time==DateTime.MinValue)
            {
                training_end_time = DateTime.Now.AddMinutes(training_time);
            }
            training_remaining_time = training_end_time - DateTime.Now;
            if  (training_remaining_time.TotalSeconds<=0)
            {
                return true;
            }
            else 
            {
                return false;
            }
        }

     }

     

     public static void LogStatus()
     {
        string lines = $"FBX exported on {DateTime.Now.ToString("HH:mm:ss tt")  + Environment.NewLine}";
        string path = System.IO.Path.Combine(Application.dataPath, "Logs/fbxexport_log.txt");
        if (!System.IO.File.Exists(path))
        {

            // Write the string to a file.
            System.IO.StreamWriter file = new System.IO.StreamWriter(path);
            file.WriteLine(lines);
            file.Close();
        }
        else 
        {
            using (System.IO.StreamWriter w = System.IO.File.AppendText(path))
            {
                w.WriteLine(lines);

            }
        }
     }
 }