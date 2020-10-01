using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace EyeTracking
{
    public class EyeTrackingSample : MonoBehaviour
    {
        readonly float TargetDistance = 2f;

        [SerializeField] Transform leftEyeTarget;
        [SerializeField] Transform rightEyeTarget;
        
        
        void Start()
        {
            MLEyes.Start();
        }

        
        void Update()
        {
            if (!MLEyes.IsStarted) return;

            leftEyeTarget.position = MLEyes.LeftEye.Center + (MLEyes.LeftEye.ForwardGaze.normalized * TargetDistance);
            rightEyeTarget.position = MLEyes.RightEye.Center + (MLEyes.RightEye.ForwardGaze.normalized * TargetDistance);
        }
    }
}

