 using UnityEditor;
 using UnityEngine;
 using UnityEditor.SceneManagement;
 using UnityEngine.SceneManagement;
 using System.IO;
 
 class EditorServer: MonoBehaviour
 {
    [MenuItem("Examples/Execute menu items")]
     static void Play()
     {
         var args = System.Environment.GetCommandLineArgs();
        //Debug.Log("Command line arguments passed: " + String.Join(" ", args));
        for (int i = 0; i < args.Length; i++)
        {
            //Debug.Log($"CXX args: {args[i]}");
            if (args[i] == "inference")
            {
                string currentDirectory = Application.dataPath;
                string basePath = Path.GetDirectoryName(currentDirectory);
                string relativePath = Path.Combine("Assets", "ML-Agents", "packerhand", "Scenes", "BoxPackingInference.unity");
                string fullPath = Path.Combine(basePath, relativePath);
                EditorSceneManager.OpenScene(fullPath);                
            }
            if (args[i] == "training")
            {
                string currentDirectory = Application.dataPath;
                string basePath = Path.GetDirectoryName(currentDirectory);
                string relativePath = Path.Combine("Assets", "ML-Agents", "packerhand", "Scenes", "BoxPackingMultiPlatformX.unity");
                string fullPath = Path.Combine(basePath, relativePath);
                EditorSceneManager.OpenScene(fullPath);
            }
        }
        UnityEditor.EditorApplication.ExecuteMenuItem("Edit/Play");
        // EditorApplication.Exit(1); 

     }
 }