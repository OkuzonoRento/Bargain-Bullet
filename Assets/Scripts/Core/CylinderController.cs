using UnityEngine;
using System.Collections.Generic;

public class CylinderController : MonoBehaviour
{
    [SerializeField] private List<bool> _cylinder = new();
    [SerializeField, Min(3)] private int _minAmmunition = 3;
    private int _maxAmmunition = 9;
    private float _minRate = 0.4f;
    private float _maxRate = 0.8f;

    public int RemainingBullets => _cylinder.Count;

    public void SetCylinder()
    {
        _cylinder.Clear();
        int random = Random.Range(_minAmmunition, _maxAmmunition);
        Vector2Int rate = new((int)(random * _minRate), (int)(random * _maxRate) + 1);
        int realCount = Random.Range(rate.x, rate.y);
        for (int c = 0; c < realCount; c++)
        {
            _cylinder.Add(true);
        }
        for (int c = 0; c < random - realCount; c++)
        {
            _cylinder.Add(false);
        }

        for (int r = _cylinder.Count - 1; r > 0; r--)
        {
            int randomIndex = Random.Range(0, r + 1);
            bool temp = _cylinder[r];
            _cylinder[r] = _cylinder[randomIndex];
            _cylinder[randomIndex] = temp;
        }
        Debug.LogWarning("実弾は " + realCount + "発、空砲は " + (random - realCount) + "発だ");
    }

    public bool GetShot()
    {
        bool shotType = _cylinder[0];
        _cylinder.RemoveAt(0);
        return shotType;
    }

    public int RemainingRealBullets
    {
        get
        {
            int count = 0;
            foreach (bool b in _cylinder) if (b) count++;
            return count;
        }
    }

    // アーティファクト & AI用メソッド ---

    /// <summary> 次の弾種を覗き見る（単眼鏡 ART_003 用） </summary>
    public bool PeekNextBullet()
    {
        if (_cylinder.Count > 0) return _cylinder[0];
        return false;
    }

    /// <summary> 次の弾を強制的に実弾化する（カウントバレル ART_010 用） </summary>
    public void ForceSetNextBulletReal()
    {
        if (_cylinder.Count > 0) _cylinder[0] = true;
    }
}