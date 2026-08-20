using System;
using System.Collections;
using BallzGame.Balls;
using BallzGame.Bricks;
using BallzGame.Managers;
using UnityEngine;
using Utils;
using Random = UnityEngine.Random;

public class BrickMovingDamageArea : MonoBehaviour
{
    [SerializeField] private int damage;
    [SerializeField] private Collider2D collider;
    [SerializeField] private float forceMagnitude;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private BallExtraDamage.BallType Type;
    public float DestroyAfter=-1;
    private void Start()
    {
        if (DestroyAfter > 0)
        {
            StartCoroutine(DestroyGameObject());
        }
    }

    private void Reset()
    {
        collider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    public IEnumerator DestroyGameObject()
    {
        yield return new WaitForSeconds(DestroyAfter);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {

        Brick brick = other.gameObject.GetComponent<Brick>();
        if (brick != null)
        {
            if (brick.Status.Contains(Brick.BrickStatus.DisableEffect))
            {
                GameManager.DoBlockFloatText(brick.transform.position);
                return;
            }

            brick.TakeDamage(damage+GameManager.Instance.BallExtraDamageController.GetExtraDamage(Type),this, rb.linearVelocity*forceMagnitude);
        }

    }
}