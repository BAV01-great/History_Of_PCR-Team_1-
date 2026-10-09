using UnityEngine;

namespace PCR
{
    public class DoorOpener : MonoBehaviour
    {
        public Transform Hinge;
        public Vector3 Centre;
        public float OpenDistance = 2.6f, OpenAngle = 105f, Speed = 150f;
        float angle;

        void Update()
        {
            var cam = Camera.main;
            if (cam == null || Hinge == null) return;
            var d = cam.transform.position - Centre; d.y = 0;
            float target = d.magnitude < OpenDistance ? OpenAngle : 0f;
            angle = Mathf.MoveTowards(angle, target, Speed * Time.deltaTime);
            Hinge.localRotation = Quaternion.Euler(0, angle, 0);
        }
    }
}
