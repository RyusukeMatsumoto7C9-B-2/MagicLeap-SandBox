using System;
using MagicLeap.Core;
using MagicLeapTools;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace SandBox.Scripts.HandPointer
{
    /// <summary>
    /// ハンドトラッキングでのポインター.
    /// こいつだけで両手分の処理を行いたい.
    /// </summary>
    public class Pointer : MonoBehaviour
    {
        readonly float PointerRayDistance = 2f;


        // Pointerのステート.
        public enum HandPointerState
        {
            None,
            NoSelected,
            Selected,
        }


        [SerializeField] Transform mainCamera;
        [SerializeField] LineRenderer lr;
        [SerializeField] float speed = 1f;

        public HandPointerState State { get; private set; } = HandPointerState.None;

        Vector3 lastStartPos = Vector3.zero;
        Vector3 lastTargetPos = Vector3.zero;

        /// <summary>
        /// Eyeトラッキングが有効か否か.
        /// </summary>
        bool IsEyeTrackingValid => MLEyes.IsStarted && MLEyes.CalibrationStatus == MLEyes.Calibration.Good;
        

        // TODO : 後で使う,Eyeトラッキングで瞬きしたときに荒ぶるから.
        //Vector3 lastGazeRay = Vector3.zero;
        
        void Start()
        {
            MLEyes.Start();
            
        }

        
        void Update()
        {
            if (!HandInput.Ready)
            {
                lr.enabled = false;
                return;
            }
            lr.enabled = true;
            
            var right = HandInput.Right;
            // 人差し指の根元と親指の根元の中間座標を起点として
            Vector3 tempStartPos = Vector3.Lerp(right.Skeleton.Thumb.Knuckle.positionFiltered, right.Skeleton.Index.Knuckle.positionFiltered, 0.5f);
            lastStartPos = tempStartPos;
            
            Vector3 targetPos = Vector3.zero;
            (bool isValid, Vector3 pos) eyeTrackingTarget = GetEytTrackingTargetPos();
            if (eyeTrackingTarget.isValid)
            {
                State = HandPointerState.NoSelected;
                targetPos = eyeTrackingTarget.pos;
            }
            else
            {
                (bool isValid, Vector3 pos) result = GetHeadTrackingTargetPos();
                if (result.isValid)
                {
                    State = HandPointerState.NoSelected;
                    targetPos = result.pos;
                }
                else
                {
                    State = HandPointerState.None;
                }
            }
            targetPos = Vector3.Lerp(lastTargetPos, targetPos, Time.deltaTime * speed);
            lastTargetPos = targetPos;

            // Rayのスタート位置.
            Vector3 startPos = Vector3.Lerp(lastStartPos, tempStartPos, 0.5f);
            
            // Rayの描画、まだRaycastとかはやってない.
            lr.SetPositions(new []{startPos, targetPos});
        }


        /// <summary>
        /// Eyeトラッキングのターゲットを取得.
        /// </summary>
        /// <returns></returns>
        (bool, Vector3) GetEytTrackingTargetPos()
        {
            if (!IsEyeTrackingValid) return (false, Vector3.zero);
            
            bool isBlink = MLEyes.LeftEye.IsBlinking || MLEyes.RightEye.IsBlinking;
            if (isBlink) return (false, Vector3.zero);

            // Eyeトラッキングが有効ならEyeトラッキングの向きで補正する.
            float leftConfidence = MLEyes.LeftEye.CenterConfidence * -0.5f;
            float rightConfidence = MLEyes.RightEye.CenterConfidence * 0.5f;
            float eyeRatio = 0.5f + (leftConfidence + rightConfidence);
            Vector3 gazeRay =  Vector3.Lerp(MLEyes.LeftEye.ForwardGaze, MLEyes.RightEye.ForwardGaze, eyeRatio).normalized;
            Vector3 eyeTargetPos = mainCamera.position + (gazeRay * PointerRayDistance);

            return (true, eyeTargetPos);
        }

        
        /// <summary>
        /// Headトラッキングのターゲットを取得.
        /// </summary>
        /// <returns></returns>
        (bool, Vector3) GetHeadTrackingTargetPos()
        {
            if (mainCamera == null) return (false, Vector3.zero);
            
            Vector3 targetPos = Vector3.zero;
            targetPos = mainCamera.position + (mainCamera.forward.normalized * 2f);

            return (true, targetPos);
        }

    }
}