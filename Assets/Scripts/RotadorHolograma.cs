using UnityEngine;

public class RotadorHolograma : MonoBehaviour
{
    public Vector3 velocidadEjes = new Vector3(0, 15f, 0);

    void Update()
    {
        transform.Rotate(velocidadEjes * Time.deltaTime, Space.Self);
    }
}
