﻿using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BulletSharp;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;
using SharpDX.DXGI;
using SharpDX.Windows;
using Buffer = SharpDX.Direct3D10.Buffer;
using Device = SharpDX.Direct3D10.Device;
using DriverType = SharpDX.Direct3D10.DriverType;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;

namespace DemoFramework
{
    public class Demo : System.IDisposable
    {
        public RenderForm Form
        {
            get;
            private set;
        }

        Device _device;
        public Device Device
        {
            get { return _device; }
        }
        OutputMergerStage outputMerger;
        InputAssemblerStage inputAssembler;

        SwapChain _swapChain;

        public float AspectRatio
        {
            get { return (float)Width / (float)Height; }
        }

        Texture2D gBufferLight;
        Texture2D gBufferNormal;
        Texture2D gBufferDiffuse;
        Texture2D depthTexture;
        Texture2D lightDepthTexture;

        RenderTargetView renderView;
        DepthStencilView depthView;
        DepthStencilView lightDepthView;
        RenderTargetView gBufferNormalView;
        RenderTargetView gBufferDiffuseView;
        RenderTargetView gBufferLightView;
        RenderTargetView[] gBufferViews;
        DepthStencilState depthStencilState;
        DepthStencilState lightDepthStencilState;
        bool shadowsEnabled = false;
        public RenderTargetView[] renderViews = new RenderTargetView[1];
        
        VertexBufferBinding quadBinding;
        InputLayout quadBufferLayout;

        Effect effect;
        Effect effect2;
        EffectPass shadowGenPass;
        EffectPass gBufferGenPass;
        EffectPass gBufferRenderPass;

        public EffectPass GetShadowGenPass()
        {
            return shadowGenPass;
        }

        ShaderResourceView depthRes;
        ShaderResourceView lightDepthRes;
        ShaderResourceView lightBufferRes;
        ShaderResourceView normalBufferRes;
        ShaderResourceView diffuseBufferRes;

        protected int Width { get; set; }
        protected int Height { get; set; }
        protected int FullScreenWidth { get; set; }
        protected int FullScreenHeight { get; set; }
        protected float NearPlane { get; set; }
        protected float FarPlane { get; set; }
        protected float FieldOfView { get; set; }

        ShaderSceneConstants sceneConstants = new ShaderSceneConstants();
        Buffer sceneConstantsBuffer;

        protected InfoText Info { get; set; }

        Color4 ambient;
        protected Color4 Ambient
        {
            get { return ambient; }
            set { ambient = value; }
        }

        MeshFactory meshFactory;
        protected MeshFactory GetMeshFactory()
        {
            return meshFactory;
        }

        protected Input Input { get; private set; }
        protected FreeLook Freelook { get; set; }

        Clock clock = new Clock();
        float frameAccumulator;
        int frameCount;

        float _frameDelta;
        public float FrameDelta
        {
            get { return _frameDelta; }
            private set { _frameDelta = value; }
        }
        public float FramesPerSecond { get; private set; }

        public PhysicsContext PhysicsContext { get; set; }

        RigidBody pickedBody;
        bool use6Dof = false;
        protected TypedConstraint pickConstraint;
        float oldPickingDist;

        [StructLayout(LayoutKind.Sequential)]
        struct ShaderSceneConstants
        {
            public Matrix View;
            public Matrix Projection;
            public Matrix ViewInverse;
            public Matrix LightViewProjection;
            public Vector4 LightPosition;
        }

        /// <summary>
        /// Disposes of object resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of object resources.
        /// </summary>
        /// <param name="disposeManagedResources">If true, managed resources should be
        /// disposed of in addition to unmanaged resources.</param>
        protected virtual void Dispose(bool disposeManagedResources)
        {
            if (disposeManagedResources)
            {
                DisposeBuffers();
                //apiContext.Dispose();
                Form.Dispose();
                PhysicsContext.Dispose();
            }
        }

        protected virtual void OnInitializeDevice()
        {
            Form.ClientSize = new System.Drawing.Size(Width, Height);

            // SwapChain description
            var desc = new SwapChainDescription()
            {
                BufferCount = 1,
                ModeDescription = new ModeDescription(Width, Height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = Form.Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            // Create Device and SwapChain
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, desc, out _device, out _swapChain);
            outputMerger = _device.OutputMerger;
            inputAssembler = _device.InputAssembler;

            Factory factory = _swapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(Form.Handle, WindowAssociationFlags.None);
        }

        void DisposeBuffers()
        {
            if (renderView != null)
                renderView.Dispose();

            if (gBufferLightView != null)
                gBufferLightView.Dispose();

            if (gBufferNormalView != null)
                gBufferNormalView.Dispose();

            if (gBufferDiffuseView != null)
                gBufferDiffuseView.Dispose();

            if (gBufferLight != null)
                gBufferLight.Dispose();
            
            if (gBufferNormal != null)
                gBufferNormal.Dispose();
            
            if (gBufferDiffuse != null)
                gBufferDiffuse.Dispose();

            if (depthTexture != null)
                depthTexture.Dispose();

            if (lightDepthTexture != null)
                lightDepthTexture.Dispose();

            if (lightBufferRes != null)
                lightBufferRes.Dispose();

            if (normalBufferRes != null)
                normalBufferRes.Dispose();

            if (diffuseBufferRes != null)
                diffuseBufferRes.Dispose();

            if (depthRes != null)
                depthRes.Dispose();

            if (lightDepthRes != null)
                lightDepthRes.Dispose();
        }

        void CreateBuffers()
        {
            DisposeBuffers();

            // New RenderTargetView from the backbuffer
            using (var bb = Texture2D.FromSwapChain<Texture2D>(_swapChain, 0))
            {
                renderView = new RenderTargetView(_device, bb);
                renderViews[0] = renderView;
            }

            Texture2DDescription gBufferDesc = new Texture2DDescription()
            {
                ArraySize = 1,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.R8G8B8A8_UNorm,
                Height = Height,
                Width = Width,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            };

            gBufferLight = new Texture2D(_device, gBufferDesc);
            gBufferLightView = new RenderTargetView(_device, gBufferLight);

            gBufferNormal = new Texture2D(_device, gBufferDesc);
            gBufferNormalView = new RenderTargetView(_device, gBufferNormal);

            gBufferDiffuse = new Texture2D(_device, gBufferDesc);
            gBufferDiffuseView = new RenderTargetView(_device, gBufferDiffuse);

            gBufferViews = new RenderTargetView[] { gBufferLightView, gBufferNormalView, gBufferDiffuseView };

            ShaderResourceViewDescription gBufferResourceDesc = new ShaderResourceViewDescription()
            {
                Format = Format.R8G8B8A8_UNorm,
                Dimension = ShaderResourceViewDimension.Texture2D,
                Texture2D = new ShaderResourceViewDescription.Texture2DResource()
                {
                    MipLevels = 1,
                    MostDetailedMip = 0
                }
            };
            lightBufferRes = new ShaderResourceView(_device, gBufferLight, gBufferResourceDesc);
            normalBufferRes = new ShaderResourceView(_device, gBufferNormal, gBufferResourceDesc);
            diffuseBufferRes = new ShaderResourceView(_device, gBufferDiffuse, gBufferResourceDesc);


            Texture2DDescription depthDesc = new Texture2DDescription()
            {
                ArraySize = 1,
                BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.R32_Typeless,
                Height = Height,
                Width = Width,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            };

            DepthStencilViewDescription depthViewDesc = new DepthStencilViewDescription()
            {
                Dimension = DepthStencilViewDimension.Texture2D,
                Format = Format.D32_Float,
            };

            ShaderResourceViewDescription resourceDesc = new ShaderResourceViewDescription()
            {
                Format = Format.R32_Float,
                Dimension = ShaderResourceViewDimension.Texture2D,
                Texture2D = new ShaderResourceViewDescription.Texture2DResource()
                {
                    MipLevels = 1,
                    MostDetailedMip = 0
                }
            };

            depthTexture = new Texture2D(_device, depthDesc);
            depthView = new DepthStencilView(_device, depthTexture, depthViewDesc);
            depthRes = new ShaderResourceView(_device, depthTexture, resourceDesc);

            lightDepthTexture = new Texture2D(_device, depthDesc);
            lightDepthView = new DepthStencilView(_device, lightDepthTexture, depthViewDesc);
            lightDepthRes = new ShaderResourceView(_device, lightDepthTexture, resourceDesc);

            effect2.GetVariableByName("lightBuffer").AsShaderResource().SetResource(lightBufferRes);
            effect2.GetVariableByName("normalBuffer").AsShaderResource().SetResource(normalBufferRes);
            effect2.GetVariableByName("diffuseBuffer").AsShaderResource().SetResource(diffuseBufferRes);
            effect2.GetVariableByName("depthMap").AsShaderResource().SetResource(depthRes);
            effect2.GetVariableByName("lightDepthMap").AsShaderResource().SetResource(lightDepthRes);

            _device.Rasterizer.SetViewports(new Viewport(0, 0, Width, Height));
        }

        void Initialize()
        {
            // shader.fx

            ShaderFlags shaderFlags = ShaderFlags.None;
            //ShaderFlags shaderFlags = ShaderFlags.Debug | ShaderFlags.SkipOptimization;
            ShaderBytecode shaderByteCode = ShaderBytecode.CompileFromFile(Application.StartupPath + "\\shader.fx", "fx_4_0", shaderFlags, EffectFlags.None);

            effect = new Effect(_device, shaderByteCode);
            EffectTechnique technique = effect.GetTechniqueByIndex(0);
            shadowGenPass = technique.GetPassByIndex(0);
            gBufferGenPass = technique.GetPassByIndex(1);

            BufferDescription sceneConstantsDesc = new BufferDescription()
            {
                SizeInBytes = Marshal.SizeOf(typeof(ShaderSceneConstants)),
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None
            };

            sceneConstantsBuffer = new Buffer(_device, sceneConstantsDesc);
            EffectConstantBuffer effectConstantBuffer = effect.GetConstantBufferByName("scene");
            effectConstantBuffer.SetConstantBuffer(sceneConstantsBuffer);

            RasterizerStateDescription desc = new RasterizerStateDescription()
            {
                CullMode = CullMode.None,
                FillMode = FillMode.Solid,
                IsFrontCounterClockwise = true,
                DepthBias = 0,
                DepthBiasClamp = 0,
                SlopeScaledDepthBias = 0,
                IsDepthClipEnabled = true,
            };
            _device.Rasterizer.State = new RasterizerState(_device, desc);

            DepthStencilStateDescription depthDesc = new DepthStencilStateDescription()
            {
                IsDepthEnabled = true,
                IsStencilEnabled = false,
                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.Less
            };
            depthStencilState = new DepthStencilState(_device, depthDesc);

            DepthStencilStateDescription lightDepthStateDesc = new DepthStencilStateDescription()
            {
                IsDepthEnabled = true,
                IsStencilEnabled = false,
                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.Less
            };
            lightDepthStencilState = new DepthStencilState(_device, lightDepthStateDesc);


            // grender.fx

            shaderByteCode = ShaderBytecode.CompileFromFile(Application.StartupPath + "\\grender.fx", "fx_4_0", shaderFlags, EffectFlags.None);

            effect2 = new Effect(_device, shaderByteCode);
            technique = effect2.GetTechniqueByIndex(0);
            gBufferRenderPass = technique.GetPassByIndex(0);

            Buffer quad = MeshFactory.CreateScreenQuad(_device);
            quadBinding = new VertexBufferBinding(quad, 20, 0);
            Matrix quadProjection = Matrix.OrthoLH(1, 1, 0.1f, 1.0f);
            effect2.GetVariableByName("ViewProjection").AsMatrix().SetMatrix(quadProjection);

            InputElement[] elements = new InputElement[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, 12, 0, InputClassification.PerVertexData, 0),
            };
            quadBufferLayout = new InputLayout(_device, gBufferRenderPass.Description.Signature, elements);


            Info = new InfoText(_device);
            meshFactory = new MeshFactory(this);

            OnInitialize();
            CreateBuffers();
            SetSceneConstants();
        }

        protected void SetSceneConstants()
        {
            sceneConstants.View = Freelook.View;
            sceneConstants.Projection = Matrix.PerspectiveFovLH(FieldOfView, AspectRatio, NearPlane, FarPlane);
            sceneConstants.ViewInverse = Matrix.Invert(Freelook.View);

            Vector3 light = new Vector3(20, 30, 10);
            sceneConstants.LightPosition = new Vector4(light, 1);
            Texture2DDescription depthBuffer = lightDepthTexture.Description;
            Matrix lightView = Matrix.LookAtLH(light, Vector3.Zero, Freelook.Up);
            Matrix lightProjection = Matrix.OrthoLH(depthBuffer.Width / 8, depthBuffer.Height / 8, NearPlane, FarPlane);
            sceneConstants.LightViewProjection = lightView * lightProjection;

            using (var data = sceneConstantsBuffer.Map(MapMode.WriteDiscard))
            {
                Marshal.StructureToPtr(sceneConstants, data.DataPointer, false);
                sceneConstantsBuffer.Unmap();
            }

            effect2.GetVariableByName("InverseProjection").AsMatrix().SetMatrix(Matrix.Invert(sceneConstants.View * sceneConstants.Projection));
            effect2.GetVariableByName("InverseView").AsMatrix().SetMatrix(sceneConstants.ViewInverse);
            effect2.GetVariableByName("LightInverseViewProjection").AsMatrix().SetMatrix(Matrix.Invert(sceneConstants.LightViewProjection));
            effect2.GetVariableByName("LightPosition").AsVector().Set(sceneConstants.LightPosition);
        }

        void Render()
        {
            frameAccumulator += _frameDelta;
            ++frameCount;
            if (frameAccumulator >= 1.0f)
            {
                FramesPerSecond = frameCount / frameAccumulator;

                frameAccumulator = 0.0f;
                frameCount = 0;
            }

            // Clear targets
            _device.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
            _device.ClearRenderTargetView(renderView, ambient);
            _device.ClearRenderTargetView(gBufferLightView, ambient);
            _device.ClearRenderTargetView(gBufferNormalView, ambient);
            _device.ClearRenderTargetView(gBufferDiffuseView, ambient);

            meshFactory.InitInstancedRender();

            // Depth map pass
            if (shadowsEnabled)
            {
                _device.ClearDepthStencilView(lightDepthView, DepthStencilClearFlags.Depth, 1.0f, 0);
                outputMerger.SetDepthStencilState(lightDepthStencilState, 0);
                outputMerger.SetRenderTargets(0, new RenderTargetView[0], lightDepthView);
                shadowGenPass.Apply();
                OnRender();
                effect2.GetVariableByName("lightDepthMap").AsShaderResource().SetResource(lightDepthRes);
            }

            // Render pass
            outputMerger.SetDepthStencilState(depthStencilState, 0);
            outputMerger.SetRenderTargets(3, gBufferViews, depthView);
            gBufferGenPass.Apply();
            OnRender();

            
            // G-buffer render pass
            outputMerger.SetDepthStencilState(null, 0);
            outputMerger.SetRenderTargets(1, renderViews, null);
            gBufferRenderPass.Apply();

            inputAssembler.SetVertexBuffers(0, quadBinding);
            inputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            inputAssembler.InputLayout = quadBufferLayout;
            _device.Draw(4, 0);

            Info.OnRender(FramesPerSecond);

            _swapChain.Present(0, PresentFlags.None);
        }

        /// <summary>
        /// Updates sample state.
        /// </summary>
        private void Update()
        {
            _frameDelta = clock.Update();
            OnUpdate();
        }

        /// <summary>
        /// Runs the demo.
        /// </summary>
        public void Run()
        {
            bool isFormClosed = false;
            bool formIsResizing = false;

            Form = new RenderForm();
            /*
            currentFormWindowState = Form.WindowState;
            Form.Resize += (o, args) =>
            {
                if (Form.WindowState != currentFormWindowState)
                {
                    if (togglingFullScreen == false)
                        HandleResize(o, args);
                }

                currentFormWindowState = Form.WindowState;
            };
            */
            Form.ResizeBegin += (o, args) => { formIsResizing = true; };
            Form.ResizeEnd += (o, args) =>
            {
                Width = Form.ClientSize.Width;
                Height = Form.ClientSize.Height;

                renderView.Dispose();
                depthView.Dispose();
                _swapChain.ResizeBuffers(_swapChain.Description.BufferCount, Width, Height, _swapChain.Description.ModeDescription.Format, _swapChain.Description.Flags);

                CreateBuffers();

                SetSceneConstants();
                formIsResizing = false;
            };

            //Form.Closed += (o, args) => { isFormClosed = true; };

            Input = new Input(Form);

            Width = 1024;
            Height = 768;
            FullScreenWidth = Screen.PrimaryScreen.Bounds.Width;
            FullScreenHeight = Screen.PrimaryScreen.Bounds.Height;
            NearPlane = 1.0f;
            FarPlane = 200.0f;

            FieldOfView = (float)Math.PI / 4;
            Freelook = new FreeLook(Input);
            ambient = new Color4(Color.Gray.ToArgb());

            try
            {
                OnInitializeDevice();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Could not create DirectX 10 device.");
                return;
            }

            Initialize();

            clock.Start();
            RenderLoop.Run(Form, () =>
            {
                OnHandleInput();
                Update();
                Render();
                Input.ClearKeyCache();
            });
        }

        protected virtual void OnInitialize() { }

        protected virtual void OnRender()
        {
            meshFactory.RenderInstanced();
        }

        protected virtual void OnUpdate()
        {
            if (Freelook.Update(_frameDelta))
                SetSceneConstants();
            PhysicsContext.Update(_frameDelta);
        }

        protected virtual void OnHandleInput()
        {
            if (Input.KeysPressed.Count != 0)
            {
                Keys key = Input.KeysPressed[0];
                switch (key)
                {
                    case Keys.Escape:
                    case Keys.Q:
                        Form.Close();
                        return;
                    case Keys.F3:
                        //IsDebugDrawEnabled = !IsDebugDrawEnabled;
                        break;
                    case Keys.F11:
                        //ToggleFullScreen();
                        break;
                    case Keys.G:
                        shadowsEnabled = !shadowsEnabled;
                        break;
                    case Keys.Space:
                        PhysicsContext.ShootBox(Freelook.Eye, GetRayTo(Input.MousePoint, Freelook.Eye, Freelook.Target, FieldOfView));
                        break;
                }
            }

            if (Input.MousePressed != MouseButtons.None)
            {
                Vector3 rayTo = GetRayTo(Input.MousePoint, Freelook.Eye, Freelook.Target, FieldOfView);

                if (Input.MousePressed == MouseButtons.Right)
                {
                    if (PhysicsContext.World != null)
                    {
                        Vector3 rayFrom = Freelook.Eye;

                        CollisionWorld.ClosestRayResultCallback rayCallback = new CollisionWorld.ClosestRayResultCallback(rayFrom, rayTo);
                        PhysicsContext.World.RayTest(rayFrom, rayTo, rayCallback);
                        if (rayCallback.HasHit)
                        {
                            RigidBody body = rayCallback.CollisionObject as RigidBody;
                            if (body != null)
                            {
                                if (!(body.IsStaticObject || body.IsKinematicObject))
                                {
                                    pickedBody = body;
                                    pickedBody.ActivationState = ActivationState.DisableDeactivation;

                                    Vector3 pickPos = rayCallback.HitPointWorld;
                                    Vector3 localPivot = Vector3.TransformCoordinate(pickPos, Matrix.Invert(body.CenterOfMassTransform));

                                    if (use6Dof)
                                    {
                                        Generic6DofConstraint dof6 = new Generic6DofConstraint(body, Matrix.Translation(localPivot), false);
                                        dof6.LinearLowerLimit = Vector3.Zero;
                                        dof6.LinearUpperLimit = Vector3.Zero;
                                        dof6.AngularLowerLimit = Vector3.Zero;
                                        dof6.AngularUpperLimit = Vector3.Zero;

                                        PhysicsContext.World.AddConstraint(dof6);
                                        pickConstraint = dof6;

                                        dof6.SetParam(ConstraintParam.StopCfm, 0.8f, 0);
                                        dof6.SetParam(ConstraintParam.StopCfm, 0.8f, 1);
                                        dof6.SetParam(ConstraintParam.StopCfm, 0.8f, 2);
                                        dof6.SetParam(ConstraintParam.StopCfm, 0.8f, 3);
                                        dof6.SetParam(ConstraintParam.StopCfm, 0.8f, 4);
                                        dof6.SetParam(ConstraintParam.StopCfm, 0.8f, 5);

                                        dof6.SetParam(ConstraintParam.StopErp, 0.1f, 0);
                                        dof6.SetParam(ConstraintParam.StopErp, 0.1f, 1);
                                        dof6.SetParam(ConstraintParam.StopErp, 0.1f, 2);
                                        dof6.SetParam(ConstraintParam.StopErp, 0.1f, 3);
                                        dof6.SetParam(ConstraintParam.StopErp, 0.1f, 4);
                                        dof6.SetParam(ConstraintParam.StopErp, 0.1f, 5);
                                    }
                                    else
                                    {
                                        Point2PointConstraint p2p = new Point2PointConstraint(body, localPivot);
                                        PhysicsContext.World.AddConstraint(p2p);
                                        pickConstraint = p2p;
                                        p2p.Setting.ImpulseClamp = 30;
                                        //very weak constraint for picking
                                        p2p.Setting.Tau = 0.001f;
                                        /*
                                        p2p.SetParam(ConstraintParams.Cfm, 0.8f, 0);
                                        p2p.SetParam(ConstraintParams.Cfm, 0.8f, 1);
                                        p2p.SetParam(ConstraintParams.Cfm, 0.8f, 2);
                                        p2p.SetParam(ConstraintParams.Erp, 0.1f, 0);
                                        p2p.SetParam(ConstraintParams.Erp, 0.1f, 1);
                                        p2p.SetParam(ConstraintParams.Erp, 0.1f, 2);
                                        */
                                    }
                                    use6Dof = !use6Dof;

                                    oldPickingDist = (pickPos - rayFrom).Length();
                                }
                            }
                        }
                    }
                }
            }
            else if (Input.MouseReleased == MouseButtons.Right)
            {
                RemovePickingConstraint();
            }

            // Mouse movement
            if (Input.MouseDown == MouseButtons.Right)
            {
                if (pickConstraint != null)
                {
                    Vector3 newRayTo = GetRayTo(Input.MousePoint, Freelook.Eye, Freelook.Target, FieldOfView);

                    if (pickConstraint.ConstraintType == TypedConstraintType.D6)
                    {
                        Generic6DofConstraint pickCon = pickConstraint as Generic6DofConstraint;

                        //keep it at the same picking distance
                        Vector3 rayFrom = Freelook.Eye;
                        Vector3 dir = newRayTo - rayFrom;
                        dir.Normalize();
                        dir *= oldPickingDist;
                        Vector3 newPivotB = rayFrom + dir;

                        Matrix tempFrameOffsetA = pickCon.FrameOffsetA;
                        tempFrameOffsetA.M41 = newPivotB.X;
                        tempFrameOffsetA.M42 = newPivotB.Y;
                        tempFrameOffsetA.M43 = newPivotB.Z;
                        pickCon.FrameOffsetA = tempFrameOffsetA;
                    }
                    else
                    {
                        Point2PointConstraint pickCon = pickConstraint as Point2PointConstraint;

                        //keep it at the same picking distance
                        Vector3 rayFrom = Freelook.Eye;
                        Vector3 dir = newRayTo - rayFrom;
                        dir.Normalize();
                        dir *= oldPickingDist;
                        pickCon.PivotInB = rayFrom + dir;
                    }
                }
            }
        }

        void RemovePickingConstraint()
        {
            if (pickConstraint != null && PhysicsContext.World != null)
            {
                PhysicsContext.World.RemoveConstraint(pickConstraint);
                pickConstraint.Dispose();
                pickConstraint = null;
                pickedBody.ForceActivationState(ActivationState.ActiveTag);
                pickedBody.DeactivationTime = 0;
                pickedBody = null;
            }
        }

        protected Vector3 GetRayTo(Point point, Vector3 eye, Vector3 target, float fov)
        {
            float aspect;

            Vector3 rayFrom = eye;
            Vector3 rayForward = target - eye;
            rayForward.Normalize();
            float farPlane = 10000.0f;
            rayForward *= farPlane;

            Vector3 vertical = Vector3.UnitY;

            Vector3 hor = Vector3.Cross(rayForward, vertical);
            hor.Normalize();
            vertical = Vector3.Cross(hor, rayForward);
            vertical.Normalize();

            float tanFov = (float)Math.Tan(fov / 2);
            hor *= 2.0f * farPlane * tanFov;
            vertical *= 2.0f * farPlane * tanFov;

            if (Form.ClientSize.Width > Form.ClientSize.Height)
            {
                aspect = (float)Form.ClientSize.Width / (float)Form.ClientSize.Height;
                hor *= aspect;
            }
            else
            {
                aspect = (float)Form.ClientSize.Height / (float)Form.ClientSize.Width;
                vertical *= aspect;
            }

            Vector3 rayToCenter = rayFrom + rayForward;
            Vector3 dHor = hor / (float)Form.ClientSize.Width;
            Vector3 dVert = vertical / (float)Form.ClientSize.Height;

            Vector3 rayTo = rayToCenter - 0.5f * hor + 0.5f * vertical;
            rayTo += (Form.ClientSize.Width - point.X) * dHor;
            rayTo -= point.Y * dVert;
            return rayTo;
        }
    }
}
