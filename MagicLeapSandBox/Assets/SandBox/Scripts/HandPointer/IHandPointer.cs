using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.MagicLeap;

namespace SandBox.Scripts.HandPointer
{
    public interface IHandPointer
    {
        float PointerRayDistance { get; set; }
        MLHandTracking.HandKeyPose SelectKeyPose { get; set; }
        MLHandTracking.HandKeyPose RayDrawKeyPose { get; set; }
        void RegisterOnSelectHandler(UnityAction<(Vector3, GameObject)> handler);
    }
}