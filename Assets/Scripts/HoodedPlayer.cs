// In your HoodedPlayer script
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class HoodedPlayer : NetworkBehaviour
{
    private SpiderWeb currentWeb = null;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        SpiderWeb web = collision.collider.GetComponentInParent<SpiderWeb>();
        if (web != null)
        {
            currentWeb = web;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        SpiderWeb web = collision.collider.GetComponentInParent<SpiderWeb>();
        if (web != null && currentWeb == web)
        {
            currentWeb = null;
        }
    }

    private void Update()
    {
        if (!IsOwner) return; // only handle input for your player

        if (currentWeb != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            //currentWeb.DestroyWebServerRpc();
        }
    }
}