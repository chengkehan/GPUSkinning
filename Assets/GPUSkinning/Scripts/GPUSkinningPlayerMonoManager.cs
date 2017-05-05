using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUSkinningPlayerMonoManager
{
    private List<GPUSkinningPlayerResources> items = new List<GPUSkinningPlayerResources>();

    public void Register(GPUSkinningAnimation anim, Mesh mesh, Material originalMtrl, TextAsset textureRawData, GPUSkinningPlayerMono player, out GPUSkinningPlayerResources resources)
    {
        resources = null;

        if (anim == null || originalMtrl == null || textureRawData == null || player == null)
        {
            return;
        }

        GPUSkinningPlayerResources item = null;

        int numItems = items.Count;
        for(int i = 0; i < numItems; ++i)
        {
            if(items[i].anim.guid == anim.guid)
            {
                item = items[i];
                break;
            }
        }

        if(item == null)
        {
            item = new GPUSkinningPlayerResources();
            items.Add(item);
        }

        if(item.anim == null)
        {
            item.anim = anim;
        }

        if(item.mesh == null)
        {
            item.mesh = mesh;
        }

        item.InitMaterial(originalMtrl, HideFlags.None);

        if(item.texture == null)
        {
            item.texture = GPUSkinningUtil.CreateTexture2D(textureRawData, anim);
        }

        if (!item.players.Contains(player))
        {
            item.players.Add(player);
            item.AddCullingBounds();
        }

        resources = item;
    }

    public void Unregister(GPUSkinningPlayerMono player)
    {
        if(player == null)
        {
            return;
        }

        int numItems = items.Count;
        for(int i = 0; i < numItems; ++i)
        {
            int playerIndex = items[i].players.IndexOf(player);
            if(playerIndex != -1)
            {
                items[i].players.RemoveAt(playerIndex);
                items[i].RemoveCullingBounds(playerIndex);
                if(items[i].players.Count == 0)
                {
                    items[i].Destroy();
                    items.RemoveAt(i);
                }
                break;
            }
        }
    }
}
