using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// A story character (Zofia, receptionist, Gruber…) standing at its map point: idle pose, and while a dialogue plays
    /// and the player is within a few metres it turns to face the player (plus the "Rozmowa" pose when the Animator has it).
    /// </summary>
    public class StoryNpcFacing : MonoBehaviour
    {
        const float Range = 9f, TurnSpeed = 5f;
        Animator anim;
        bool talking;

        void Start()
        {
            anim = AnimParams.Of(gameObject);
            AnimParams.SetFloat(anim, "Speed", 0f);
        }

        void Update()
        {
            bool near = false;
            if (Dialog.IsPlaying)
            {
                var p = FindAnyObjectByType<FirstPersonController>();
                if (p != null)
                {
                    Vector3 to = p.transform.position - transform.position;
                    to.y = 0f;
                    if (to.sqrMagnitude < Range * Range && to.sqrMagnitude > 0.01f)
                    {
                        near = true;
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), TurnSpeed * Time.deltaTime);
                    }
                }
            }
            if (near != talking)
            {
                talking = near;
                AnimParams.SetBool(anim, "Rozmowa", talking);
            }
        }
    }
}
