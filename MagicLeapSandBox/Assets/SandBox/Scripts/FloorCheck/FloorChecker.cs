using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if PLATFORM_LUMIN
using UnityEngine.XR.MagicLeap;
#endif


namespace FriendsMl.Systems.Spatial
{
    /// <summary>
    /// MagicLeapToolsのFloorOnPlaceを改造したクラス.
    /// 床検知をランタイムにできるようにする.
    /// </summary>
    public class FloorChecker : MonoBehaviour
    {
        readonly float HeadLocationIdleThreshold = 0.003f;
        readonly float HeadRotationIdleThreshold = .3f;
        readonly int HistoryCount = 5;
        readonly float HeadIdleRequiredDuration = .2f;
        
        // Public Properties:
        public Vector3 Location
        {
            get;
            private set;
        }

        
        [Tooltip("Content that will be turned on and placed when a suitable location is found.")]
        [SerializeField] GameObject content;
        
        [Tooltip("Does content's content match it's transform forward?")]
        [SerializeField] bool flippedForward;
        

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
        

        //Init:
        void Awake()
        {
            //refs:
            mainCamera = Camera.main.transform;

            //requirements:
            if (FindObjectOfType<MLSpatialMapper>() == null)
            {
                Debug.LogError("PlaceOnFloor requires and instance of the MLSpatialMapper in your scene.");
            }
        }
        

        //Flow:
        void OnEnable()
        {
            //we can't operate on own content since it disables the content:
            if (content == gameObject)
            {
                Debug.LogError("StartInOpenArea can not be used on it's own content.  Move the StartInOpenArea to a separate object.", this);
                enabled = false;
            }

            //hide content:
            if (content != null)
            {
                content.SetActive(false);
            }

            //sets:
            headLocationHistory = new List<Vector3>();
            headRotationHistory = new List<Quaternion>();
        }

        
        //Loops:
        void Update()
        {
            //let headpose warmup a little:
            if (Time.frameCount < 3)
            {
                return;
            }

            HeadActivityDetermination(); 

            // TODO : とりあえずサンプルでここに配置するスクリプトを記述している.
            (bool hit, Vector3 pos) result = LookingAtFloorDetermination(new Ray(mainCamera.position, mainCamera.forward));
            if (result.hit)
            {
                //place content:
                if (content != null)
                {
                    content.SetActive(true);
                    content.transform.position = Location;

                    //face user after placement:
                    Vector3 to = Vector3.Normalize(Location - mainCamera.position);
                    Vector3 flat = Vector3.ProjectOnPlane(to, Vector3.up);
                    if (!flippedForward)
                    {
                        flat *= -1;
                    }
                    content.transform.rotation = Quaternion.LookRotation(flat);
                }
            }
        }
        

        //Coroutines:
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
        /// 指定したRayの位置に床があるか否か、ある場合はその座標も返す.
        /// </summary>
        /// <param name="ray"></param>
        /// <returns></returns>
        public (bool, Vector3) LookingAtFloorDetermination(
            Ray ray)
        {
            //cast to see if we are looking at the floor:
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                MagicLeapTools.SurfaceType surface = MagicLeapTools.SurfaceDetails.Analyze(hit);
                
                if (surface == MagicLeapTools.SurfaceType.Floor)
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