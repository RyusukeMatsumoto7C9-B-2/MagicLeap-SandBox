using System;
using System.Collections.Generic;
using MagicLeapTools;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.WSA;
using UnityEngine.XR.MagicLeap;

namespace SandBox.Scripts.HandPointer
{
    /// <summary>
    /// ハンドトラッキングでのポインター.
    /// こいつだけで両手分の処理を行いたい.
    /// </summary>
    public class HandPointer : MonoBehaviour, IHandPointer
    {

        #region --- class OnSelectEvent ---
        class OnSelectEvent : UnityEvent<(Vector3, GameObject)> { }
        #endregion --- class OnSelectEvent ---

        
        #region --- class PointerPosition ---
        class PointerPosition
        {
            public Vector3 LeftStart { get; private set; } = Vector3.zero;
            public Vector3 RightStart { get; private set; } = Vector3.zero;
            public Vector3 Target { get; private set; } = Vector3.zero;


            public void SetTarget(Vector3 target) => Target = target;
        
            
            public void CopyStartPosition(
                PointerPosition src)
            {
                LeftStart = src.LeftStart;
                RightStart = src.RightStart;
            }

            
            public void SetStartPosition(
                Vector3 left,
                Vector3 right)
            {
                LeftStart = left;
                RightStart = right;
            }
        }
        #endregion --- class PointerPosition ---

        
        // Pointerのステート.
        public enum HandPointerState
        {
            None,
            NoSelected,
            Selected,
        }


        [SerializeField] Transform mainCamera;
        [SerializeField] float speed = 1f;
        [SerializeField] GameObject cursorPrefab; // ポインターの先端に配置するカーソルのプレハブ,設定されていなければ利用しない.

        public float PointerRayDistance { get; set; } = 2f;
        public MLHandTracking.HandKeyPose SelectKeyPose { get; set; } = MLHandTracking.HandKeyPose.Pinch;
        public MLHandTracking.HandKeyPose RayDrawKeyPose { get; set; } = MLHandTracking.HandKeyPose.OpenHand;
        public HandPointerState LeftHandState { get; private set; } = HandPointerState.None;
        public HandPointerState RightHandState { get; private set; } = HandPointerState.None;

        OnSelectEvent onSelect = new OnSelectEvent();
        OnSelectEvent onSelectContinue = new OnSelectEvent();
        PointerPosition lastPointerPosition;
        PointerPosition currentPointerPosition;
        HandPointerCursor leftCursor;
        HandPointerCursor rightCursor;

        /// <summary>
        /// Eyeトラッキングが有効か否か.
        /// </summary>
        bool IsEyeTrackingValid => MLEyes.IsStarted && MLEyes.CalibrationStatus == MLEyes.Calibration.Good;

        /// <summary>
        /// 描画しているか否か.
        /// </summary>
        public bool IsShow { get; private set; } = true;


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

            leftCursor = new HandPointerCursor(CreateLineRenderer("LeftLineRenderer"), CreateCursor("LeftHandCursor"));
            rightCursor = new HandPointerCursor(CreateLineRenderer("RightLineRenderer"), CreateCursor("RightHandCursor"));
            
            currentPointerPosition = new PointerPosition();
            lastPointerPosition = new PointerPosition();
        }

        
        void Update()
        {
            DrawRay();
            
            if (LeftHandState == HandPointerState.Selected)
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


        /// <summary>
        /// HandPointerのカーソル生成.
        /// </summary>
        GameObject CreateCursor(
            string name)
        {
            if (cursorPrefab == null) return null;
            GameObject cursor = Instantiate(cursorPrefab, transform);
            cursor.name = name;
            return cursor;
        }


        private void DrawRay()
        {
            if (!HandInput.Ready || !IsShow)
            {
                LeftHandState = RightHandState = HandPointerState.None;
                return;
            }
            LeftHandState = HandInput.Left.Visible ? LeftHandState: HandPointerState.None;
            RightHandState = HandInput.Right.Visible ? RightHandState: HandPointerState.None;
            
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
                    LeftHandState = RightHandState = HandPointerState.None;
                }
            }
            lastPointerPosition.SetTarget(currentPointerPosition.Target);
            currentPointerPosition.SetTarget(Vector3.Lerp(lastPointerPosition.Target, tempTargetPosition, Time.deltaTime * speed));

            // Rayのスタート位置計算.
            lastPointerPosition.CopyStartPosition(currentPointerPosition);
            currentPointerPosition.SetStartPosition(
                Vector3.Lerp(lastPointerPosition.LeftStart, GetRayStartPosition(HandInput.Left), 0.5f),
                Vector3.Lerp(lastPointerPosition.RightStart, GetRayStartPosition(HandInput.Right), 0.5f));

            // カーソルの更新.
            leftCursor.Update(LeftHandState, currentPointerPosition.LeftStart, currentPointerPosition.Target);
            rightCursor.Update(RightHandState, currentPointerPosition.RightStart, currentPointerPosition.Target);
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
            return Physics.Raycast(ray, out hit, maxDistance) ? (hit.point, hit.collider.gameObject) : (Vector3.zero, null);
        }

        
        /// <summary>
        /// 選択したターゲットを取得する,洗濯できていない場合は Item2 はnullになる.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        (Vector3, GameObject) GetSelect(
            MLHandTracking.HandType type)
        {
            Vector3 startPosition = (type == MLHandTracking.HandType.Left) ? currentPointerPosition.LeftStart : currentPointerPosition.RightStart;
            return GetRayCastHitTarget(new Ray(startPosition, currentPointerPosition.Target - startPosition), PointerRayDistance);
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
                    LeftHandState = pose == SelectKeyPose ? HandPointerState.Selected : HandPointerState.NoSelected;
                    break;
                
                case MLHandTracking.HandType.Right:
                    RightHandState = pose == SelectKeyPose ? HandPointerState.Selected : HandPointerState.NoSelected;
                    break;
            }

            if (LeftHandState == HandPointerState.Selected)
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


        /// <summary>
        /// 選択のイベントハンドラを登録.
        /// </summary>
        /// <param name="callback"></param>
        public void RegisterOnSelectHandler(
            UnityAction<(Vector3, GameObject)> callback)
        {
            if (onSelect == null)
                onSelect = new OnSelectEvent();
            onSelect.AddListener(callback);
        }
        
        
        /// <summary>
        /// 長選択のイベントハンドラを登録.
        /// </summary>
        /// <param name="callback"></param>
        public void RegisterOnSelectContinueHandler(
            UnityAction<(Vector3, GameObject)> callback)
        {
            if (onSelectContinue == null)
                onSelectContinue = new OnSelectEvent();
            onSelectContinue.AddListener(callback);
        }


        /// <summary>
        /// HandPointerを有効化.
        /// </summary>
        public void Show() => IsShow = true;


        /// <summary>
        /// HandPointerを無効化.
        /// </summary>
        public void Hide() => IsShow = false;
    }
}