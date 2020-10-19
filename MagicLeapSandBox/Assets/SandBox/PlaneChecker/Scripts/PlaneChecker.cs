using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if PLATFORM_LUMIN
using UnityEngine.XR.MagicLeap;
#endif


namespace PlaneCheck
{
    /// <summary>
    /// MagicLeapToolsのFloorOnPlaceを改造したクラス.
    /// 任意の平面を判定する.
    /// </summary>
    public class PlaneChecker : MonoBehaviour
    {
        readonly float HeadLocationIdleThreshold = 0.003f;
        readonly float HeadRotationIdleThreshold = .3f;
        readonly int HistoryCount = 5;
        readonly float HeadIdleRequiredDuration = .2f;
        
        
        public Vector3 Location
        {
            get;
            private set;
        }
       
        List<Vector3> headLocationHistory;
        List<Quaternion> headRotationHistory;
        float headLocationVelocity;
        float headRotationVelocity;
        Transform mainCamera;
        bool headLocationIdle;
        bool headRotationIdle;
        bool headTemporarilyIdle;
        bool headIdle;
        bool placementValid;
 
        
        void Awake()
        {
            mainCamera = Camera.main.transform;
            
            if (FindObjectOfType<MLSpatialMapper>() == null)
            {
                Debug.LogError("PlaceOnFloor requires and instance of the MLSpatialMapper in your scene.");
            }
        }
        

        void OnEnable()
        {
            headLocationHistory = new List<Vector3>();
            headRotationHistory = new List<Quaternion>();
        }

        
        void Update()
        {
            if (Time.frameCount < 3)
            {
                return;
            }

            HeadActivityDetermination(); 
        }
        

        IEnumerator HeadIdleTimeout()
        {
            yield return new WaitForSeconds(HeadIdleRequiredDuration);
            headIdle = true;
        }

        
        void HeadActivityDetermination()
        {
            //history:
            headLocationHistory.Add(mainCamera.position);
            if (HistoryCount < headLocationHistory.Count)
                headLocationHistory.RemoveAt(0);

            headRotationHistory.Add(mainCamera.rotation);
            if (HistoryCount < headRotationHistory.Count)
                headRotationHistory.RemoveAt(0);

            //location velocity:
            if (headLocationHistory.Count == HistoryCount)
            {
                headLocationVelocity = 0;
                for (int i = 1; i < headLocationHistory.Count; i++)
                {
                    headLocationVelocity += Vector3.Distance(headLocationHistory[i], headLocationHistory[i - 1]);
                }
                headLocationVelocity /= headLocationHistory.Count;

                //idle detection:
                if (headLocationVelocity <= HeadLocationIdleThreshold)
                {
                    if (!headLocationIdle)
                    {
                        headLocationIdle = true;
                    }
                }
                else
                {
                    if (headLocationIdle)
                    {
                        headLocationIdle = false;
                    }
                }
            }

            //rotation velocity:
            if (headRotationHistory.Count == HistoryCount)
            {
                headRotationVelocity = 0;
                for (int i = 1; i < headRotationHistory.Count; i++)
                {
                    headRotationVelocity += Quaternion.Angle(headRotationHistory[i], headRotationHistory[i - 1]);
                }
                headRotationVelocity /= headRotationHistory.Count;

                //idle detection:
                if (headRotationVelocity <= HeadRotationIdleThreshold)
                {
                    if (!headRotationIdle)
                    {
                        headRotationIdle = true;
                    }
                }
                else
                {
                    if (headRotationIdle)
                    {
                        headRotationIdle = false;
                    }
                }
            }

            //absolute idle head determination:
            if (headLocationIdle && headRotationIdle)
            {
                if (!headTemporarilyIdle)
                {
                    headTemporarilyIdle = true;
                    StartCoroutine(HeadIdleTimeout());
                }
            }
            else
            {
                if (headTemporarilyIdle)
                {
                    headIdle = false;
                    headTemporarilyIdle = false;
                    StopCoroutine(HeadIdleTimeout());
                }
            }
        }
   
        
        /// <summary>
        /// 指定したRayの位置に任意の面があるか否か、ある場合はその座標も返す.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="surfaceType"></param>
        /// <returns></returns>
        public (bool, Vector3) LookingAtFloorDetermination(
            Ray ray,
            MagicLeapTools.SurfaceType surfaceType)
        {
            //cast to see if we are looking at the floor:
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                MagicLeapTools.SurfaceType surface = MagicLeapTools.SurfaceDetails.Analyze(hit);
                
                if (surface == surfaceType)
                {
                    Location = hit.point;
                    placementValid = true;
                    return (true, Location);
                }
                else
                {
                    placementValid = false;
                    return (false, Vector3.zero);
                }
            }
            else
            {
                placementValid = false;
                return (false, Vector3.zero);
            }
        }

        
    }
}