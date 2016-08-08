using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using SharpDX.Windows;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;

using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace Sesion2_Lab01 {
    public class NativeApplication {

        private const int App_Width = 800;
        private const int App_Height = 600;

        // Es como el Windows.Forms, pero este se utiliza nativamente para el DirectX
        // Se necesita las librerias:
        //  1. System.Windows.Forms
        //  2. SharpDX.Windows
        private RenderForm mRenderForm;

        private Texture2D mBackBufferFBO;
        private RenderTargetView mRenderTargetView;
        private Device mDevice;
        private DeviceContext mDeviceContext;

        private Factory mFactory;

        private VertexShader mVertexShader;
        private PixelShader mPixelShader;
        private InputLayout mInputLayout;

        private SwapChain mSwapChain;
        private SwapChainDescription mSwapChainDescription;

        public NativeApplication() {
            mRenderForm = new RenderForm("Sesion 2::Aplicacion Nativa DirectX 11");
            //Isil2010ct
            mSwapChainDescription = new SwapChainDescription(); // es una estructura, no es necesario construirlo
            mSwapChainDescription.BufferCount = 1;
            mSwapChainDescription.IsWindowed = true; // es ventana
            mSwapChainDescription.SwapEffect = SwapEffect.Discard;
            mSwapChainDescription.Usage = Usage.RenderTargetOutput;
            mSwapChainDescription.OutputHandle = mRenderForm.Handle; // le pasamos el puntero de nuestro RenderForm
            mSwapChainDescription.SampleDescription.Count = 1;
            mSwapChainDescription.SampleDescription.Quality = 0;
            mSwapChainDescription.ModeDescription.Width = NativeApplication.App_Width;  // aqui definimos el ancho de la ventana
            mSwapChainDescription.ModeDescription.Height = NativeApplication.App_Height; // aqui definimos el alto de la ventana
            mSwapChainDescription.ModeDescription.RefreshRate.Numerator = 60; // aqui definimos el Frame Rate
            mSwapChainDescription.ModeDescription.RefreshRate.Denominator = 1; // siempre por defecto 1... la matematica es Denominator / Numerator
            mSwapChainDescription.ModeDescription.Format = Format.R8G8B8A8_UNorm; // se define el formato del color de la ventana

            // Creamos la tarjeta grafica usando el SwapChainDescription
            // Esto nos devuelve:
            //  1. Device -> La tarjeta grafica
            //  2. SwapChain -> Control del Frame Rate
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, mSwapChainDescription,
                out mDevice, out mSwapChain);

            // recogemos el contexto de la tarjeta de video
            mDeviceContext = mDevice.ImmediateContext;

            // recogemos el padre constructor de la ventana
            mFactory = mSwapChain.GetParent<Factory>();
            // ignoramos todos los eventos de la ventana
            mFactory.MakeWindowAssociation(mRenderForm.Handle, WindowAssociationFlags.IgnoreAll);

            // creamos un frame buffer object, y usamos este back buffer para almacenar la data del render
            mBackBufferFBO = Texture2D.FromSwapChain<Texture2D>(mSwapChain, 0);

            // creamos un render target view para manejar el render usando nuestro frame buffer object
            mRenderTargetView = new RenderTargetView(mDevice, mBackBufferFBO);

            // compilamos nuestro vertex shader
            CompilationResult vertexShaderByteCode = ShaderBytecode.CompileFromFile("Content/Fx_Primitive.fx",
                "VS", "vs_4_0", ShaderFlags.None, EffectFlags.None);
            mVertexShader = new VertexShader(mDevice, vertexShaderByteCode);

            // compilamos nuestro fragment shader
            CompilationResult pixelShaderByteCode = ShaderBytecode.CompileFromFile("Content/Fx_Primitive.fx",
                "PS", "ps_4_0", ShaderFlags.None, EffectFlags.None);
            mPixelShader = new PixelShader(mDevice, pixelShaderByteCode);

            // creamos un shader signature para poder acceder y obtener los elementos de entrada
            // de nuestro shader
            ShaderSignature shaderSignature = ShaderSignature.GetInputSignature(vertexShaderByteCode);

            // creamos el contenedor de nuestras variables de entrada
            InputElement[] inputElements = new InputElement[3];
            // segun nuestro shader es: Posicion de los vertices
            inputElements[0] = new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0);
            // segun nuestro shader es: El color de cada vertice
            inputElements[1] = new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 16, 0);
            // segun nuestro shader es: La velocidad implementada para nuestro movimiendo trigonometrico
            inputElements[2] = new InputElement("POSITION", 1, Format.R32G32B32A32_Float, 32, 0);

            // construimos el Input Layout, le pasamos el Shader Signature que contiene la referencia
            // de nuestro Shader, el Input Element que contiene nuestro parametros de entrada, y 
            // mediante esto crea un Objeto para poder enviarlo a la tarjeta de video para poder usarlo
            mInputLayout = new InputLayout(mDevice, shaderSignature, inputElements);

            // Ahora inicializamos algunos datos para poder dibujar
            PreConfiguration();

            // Ahora creamos algo muy importante! Nuestro Render Loop, donde ira nuestro Draw y Update!
            RenderLoop.Run(mRenderForm, OnRenderLoop);
        }
        
        // variables para dibujar nuestro primitivo
        private float velX = 1f;
        private float velY = 1f;

        private Vector4[] mVertices;
        private VertexBufferBinding mVertexBufferBinding;

        private void PreConfiguration() {
            // preparamos nuestros parametros para dibujar

            // Creamos nuestro Viewport que define el tamanio de nuestras dimension de dibujo para
            // el DirectX
            mDeviceContext.Rasterizer.SetViewports(new Viewport(0, 0, NativeApplication.App_Width,
                NativeApplication.App_Height, 0.0f, 1.0f));

            // ahora transferimos a la tarjeta de video el Input Layout que define los parametros
            // de entrada de nuestro Shader
            mDeviceContext.InputAssembler.InputLayout = mInputLayout;
            // definimos ahora de que manera se va a tratar la data que entra y como se dibuja
            mDeviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            // aqui vamos a transferir el Vertex y Fragment Shader que ya habiamos creado
            mDeviceContext.VertexShader.Set(mVertexShader);
            mDeviceContext.PixelShader.Set(mPixelShader);

            // ahora pasamos nuestro Frame Buffer Object
            mDeviceContext.OutputMerger.SetTargets(mRenderTargetView);

            // creamos nustros vertices
            mVertices = new Vector4[9];
            // nuestro primer vertice
            mVertices[0] = new Vector4(0.0f, 0.5f, 0.5f, 1.0f); 
            mVertices[1] = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
            mVertices[2] = new Vector4(velX, velY, 1.0f, 1.0f);
            // nuestro segundo vertice
            mVertices[3] = new Vector4(0.5f, -0.5f, 0.5f, 1.0f);
            mVertices[4] = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
            mVertices[5] = new Vector4(velX, velY, 1.0f, 1.0f);
            // nuestro tercer vertice
            mVertices[6] = new Vector4(-0.5f, -0.5f, 0.5f, 1.0f);
            mVertices[7] = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
            mVertices[8] = new Vector4(velX, velY, 1.0f, 1.0f);
        }

        private void OnRenderLoop() {
            Update();
            Draw();
        }

        private void Update() {
            // actualizamos nuestro valores para crear nuestro efecto al dibujar
            velX += 0.001f;
            velY -= 0.001f;

            // ahora actualizamos nuestros vertices para poder visualizar los cambios de nuestro efecto
            mVertices[2] = new Vector4(velX, velY, 1.0f, 1.0f); // vertice 1 
            mVertices[5] = new Vector4(velX, velY, 1.0f, 1.0f); // vertice 2
            mVertices[5] = new Vector4(velX, velY, 1.0f, 1.0f); // vertice 3

            // ahora creamos nuestro Buffer para poder almacenar los Vertice de una manera
            // que la tarjeta de video pueda leer y transferir los vertices a los Shaders
            Buffer vertexBuffer = Buffer.Create(mDevice, BindFlags.VertexBuffer, mVertices);

            // ahora mandamos nuestro Buffer a la tarjeta de video para que lo pueda transferir 
            // al Shader
            mVertexBufferBinding.Buffer = vertexBuffer;
            mVertexBufferBinding.Stride = 48;
            mVertexBufferBinding.Offset = 0;

            mDeviceContext.InputAssembler.SetVertexBuffers(0, mVertexBufferBinding);
        }

        private void Draw() {
            // aqui definimos nuestro color usando Color4
            Color4 clearColor = Color4.Black;
            clearColor.Red = 0f;
            clearColor.Green = 1f;
            clearColor.Blue = 0f;

            // aqui limpiamos nuestra ventana con un color solido
            mDeviceContext.ClearRenderTargetView(mRenderTargetView, clearColor);

            // dibujamos nuestros vertices!
            mDeviceContext.Draw(3, 0);

            // hacemos un swap de buffers
            mSwapChain.Present(0, PresentFlags.None);
        }
    }
}
