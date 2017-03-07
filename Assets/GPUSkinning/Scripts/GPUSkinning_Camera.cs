using UnityEngine;
using System.Collections;

public class GPUSkinning_Camera : MonoBehaviour
{
    public static GPUSkinning_Camera instance = null;

    public event System.Action onPostRender;

    private void Awake()
    {
        instance = this;
    }

    private void OnDestroy()
    {
        instance = null;
    }

    private void OnPostRender()
    {
        if(onPostRender != null)
        {
            onPostRender();
        }
    }
}
