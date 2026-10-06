/*
Date: 30/9/2026
Author: Tony
Use by: Drag script to the 3D object target
Purpose: Rotating the target by press w,a,s,d

*/
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]

public class PlayerMover : MonoBehaviour
{
    // ===== Field =====
    [Range(0,100)]
    [SerializeField] float speed = 1f;
    [Range(-360,360)]
    [SerializeField] float rotate = 90f;
    [Range(0,10)]
    [SerializeField] float flip = 3f;

    private float horizontal;
    private float vertical;

    public float Horizontal => horizontal;// public float Horizontal{get {return horizontal;}}
    public float Vertical => vertical;
    private float timer; //timer in second


    // ===== Unity Message =====
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("PlayerMover.cs running...");
        Debug.Log("Name object = " + gameObject.name);
    }

    // Update is called once per frame
    void Update()
    {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        Vector3 direction = new Vector3(horizontal,0,vertical);
        transform.Translate(direction * speed * Time.deltaTime,Space.World);

        FlipRotate();
        transform.Rotate(0f,rotate*Time.deltaTime,0f);
    }

    // ===== My Method =====
    void FlipRotate()
    {
        if (flip <= 0) return;
        timer += Time.deltaTime;
        if (timer >= flip)
        {
            rotate = -rotate;
            timer -= flip;
        }
    }
}
