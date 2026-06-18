using UnityEngine;

namespace StarterAssets
{
    public class SurfaceOverrideZone : MonoBehaviour
    {
        [SerializeField] private PlayerSurfaceType surfaceType = PlayerSurfaceType.Concrete;

        public PlayerSurfaceType SurfaceType => surfaceType;
    }
}
