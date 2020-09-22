using MagicLeapTools;
using UnityEngine;

namespace PlaneCheck
{
    
    /// <summary>
    /// トリガを入力したときに任意の平面を判定し、床の場合はオブジェクトを配置するサンプル.
    /// </summary>
    [RequireComponent(typeof(PlaneChecker),typeof(AudioSource))]
    public class PlaneCheckOnPlaceContent : MonoBehaviour
    {

        [SerializeField] AudioClip pressClip;
        [SerializeField] AudioClip successClip;
        [SerializeField] AudioClip failedClip;
        [SerializeField] GameObject content;
        [SerializeField] Pointer pointer;
        [SerializeField] RuntimeConsole runtimeConsole;
        PlaneChecker planeChecker;
        AudioSource audio;
        SurfaceType[] surfaceTypes;
        int index = 0;

        void Start()
        {
            planeChecker = GetComponent<PlaneChecker>();
            audio = GetComponent<AudioSource>();

            // RuntimeConsoleに自分が指定した文字列だけ表示したい.
            runtimeConsole.errors = false;
            runtimeConsole.logs = false;
            runtimeConsole.warnings = false;

            // 今回はこの三種類のみをチェック.
            surfaceTypes = new[]
            {
                SurfaceType.Floor,   // 床.
                SurfaceType.Wall,    // 壁.
                SurfaceType.Ceiling, // 天井.
            };
        }


        public void OnBumperButtonDown()
        {
            index = (int)Mathf.Repeat(index + 1, surfaceTypes.Length);
            runtimeConsole.logText.text = surfaceTypes[index].ToString();
        }


        public void OnTriggerDown()
        {
            audio.PlayOneShot(pressClip);
            (bool onSurfaceType, Vector3 pos ) result = planeChecker.LookingAtFloorDetermination(new Ray(pointer.Origin, pointer.Direction), surfaceTypes[index]);
            if (result.onSurfaceType)
            {
                audio.PlayOneShot(successClip);
                content.transform.position = result.pos;
            }
            else
            {
                audio.PlayOneShot(failedClip);
            }
        }



    }
}