using System;
using UnityEngine.XR.MagicLeap;
using MagicLeapTools;
using UnityEngine;

namespace SandBox.Scripts.HandPointer
{
    public class HandPointerCursor : MonoBehaviour
    {
#if PLATFORM_LUMIN
        public HandPointer pointer;


        void Start()
        {
            HandInput.OnReady += () =>
            {
                HandInput.Right.Gesture.OnKeyPoseChanged += OnHandGesturePoseChanged;                
                
                pointer.RegisterSelectProc(() => { return isSelect; });
            };


        }


        private void LateUpdate()
        {
            if (pointer == null)
            {
                return;
            }
        }



        bool isSelect = false;
        private void OnHandGesturePoseChanged(
            ManagedHand hand,
            MLHandTracking.HandKeyPose pose)
        {
            // 左右の判定はこんな感じ.
            string lr = hand.Hand.Type == MLHandTracking.HandType.Left ? "left" : "right";
            Debug.Log($"{pose} {lr}");

            // 取得されたジェスチャ.
            switch (pose)
            {
                case MLHandTracking.HandKeyPose.C:
                    break;
                
                case MLHandTracking.HandKeyPose.Finger:
                    break;
                
                case MLHandTracking.HandKeyPose.Fist:
                    break;
                
                case MLHandTracking.HandKeyPose.L:
                    break;
                
                case MLHandTracking.HandKeyPose.Ok:
                    isSelect = true;
                    break;
                
                case MLHandTracking.HandKeyPose.Pinch:
                    break;
                
                case MLHandTracking.HandKeyPose.Thumb:
                    break;
                
                case MLHandTracking.HandKeyPose.NoHand:
                    break;
                
                case MLHandTracking.HandKeyPose.NoPose:
                    isSelect = false; 
                    break;
                
                case MLHandTracking.HandKeyPose.OpenHand:
                    break;
            }
        }
        
#endif
    }
}

