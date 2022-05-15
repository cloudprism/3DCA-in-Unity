using UnityEngine;

public class Rotate : MonoBehaviour
{
    public float rateX, rateY, rateZ;

    void Update()
    {
        transform.rotation = Quaternion.Euler(Time.time * rateX * 360f, Time.time * rateY * 360f, Time.time * rateZ * 360f);
    }
}
