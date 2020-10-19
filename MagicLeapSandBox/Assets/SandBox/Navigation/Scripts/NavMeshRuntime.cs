using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace SandBox.Navigation
{
    /// <summary>
    /// ランタイムに生成された空間メッシュにMyNavMesySourceTagをアタッチし、ランタイムにNavMeshを作成する機能に処理してもらう.
    /// </summary>
    public class NavMeshRuntime : MonoBehaviour
    {
        [SerializeField] MLSpatialMapper spatialMapper;

        void Start()
        {
            // ここでSpatialMapperのメッシュが追加された際に該当するidのメッシュにMyNavMeshSourceTagをアタッチしている.
            spatialMapper.meshAdded += (id) =>
                {
                    spatialMapper.meshIdToGameObjectMap[id].AddComponent<MyNavMeshSourceTag>();
                } ;
        }
    }
}