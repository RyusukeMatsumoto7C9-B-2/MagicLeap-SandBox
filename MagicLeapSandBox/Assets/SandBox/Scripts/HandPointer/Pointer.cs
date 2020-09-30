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
            
            var indexKnuckle = right.Skeleton.Index.Knuckle.positionFiltered;
            var middleKnuckle = right.Skeleton.Middle.Knuckle.positionFiltered;
            var wristCenter = right.Skeleton.WristCenter.positionFiltered;

            Vector3 targetPos = middleKnuckle + (source.forward * 2f);
            lr.SetPositions(new []{middleKnuckle, targetPos});
            
        }
    }
}