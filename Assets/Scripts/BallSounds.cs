using UnityEngine;

public class BallSounds : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (SoundManager.Instance == null) return;

        string tag = collision.gameObject.tag;

        if (tag == "BlueTeam" || tag == "RedTeam")
            SoundManager.Instance.PlayBallHitPuck();
        else if (tag == "Wall")
            SoundManager.Instance.PlayBallHitWall();
        else if (tag == "Post")
            SoundManager.Instance.PlayBallHitPost();
    }
}