using UnityEngine;

public class PuckSounds : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (SoundManager.Instance == null) return;

        string tag = collision.gameObject.tag;

        if (tag == "BlueTeam" || tag == "RedTeam" || tag == "Ball")
            SoundManager.Instance.PlayPuckHitPuck();
    }
}