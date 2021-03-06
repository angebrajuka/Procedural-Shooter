using UnityEngine;

public class PlayerEyes : MonoBehaviour
{
    const int defaultLayerMask = (Layers.PLAYER_ALL | Layers.GRAPPLE_HOOK);

    public static bool Raycast(out RaycastHit hit, out Vector3 direction, float range=Mathf.Infinity, Vector3 positionOffset=default(Vector3), Vector3 directionOffset=default(Vector3), int layermask=defaultLayerMask)
    {
        if(positionOffset == default(Vector3))
        {
            positionOffset = Vector3.zero;
        }
        else
        {
            positionOffset = PlayerMovement.instance.t_camera.TransformPoint(positionOffset)-PlayerMovement.instance.t_camera.position;
        }

        if(directionOffset == default(Vector3))
        {
            directionOffset = Vector3.zero;
        }
        direction = PlayerMovement.instance.t_camera.TransformDirection((Vector3.forward+directionOffset).normalized);
        return Physics.Raycast(PlayerMovement.instance.t_camera.position+positionOffset, direction, out hit, range, layermask);
    }
}