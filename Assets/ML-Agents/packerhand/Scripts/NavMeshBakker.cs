using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Example : MonoBehaviour
{
    // public NavMeshBuildSettings navMeshBuildSettings;

    public List<NavMeshBuildSource> sources;
    public List<NavMeshBuildMarkup> markups;
    

    void Start()
    {
        // sources stored in a results list (parameter 6)
        sources = new List<NavMeshBuildSource>();
        // markups on sources, which allows finer control over how objects are collected (parameter 5)
        markups = new List<NavMeshBuildMarkup>();
        
        // Area type to assign to sources (parameter 4)
        int areaIndex = NavMesh.GetAreaFromName("Walkable");

        // geometry formed by collecting meshes (parameter 3)
        NavMeshCollectGeometry testGeometry = NavMeshCollectGeometry.RenderMeshes;

        NavMeshBuilder.CollectSources(new Bounds(Vector3.zero, Vector3.one * 1000f), 0, testGeometry, areaIndex, markups, sources);
    }


    void Update()
    {
        NavMeshData navMeshData = new NavMeshData();
        NavMeshBuilder.UpdateNavMeshData(navMeshData, NavMesh.GetSettingsByID(0), sources, new Bounds(Vector3.zero, Vector3.one * 100f));
        NavMesh.AddNavMeshData(navMeshData);

        // changing NavMesh based on NavMesgAgent goes in update here
            // define dimensions (w,l,h) of NavMeshAgent class
            // bake new NavMesh using NavMeshAgent
    }
}
