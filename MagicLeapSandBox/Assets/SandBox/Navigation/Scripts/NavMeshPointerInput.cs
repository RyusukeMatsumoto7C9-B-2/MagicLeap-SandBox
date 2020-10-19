using MagicLeapTools;
using UnityEngine;


namespace SandBox.Navigation
{
    public class NavMeshPointerInput : MonoBehaviour
    {

        [SerializeField] Pointer pointer;
        [SerializeField] MoveToClickPoint moveToClickPoint;
        

        /// <summary>
        /// Trigger押下処理,指定した座標にAgentが移動するようにしている.
        /// </summary>
        public void OnTriggerDown()
        {
            RaycastHit hit;
            if (Physics.Raycast(new Ray(pointer.Origin, pointer.Direction), out hit))
            {
                moveToClickPoint.OnInputClicked(hit.point);
            }
        }


        /// <summary>
        /// Bumperボタン押下処理.
        /// 指定した座標にAgentを配置しなおすために利用.
        /// </summary>
        public void OnBumperButtonDown()
        {
            RaycastHit hit;
            if (Physics.Raycast(new Ray(pointer.Origin, pointer.Direction), out hit))
            {
                moveToClickPoint.SetPosition(hit.point);
            }
        }


        /// <summary>
        /// HomeButton押下処理.
        /// 実機動作時にアプリを終了させるため.
        /// </summary>
        public void OnHomeButtonDown()
        {
            Application.Quit();
        }

    }

}

