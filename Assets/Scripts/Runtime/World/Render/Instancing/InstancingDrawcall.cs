using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

public class InstancingDrawcall
{
    public struct Key : System.IEquatable<Key>
    {
        /// <summary>
        /// mesh的实例id
        /// </summary>
        public int mesh;

        /// <summary>
        /// 材质id
        /// </summary>
        public int material;

        public void Init(int _mesh, int _material)
        {
            mesh = _mesh;
            material = _material;
        }

        public void Clear()
        {
            mesh = 0;
            material = 0;
        }

        public bool Equals(Key other)
        {
            return mesh == other.mesh && material == other.material;
        }

        public override bool Equals(object other)
        {
            if (!(other is Key))
                return false;
            return Equals((Key)other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return mesh.GetHashCode() ^ (material.GetHashCode() << 2);
            }
        }

        public static bool operator ==(Key lhs, Key rhs)
        {
            return lhs.mesh == rhs.mesh && lhs.material == rhs.material;
        }

        public static bool operator !=(Key lhs, Key rhs)
        {
            return !(lhs == rhs);
        }
    }

    public static readonly int s_TransformBufferPropID = Shader.PropertyToID("_TransformBuffer");

    private Key m_Key = new Key();
    public Key key { get { return m_Key; } }

    private InstancingCore m_InstancingCore;

    /// <summary>
    /// 渲染的mesh
    /// </summary>
    private Mesh m_InstanceMesh;

    /// <summary>
    /// 渲染的Material
    /// </summary>
    private Material m_InstanceMaterial;

    /// <summary>
    /// 是否投影
    /// </summary>
    private ShadowCastingMode m_ShadowCastingMode;

    /// <summary>
    /// 是否接受阴影
    /// </summary>
    private bool m_ReceiveShadows = true;

    private ComputeBuffer m_ArgsBuffer;

    private MaterialPropertyBlock m_MaterialPropertyBlock;

    public void Init(InstancingCore core, Mesh mesh, Material material, ShadowCastingMode shadowCastingMode, bool receiveShadows)
    {
        Debug.Assert(m_ArgsBuffer == null);

        m_InstancingCore = core;
        m_InstanceMesh = mesh;
        m_InstanceMaterial = material;
        m_ShadowCastingMode = shadowCastingMode;
        m_ReceiveShadows = receiveShadows;
        m_Key.Init(mesh.GetInstanceID(), material.GetInstanceID());

        // Argument buffer
        uint[] args = new uint[5];
        args[0] = (uint)m_InstanceMesh.GetIndexCount(0);
        args[1] = (uint)0;
        args[2] = (uint)m_InstanceMesh.GetIndexStart(0);
        args[3] = (uint)m_InstanceMesh.GetBaseVertex(0);
        m_ArgsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        m_ArgsBuffer.SetData(args);

        m_MaterialPropertyBlock = new MaterialPropertyBlock();
    }

    public void Clear()
    {
        if (m_ArgsBuffer != null)
        {
            m_ArgsBuffer.Release();
            m_ArgsBuffer = null;
        }

        m_InstancingCore = null;
        m_InstanceMesh = null;
        m_InstanceMaterial = null;
        m_ShadowCastingMode = ShadowCastingMode.Off;
        m_ReceiveShadows = true;
        m_MaterialPropertyBlock = null;

        m_Key.Clear();
    }

    public void Submit(ref ComputeBuffer visibleBuffer)
    {
        ComputeBuffer.CopyCount(visibleBuffer, m_ArgsBuffer, 4);
        m_MaterialPropertyBlock.SetBuffer(s_TransformBufferPropID, visibleBuffer);
    }

    public void Render(Bounds bounds)
    {
        Graphics.DrawMeshInstancedIndirect(m_InstanceMesh, 0, m_InstanceMaterial, bounds, m_ArgsBuffer, 0, m_MaterialPropertyBlock, m_ShadowCastingMode, m_ReceiveShadows);
    }
}
