﻿using System.Collections.Generic;
using System.Runtime.InteropServices;
using BulletSharp;
using BulletSharp.SoftBody;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace DemoFramework.Xna
{
    public struct InstanceData
    {
        public Microsoft.Xna.Framework.Matrix WorldTransform;
        public Microsoft.Xna.Framework.Vector3 Color;

        public static readonly int SizeInBytes = Marshal.SizeOf(typeof(InstanceData));
    }

    // Contains the geometry buffers and information of all instances of a particular shape.
    public class ShapeData : System.IDisposable
    {
        public VertexBuffer VertexBuffer;
        public int VertexCount;

        public IndexBuffer IndexBuffer;
        public int IndexCount;

        //public Buffer InstanceDataBuffer;
        public List<InstanceData> InstanceDataList;

        public PrimitiveType PrimitiveType;
        //public VertexBufferBinding[] BufferBindings;

        public Vector3[] SoftBodyData;

        public ShapeData()
        {
            InstanceDataList = new List<InstanceData>();
            PrimitiveType = PrimitiveType.TriangleList;
            //BufferBindings = new VertexBufferBinding[2];
        }

        static readonly VertexDeclaration vertexDecl = new VertexDeclaration(new VertexElement[] {
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0)
        });

        public void SetVertexBuffer(GraphicsDevice device, Vector3[] vectors)
        {
            VertexCount = vectors.Length;
            VertexBuffer = new VertexBuffer(device, vertexDecl, VertexCount, BufferUsage.None);
            VertexBuffer.SetData(vectors);
            //BufferBindings[0] = new VertexBufferBinding(VertexBuffer, 0, 0);
        }

        public void SetVertexNormalBuffer(GraphicsDevice device, Vector3[] vectors)
        {
            VertexCount = vectors.Length / 2;
            VertexBuffer = new VertexBuffer(device, vertexDecl, VertexCount, BufferUsage.None);
            VertexBuffer.SetData(vectors);
            //BufferBindings[0] = new VertexBufferBinding(VertexBuffer, 0, 0);
        }

        // Used with soft bodies
        public void SetDynamicVertexBuffer(GraphicsDevice device, Vector3[] vectors)
        {
            if (VertexBuffer != null && VertexCount == vectors.Length)
            {
                // Update existing buffer
                VertexBuffer.SetData(vectors);
            }
            else
            {
                // Create new buffer
                if (VertexBuffer != null)
                    VertexBuffer.Dispose();

                SetVertexBuffer(device, vectors);
            }
        }

        // Used with soft bodies
        public void SetDynamicVertexNormalBuffer(GraphicsDevice device, Vector3[] vectors)
        {
            if (VertexBuffer != null && VertexCount * 2 == vectors.Length)
            {
                // Update existing buffer
                VertexBuffer.SetData(vectors);
            }
            else
            {
                // Create new buffer
                if (VertexBuffer != null)
                    VertexBuffer.Dispose();

                SetVertexNormalBuffer(device, vectors);
            }
        }

        public void SetIndexBuffer(GraphicsDevice device, ushort[] indices)
        {
            IndexBuffer = new IndexBuffer(device, typeof(ushort), indices.Length, BufferUsage.None);
            IndexBuffer.SetData(indices);
        }

        public void SetIndexBuffer(GraphicsDevice device, uint[] indices)
        {
            IndexBuffer = new IndexBuffer(device, typeof(uint), indices.Length, BufferUsage.None);
            IndexBuffer.SetData(indices);
        }

        public void Dispose()
        {
            if (VertexBuffer != null)
            {
                VertexBuffer.Dispose();
                VertexBuffer = null;
            }
            if (IndexBuffer != null)
            {
                IndexBuffer.Dispose();
                IndexBuffer = null;
            }
            /*
            if (InstanceDataBuffer != null)
            {
                InstanceDataBuffer.Dispose();
                InstanceDataBuffer = null;
            }
            */
        }
    }

    // This class creates graphical objects (boxes, cones, cylinders, spheres) on the fly.
    public class MeshFactory : DemoFramework.MeshFactory
    {
        Demo demo;
        GraphicsDevice device;
        Dictionary<CollisionShape, ShapeData> shapes = new Dictionary<CollisionShape, ShapeData>();
        List<CollisionShape> removeList = new List<CollisionShape>();

        Microsoft.Xna.Framework.Vector3 groundColor = Color.Green.ToVector3();
        Microsoft.Xna.Framework.Vector3 activeColor = Color.Orange.ToVector3();
        Microsoft.Xna.Framework.Vector3 passiveColor = Color.Red.ToVector3();
        Microsoft.Xna.Framework.Vector3 softBodyColor = Color.LightBlue.ToVector3();
        Microsoft.Xna.Framework.Vector3 linkColor = Color.Black.ToVector3();

        public MeshFactory(XnaGraphics graphics)
        {
            this.demo = graphics.Demo;
            this.device = graphics.Device;
        }

        public void Dispose()
        {
            shapes.Clear();
        }

        ShapeData CreateShape(CollisionShape shape)
        {
            ShapeData shapeData = new ShapeData();
            uint[] indices;
            Vector3[] vertices = CreateShape(shape, out indices);
            shapeData.SetVertexNormalBuffer(device, vertices);

            if (indices != null)
            {
                shapeData.IndexCount = indices.Length;
                ushort[] indices_s = CompactIndexBuffer(indices);
                if (indices_s != null)
                {
                    shapeData.SetIndexBuffer(device, indices_s);
                }
                else
                {
                    shapeData.SetIndexBuffer(device, indices);
                }
            }

            return shapeData;
        }

        public ShapeData CreateSoftBody()
        {
            // Soft body geometry is recreated each frame. Nothing to do here.
            return new ShapeData();
        }

        public override void RemoveShape(CollisionShape shape)
        {
            if (shapes.ContainsKey(shape))
            {
                shapes[shape].Dispose();
                shapes.Remove(shape);
            }
        }

        ShapeData InitShapeData(CollisionShape shape)
        {
            ShapeData shapeData;

            if (shapes.TryGetValue(shape, out shapeData) == false)
            {
                switch (shape.ShapeType)
                {
                    case BroadphaseNativeType.SoftBodyShape:
                        shapeData = CreateSoftBody();
                        break;
                    case BroadphaseNativeType.Convex2DShape:
                        return InitShapeData((shape as Convex2DShape).ChildShape);
                    default:
                        shapeData = CreateShape(shape);
                        break;
                }

                // Create an initial instance data buffer for a single instance
                //instanceDataDesc.SizeInBytes = InstanceData.SizeInBytes;
                //shapeData.InstanceDataBuffer = new Buffer(device, instanceDataDesc);
                //shapeData.BufferBindings[1] = new VertexBufferBinding(shapeData.InstanceDataBuffer, instanceDataDesc.SizeInBytes, 0);

                shapes.Add(shape, shapeData);
            }

            return shapeData;
        }

        void InitInstanceData(CollisionObject colObj, CollisionShape shape, ref BulletSharp.Matrix transform)
        {
            switch (shape.ShapeType)
            {
                case BroadphaseNativeType.CompoundShape:
                    foreach (CompoundShapeChild child in (shape as CompoundShape).ChildList)
                    {
                        BulletSharp.Matrix childTransform = child.Transform * transform;
                        InitInstanceData(colObj, child.ChildShape, ref childTransform);
                    }
                    break;
                case BroadphaseNativeType.SoftBodyShape:
                    ShapeData shapeData = InitShapeData(shape);
                    UpdateSoftBody(colObj as SoftBody, shapeData);

                    shapeData.InstanceDataList.Add(new InstanceData()
                    {
                        WorldTransform = MathHelper.Convert(transform),
                        Color = softBodyColor
                    });
                    break;
                default:
                    InitShapeData(shape).InstanceDataList.Add(new InstanceData()
                    {
                        WorldTransform = MathHelper.Convert(transform),
                        Color = "Ground".Equals(colObj.UserObject) ? groundColor :
                            colObj.ActivationState == ActivationState.ActiveTag ? activeColor : passiveColor
                    });
                    break;
            }
        }

        public void InitInstancedRender(AlignedCollisionObjectArray objects)
        {
            // Clear instance data
            foreach (ShapeData s in shapes.Values)
                s.InstanceDataList.Clear();
            removeList.Clear();

            int i = objects.Count - 1;
            for (; i >= 0; i--)
            {
                CollisionObject colObj = objects[i];

                BulletSharp.Matrix transform;
                if (colObj is RigidBody)
                {
                    DefaultMotionState motionState = (colObj as RigidBody).MotionState as DefaultMotionState;
                    if (motionState != null)
                    {
                        transform = motionState.GraphicsWorldTrans;
                    }
                    else
                    {
                        colObj.GetWorldTransform(out transform);
                    }
                }
                else if (colObj is SoftBody)
                {
                    if (demo.IsDebugDrawEnabled)
                        continue;
                    transform = BulletSharp.Matrix.Identity;
                }
                else
                {
                    colObj.GetWorldTransform(out transform);
                }
                InitInstanceData(colObj, colObj.CollisionShape, ref transform);
            }

            foreach (KeyValuePair<CollisionShape, ShapeData> sh in shapes)
            {
                ShapeData s = sh.Value;

                if (s.InstanceDataList.Count == 0)
                {
                    removeList.Add(sh.Key);
                }

                /*
                // Is the instance buffer the right size?
                if (s.InstanceDataBuffer.Description.SizeInBytes != s.InstanceDataList.Count * InstanceData.SizeInBytes)
                {
                    // No, recreate it
                    s.InstanceDataBuffer.Dispose();

                    if (s.InstanceDataList.Count == 0)
                    {
                        if (s.IndexBuffer != null)
                            s.IndexBuffer.Dispose();
                        s.VertexBuffer.Dispose();
                        removeList.Add(sh.Key);
                        continue;
                    }

                    instanceDataDesc.SizeInBytes = s.InstanceDataList.Count * InstanceData.SizeInBytes;
                    s.InstanceDataBuffer = new Buffer(device, instanceDataDesc);
                    s.BufferBindings[1] = new VertexBufferBinding(s.InstanceDataBuffer, InstanceData.SizeInBytes, 0);
                }

                // Copy the instance data over to the instance buffer
                using (var data = s.InstanceDataBuffer.Map(MapMode.WriteDiscard))
                {
                    data.WriteRange(s.InstanceDataList.ToArray());
                    s.InstanceDataBuffer.Unmap();
                }
                */
            }

            if (removeList.Count != 0)
            {
                for (i = removeList.Count - 1; i >= 0; i--)
                {
                    shapes.Remove(removeList[i]);
                }
            }
        }

        public void RenderInstanced(BasicEffect effect)
        {
            EffectPass pass = effect.CurrentTechnique.Passes[0];

            foreach (ShapeData s in shapes.Values)
            {
                device.SetVertexBuffer(s.VertexBuffer);
                foreach (InstanceData instance in s.InstanceDataList)
                {
                    effect.DiffuseColor = instance.Color;
                    effect.World = instance.WorldTransform;
                    pass.Apply();

                    if (s.IndexBuffer != null)
                    {
                        device.Indices = s.IndexBuffer;
                        device.DrawIndexedPrimitives(s.PrimitiveType, 0, 0,
                            s.VertexCount, 0, s.IndexCount / 3);
                    }
                    else
                    {
                        device.DrawPrimitives(s.PrimitiveType, 0, s.VertexCount / 3);
                    }
                }
            }
            device.SetVertexBuffer(null);
        }

        public void UpdateSoftBody(SoftBody softBody, ShapeData shapeData)
        {
            // Could just allocate a Vector3 array here at each frame, but reusing shapeData.SoftBodyData is faster.
            // Probably uses more memory though.
            shapeData.VertexCount = softBody.GetVertexNormalData(out shapeData.SoftBodyData);
            shapeData.SetDynamicVertexBuffer(device, shapeData.SoftBodyData);

            if (softBody.Faces.Count == 0 && softBody.Tetras.Count == 0)
            {
                shapeData.PrimitiveType = PrimitiveType.LineList;
            }
        }

        /*
        public void RenderSoftBodyTextured(SoftBody softBody)
        {
            if (!(softBody.UserObject is Array))
                return;

            object[] userObjArr = softBody.UserObject as object[];
            FloatArray vertexBuffer = userObjArr[0] as FloatArray;
            IntArray indexBuffer = userObjArr[1] as IntArray;

            int vertexCount = (vertexBuffer.Count / 8);

            if (vertexCount > 0)
            {
                int faceCount = indexBuffer.Count / 2;

                bool index32 = vertexCount > 65536;

                Mesh mesh = new Mesh(device, faceCount, vertexCount,
                    MeshFlags.SystemMemory | (index32 ? MeshFlags.Use32Bit : 0),
                    VertexFormat.Position | VertexFormat.Normal | VertexFormat.Texture1);

                DataStream indices = mesh.LockIndexBuffer(LockFlags.Discard);
                if (index32)
                {
                    foreach (int i in indexBuffer)
                        indices.Write(i);
                }
                else
                {
                    foreach (int i in indexBuffer)
                        indices.Write((short)i);
                }
                mesh.UnlockIndexBuffer();

                DataStream verts = mesh.LockVertexBuffer(LockFlags.Discard);
                foreach (float f in vertexBuffer)
                    verts.Write(f);
                mesh.UnlockVertexBuffer();

                mesh.ComputeNormals();
                mesh.DrawSubset(0);
                mesh.Dispose();
            }
        }
        */
    }
}
