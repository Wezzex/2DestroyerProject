using UnityEngine;
using System.Collections;

public class csDestroyEffect : MonoBehaviour {


    private void OnDisable()
    {
        Destroy(gameObject); 
    }
}
