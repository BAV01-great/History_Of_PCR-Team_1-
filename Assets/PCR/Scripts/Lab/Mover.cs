using UnityEngine;

namespace PCR
{
    public class Mover : MonoBehaviour
    {
        public float Speed = 5f, Min = -70f, Max = 70f;
        void Update()
        {
            var p = transform.position;
            p.x += Speed * Time.deltaTime;
            if (Speed > 0f && p.x > Max) p.x = Min;
            else if (Speed < 0f && p.x < Min) p.x = Max;
            transform.position = p;
        }
    }
}
