using UnityEngine;

public class Destructible : MonoBehaviour, IDamageable
{
    [SerializeField] private int hitsToBreak = 1;
    [SerializeField] private GameObject breakEffect;

    private bool isBroken;

    public void TakeDamage(int damage)
    {
        hitsToBreak -= damage;
        if (hitsToBreak > 0 || isBroken) return;

        isBroken = true;
        if (breakEffect) Instantiate(breakEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}