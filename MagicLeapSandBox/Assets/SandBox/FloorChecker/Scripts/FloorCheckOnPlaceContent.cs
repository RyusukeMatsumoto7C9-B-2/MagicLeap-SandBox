using System;
using MagicLeapTools;
using UnityEngine;

namespace FloorCheck
{
    
    /// <summary>
    /// トリガを入力したときに床を判定し、床の場合はオブジェクトを配置するサンプル.
    /// </summary>
    [RequireComponent(typeof(FloorChecker),typeof(AudioSource))]
    public class FloorCheckOnPlaceContent : MonoBehaviour
    {

        [SerializeField] AudioClip pressClip;
        [SerializeField] AudioClip successClip;
        [SerializeField] AudioClip failedClip;
        [SerializeField] GameObject content;
        [SerializeField] Pointer pointer;
        FloorChecker floorChecker;
        AudioSource audio;

        
        void Start()
        {
            floorChecker = GetComponent<FloorChecker>();
            audio = GetComponent<AudioSource>();
        }


        public void OnTriggerDown()
        {
            audio.PlayOneShot(pressClip);
            (bool onFloor, Vector3 pos ) result = floorChecker.LookingAtFloorDetermination(new Ray(pointer.Origin, pointer.Direction));
            if (result.onFloor)
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