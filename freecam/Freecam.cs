using JetBrains.Annotations;
using UnityEngine;

namespace Terkoiz.Freecam
{
    /// <summary>
    /// A simple free camera to be added to a Unity game object.
    /// 
    /// Full credit to Ashley Davis on GitHub for the inital code:
    /// https://gist.github.com/ashleydavis/f025c03a9221bc840a2b
    /// 
    /// </summary>
    public class Freecam : MonoBehaviour
    {
        public bool IsActive = false;
        
        [UsedImplicitly]
        public void Update()
        {
            if (!IsActive)
            {
                return;
            }
            
            var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var movementSpeed = fastMode ? FreecamPlugin.CameraFastMoveSpeed.Value : FreecamPlugin.CameraMoveSpeed.Value;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                transform.position += (-transform.right * (movementSpeed * Time.deltaTime));
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                transform.position += (transform.right * (movementSpeed * Time.deltaTime));
            }

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                transform.position += (transform.forward * (movementSpeed * Time.deltaTime));
            }

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                transform.position += (-transform.forward * (movementSpeed * Time.deltaTime));
            }

            if (FreecamPlugin.CameraHeightMovement.Value)
            {
                if (Input.GetKey(KeyCode.Q))
                {
                    transform.position += (transform.up * (movementSpeed * Time.deltaTime));
                }

                if (Input.GetKey(KeyCode.E))
                {
                    transform.position += (-transform.up * (movementSpeed * Time.deltaTime));
                }

                if (Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.PageUp))
                {
                    transform.position += (Vector3.up * (movementSpeed * Time.deltaTime));
                }

                if (Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.PageDown))
                {
                    transform.position += (-Vector3.up * (movementSpeed * Time.deltaTime));
                }
            }

            float newRotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * FreecamPlugin.CameraLookSensitivity.Value;
            float newRotationY = transform.localEulerAngles.x - Input.GetAxis("Mouse Y") * FreecamPlugin.CameraLookSensitivity.Value;
            transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);

            if (FreecamPlugin.CameraMousewheelZoom.Value)
            {
                float axis = Input.GetAxis("Mouse ScrollWheel");
                if (axis != 0)
                {
                    var zoomSensitivity = fastMode ? FreecamPlugin.CameraFastZoomSpeed.Value : FreecamPlugin.CameraZoomSpeed.Value;
                    transform.position += transform.forward * (axis * zoomSensitivity);
                }
            }
        }

        [UsedImplicitly]
        private void OnDestroy()
        {
            Destroy(this);
        }
    }
}