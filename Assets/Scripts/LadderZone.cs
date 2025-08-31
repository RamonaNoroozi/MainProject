using UnityEngine;
  public class LadderZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var controller = other.GetComponent<PlayerControllerNew>();
            if (controller != null)
                controller.SetOnLadder(true);
            var controller2 = other.GetComponent<PlayerControllerNew2>();
            if (controller2 != null)
                controller2.SetOnLadder(true);
            var controller3 = other.GetComponent<PlayerClimb>();
            if (controller3 != null)
                controller3.SetOnLadder(true);

        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var controller = other.GetComponent<PlayerControllerNew>();
            if (controller != null)
                controller.SetOnLadder(false);
            var controller2 = other.GetComponent<PlayerControllerNew2>();
            if (controller2 != null)
                controller2.SetOnLadder(false);
            var controller3 = other.GetComponent<PlayerClimb>();
            if (controller3 != null)
                controller3.SetOnLadder(false);

        }
    }
}
