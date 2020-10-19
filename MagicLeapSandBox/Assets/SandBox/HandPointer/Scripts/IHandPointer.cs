using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.MagicLeap;

namespace SandBox.Scripts.HandPointer
{
    public interface IHandPointer
    {
        float PointerRayDistance { get; set; }
        bool IsShow { get; }


        MLHandTracking.HandKeyPose SelectKeyPose { get; set; }
        MLHandTracking.HandKeyPose RayDrawKeyPose { get; set; }
        void RegisterOnSelectHandler(UnityAction<(Vector3, GameObject)> handler);
        void RegisterOnSelectContinueHandler(UnityAction<(Vector3, GameObject)> handler);
        void Show();
        void Hide();
    }
}