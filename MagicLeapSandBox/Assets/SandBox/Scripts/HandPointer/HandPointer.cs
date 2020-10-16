using System;
using System.Collections.Generic;
using MagicLeapTools;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.MagicLeap;

namespace SandBox.Scripts.HandPointer
{
    /// <summary>
    /// ハンドトラッキングでのポインター.
    /// こいつだけで両手分の処理を行いたい.
    /// </summary>
    public class HandPointer : MonoBehaviour, IHandPointer
    {
        public class OnSelectEvent : UnityEvent<(Vector3, GameObject)> { }

        private struct PointerStartPosition
        {
            public Vector3 left;
            public Vector3 right;
        }


        // Pointerのステート.
        public enum HandPointerState
        {
            None,
            NoSelected,
            Selected,
        }


        [SerializeField] Transform mainCamera;
        [SerializeField] float speed = 1f;

        public HandPointerState LefttHandSate { get; private set; } = HandPointerState.None;
        public HandPointerState RightHandState { get; private set; } = HandPointerState.None;
        public float PointerRayDistance { get; set; } = 2f;
        public MLHandTracking.HandKeyPose SelectKeyPose { get; set; } = MLHandTracking.HandKeyPose.Pinch;
        public MLHandTracking.HandKeyPose RayDrawKeyPose { get; set; } = MLHandTracking.HandKeyPose.OpenHand;
        OnSelectEvent onSelect = new OnSelectEvent();
        OnSelectEvent onSelectContinue = new OnSelectEvent();

        LineRenderer lLineRenderer;
        LineRenderer rLineRenderer;
        Vector3 lastTargetPosition = Vector3.zero;
        Vector3 currentTargetPosition = Vector3.zero;
        PointerStartPosition lastStartPosition;
        PointerStartPosition currentStartPosition;

        
        /// <summary>
        /// Eyeトラッキングが有効か否か.
        /// </summary>
        bool IsEyeTrackingValid => MLEyes.IsStarted && MLEyes.CalibrationStatus == MLEyes.Calibration.Good;

        
        void Start()
        {
            if (HandInput.Ready)
            {
                HandInput.Left.Gesture.OnKeyPoseChanged += OnHandGesturePoseChange;
                HandInput.Right.Gesture.OnKeyPoseChanged += OnHandGesturePoseChange;
            }
            else
            {
                HandInput.OnReady += () =>
                {
                    HandInput.Left.Gesture.OnKeyPoseChanged += OnHandGesturePoseChange;
                    HandInput.Right.Gesture.OnKeyPoseChanged += OnHandGesturePoseChange;
                };
            }

            MLEyes.Start();
            lLineRenderer = CreateLineRenderer("LeftLineRenderer");
            rLineRenderer = CreateLineRenderer("RightLineRenderer");
            
            lastStartPosition = new PointerStartPosition() {left = Vector3.zero, right = Vector3.zero};
        }

        
        void Update()
        {
            DrawRay();
            
            if (LefttHandSate == HandPointerState.Selected)
            {
                var result = GetSelect(MLHandTracking.HandType.Left);
                if (result.Item2 != null)
                    onSelectContinue?.Invoke(result);
            }
            
            if (RightHandState == HandPointerState.Selected)
            {
                var result = GetSelect(MLHandTracking.HandType.Right);
                if (result.Item2 != null)
                    onSelectContinue?.Invoke(result);
            }
        }


        private void DrawRay()
        {
            if (!HandInput.Ready)
            {
                lLineRenderer.enabled = false;
                rLineRenderer.enabled = false;
                LefttHandSate = HandPointerState.None;
                RightHandState = HandPointerState.None;
                return;
            }
            lLineRenderer.enabled = HandInput.Left.Visible;
            rLineRenderer.enabled = HandInput.Right.Visible;
            
            Vector3 tempTargetPosition = Vector3.zero;
            (bool isValid, Vector3 pos) eyeTrackingTarget = GetEytTrackingTargetPos();
            if (eyeTrackingTarget.isValid)
            {
                tempTargetPosition = eyeTrackingTarget.pos;
            }
            else
            {
                (bool isValid, Vector3 pos) result = GetHeadTrackingTargetPos();
                if (result.isValid)
                {
                    tempTargetPosition = result.pos;
                }
                else
                {
                    LefttHandSate = HandPointerState.None;
                    RightHandState = HandPointerState.None;
                }
            }
            lastTargetPosition = currentTargetPosition;
            currentTargetPosition = Vector3.Lerp(lastTargetPosition, tempTargetPosition, Time.deltaTime * speed);

            // Rayのスタート位置計算.
            lastStartPosition = currentStartPosition;
            PointerStartPosition startPosition = new PointerStartPosition()
            {
                left = Vector3.Lerp(lastStartPosition.left, GetRayStartPosition(HandInput.Left), 0.5f),
                right = Vector3.Lerp(lastStartPosition.right, GetRayStartPosition(HandInput.Right), 0.5f)
            };
            currentStartPosition = startPosition;
            
            // Rayの描画、まだRaycastとかはやってない.
            lLineRenderer.SetPositions(new []{currentStartPosition.left, currentTargetPosition});
            rLineRenderer.SetPositions(new []{currentStartPosition.right, currentTargetPosition});
            
        }


        /// <summary>
        /// RaycastHitしたターゲットを返す、ヒットしない場合は Item2 はnullになる.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
        (Vector3, GameObject) GetRayCastHitTarget(
            Ray ray,
            float maxDistance)
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, maxDistance))
            {
                return (hit.point, hit.collider.gameObject);
            }
            return (Vector3.zero, null);
        }

        
        /// <summary>
        /// 選択したターゲットを取得する,洗濯できていない場合は Item2 はnullになる.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        (Vector3, GameObject) GetSelect(
            MLHandTracking.HandType type)
        {
            Vector3 startPosition = (type == MLHandTracking.HandType.Left) ? currentStartPosition.left : currentStartPosition.right;
            return GetRayCastHitTarget(new Ray(startPosition, currentTargetPosition - startPosition), PointerRayDistance);
        }


        /// <summary>
        /// ハンドジェスチャの変更イベント取得.
        /// </summary>
        /// <param name="hand"></param>
        /// <param name="pose"></param>
        void OnHandGesturePoseChange(
            ManagedHand hand,
            MLHandTracking.HandKeyPose pose)
        {
            switch (hand.Hand.Type)
            {
                case MLHandTracking.HandType.Left:
                    LefttHandSate = pose == SelectKeyPose ? HandPointerState.Selected : HandPointerState.NoSelected;
                    break;
                
                case MLHandTracking.HandType.Right:
                    RightHandState = pose == SelectKeyPose ? HandPointerState.Selected : HandPointerState.NoSelected;
                    break;
            }

            if (LefttHandSate == HandPointerState.Selected)
            {
                var result = GetSelect(MLHandTracking.HandType.Left);
                if (result.Item2 != null)
                    onSelect?.Invoke(result);
            }
            
            if (RightHandState == HandPointerState.Selected)
            {
                var result = GetSelect(MLHandTracking.HandType.Right);
                if (result.Item2 != null)
                    onSelect?.Invoke(result);
            }
        }


        /// <summary>
        /// LineRendererを作成.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        LineRenderer CreateLineRenderer(
            string name)
        {
            var ret = GameObject.Instantiate(new GameObject(name), transform).AddComponent<LineRenderer>();
            ret.startWidth = 0.01f;
            ret.endWidth = 0.01f;
            ret.enabled = false;
            return ret;
        }
        

        /// <summary>
        /// 人差し指の根元と親指の根元の中間座標を起点として.
        /// </summary>
        /// <param name="hand"></param>
        /// <returns></returns>
        Vector3 GetRayStartPosition(ManagedHand hand)
            => Vector3.Lerp(hand.Skeleton.Thumb.Knuckle.positionFiltered, hand.Skeleton.Index.Knuckle.positionFiltered, 0.5f);


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


        public void RegisterOnSelectHandler(
            UnityAction<(Vector3, GameObject)> callback)
        {
            if (onSelect == null)
                onSelect = new OnSelectEvent();
            onSelect.AddListener(callback);
        }
        
        
        public void RegisterOnSelectContinueHandler(
            UnityAction<(Vector3, GameObject)> callback)
        {
            if (onSelectContinue == null)
                onSelectContinue = new OnSelectEvent();
            onSelectContinue.AddListener(callback);
        }
    }
}