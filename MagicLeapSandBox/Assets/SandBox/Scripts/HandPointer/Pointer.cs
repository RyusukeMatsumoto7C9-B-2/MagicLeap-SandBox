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
        [SerializeField] LineRenderer lLineRenderer;
        [SerializeField] LineRenderer rLineRenderer;
        [SerializeField] float speed = 1f;

        public HandPointerState State { get; private set; } = HandPointerState.None;

        PointerStartPosition lastStartPos;
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
            lLineRenderer = CreateLineRenderer("LeftLineRenderer");
            rLineRenderer = CreateLineRenderer("RightLineRenderer");
            
            lastStartPos = new PointerStartPosition() {left = Vector3.zero, right = Vector3.zero};
        }

        
        void Update()
        {
            if (!HandInput.Ready)
            {
                lLineRenderer.enabled = false;
                rLineRenderer.enabled = false;
                return;
            }
            lLineRenderer.enabled = HandInput.Left.Visible;
            rLineRenderer.enabled = HandInput.Right.Visible;

            
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

            // Rayのスタート位置計算.
            PointerStartPosition startPosition = new PointerStartPosition()
            {
                left = Vector3.Lerp(lastStartPos.left, GetRayStartPosition(HandInput.Left), 0.5f),
                right = Vector3.Lerp(lastStartPos.right, GetRayStartPosition(HandInput.Right), 0.5f)
            };
            lastStartPos = startPosition;
            
            // Rayの描画、まだRaycastとかはやってない.
            lLineRenderer.SetPositions(new []{startPosition.left, targetPos});
            rLineRenderer.SetPositions(new []{startPosition.right, targetPos});
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

    }
}