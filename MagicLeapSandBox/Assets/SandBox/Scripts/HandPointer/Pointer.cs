using System;
using MagicLeap.Core;
using MagicLeapTools;
using UnityEngine;

namespace SandBox.Scripts.HandPointer
{
    public class Pointer : MonoBehaviour
    {
        [SerializeField] Transform source;
        [SerializeField] LineRenderer lr;

        Vector3 lastStartPos = Vector3.zero;

        void Start()
        {
            
        }

        void Update()
        {
            if (!HandInput.Ready)
            {
                return;
            }

            var right = HandInput.Right;
            // 人差し指の根元と親指の根元の中間座標を起点として
            Vector3 tempStartPos = Vector3.Lerp(right.Skeleton.Thumb.Knuckle.positionFiltered, right.Skeleton.Index.Knuckle.positionFiltered, 0.5f);

            Vector3 startPos = Vector3.Lerp(lastStartPos, tempStartPos, 0.5f);
            lastStartPos = tempStartPos;
            // 方向はWristCenterから見たStart?
            lr.SetPositions(new []{startPos, startPos + (source.forward.normalized * 2f)});
        }

    }
}