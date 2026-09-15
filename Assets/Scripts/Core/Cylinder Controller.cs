using UnityEngine;
using System.Collections.Generic;

public class CylinderController : MonoBehaviour
{
    [SerializeField] private List<bool> _cylinder = new();
    [SerializeField] private int _minAmmunition = 2;
    private float _correctionRate = 0.0f;

    public void SetCylinder()
    {
        //ƒVƒŠƒ“ƒ_[“à‚Éƒ‰ƒ“ƒ_ƒ€‚Å’e–ò ‘•“U

        _cylinder.Clear();
        _correctionRate = 0.0f;
        int random = Random.Range(_minAmmunition, 9);
        Debug.Log("‘•“U”F" + random);
        for (int c = 0; c < random; c++)
        {
            bool previousBullet = Random.value < 0.5f + _correctionRate;
            _cylinder.Add(previousBullet);
            if (previousBullet)
            {
                _correctionRate -= 1.0f / random;
            }
            else
            {
                _correctionRate += 1.0f / random;
            }

            Debug.Log(_cylinder[c]);
        }
    }
}
