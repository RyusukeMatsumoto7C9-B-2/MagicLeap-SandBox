using UnityEngine;
using UnityEngine.AI;

namespace SandBox.Navigation
{
    public class MoveToClickPoint : MonoBehaviour
    {
        NavMeshAgent agent;

        // パス、座標リスト、ルート表示用Renderer
        NavMeshPath path = null;
        Vector3[] positions = new Vector3[9];

    
        public LineRenderer lr;


        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            lr.enabled = false;
        }


        public void SetPosition(
            Vector3 position)
        {
            agent.enabled = false;
            transform.position = position;
            agent.enabled = true;
        }
    

        public void OnInputClicked(
            Vector3 position)
        {
            lr.enabled = true;

            //目的地の設定
            agent.destination = position;

            // パスの計算
            path = new NavMeshPath();
            NavMesh.CalculatePath(agent.transform.position, agent.destination, NavMesh.AllAreas, path);
            positions = path.corners;

            // ルートの描画
            lr.widthMultiplier = 0.2f;
            lr.positionCount = positions.Length;

            for (int i = 0; i < positions.Length; i++) {
                Debug.Log("point "+i+"="+ positions[i]);

                lr.SetPosition(i, positions[i]);

            }
        }

    }

}

