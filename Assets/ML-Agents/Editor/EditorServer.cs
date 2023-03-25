 using UnityEditor;
 using UnityEngine;
 using UnityEditor.SceneManagement;
 using UnityEngine.SceneManagement;
 
 class EditorServer: MonoBehaviour
 {
    [MenuItem("Examples/Execute menu items")]
     static void Play()
     {
        UnityEditor.EditorApplication.ExecuteMenuItem("Edit/Play");
        // EditorSceneManager.LoadSceneInPlayMode("/home/yueqi/DRL/UnityBox5/DRL-RNN-LSTM-BOX-SIM/Assets/ML-Agents/packerhand/Scenes/BoxPackingMultiPlatformY.Unity",  new LoadSceneParameters(LoadSceneMode.Single));
        // EditorApplication.Exit(1); 

     }
 }