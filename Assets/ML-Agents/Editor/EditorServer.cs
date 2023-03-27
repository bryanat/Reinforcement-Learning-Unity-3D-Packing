 using UnityEditor;
 using UnityEngine;
 using UnityEditor.SceneManagement;
 using UnityEngine.SceneManagement;
 
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
                EditorSceneManager.OpenScene("/home/yueqi/DRL/UnityBox5/DRL-RNN-LSTM-BOX-SIM/Assets/ML-Agents/packerhand/Scenes/BoxPackingInference.unity");
                
            }
            if (args[i] == "training")
            {
                EditorSceneManager.OpenScene("/home/yueqi/DRL/UnityBox5/DRL-RNN-LSTM-BOX-SIM/Assets/ML-Agents/packerhand/Scenes/BoxPackingMultiPlatformX.unity");
            }
        }
        UnityEditor.EditorApplication.ExecuteMenuItem("Edit/Play");
        // EditorApplication.Exit(1); 

     }
 }