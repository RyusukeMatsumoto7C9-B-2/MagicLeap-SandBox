using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace EyeTracking
{
    public class EyeTrackingSample : MonoBehaviour
    {
        readonly float TargetDistance = 2f;

        [SerializeField] Transform leftEyeTarget;
        [SerializeField] Transform rightEyeTarget;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip blinkSound;
        
        
        void Start()
        {
            MLEyes.Start();

            audioSource.pitch = 3f;
        }

        
        void Update()
        {
            if (!MLEyes.IsStarted) return;

            switch (MLEyes.CalibrationStatus)
            {
                case MLEyes.Calibration.Bad:
                    leftEyeTarget.gameObject.SetActive(false);
                    rightEyeTarget.gameObject.SetActive(false);
                    break;
                
                case MLEyes.Calibration.Good:
                    leftEyeTarget.gameObject.SetActive(true);
                    rightEyeTarget.gameObject.SetActive(true);
                    leftEyeTarget.position = MLEyes.LeftEye.Center + (MLEyes.LeftEye.ForwardGaze.normalized * TargetDistance);
                    rightEyeTarget.position = MLEyes.RightEye.Center + (MLEyes.RightEye.ForwardGaze.normalized * TargetDistance);

                    if ((MLEyes.LeftEye.IsBlinking || MLEyes.RightEye.IsBlinking) && !audioSource.isPlaying)
                        audioSource.PlayOneShot(blinkSound);
                    break; 
                
                case MLEyes.Calibration.None:
                    leftEyeTarget.gameObject.SetActive(false);
                    rightEyeTarget.gameObject.SetActive(false);
                    break;
            }

        }
    }
}

