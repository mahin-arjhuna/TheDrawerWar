using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;

public partial struct SubSceneSystem : ISystem
{
    private EntityQuery newRequests;
    private NativeArray<SceneLoader> requests;
    private int subsceneIndex;

    public void OnCreate(ref SystemState state)
    {
        subsceneIndex = 0;
        newRequests = state.GetEntityQuery(typeof(SceneLoader));
        requests = newRequests.ToComponentDataArray<SceneLoader>(Allocator.Temp);
        SceneSystem.LoadSceneAsync(state.World.Unmanaged, requests[subsceneIndex].SceneReference);
    }
    public void OnUpdate(ref SystemState state)
    {

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Switching subscenes");
            subsceneIndex = (subsceneIndex + 1) % requests.Count();
            SceneSystem.LoadSceneAsync(state.World.Unmanaged, requests[subsceneIndex].SceneReference);
        }
    }

    public void OnDestroy(ref SystemState state)
    {
        requests.Dispose();
        state.EntityManager.DestroyEntity(newRequests);
    }
}
