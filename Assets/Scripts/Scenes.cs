using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class Scenes : MonoBehaviour
{
    public void ChangedScenes(int numberScenes)
    {
        if (numberScenes == -1) 
            SceneManager.LoadScene(ScenesData.LastScene);
        else
            SceneManager.LoadScene(numberScenes);
    }
    public void ChangedLastScenes(int numberScenes) => ScenesData.LastScene = numberScenes;
}
