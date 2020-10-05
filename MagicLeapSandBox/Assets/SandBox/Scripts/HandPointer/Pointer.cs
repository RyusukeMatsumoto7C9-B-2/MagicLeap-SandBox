using System;
using MagicLeap.Core;
using MagicLeapTools;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace SandBox.Scripts.HandPointer
{
    public class Pointer : MonoBehaviour
    {
        readonly float PointerRayDistance = 2f;
        
        [SerializeField] Transform mainCamera;
        [SerializeField] LineRenderer lr;
        [SerializeField] float speed = 1f;

        Vector3 lastStartPos = Vector3.zero;
        Vector3 lastTargetPos = Vector3.zero;

        bool IsEyeTrackingValid
        {
            get { return false; }
        }


        // TODO : 後で使う,Eyeトラッキングで瞬きしたときに荒ぶるから.
        //Vector3 lastGazeRay = Vector3.zero;
        
        void Start()
        {
            MLEyes.Start();
            
        }

        
        void Update()
        {
            if (!HandInput.Ready && !MLCamera.IsStarted)
            {
                return;
            }
            
            var right = HandInput.Right;
            // 人差し指の根元と親指の根元の中間座標を起点として
            Vector3 tempStartPos = Vector3.Lerp(right.Skeleton.Thumb.Knuckle.positionFiltered, right.Skeleton.Index.Knuckle.positionFiltered, 0.5f);
            Vector3 startPos = Vector3.Lerp(lastStartPos, tempStartPos, 0.5f);
            lastStartPos = tempStartPos;
            
            Vector3 targetPos = Vector3.zero;
            bool isBlink = MLEyes.LeftEye.IsBlinking || MLEyes.RightEye.IsBlinking;
            if (MLEyes.IsStarted && MLEyes.CalibrationStatus == MLEyes.Calibration.Good && !isBlink)
            {
                // Eyeトラッキングが有効ならEyeトラッキングの向きで補正する.
                float leftConfidence = MLEyes.LeftEye.CenterConfidence * -0.5f;
                float rightConfidence = MLEyes.RightEye.CenterConfidence * 0.5f;
                float eyeRatio = 0.5f + (leftConfidence + rightConfidence);
                Vector3 gazeRay =  Vector3.Lerp(MLEyes.LeftEye.ForwardGaze, MLEyes.RightEye.ForwardGaze, eyeRatio).normalized;
                Vector3 eyeTargetPos = mainCamera.position + (gazeRay * PointerRayDistance);
                targetPos = eyeTargetPos;
            }
            else
            {
                // Eyeトラッキングが無効ないしあまりよくなければMainCameraの向きで補正する.
                targetPos = startPos + (mainCamera.forward.normalized * 2f);
                Vector3 dir1 = (startPos - mainCamera.position).normalized;
                targetPos = targetPos + (dir1 * Vector3.Distance(startPos, mainCamera.position));
            }
            targetPos = Vector3.Lerp(lastTargetPos, targetPos, Time.deltaTime * speed);
            lastTargetPos = targetPos;
            lr.SetPositions(new []{startPos, targetPos});
        }

    }
}