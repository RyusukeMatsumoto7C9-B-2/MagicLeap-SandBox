using UnityEngine;

namespace SandBox.Scripts.HandPointer
{
    /// <summary>
    /// HandPointerのカーソル.
    /// </summary>
    public class HandPointerCursor
    {

        LineRenderer lineRenderer;
        GameObject cursor = null;

        public HandPointerCursor(
            LineRenderer _lineRenderer,
            GameObject _cursor)
        {
            lineRenderer = _lineRenderer;
            cursor = _cursor;
        }


        public void Update(
            HandPointer.HandPointerState state,
            Vector3 startPosition,
            Vector3 endPosition)
        {
            if (state == HandPointer.HandPointerState.None)
            {
                Hide();
                return;
            }
            Show();

            RaycastHit hit;
            var ray = new Ray(startPosition, endPosition - startPosition);
            if (Physics.Raycast(ray, out hit, Vector3.Distance(startPosition, endPosition)))
                endPosition = hit.point;
            
            lineRenderer.SetPositions(new []{startPosition, endPosition});
                
            // ここでカーソルに適用.
            if (cursor != null)
            {
                cursor.SetActive(true);
                cursor.transform.SetPositionAndRotation(endPosition, cursor.transform.rotation);
            }
        }

            
        void Hide()
        {
            lineRenderer.enabled = false;
            if (cursor != null)
                cursor.SetActive(false);
        }


        void Show()
        {
            lineRenderer.enabled = true;
            if (cursor != null)
                cursor.SetActive(true);
        }
    }
}