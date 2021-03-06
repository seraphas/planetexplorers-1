using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

//辅助函数库

public class AuxLib
{
    Vector3 m_axisPX = new Vector3(1.0f, 0.0f, 0.0f);
    Vector3 m_axisNX = new Vector3(-1.0f, 0.0f, 0.0f);
    Vector3 m_axisPY = new Vector3(0.0f, 1.0f, 0.0f);
    Vector3 m_axisNY = new Vector3(0.0f, -1.0f, 0.0f);
    Vector3 m_axisPZ = new Vector3(0.0f, 0.0f, 1.0f);
    Vector3 m_axisNZ = new Vector3(0.0f, 0.0f, -1.0f);
    
    const int PXAxis = 0;
    const int NXAxis = 1;
    const int PZAxis = 2;
    const int NZAxis = 3;
    const int PYAxis = 4;
    const int NYAxis = 5;
    const int NoDir = 6;

    static AuxLib m_this;
    public static AuxLib Get()
    {
        if (m_this == null)
            m_this = new AuxLib();
        return m_this;
    }

    //射线射线最近距离平方
    public float DistSqRayRay(ref Ray _ray0, ref Ray _ray1, ref float _s0, ref float _s1)
    {
        Vector3 _diff = _ray0.origin - _ray1.origin;
        float a01 = -Vector3.Dot(_ray0.direction, _ray1.direction);
        float b0 = Vector3.Dot(_diff, _ray0.direction);
        float c = _diff.sqrMagnitude; //_diff.SquaredLength();
        float det = Mathf.Abs(1.0f - a01 * a01);
        float b1, sqrDist;

        if (det >= float.Epsilon)
        {
            // Rays are not parallel.
            b1 = -Vector3.Dot(_diff, _ray1.direction);
            _s0 = a01 * b1 - b0;
            _s1 = a01 * b0 - b1;

            if (_s0 >= 0.0f)
            {
                if (_s1 >= 0.0f)  // region 0 (interior)
                {
                    // Minimum at two interior points of rays.
                    float invDet = 1.0f / det;
                    _s0 *= invDet;
                    _s1 *= invDet;
                    sqrDist = _s0 * (_s0 + a01 * _s1 + 2.0f * b0) + _s1 * (a01 * _s0 + _s1 + 2.0f * b1) + c;
                }
                else  // region 3 (side)
                {
                    _s1 = 0.0f;
                    if (b0 >= 0.0f)
                    {
                        _s0 = 0.0f;
                        sqrDist = c;
                    }
                    else
                    {
                        _s0 = -b0;
                        sqrDist = b0 * _s0 + c;
                    }
                }
            }
            else
            {
                if (_s1 >= 0.0f)  // region 1 (side)
                {
                    _s0 = 0.0f;
                    if (b1 >= 0.0f)
                    {
                        _s1 = 0.0f;
                        sqrDist = c;
                    }
                    else
                    {
                        _s1 = -b1;
                        sqrDist = b1 * _s1 + c;
                    }
                }
                else  // region 2 (corner)
                {
                    if (b0 < 0.0f)
                    {
                        _s0 = -b0;
                        _s1 = 0.0f;
                        sqrDist = b0 * _s0 + c;
                    }
                    else
                    {
                        _s0 = 0.0f;
                        if (b1 >= 0.0f)
                        {
                            _s1 = 0.0f;
                            sqrDist = c;
                        }
                        else
                        {
                            _s1 = -b1;
                            sqrDist = b1 * _s1 + c;
                        }
                    }
                }
            }
        }
        else
        {
            // Rays are parallel.
            if (a01 > 0.0f)
            {
                // Opposite direction vectors.
                _s1 = 0.0f;
                if (b0 >= 0.0f)
                {
                    _s0 = 0.0f;
                    sqrDist = c;
                }
                else
                {
                    _s0 = -b0;
                    sqrDist = b0 * _s0 + c;
                }
            }
            else
            {
                // Same direction vectors.
                if (b0 >= 0.0f)
                {
                    b1 = -Vector3.Dot(_diff, _ray1.direction);
                    _s0 = 0.0f;
                    _s1 = -b1;
                    sqrDist = b1 * _s1 + c;
                }
                else
                {
                    _s0 = -b0;
                    _s1 = 0.0f;
                    sqrDist = b0 * _s0 + c;
                }
            }
        }

        // Account for numerical round-off errors.
        if (sqrDist < 0.0f)
        {
            sqrDist = 0.0f;
        }
        return sqrDist;

    }

    //获取最近射线方向
    public void GetNearestRayDir(ref Ray _screenRay, ref Vector3 _srcPos, ref Vector3 _dir, ref Vector3 _dstPos)
    {
        Ray[] _rayGroup = new Ray[6];
        _rayGroup[PXAxis] = new Ray(_srcPos, m_axisPX);
        _rayGroup[NXAxis] = new Ray(_srcPos, m_axisNX);
        _rayGroup[PZAxis] = new Ray(_srcPos, m_axisPZ);
        _rayGroup[NZAxis] = new Ray(_srcPos, m_axisNZ);
        _rayGroup[PYAxis] = new Ray(_srcPos, m_axisPY);
        _rayGroup[NYAxis] = new Ray(_srcPos, m_axisNY);

        float _minDistSq = float.MaxValue;
		float _s = 0.0f/*, _t = 0.0f*/;
        int _iDir = 0;

        for (int _i = 0; _i != 6; ++_i)
        {
            if (_rayGroup[_i].direction.x == 0.0f && _rayGroup[_i].direction.y == 0.0f && _rayGroup[_i].direction.z == 0.0f)
                continue;
            float _tempS = 0.0f, _tempT = 0.0f;
            float _distSq = DistSqRayRay(ref _rayGroup[_i], ref _screenRay, ref _tempS, ref _tempT);
            if (_minDistSq > _distSq)
            {
                _minDistSq = _distSq;
                _s = _tempS;
                //_t = _tempT;
                _iDir = _i;
            }
        }

        _dir = _rayGroup[_iDir].direction;
        _dstPos = _rayGroup[_iDir].origin + _rayGroup[_iDir].direction * _s;
    }

    //随机值
    public float RandomValue()
    {
        float _value = 0.001f * (float)UnityEngine.Random.Range(0, 1000);
        return _value;
    }

    //传入浮点数必须小于等于1.0f
    public float RandomValue(float _fBaseValue)
    {
        int _baseInt = (int)(_fBaseValue * 1000.0f);
        float _value = 0.001f * (float)UnityEngine.Random.Range(_baseInt, 1000);
        return _value;
    }

    //随机颜色
    public Color RandomColor()
    {
        Color _clr = new Color();
        _clr.r = RandomValue();
        _clr.g = RandomValue();
        _clr.b = RandomValue();
        return _clr;
    }

    //带基本颜色的随机颜色
    public Color RandomColor(Color _baseClr)
    {
        Color _clr = new Color();
        _clr.r = RandomValue(_baseClr.r);
        _clr.g = RandomValue(_baseClr.g);
        _clr.b = RandomValue(_baseClr.b);
        return _clr; 
    }

    //读写IntVector3
    public void ReadIntVector3(BinaryReader _in, IntVector3 _value)
    {
        _value.x = _in.ReadInt32();
        _value.y = _in.ReadInt32();
        _value.z = _in.ReadInt32();
    }

    public void WriteIntVector3(BinaryWriter _out, IntVector3 _value)
    {
        _out.Write(_value.x);
        _out.Write(_value.y);
        _out.Write(_value.z);
    }

    //读写Vector3
    public void ReadVector3(BinaryReader _in, ref Vector3 _value)
    {
        _value.x = _in.ReadSingle();
        _value.y = _in.ReadSingle();
        _value.z = _in.ReadSingle();
    }

    public void WriteVector3(BinaryWriter _out, ref Vector3 _value)
    {
        _out.Write(_value.x);
        _out.Write(_value.y);
        _out.Write(_value.z);
    }

    //读写color
    public void ReadColor(BinaryReader _in, ref Color _clr)
    {
        _clr.r = _in.ReadSingle();
        _clr.g = _in.ReadSingle();
        _clr.b = _in.ReadSingle();
        _clr.a = _in.ReadSingle();
    }

    public void WriteColor(BinaryWriter _out, ref Color _clr)
    {
        _out.Write(_clr.r);
        _out.Write(_clr.g);
        _out.Write(_clr.b);
        _out.Write(_clr.a);
    }
}

