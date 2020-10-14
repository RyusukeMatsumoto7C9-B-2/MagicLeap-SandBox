using UnityEngine;

namespace SandBox.Scripts.HandPointer
{
    public class HandPointerCursor : MonoBehaviour
    {
#if PLATFORM_LUMIN
        public HandPointer pointer;


        private void LateUpdate()
        {
            if (pointer == null)
            {
                return;
            }
        }
#endif
    }
}

