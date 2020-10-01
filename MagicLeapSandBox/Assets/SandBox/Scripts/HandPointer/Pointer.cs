using System;
using MagicLeap.Core;
using MagicLeapTools;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace SandBox.Scripts.HandPointer
{
    public class Pointer : MonoBehaviour
    {
        [SerializeField] Transform source;
        [SerializeField] LineRenderer lr;

        Vector3 lastStartPos = Vector3.zero;

        //Vector3 lastGazeRay = Vector3.zero;
        
        void Start()
        {
            MLEyes.Start();
        }

        
        void Update()
        {
            if (!HandInput.Ready && !MLEyes.IsStarted)
            {
                return;
            }

            //EyeTracking.
            /*
            Vector3 gazeRay = Vector3.Lerp(MLEyes.RightEye.ForwardGaze, MLEyes.LeftEye.ForwardGaze, 0.5f).normalized;
            Vector3 currentRay = Vector3.Lerp(lastGazeRay, gazeRay, Time.deltaTime);
            lastGazeRay = gazeRay;
            */

            var right = HandInput.Right;
            // 人差し指の根元と親指の根元の中間座標を起点として
            Vector3 tempStartPos = Vector3.Lerp(right.Skeleton.Thumb.Knuckle.positionFiltered, right.Skeleton.Index.Knuckle.positionFiltered, 0.5f);

            Vector3 startPos = Vector3.Lerp(lastStartPos, tempStartPos, 0.5f);
            lastStartPos = tempStartPos;

            Vector3 targetPos = startPos + (source.forward.normalized * 2f);
            //Vector3 targetPos = startPos + (currentRay * 2f);
            Vector3 a = (startPos - MLEyes.FixationPoint).normalized;
            targetPos = targetPos + (a * Vector3.Distance(startPos, source.position));
            
            // 方向はWristCenterから見たStart?
            lr.SetPositions(new []{startPos, targetPos});
        }

    }
}