using System;
using UnityEngine.XR.MagicLeap;
using MagicLeapTools;
using UnityEngine;

namespace SandBox.Scripts.HandPointer
{
    public class HandPointerCursor : MonoBehaviour
    {
        [SerializeField] HandPointer pointer;
        [SerializeField] Transform targetObj;


        void Start()
        {
            pointer = GetComponent<HandPointer>();
            if (pointer != null)
                pointer.RegisterOnSelectHandler(OnSelectHandler);
        }


        private void LateUpdate()
        {
            if (pointer == null)
            {
                return;
            }
        }


        private void OnSelectHandler(
            (Vector3, GameObject) target)
        {
            Debug.Log($"target : {target.Item2.name}");
            targetObj.position = target.Item1;
        }

   }
}

